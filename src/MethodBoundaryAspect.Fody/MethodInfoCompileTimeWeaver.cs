using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace MethodBoundaryAspect.Fody
{
    public class MethodInfoCompileTimeWeaver
    {
        private readonly ModuleDefinition _mainModule;
        private TypeDefinition _methodInfosClass;
        private MethodReference _getMethodFromHandle;
        private MethodReference _getMethodFromHandleWithType;

        // the method returning the cached method info of a woven method
        private readonly Dictionary<MethodDefinition, MethodDefinition> _gettersCache
            = new Dictionary<MethodDefinition, MethodDefinition>();

        private const string CacheClass = "MethodInfos";

        public MethodInfoCompileTimeWeaver(ModuleDefinition module)
        {
            _mainModule = module;
        }

        private void EnsureInit()
        {
            if (_methodInfosClass != null)
                return;
            
            _methodInfosClass = CompileTimeClassBuilder.CreateStaticClass(_mainModule, CacheClass, GetTypeReference(typeof(object)));

            // taken from https://gist.github.com/jbevain/390902
            _getMethodFromHandle = ImportGetMethodFromHandle(typeof(RuntimeMethodHandle));
            _getMethodFromHandleWithType = ImportGetMethodFromHandle(typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle));
        }

        /// <summary>
        /// Adds a field and a method returning the method info of <paramref name="method"/>. The method info is
        /// loaded on the first call, not for all woven methods at once: that would take time at startup, and a method
        /// which can't be loaded (e.g. a type of a missing assembly in its signature) would break all woven methods.
        /// </summary>
        public void AddMethod(MethodDefinition method)
        {
            EnsureInit();

            var typeReferenceMethodBase = GetTypeReference(typeof(MethodBase));
            // no hash of the method name: SHA256Managed throws on .NET Framework (Visual Studio) if FIPS mode is enabled
            var identifier = $"{_gettersCache.Count}_{method.Name}";

            var field = new FieldDefinition($"_methodInfo_{identifier}", FieldAttributes.Private | FieldAttributes.Static, typeReferenceMethodBase);
            _methodInfosClass.Fields.Add(field);

            var getter = new MethodDefinition($"GetMethodInfo_{identifier}",
                MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig,
                typeReferenceMethodBase)
            {
                AggressiveInlining = true
            };

            // var methodInfo = _methodInfo; if (methodInfo == null) _methodInfo = methodInfo = GetMethodFromHandle(...);
            // concurrent first calls may both load it, which is harmless
            var processor = getter.Body.GetILProcessor();
            CompileTimeClassBuilder.AppendLazyGetterBody(processor, field, () =>
            {
                processor.Append(Instruction.Create(OpCodes.Ldtoken, method));
                if (ContainsOpenTypeRecursive(method))
                {
                    // a method of a generic type needs its declaring type, the method info is the open
                    // definition for every instantiation, so it is cached as well
                    processor.Append(Instruction.Create(OpCodes.Ldtoken, method.DeclaringType));
                    processor.Append(Instruction.Create(OpCodes.Call, _getMethodFromHandleWithType));
                }
                else
                    processor.Append(Instruction.Create(OpCodes.Call, _getMethodFromHandle));
            });
            getter.Body.Optimize();

            _methodInfosClass.Methods.Add(getter);
            _gettersCache.Add(method, getter);
        }

        public InstructionBlock PushMethodInfoOnStack(MethodDefinition method, VariablePersistable variablePersistable)
        {
            var getter = _gettersCache[method];
            var instructions = new List<Instruction> { Instruction.Create(OpCodes.Call, getter) };
            var store = variablePersistable.Store(
                new InstructionBlock("", instructions),
                variablePersistable.PersistedType);
            return new InstructionBlock($"Load method info for '{method.Name}'", store.Instructions);
        }

        private static bool ContainsOpenTypeRecursive(MethodDefinition method)
        {
            if (method.HasGenericParameters)
                return true;

            var parentType = method.DeclaringType;
            while (parentType != null)
            {
                if (parentType.HasGenericParameters)
                    return true;

                parentType = parentType.DeclaringType;
            }

            return false;
        }

        private TypeReference GetTypeReference(Type type)
        {
            return GetTypeReference(type.FullName);
        }

        private TypeReference GetTypeReference(string name)
        {
            switch (name)
            {
                case "System.Object":
                    return _mainModule.TypeSystem.Object;
                case "System.Void":
                    return _mainModule.TypeSystem.Void;
                default:
                {
                    var split = name.Split('.');
                    var namespaceName = string.Join(".", split.Take(split.Length - 1));
                    var typeName = split.Last();
                    return new TypeReference(namespaceName, typeName, _mainModule, _mainModule.TypeSystem.CoreLibrary);
                }
            }
        }

        // Resolve from the target module's core library. Importing typeof(MethodBase).GetMethod(...)
        // would reference the core library of the runtime hosting the weaver, i.e. System.Private.CoreLib
        // when Fody runs under Core MSBuild (dotnet build), which a .NET Framework target cannot load.
        private MethodReference ImportGetMethodFromHandle(params Type[] parameterTypes)
        {
            var methodBaseDefinition = GetTypeReference(typeof(MethodBase)).Resolve()
                ?? throw new InvalidOperationException(
                    $"Could not resolve '{typeof(MethodBase).FullName}' from core library '{_mainModule.TypeSystem.CoreLibrary.Name}'.");

            var parameterTypeNames = parameterTypes.Select(t => t.FullName);
            var getMethodFromHandle = methodBaseDefinition.Methods.Single(m =>
                m.Name == nameof(MethodBase.GetMethodFromHandle)
                && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameterTypeNames));

            return _mainModule.ImportReference(getMethodFromHandle);
        }
    }
}
