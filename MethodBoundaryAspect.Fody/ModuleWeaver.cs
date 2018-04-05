using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.Ordering;
using Mono.Cecil;
using Mono.Cecil.Cil;
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
    /// </summary>
    public class ModuleWeaver
    {
        public readonly List<string> AdditionalAssemblyResolveFolders = new List<string>();

        private readonly List<string> _classFilters = new List<string>();
        private readonly List<string> _methodFilters = new List<string>();
        private readonly List<string> _propertyFilter = new List<string>();

        public ModuleWeaver()
        {
            InitLogging();
        }

        public ModuleDefinition ModuleDefinition { get; set; }

        public Action<string> LogDebug { get; set; }
        public Action<string> LogInfo { get; set; }
        public Action<string> LogWarning { get; set; }
        public Action<string, SequencePoint> LogWarningPoint { get; set; }
        public Action<string> LogError { get; set; }
        public Action<string, SequencePoint> LogErrorPoint { get; set; }

        public int TotalWeavedTypes { get; private set; }
        public int TotalWeavedMethods { get; private set; }
        public int TotalWeavedPropertyMethods { get; private set; }

        public MethodDefinition LastWeavedMethod { get; private set; }
        public List<string> MethodFilters => _methodFilters;
        public List<string> TypeFilters => _classFilters;

        public void Execute()
        {
            Execute(ModuleDefinition);
        }

        public string CreateShadowAssemblyPath(string assemblyPath)
        {
            var fileInfoSource = new FileInfo(assemblyPath);
            return
                fileInfoSource.DirectoryName
                + Path.DirectorySeparatorChar
                + Path.GetFileNameWithoutExtension(fileInfoSource.Name)
                + "_Weaved_"
                + fileInfoSource.Extension.ToLower();
        }

        public string WeaveToShadowFile(string assemblyPath)
        {
            var shadowAssemblyPath = CreateShadowAssemblyPath(assemblyPath);
            File.Copy(assemblyPath, shadowAssemblyPath, true);

            var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
            var shadowPdbPath = CreateShadowAssemblyPath(pdbPath);

            if (File.Exists(pdbPath))
                File.Copy(pdbPath, shadowPdbPath, true);

            Weave(shadowAssemblyPath);
            return shadowAssemblyPath;
        }

        public void Weave(string assemblyPath)
        {
            var readerParameters = new ReaderParameters
            {
                ReadSymbols = true,
                SymbolReaderProvider = new PdbReaderProvider(),
				ReadWrite = true,
            };

            if (AdditionalAssemblyResolveFolders.Any())
                readerParameters.AssemblyResolver = new FolderAssemblyResolver(AdditionalAssemblyResolveFolders);

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
            _classFilters.Add(classFilter);
        }

        public void AddMethodFilter(string methodFilter)
        {
            _methodFilters.Add(methodFilter);
        }

        public void AddPropertyFilter(string propertyFilter)
        {
            _propertyFilter.Add(propertyFilter);
        }
        public void AddAdditionalAssemblyResolveFolder(string folderName)
        {
            AdditionalAssemblyResolveFolders.Add(folderName);
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
            var assemblyMethodBoundaryAspects = module.Assembly.CustomAttributes;

            foreach (var type in module.Types)
                WeaveType(module, type, assemblyMethodBoundaryAspects);
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

                var aspectInfos = assemblyMethodBoundaryAspects
                    .Concat(classMethodBoundaryAspects)
                    .Concat(methodMethodBoundaryAspects)
                    .Where(IsMethodBoundaryAspect)
                    .Select(x => new AspectInfo(x))
                    .ToList();
                if (aspectInfos.Count == 0)
                    continue;

                weavedAtLeastOneMethod = WeaveMethod(
                    module,
                    method,
                    aspectInfos);
            }   

            if (weavedAtLeastOneMethod)
                TotalWeavedTypes++;
        }

        private bool WeaveMethod(
            ModuleDefinition module, 
            MethodDefinition method,
            List<AspectInfo> aspectInfos)
        {
            aspectInfos = AspectOrderer.Order(aspectInfos);
            var aspectInfosWithMethods = aspectInfos
                .Where(x => !x.SkipProperties || (!method.IsGetter && !method.IsSetter))
                .ToList();

            var methodWeaver = new MethodWeaver();
            methodWeaver.Weave(module, method, aspectInfosWithMethods);
            if (methodWeaver.WeaveCounter == 0)
                return false;

            if (method.IsGetter || method.IsSetter)
                TotalWeavedPropertyMethods++;
            else
                TotalWeavedMethods++;

            LastWeavedMethod = method;
            return true;
        }

        private bool IsMethodBoundaryAspect(TypeDefinition attributeTypeDefinition)
        {
            var currentType = attributeTypeDefinition.BaseType;
            do
            {
                if (currentType.FullName == typeof(OnMethodBoundaryAspect).FullName)
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

            return !(method.IsAbstract // abstract or interface method
                     || method.IsConstructor
                     || name.StartsWith("<") // anonymous
                     || method.IsPInvokeImpl); // extern
        }

        private bool IsUserFiltered(string fullName, string name)
        {
            if (_classFilters.Any())
            {
                var classFullName = fullName;
                var matched = _classFilters.Contains(classFullName);
                if (!matched)
                    return true;
            }

            if (_methodFilters.Any())
            {
                var methodFullName = string.Format("{0}.{1}", fullName, name);
                var matched = _methodFilters.Contains(methodFullName);
                if (!matched)
                    return true;
            }

            if (_propertyFilter.Any())
            {
                var propertySetterFullName = string.Format("{0}.{1}", fullName, name);
                var propertyGetterFullName = string.Format("{0}.{1}", fullName, name);
                var matched = _propertyFilter.Contains(propertySetterFullName) ||
                              _methodFilters.Contains(propertyGetterFullName);
                if (!matched)
                    return true;
            }

            return false;
        }

        private static bool IsIgnoredByWeaving(ICustomAttributeProvider method)
        {
            return method.CustomAttributes.Any(x => x.AttributeType.FullName == typeof(DisableWeavingAttribute).FullName);
        }
    }
}