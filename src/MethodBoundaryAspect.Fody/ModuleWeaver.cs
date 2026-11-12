using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Fody;
using MethodBoundaryAspect.Fody.Ordering;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using Mono.Collections.Generic;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// The Fody weaver: collects the aspects of all methods of the module (from the assembly, the classes and
    /// the methods) and rewrites the methods to call them, see <see cref="MethodWeaverFactory"/>.
    /// </summary>
    public class ModuleWeaver : BaseModuleWeaver
    {
        private MethodInfoCompileTimeWeaver _methodInfoCompileTimeWeaver;
        private ExecutionArgsUsageAnalyzer _executionArgsUsageAnalyzer;
        private AspectInstanceReuseAnalyzer _aspectInstanceReuseAnalyzer;
        private AspectInstanceCompileTimeWeaver _aspectInstanceCompileTimeWeaver;

        /// <summary>
        /// Suppresses the warning for woven methods using ref struct values (e.g. Span&lt;T&gt;).
        /// Configurable in FodyWeavers.xml: &lt;MethodBoundaryAspect SuppressRefStructWarnings="true" /&gt;
        /// </summary>
        public bool SuppressRefStructWarnings { get; set; }

        /// <summary>
        /// Disables the MethodExecutionArgs optimization for all aspects: all properties are always provided,
        /// even if the aspects don't use them.
        /// Configurable in FodyWeavers.xml: &lt;MethodBoundaryAspect DisableExecutionArgsOptimization="true" /&gt;
        /// </summary>
        public bool DisableExecutionArgsOptimization { get; set; }

        /// <summary>
        /// Full names of the aspects for which the MethodExecutionArgs optimization is disabled.
        /// Configurable in FodyWeavers.xml:
        /// &lt;MethodBoundaryAspect&gt;&lt;DisableExecutionArgsOptimization Aspect="MyNamespace.MyAspect" /&gt;&lt;/MethodBoundaryAspect&gt;
        /// </summary>
        public List<string> DisableExecutionArgsOptimizationAspects { get; } = new List<string>();

        /// <summary>
        /// Disables sharing one instance of an aspect between all calls, if the aspect does not hold any state:
        /// a new instance is created for every call.
        /// Configurable in FodyWeavers.xml: &lt;MethodBoundaryAspect DisableAspectInstanceReuse="true" /&gt;
        /// </summary>
        public bool DisableAspectInstanceReuse { get; set; }

        /// <summary>
        /// Full names of the aspects which are created for every call.
        /// Configurable in FodyWeavers.xml:
        /// &lt;MethodBoundaryAspect&gt;&lt;DisableAspectInstanceReuse Aspect="MyNamespace.MyAspect" /&gt;&lt;/MethodBoundaryAspect&gt;
        /// </summary>
        public List<string> DisableAspectInstanceReuseAspects { get; } = new List<string>();

        // aspects for which the warning about the obsolete roles was written
        private readonly HashSet<string> _warnedAboutAspectRoles = new HashSet<string>();

        // aspects which were checked for filters set in their code
        private readonly HashSet<string> _checkedAspectFilters = new HashSet<string>();

        // attribute types of the module which were checked to be aspects
        private readonly Dictionary<string, bool> _isAspectAttributeCache = new Dictionary<string, bool>();

        public int TotalWeavedTypes { get; private set; }
        public int TotalWeavedMethods { get; private set; }
        public int TotalWeavedProperties { get; private set; }

        public List<string> PropertyFilter { get; } = new List<string>();
        public List<string> MethodFilters { get; } = new List<string>();
        public List<string> TypeFilters { get; } = new List<string>();
        
        public override void Execute()
        {
            Execute(ModuleDefinition);
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            // the weaver does not use FindType, it imports the types it needs from the core library of the module
            return Enumerable.Empty<string>();
        }

        private string CreateShadowAssemblyPath(string assemblyPath, string prefix)
        {
            var fileInfoSource = new FileInfo(assemblyPath);
            return
                fileInfoSource.DirectoryName
                + Path.DirectorySeparatorChar
                + "_" + prefix + "_"
                + Path.GetFileNameWithoutExtension(fileInfoSource.Name)
                + "_Weaved_"
                + fileInfoSource.Extension.ToLower();
        }

        public string WeaveToShadowFile(string assemblyPath, IAssemblyResolver assemblyResolver)
        {
            // unique even for two calls within a few milliseconds (Environment.TickCount has a resolution of about 16 ms)
            var prefix = Guid.NewGuid().ToString("N").Substring(0, 12);
            var shadowAssemblyPath = CreateShadowAssemblyPath(assemblyPath, prefix);
            File.Copy(assemblyPath, shadowAssemblyPath, true);

            var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
            var shadowPdbPath = CreateShadowAssemblyPath(pdbPath, prefix);

            if (File.Exists(pdbPath))
                File.Copy(pdbPath, shadowPdbPath, true);

            Weave(shadowAssemblyPath, assemblyResolver);
            return shadowAssemblyPath;
        }

        public void Weave(string assemblyPath, IAssemblyResolver assemblyResolver)
        {
            var readerParameters = new ReaderParameters
            {
                ReadSymbols = true,
                SymbolReaderProvider = new PdbReaderProvider(),
                ReadWrite = true,
                AssemblyResolver = assemblyResolver
            };

            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters))
            {
                var module = assemblyDefinition.MainModule;
                Execute(module);

                var writerParameters = new WriterParameters
                {
                    WriteSymbols = true,
                    SymbolWriterProvider = new PdbWriterProvider()
                };
                assemblyDefinition.Write(writerParameters);
            }
        }

        public void AddClassFilter(string classFilter)
        {
            TypeFilters.Add(classFilter);
        }

        public void AddMethodFilter(string methodFilter)
        {
            MethodFilters.Add(methodFilter);
        }

        public void AddPropertyFilter(string propertyFilter)
        {
            PropertyFilter.Add(propertyFilter);
        }

        private void Execute(ModuleDefinition module)
        {
            var suppressRefStructWarnings = Config?.Attribute(nameof(SuppressRefStructWarnings));
            if (suppressRefStructWarnings != null)
                SuppressRefStructWarnings = ParseBool(suppressRefStructWarnings);

            WarnAboutUnknownSettings();
            ReadExecutionArgsOptimizationConfig();
            ReadAspectInstanceReuseConfig();

            _isAspectAttributeCache.Clear();
            _filterRegexes.Clear(); // RegexMatchTimeout can differ between runs
            _warnedAboutAspectRoles.Clear();
            _checkedAspectFilters.Clear();
            _methodInfoCompileTimeWeaver = new MethodInfoCompileTimeWeaver(module);
            _executionArgsUsageAnalyzer = new ExecutionArgsUsageAnalyzer(
                DisableExecutionArgsOptimization,
                DisableExecutionArgsOptimizationAspects);
            _aspectInstanceReuseAnalyzer = new AspectInstanceReuseAnalyzer(
                DisableAspectInstanceReuse,
                DisableAspectInstanceReuseAspects);
            _aspectInstanceCompileTimeWeaver = new AspectInstanceCompileTimeWeaver(module, _aspectInstanceReuseAnalyzer);

            var assemblyMethodBoundaryAspects = module.Assembly.CustomAttributes;

            if (!IsUserFiltered(string.Empty, string.Empty))
                WarnAboutIgnoredAspects(module.CustomAttributes, "the module");

            foreach (var type in module.Types.ToList())
                WeaveTypeAndNestedTypes(module, type, assemblyMethodBoundaryAspects, new List<Collection<CustomAttribute>>());

            WarnAboutUnmatchedAspects(
                nameof(DisableExecutionArgsOptimization),
                DisableExecutionArgsOptimizationAspects,
                _executionArgsUsageAnalyzer.MatchedDisabledAspects);
            WarnAboutUnmatchedAspects(
                nameof(DisableAspectInstanceReuse),
                DisableAspectInstanceReuseAspects,
                _aspectInstanceReuseAnalyzer.MatchedDisabledAspects);
        }

        private static readonly string[] KnownAttributes =
        {
            nameof(SuppressRefStructWarnings), nameof(DisableExecutionArgsOptimization), nameof(DisableAspectInstanceReuse)
        };

        private static readonly string[] KnownElements =
        {
            nameof(DisableExecutionArgsOptimization), nameof(DisableAspectInstanceReuse)
        };

        // a typo would be ignored silently, e.g. SupressRefStructWarnings
        private void WarnAboutUnknownSettings()
        {
            if (Config == null)
                return;

            foreach (var attribute in Config.Attributes().Where(a => !KnownAttributes.Contains(a.Name.LocalName)))
                WriteWarning($"Unknown setting '{attribute.Name.LocalName}' in FodyWeavers.xml is ignored. Known attributes: {string.Join(", ", KnownAttributes)}.");

            foreach (var element in Config.Elements().Where(e => !KnownElements.Contains(e.Name.LocalName)))
                WriteWarning($"Unknown setting '{element.Name.LocalName}' in FodyWeavers.xml is ignored. Known elements: {string.Join(", ", KnownElements)}.");
        }

        private void WarnAboutUnmatchedAspects(string settingName, IEnumerable<string> configuredAspects, ISet<string> matchedAspects)
        {
            foreach (var aspect in configuredAspects)
            {
                if (!matchedAspects.Contains(AspectTypeName.Normalize(aspect)))
                    WriteWarning($"Aspect '{aspect}' configured in {settingName} of FodyWeavers.xml was not found in any woven method. Use the full type name, e.g. 'MyNamespace.MyAspect'.");
            }
        }

        private void ReadAspectInstanceReuseConfig()
        {
            var disableAll = ReadAspectSetting(nameof(DisableAspectInstanceReuse), DisableAspectInstanceReuseAspects);
            if (disableAll.HasValue)
                DisableAspectInstanceReuse = disableAll.Value;
        }

        private void ReadExecutionArgsOptimizationConfig()
        {
            var disableAll = ReadAspectSetting(nameof(DisableExecutionArgsOptimization), DisableExecutionArgsOptimizationAspects);
            if (disableAll.HasValue)
                DisableExecutionArgsOptimization = disableAll.Value;
        }

        /// <summary>
        /// Reads a setting which is an attribute for all aspects and an element per aspect, e.g.
        /// &lt;MethodBoundaryAspect Name="true"&gt;&lt;Name Aspect="MyNamespace.MyAspect" /&gt;&lt;/MethodBoundaryAspect&gt;.
        /// </summary>
        /// <returns>The value of the attribute, null if it is not set.</returns>
        private bool? ReadAspectSetting(string name, List<string> aspects)
        {
            if (Config == null)
                return null;

            var attribute = Config.Attribute(name);
            var forAllAspects = attribute == null ? (bool?)null : ParseBool(attribute);

            foreach (var element in Config.Elements(name))
            {
                var aspect = element.Attribute("Aspect")?.Value;
                if (string.IsNullOrWhiteSpace(aspect))
                    throw new WeavingException($"<{name}> in FodyWeavers.xml requires the full type name of the aspect, e.g. <{name} Aspect=\"MyNamespace.MyAspect\" />.");

                // the weaver can run more than once (e.g. in tests), the configuration must not be added twice
                if (!aspects.Contains(aspect.Trim()))
                    aspects.Add(aspect.Trim());
            }

            return forAllAspects;
        }

        private static string ShortName(string fullName)
        {
            var name = fullName.Substring(fullName.LastIndexOfAny(new[] { '.', '/' }) + 1);
            var genericMarker = name.IndexOf('`');
            if (genericMarker >= 0)
                name = name.Substring(0, genericMarker);
            return name.EndsWith("Attribute") ? name.Substring(0, name.Length - "Attribute".Length) : name;
        }

        /// <summary>
        /// The time a regular expression of a filter may take to match a name. A pattern with catastrophic backtracking
        /// would hang the build otherwise.
        /// </summary>
        public TimeSpan RegexMatchTimeout { get; set; } = TimeSpan.FromSeconds(30);

        // the filters are the same for many methods, Regex.IsMatch only caches 15 expressions
        private readonly Dictionary<string, Regex> _filterRegexes = new Dictionary<string, Regex>();

        // The filters are checked one after the other for a method, so a method which fails the first filter never
        // reaches the others. Every filter of an aspect is compiled before, an invalid expression fails the build.
        private void CompileFilters(AspectInfo aspectInfo)
        {
            GetFilterRegex(aspectInfo.NamespaceFilter, aspectInfo, nameof(AspectInfo.NamespaceFilter));
            GetFilterRegex(aspectInfo.TypeNameFilter, aspectInfo, nameof(AspectInfo.TypeNameFilter));
            GetFilterRegex(aspectInfo.MethodNameFilter, aspectInfo, nameof(AspectInfo.MethodNameFilter));
        }

        private Regex GetFilterRegex(string filter, AspectInfo aspectInfo, string filterName)
        {
            if (string.IsNullOrEmpty(filter))
                return null;

            if (_filterRegexes.TryGetValue(filter, out var regex))
                return regex;

            try
            {
                regex = new Regex(filter, RegexOptions.None, RegexMatchTimeout);
            }
            catch (ArgumentException e)
            {
                throw new WeavingException($"Aspect '{aspectInfo.Name}' has an invalid regular expression in {filterName}: {e.Message}");
            }

            _filterRegexes[filter] = regex;
            return regex;
        }

        private bool MatchesFilter(string value, string filter, AspectInfo aspectInfo, string filterName)
        {
            var regex = GetFilterRegex(filter, aspectInfo, filterName);
            if (regex == null)
                return true;

            try
            {
                return regex.IsMatch(value);
            }
            catch (RegexMatchTimeoutException)
            {
                throw new WeavingException($"Aspect '{aspectInfo.Name}' needs too long to match the regular expression in {filterName} ('{filter}' against '{value}', limit {RegexMatchTimeout.TotalSeconds:0.###} seconds).");
            }
        }

        // the schema (xs:boolean) allows true, false, 1 and 0
        private static bool ParseBool(XAttribute attribute)
        {
            if (attribute.Value == "1")
                return true;
            if (attribute.Value == "0")
                return false;

            if (!bool.TryParse(attribute.Value, out var value))
                throw new WeavingException($"Invalid value '{attribute.Value}' for {attribute.Name} in FodyWeavers.xml, expected 'true' or 'false'.");
            return value;
        }

        private void WeaveTypeAndNestedTypes(ModuleDefinition module, TypeDefinition type,
            Collection<CustomAttribute> assemblyMethodBoundaryAspects,
            List<Collection<CustomAttribute>> enclosingTypeAttributes)
        {
            // Compiler generated types (async and iterator state machines, closures, anonymous types)
            // are no user code. The state machines of async methods are woven with the async method,
            // weaving their MoveNext method again called the aspects for each step of the state machine,
            // see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/66
            if (type.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName))
                return;

            // Aspects (and their base aspects and nested helper types) are not woven. An assembly or class
            // aspect woven into the aspects of the same assembly called itself, directly or via another aspect,
            // until a StackOverflowException, see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/70
            if (IsMethodBoundaryAspect(type))
                return;

            if (!IsUserFiltered(type.FullName, string.Empty))
                WarnAboutIgnoredAspects(type);

            WeaveType(module, type, assemblyMethodBoundaryAspects, enclosingTypeAttributes);
            if (type.HasNestedTypes)
            {
                // the aspects of a class also hit its nested classes, as a level of their own for the order index
                var nestedEnclosingTypeAttributes = new List<Collection<CustomAttribute>>(enclosingTypeAttributes)
                {
                    type.CustomAttributes
                };
                foreach (var nestedType in type.NestedTypes)
                    WeaveTypeAndNestedTypes(module, nestedType, assemblyMethodBoundaryAspects, nestedEnclosingTypeAttributes);
            }
        }

        // The attribute has no [AttributeUsage], so the compiler accepts an aspect everywhere, but the weaver only reads
        // the aspects of assemblies, types, methods and properties (and their accessors).
        private void WarnAboutIgnoredAspects(TypeDefinition type)
        {
            foreach (var field in type.Fields)
                WarnAboutIgnoredAspects(field.CustomAttributes, $"the field '{field.FullName}'");

            foreach (var eventDefinition in type.Events)
                WarnAboutIgnoredAspects(eventDefinition.CustomAttributes, $"the event '{eventDefinition.FullName}'");

            foreach (var method in type.Methods)
            {
                if (method.IsConstructor)
                    WarnAboutIgnoredAspects(method.CustomAttributes, $"the constructor '{method.FullName}'");
                else if (!method.HasBody)
                    WarnAboutIgnoredAspects(method.CustomAttributes, $"the {DescribeMethodWithoutBody(method)} method '{method.FullName}'", GetHintForMethodWithoutBody(method));

                WarnAboutIgnoredAspects(method.MethodReturnType.CustomAttributes, $"the return value of '{method.FullName}'");
                foreach (var parameter in method.Parameters)
                    WarnAboutIgnoredAspects(parameter.CustomAttributes, $"the parameter '{parameter.Name}' of '{method.FullName}'");
            }

            // the aspects of a property are woven into its accessors
            foreach (var property in type.Properties)
            {
                var accessor = property.GetMethod ?? property.SetMethod;
                if (accessor == null || (property.GetMethod?.HasBody ?? false) || (property.SetMethod?.HasBody ?? false))
                    continue;

                WarnAboutIgnoredAspects(property.CustomAttributes, $"the {DescribeMethodWithoutBody(accessor)} property '{property.FullName}'", GetHintForMethodWithoutBody(accessor));
            }
        }

        private static string DescribeMethodWithoutBody(MethodDefinition method)
        {
            if (!method.IsAbstract)
                return "extern";

            return method.DeclaringType.IsInterface ? "interface" : "abstract";
        }

        // a common mistake: the aspect is expected to run for the overrides and implementations
        private static string GetHintForMethodWithoutBody(MethodDefinition method)
        {
            return method.IsAbstract
                ? "It has no body to weave and aspects are not inherited. Apply the aspect to the methods which override or implement it."
                : "It has no body to weave. Remove the aspect.";
        }

        private void WarnAboutIgnoredAspects(Collection<CustomAttribute> attributes, string target, string hint = null)
        {
            foreach (var attribute in attributes.Where(IsMethodBoundaryAspect))
            {
                WriteWarning(
                    $"Aspect '{attribute.AttributeType.FullName}' is applied to {target}, where it is ignored. " +
                    (hint ?? "Aspects work on methods, properties, classes and assemblies. Remove it or move it to the method."));
            }
        }

        private void WeaveType(
            ModuleDefinition module,
            TypeDefinition type,
            Collection<CustomAttribute> assemblyMethodBoundaryAspects,
            List<Collection<CustomAttribute>> enclosingTypeAttributes)
        {
            var classMethodBoundaryAspects = type.CustomAttributes;

            // two properties could share an accessor (hand written IL), ToDictionary would throw outside of the try block
            // which names the method
            var propertyGetters = new Dictionary<MethodDefinition, PropertyDefinition>();
            var propertySetters = new Dictionary<MethodDefinition, PropertyDefinition>();
            foreach (var property in type.Properties)
            {
                if (property.GetMethod != null && !propertyGetters.ContainsKey(property.GetMethod))
                    propertyGetters.Add(property.GetMethod, property);
                if (property.SetMethod != null && !propertySetters.ContainsKey(property.SetMethod))
                    propertySetters.Add(property.SetMethod, property);
            }

            var typeNamespace = GetNamespace(type);
            var weavedAtLeastOneMethod = false;
            bool WeaveMethodWithAspects(MethodDefinition method)
            {
                if (!IsWeavableMethod(method))
                    return false;

                // the aspects of a property and of its accessor (int X { [Aspect] get; }) are both on method level
                IEnumerable<CustomAttribute> methodMethodBoundaryAspects;

                if (method.IsGetter && propertyGetters.TryGetValue(method, out var getterProperty))
                    methodMethodBoundaryAspects = getterProperty.CustomAttributes.Concat(method.CustomAttributes).ToList();
                else if (method.IsSetter && propertySetters.TryGetValue(method, out var setterProperty))
                    methodMethodBoundaryAspects = setterProperty.CustomAttributes.Concat(method.CustomAttributes).ToList();
                else
                    methodMethodBoundaryAspects = method.CustomAttributes;

                var methodVisibility = GetMethodVisibility(method);

                var candidateInfos = assemblyMethodBoundaryAspects
                    .Concat(enclosingTypeAttributes.SelectMany(attributes => attributes))
                    .Concat(classMethodBoundaryAspects)
                    .Concat(methodMethodBoundaryAspects)
                    .Where(IsMethodBoundaryAspect)
                    .Select(x => new AspectInfo(x))
                    .ToList();
                candidateInfos.ForEach(CompileFilters);

                var aspectInfos = candidateInfos
                    .Where(info => info.HasTargetMemberAttribute(methodVisibility))
                    .Where(info => MatchesFilter(typeNamespace, info.NamespaceFilter, info, nameof(AspectInfo.NamespaceFilter)))
                    .Where(info => MatchesFilter(type.Name, info.TypeNameFilter, info, nameof(AspectInfo.TypeNameFilter)))
                    .Where(info => MatchesFilter(method.Name, info.MethodNameFilter, info, nameof(AspectInfo.MethodNameFilter)))
                    .ToList();
                if (aspectInfos.Count == 0)
                    return false;

                foreach (var aspectInfo in aspectInfos.Where(x => x.UsesAspectRoles && _warnedAboutAspectRoles.Add(x.Name)))
                {
                    WriteWarning(
                        $"Aspect '{aspectInfo.Name}' uses [ProvideAspectRole] or [AspectRoleDependency] to define the order of aspects. " +
                        "They are obsolete and will be removed in version 4, use [AspectOrderIndex] instead.");
                }

                foreach (var aspectInfo in aspectInfos.Where(x => _checkedAspectFilters.Add(x.Name)))
                {
                    var ignoredValues = IgnoredAspectValuesAnalyzer.GetValuesSetInAspectCode(aspectInfo.AspectTypeDefinition);
                    if (ignoredValues.Count > 0)
                    {
                        WriteWarning(
                            $"Aspect '{aspectInfo.Name}' sets {string.Join(", ", ignoredValues)} in its code (in a constructor or by overriding the property). " +
                            "The weaver does not run the code of aspects, the value is ignored. " +
                            $"Set it where the aspect is applied instead, e.g. [{ShortName(aspectInfo.AspectTypeDefinition.Name)}({ignoredValues[0]} = ...)].");
                    }
                }

                foreach (var aspectInfo in aspectInfos)
                    aspectInfo.InitOrderIndex(assemblyMethodBoundaryAspects, enclosingTypeAttributes, classMethodBoundaryAspects, methodMethodBoundaryAspects);

                return WeaveMethod(
                    module,
                    method,
                    aspectInfos,
                    _methodInfoCompileTimeWeaver);
            }

            foreach (var method in type.Methods.ToList())
            {
                try
                {
                    weavedAtLeastOneMethod |= WeaveMethodWithAspects(method);
                }
                catch (Exception e) when (!(e is WeavingException))
                {
                    throw CreateWeavingException(method, e);
                }
            }

            if (weavedAtLeastOneMethod)
                TotalWeavedTypes++;
        }

        // Fody writes an unexpected exception with its stack trace as "An unhandled exception occurred", without
        // saying which method caused it. Invalid configurations of the aspects are errors of the user, not bugs.
        private static WeavingException CreateWeavingException(MethodDefinition method, Exception exception)
        {
            var isConfigurationError = exception is InvalidAspectConfigurationException || exception is NotSupportedException;
            var reason = isConfigurationError ? exception.Message : exception.ToString();

            return new WeavingException($"Method '{method.FullName}' could not be woven: {reason}")
            {
                SequencePoint = method.DebugInformation.SequencePoints.FirstOrDefault()
            };
        }

        private bool WeaveMethod(
            ModuleDefinition module,
            MethodDefinition method,
            List<AspectInfo> aspectInfos,
            MethodInfoCompileTimeWeaver methodInfoCompileTimeWeaver)
        {
            aspectInfos = AspectOrderer.Order(aspectInfos);
            var aspectInfosWithMethods = aspectInfos
                .Where(x => !x.SkipProperties || (!method.IsGetter && !method.IsSetter))
                .ToList();

            var methodWeaver = MethodWeaverFactory.MakeWeaver(
                module, method, aspectInfosWithMethods, methodInfoCompileTimeWeaver, _executionArgsUsageAnalyzer, _aspectInstanceCompileTimeWeaver, out var unsupportedReason);
            if (methodWeaver == null)
            {
                // not touched at all, failing the build would block the whole project for a single method
                WriteWarning(
                    $"Method '{method.FullName}' is not woven: {unsupportedReason}. " +
                    "Exclude the method from weaving with [DisableWeaving] to remove this warning.",
                    method);
                return false;
            }

            methodWeaver.Weave();
            if (methodWeaver.WeaveCounter == 0)
                return false;

            if (method.IsGetter || method.IsSetter)
                TotalWeavedProperties++;
            else
                TotalWeavedMethods++;

            WarnAboutRefStructValues(method);
            return true;
        }

        // only for methods which are changed: the aspects of the others do nothing (they override no aspect method
        // or are skipped for properties)
        private void WarnAboutRefStructValues(MethodDefinition method)
        {
            var byRefLikeValues = method.GetByRefLikeValueNames().ToList();
            if (!byRefLikeValues.Any() || SuppressRefStructWarnings)
                return;

            // Ref structs (e.g. Span<T>) cannot be boxed into MethodExecutionArgs,
            // they are woven as null, see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/129
            WriteWarning(
                $"Method '{method.FullName}' uses ref struct values which cannot be passed to the aspect ({string.Join(", ", byRefLikeValues)}). " +
                "They are null in MethodExecutionArgs and changes to them are ignored. " +
                "To suppress this warning add SuppressRefStructWarnings=\"true\" to the MethodBoundaryAspect element in FodyWeavers.xml, " +
                "or exclude the method from weaving with [DisableWeaving].",
                method);
        }

        private bool IsMethodBoundaryAspect(TypeDefinition attributeTypeDefinition)
        {
            var currentType = attributeTypeDefinition?.BaseType;
            if (currentType == null) // can be null for Visual Basic Winforms projects, see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/126
                return false;

            do
            {
                if (currentType.FullName == AttributeFullNames.OnMethodBoundaryAspect)
                    return true;

                // Resolve() returns null if the assembly of a base type cannot be found, see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/119
                currentType = currentType.Resolve()?.BaseType;
            } while (currentType != null);

            return false;
        }

        private bool IsMethodBoundaryAspect(CustomAttribute customAttribute)
        {
            // the attributes of the assembly and the classes are checked for every method
            var attributeType = customAttribute.AttributeType;
            var key = attributeType.Scope?.Name + ":" + attributeType.FullName;
            if (!_isAspectAttributeCache.TryGetValue(key, out var isAspect))
            {
                isAspect = IsMethodBoundaryAspect(attributeType.Resolve());
                _isAspectAttributeCache.Add(key, isAspect);
            }

            return isAspect;
        }

        private bool IsWeavableMethod(MethodDefinition method)
        {
            var fullName = method.DeclaringType.FullName;
            var name = method.Name;

            if (IsIgnoredByWeaving(method))
                return false;

            if (IsUserFiltered(fullName, name))
                return false;

            if (!method.HasBody) // e.g. the methods of a delegate, which the runtime implements
                return false;

            return !(method.IsAbstract // abstract or interface method
                     || method.IsConstructor
                     || IsFinalizer(method)
                     || name.StartsWith("<") // anonymous
                     || method.IsPInvokeImpl); // extern
        }

        // the aspect would run on the finalizer thread, and an exception of an aspect ends the process
        private static bool IsFinalizer(MethodDefinition method)
        {
            return method.Name == "Finalize"
                   && method.IsVirtual
                   && !method.HasParameters
                   && method.ReturnType.MetadataType == MetadataType.Void;
        }

        private bool IsUserFiltered(string fullName, string name)
        {
            if (TypeFilters.Any())
            {
                var classFullName = fullName;
                var matched = TypeFilters.Contains(classFullName);
                if (!matched)
                    return true;
            }

            if (MethodFilters.Any())
            {
                var methodFullName = $"{fullName}.{name}";
                var matched = MethodFilters.Contains(methodFullName);
                if (!matched)
                    return true;
            }

            if (PropertyFilter.Any())
            {
                // accessor names, e.g. "Namespace.Type.get_Name"
                var accessorFullName = $"{fullName}.{name}";
                if (!PropertyFilter.Contains(accessorFullName))
                    return true;
            }

            return false;
        }

        private static bool IsIgnoredByWeaving(MethodDefinition method)
        {
            if (ContainsDisableWeavingAttribute(method.CustomAttributes))
                return true;

            // the aspects of a property are read from the property, so is [DisableWeaving]
            var property = method.DeclaringType.Properties.FirstOrDefault(p => p.GetMethod == method || p.SetMethod == method);
            if (property != null && ContainsDisableWeavingAttribute(property.CustomAttributes))
                return true;

            var eventDefinition = method.DeclaringType.Events.FirstOrDefault(e => e.AddMethod == method || e.RemoveMethod == method);
            if (eventDefinition != null && ContainsDisableWeavingAttribute(eventDefinition.CustomAttributes))
                return true;

            var currentClass = method.DeclaringType;
            do
            {
                if (ContainsDisableWeavingAttribute(currentClass.CustomAttributes))
                    return true;

                currentClass = currentClass.DeclaringType;
            } while (currentClass != null);

            if (ContainsDisableWeavingAttribute(method.Module.CustomAttributes))
                return true;

            if (ContainsDisableWeavingAttribute(method.Module.Assembly.CustomAttributes))
                return true;

            return false;
        }

        private static bool ContainsDisableWeavingAttribute(IEnumerable<CustomAttribute> customAttributes)
        {
            return customAttributes.Any(x => x.AttributeType.FullName == AttributeFullNames.DisableWeavingAttribute);
        }

        // nested types have no namespace of their own, it is the one of the outermost declaring type
        private static string GetNamespace(TypeDefinition type)
        {
            while (type.DeclaringType != null)
                type = type.DeclaringType;

            return type.Namespace;
        }

        private static MethodAttributes GetMethodVisibility(MethodDefinition method) =>
            method.Attributes & MethodAttributes.MemberAccessMask;
    }
}