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
                SymbolReaderProvider = new PdbReaderProvider()
            };

            if (AdditionalAssemblyResolveFolders.Any())
                readerParameters.AssemblyResolver = new FolderAssemblyResolver(AdditionalAssemblyResolveFolders);
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);

            var module = assemblyDefinition.MainModule;
            Execute(module);

            var writerParameters = new WriterParameters
            {
                WriteSymbols = true,
                SymbolWriterProvider = new PdbWriterProvider()
            };
            assemblyDefinition.Write(assemblyPath, writerParameters);
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
            {
                var classMethodBoundaryAspects = type.CustomAttributes;

                var weavedAtLeastOneMethod = false;
                foreach (var method in type.Methods.Where(IsWeavableMethod))
                {
                    var methodMethodBoundaryAspects = method.CustomAttributes;
                    
                    if (method.IsGetter || method.IsSetter)
                    {
                        var propertyNameParts = method.Name.Split(new[] { '_' });
                        var propertyName = string.Join("_", propertyNameParts.Skip(1));
                        var propertyDefinition = type.Properties.Single(x => x.Name == propertyName);
                        methodMethodBoundaryAspects = propertyDefinition.CustomAttributes;
                    }

                    var aspectInfos = assemblyMethodBoundaryAspects
                        .Concat(classMethodBoundaryAspects)
                        .Concat(methodMethodBoundaryAspects)
                        .Where(IsMethodBoundaryAspect)
                        .Select(x => new AspectInfo(x))
                        .ToList();

                    if (aspectInfos.Count == 0)
                        continue;

                    aspectInfos = AspectOrderer.Order(aspectInfos);
                    aspectInfos.Reverse(); // last aspect has to be weaved in first

                    using (var methodWeaver = new MethodWeaver())
                    {
                        foreach (var aspectInfo in aspectInfos)
                        {
                            ////var log = string.Format("Weave OnMethodBoundaryAspect '{0}' in method '{1}' from class '{2}'",
                            ////    attributeTypeDefinition.Name,
                            ////    method.Name,
                            ////    method.DeclaringType.FullName);
                            ////LogWarning(log);

                            if (aspectInfo.SkipProperties && (method.IsGetter || method.IsSetter))
                                continue;

                            var aspectTypeDefinition = aspectInfo.AspectAttribute.AttributeType;

                            var overriddenAspectMethods = GetUsedAspectMethods(aspectTypeDefinition);
                            if (overriddenAspectMethods == AspectMethods.None)
                                continue;

                            methodWeaver.Weave(method, aspectInfo.AspectAttribute, overriddenAspectMethods, module);
                        }

                        if (methodWeaver.WeaveCounter == 0)
                            continue;
                    }

                    weavedAtLeastOneMethod = true;

                    if (method.IsGetter || method.IsSetter)
                        TotalWeavedPropertyMethods++;
                    else
                        TotalWeavedMethods++;

                    LastWeavedMethod = method;
                }

                if (weavedAtLeastOneMethod)
                    TotalWeavedTypes++;
            }
        }

        private AspectMethods GetUsedAspectMethods(TypeReference aspectTypeDefinition)
        {
            var overloadedMethods = aspectTypeDefinition.Resolve().Methods;

            var aspectMethods = AspectMethods.None;
            if (overloadedMethods.Any(x => x.Name == "OnEntry"))
                aspectMethods |= AspectMethods.OnEntry;
            if (overloadedMethods.Any(x => x.Name == "OnExit"))
                aspectMethods |= AspectMethods.OnExit;
            if (overloadedMethods.Any(x => x.Name == "OnException"))
                aspectMethods |= AspectMethods.OnException;
            return aspectMethods;
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
            if (_classFilters.Any())
            {
                var classFullName = method.DeclaringType.FullName;
                var matched = _classFilters.Contains(classFullName);
                if (!matched)
                    return false;
            }

            if (_methodFilters.Any())
            {
                var methodFullName = string.Format("{0}.{1}", method.DeclaringType.FullName, method.Name);
                var matched = _methodFilters.Contains(methodFullName);
                if (!matched)
                    return false;
            }

            if (_propertyFilter.Any())
            {
                var propertySetterFullName = string.Format("{0}.{1}", method.DeclaringType.FullName, method.Name);
                var propertyGetterFullName = string.Format("{0}.{1}", method.DeclaringType.FullName, method.Name);
                var matched = _propertyFilter.Contains(propertySetterFullName) || _methodFilters.Contains(propertyGetterFullName);
                if (!matched)
                    return false;
            }

            return !(method.IsAbstract // abstract or interface method
                     || method.IsConstructor
                     || method.Name.StartsWith("<") // anonymous
                     || method.IsPInvokeImpl); // extern
        }
    }
}