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

        public bool DisableCompileTimeMethodInfos { get; set; }

        /// <summary>
        /// Suppresses the warning for weaved methods using ref struct values (e.g. Span&lt;T&gt;).
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

        // aspects for which the warning about the obsolete roles was written
        private readonly HashSet<string> _warnedAboutAspectRoles = new HashSet<string>();

        // aspects which were checked for filters set in their code
        private readonly HashSet<string> _checkedAspectFilters = new HashSet<string>();

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
            yield return "mscorlib";
            yield return "System";
            yield return "System.Runtime";
            yield return "System.Reflection";
            yield return "System.Diagnostics";
            yield return "netstandard";
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
            var prefix = Environment.TickCount.ToString();
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

            ReadExecutionArgsOptimizationConfig();

            _methodInfoCompileTimeWeaver = new MethodInfoCompileTimeWeaver(module)
            {
                IsEnabled = !DisableCompileTimeMethodInfos
            };
            _executionArgsUsageAnalyzer = new ExecutionArgsUsageAnalyzer(
                DisableExecutionArgsOptimization,
                DisableExecutionArgsOptimizationAspects);

            var assemblyMethodBoundaryAspects = module.Assembly.CustomAttributes;

            foreach (var type in module.Types.ToList())
                WeaveTypeAndNestedTypes(module, type, assemblyMethodBoundaryAspects, new List<Collection<CustomAttribute>>());

            foreach (var aspect in DisableExecutionArgsOptimizationAspects)
            {
                if (!_executionArgsUsageAnalyzer.MatchedDisabledAspects.Contains(ExecutionArgsUsageAnalyzer.NormalizeTypeName(aspect)))
                    WriteWarning($"Aspect '{aspect}' configured in DisableExecutionArgsOptimization of FodyWeavers.xml was not found in any weaved method. Use the full type name, e.g. 'MyNamespace.MyAspect'.");
            }
        }

        private void ReadExecutionArgsOptimizationConfig()
        {
            if (Config == null)
                return;

            var attribute = Config.Attribute(nameof(DisableExecutionArgsOptimization));
            if (attribute != null)
                DisableExecutionArgsOptimization = ParseBool(attribute);

            foreach (var element in Config.Elements(nameof(DisableExecutionArgsOptimization)))
            {
                var aspect = element.Attribute("Aspect")?.Value;
                if (string.IsNullOrWhiteSpace(aspect))
                    throw new WeavingException($"<{nameof(DisableExecutionArgsOptimization)}> in FodyWeavers.xml requires the full type name of the aspect, e.g. <{nameof(DisableExecutionArgsOptimization)} Aspect=\"MyNamespace.MyAspect\" />.");
                DisableExecutionArgsOptimizationAspects.Add(aspect.Trim());
            }
        }

        private static string ShortName(string fullName)
        {
            var name = fullName.Substring(fullName.LastIndexOfAny(new[] { '.', '/' }) + 1);
            return name.EndsWith("Attribute") ? name.Substring(0, name.Length - "Attribute".Length) : name;
        }

        private static bool MatchesFilter(string value, string filter, AspectInfo aspectInfo, string filterName)
        {
            if (string.IsNullOrEmpty(filter))
                return true;

            try
            {
                return Regex.IsMatch(value, filter);
            }
            catch (ArgumentException e)
            {
                throw new WeavingException($"Aspect '{aspectInfo.Name}' has an invalid regular expression in {filterName}: {e.Message}");
            }
        }

        private static bool ParseBool(XAttribute attribute)
        {
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

        private void WeaveType(
            ModuleDefinition module,
            TypeDefinition type,
            Collection<CustomAttribute> assemblyMethodBoundaryAspects,
            List<Collection<CustomAttribute>> enclosingTypeAttributes)
        {
            var classMethodBoundaryAspects = type.CustomAttributes;

            var propertyGetters = type.Properties
                .Where(x => x.GetMethod != null)
                .ToDictionary(x => x.GetMethod);

            var propertySetters = type.Properties
                .Where(x => x.SetMethod != null)
                .ToDictionary(x => x.SetMethod);

            // the aspects of an event are read from the event, like the ones of a property
            var eventAccessors = new Dictionary<MethodDefinition, Collection<CustomAttribute>>();
            foreach (var eventDefinition in type.Events)
            {
                if (eventDefinition.AddMethod != null)
                    eventAccessors[eventDefinition.AddMethod] = eventDefinition.CustomAttributes;
                if (eventDefinition.RemoveMethod != null)
                    eventAccessors[eventDefinition.RemoveMethod] = eventDefinition.CustomAttributes;
            }

            var typeNamespace = GetNamespace(type);
            var weavedAtLeastOneMethod = false;
            foreach (var method in type.Methods.ToList())
            {
                if (!IsWeavableMethod(method))
                    continue;

                Collection<CustomAttribute> methodMethodBoundaryAspects;

                if (method.IsGetter)
                    methodMethodBoundaryAspects = propertyGetters[method].CustomAttributes;
                else if (method.IsSetter)
                    methodMethodBoundaryAspects = propertySetters[method].CustomAttributes;
                else if (eventAccessors.TryGetValue(method, out var eventAttributes))
                    methodMethodBoundaryAspects = eventAttributes;
                else
                    methodMethodBoundaryAspects = method.CustomAttributes;

                var methodVisibility = GetMethodVisibility(method);

                var aspectInfos = assemblyMethodBoundaryAspects
                    .Concat(enclosingTypeAttributes.SelectMany(attributes => attributes))
                    .Concat(classMethodBoundaryAspects)
                    .Concat(methodMethodBoundaryAspects)
                    .Where(IsMethodBoundaryAspect)
                    .Select(x => new AspectInfo(x))
                    .Where(info => info.HasTargetMemberAttribute(methodVisibility))
                    .Where(info => MatchesFilter(typeNamespace, info.NamespaceFilter, info, nameof(AspectInfo.NamespaceFilter)))
                    .Where(info => MatchesFilter(type.Name, info.TypeNameFilter, info, nameof(AspectInfo.TypeNameFilter)))
                    .Where(info => MatchesFilter(method.Name, info.MethodNameFilter, info, nameof(AspectInfo.MethodNameFilter)))
                    .ToList();
                if (aspectInfos.Count == 0)
                    continue;

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
                            $"Set it where the aspect is applied instead, e.g. [{ShortName(aspectInfo.Name)}({ignoredValues[0]} = ...)].");
                    }
                }

                foreach (var aspectInfo in aspectInfos)
                    aspectInfo.InitOrderIndex(assemblyMethodBoundaryAspects, enclosingTypeAttributes, classMethodBoundaryAspects, methodMethodBoundaryAspects);

                weavedAtLeastOneMethod |= WeaveMethod(
                    module,
                    method,
                    aspectInfos,
                    _methodInfoCompileTimeWeaver);
            }   

            if (weavedAtLeastOneMethod)
                TotalWeavedTypes++;
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
                module, method, aspectInfosWithMethods, methodInfoCompileTimeWeaver, _executionArgsUsageAnalyzer, out var unsupportedReason);
            if (methodWeaver == null)
            {
                // not touched at all, failing the build would block the whole project for a single method
                WriteWarning(
                    $"Method '{method.FullName}' is not weaved: {unsupportedReason}. " +
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
            // they are weaved as null, see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/129
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
            return IsMethodBoundaryAspect(customAttribute.AttributeType.Resolve());
        }

        private bool IsWeavableMethod(MethodDefinition method)
        {
            var fullName = method.DeclaringType.FullName;
            var name = method.Name;

            if (IsIgnoredByWeaving(method))
                return false;

            if (IsUserFiltered(fullName, name))
                return false;

            if (IsDelegate(method))
                return false;

            return !(method.IsAbstract // abstract or interface method
                     || method.IsConstructor
                     || name.StartsWith("<") // anonymous
                     || method.IsPInvokeImpl); // extern
        }

        private bool IsDelegate(MethodDefinition method)
        {
            return !method.HasBody;
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