using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Fody;
using MethodBoundaryAspect.Fody.Ordering;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using Mono.Collections.Generic;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// Used tools: 
    /// - .NET Reflector + Addins "Reflexil" / IL Spy / LinqPad
    /// - PEVerify
    /// - ILDasm
    /// - Mono.Cecil
    /// - Fody
    /// 
    /// TODO:
    /// Fix pdb files -> fixed
    /// Intetgrate with Fody -> ok
    /// Support for class aspects -> ok
    /// Support for assembly aspects -> ok
    /// Implement CompileTimeValidate
    /// Optimize weaving: Dont generate code of "OnXXX()" method is empty or not used -> ok
    /// Optimize weaving: remove runtime dependency on "MethodBoundaryAspect.Attributes" assembly
    /// Optimize weaving: only put arguments in MethodExecutionArgs if they are accessed in "OnXXX()" method
    /// Optimize weaving: store GetCurrentMethod() result in static Dictionary in MethodBoundaryAspect with generated id for lookup to prevent reflection penalty -> ok
    /// </summary>
    public class ModuleWeaver : BaseModuleWeaver
    {
        private MethodInfoCompileTimeWeaver _methodInfoCompileTimeWeaver;

        public ModuleWeaver()
        {
            InitLogging();
        }

        public bool DisableCompileTimeMethodInfos { get; set; }
        
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

        private void InitLogging()
        {
            LogDebug = m => Debug.WriteLine(m);
            LogInfo = LogDebug;
            LogWarning = LogDebug;
            LogWarningPoint = (m, p) => { };
            LogError = LogDebug;
            LogErrorPoint = (m, p) => { };
        }

        private void Execute(ModuleDefinition module)
        {
            _methodInfoCompileTimeWeaver = new MethodInfoCompileTimeWeaver(module)
            {
                IsEnabled = !DisableCompileTimeMethodInfos
            };

            var assemblyMethodBoundaryAspects = module.Assembly.CustomAttributes;

            foreach (var type in module.Types.ToList())
                WeaveTypeAndNestedTypes(module, type, assemblyMethodBoundaryAspects);

            _methodInfoCompileTimeWeaver.Finish();
        }

        private void WeaveTypeAndNestedTypes(ModuleDefinition module, TypeDefinition type,
            Collection<CustomAttribute> assemblyMethodBoundaryAspects)
        {
            WeaveType(module, type, assemblyMethodBoundaryAspects);
            if (type.HasNestedTypes)
            {
                var classMethodBoundaryAspects = new Collection<CustomAttribute>();
                foreach (var assemblyAspect in assemblyMethodBoundaryAspects)
                    classMethodBoundaryAspects.Add(assemblyAspect);
                foreach (var classAspect in type.CustomAttributes)
                {
                    if (classAspect.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName)
                        return;
                    classMethodBoundaryAspects.Add(classAspect);
                }
                foreach (var nestedType in type.NestedTypes)
                    WeaveTypeAndNestedTypes(module, nestedType, classMethodBoundaryAspects);
            }
        }

        private void WeaveType(
            ModuleDefinition module, 
            TypeDefinition type, 
            Collection<CustomAttribute> assemblyMethodBoundaryAspects)
        {
            var classMethodBoundaryAspects = type.CustomAttributes;

            var propertyGetters = type.Properties
                .Where(x => x.GetMethod != null)
                .ToDictionary(x => x.GetMethod);

            var propertySetters = type.Properties
                .Where(x => x.SetMethod != null)
                .ToDictionary(x => x.SetMethod);

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
                else
                    methodMethodBoundaryAspects = method.CustomAttributes;

                var methodVisibility = GetMethodVisibility(method);

                var aspectInfos = assemblyMethodBoundaryAspects
                    .Concat(classMethodBoundaryAspects)
                    .Concat(methodMethodBoundaryAspects)
                    .Where(IsMethodBoundaryAspect)
                    .Select(x => new AspectInfo(x))
                    .Where(info => info.HasTargetMemberAttribute(methodVisibility))
                    .ToList();
                if (aspectInfos.Count == 0)
                    continue;
                
                foreach (var aspectInfo in aspectInfos)
                    aspectInfo.InitOrderIndex(assemblyMethodBoundaryAspects, classMethodBoundaryAspects, methodMethodBoundaryAspects);

                weavedAtLeastOneMethod = WeaveMethod(
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

            var methodWeaver = MethodWeaverFactory.MakeWeaver(module, method, aspectInfosWithMethods, methodInfoCompileTimeWeaver);
            methodWeaver.Weave();
            if (methodWeaver.WeaveCounter == 0)
                return false;

            if (method.IsGetter || method.IsSetter)
                TotalWeavedProperties++;
            else
                TotalWeavedMethods++;

            return true;
        }

        private bool IsMethodBoundaryAspect(TypeDefinition attributeTypeDefinition)
        {
            var currentType = attributeTypeDefinition.BaseType;
            do
            {
                if (currentType.FullName == AttributeFullNames.OnMethodBoundaryAspect)
                    return true;

                currentType = currentType.Resolve().BaseType;
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
                var propertySetterFullName = $"{fullName}.{name}";
                var propertyGetterFullName = $"{fullName}.{name}";
                var matched = PropertyFilter.Contains(propertySetterFullName) ||
                              MethodFilters.Contains(propertyGetterFullName);
                if (!matched)
                    return true;
            }

            return false;
        }

        private static bool IsIgnoredByWeaving(MethodDefinition method)
        {
            if (ContainsDisableWeavingAttribute(method.CustomAttributes))
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

        private static MethodAttributes GetMethodVisibility(MethodDefinition method) =>
            method.Attributes & MethodAttributes.MemberAccessMask;
    }
}