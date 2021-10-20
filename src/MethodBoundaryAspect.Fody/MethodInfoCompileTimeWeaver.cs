using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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

        private readonly Dictionary<MethodDefinition, FieldDefinition> _fieldsCache
            = new Dictionary<MethodDefinition, FieldDefinition>();

        private readonly string _cacheNamespace = "OnMethodBoundaryAspectCompile";
        private readonly string _cacheClass = "MethodInfos";

        public MethodInfoCompileTimeWeaver(ModuleDefinition module)
        {
            _mainModule = module;
        }

        public bool IsEnabled { get; set; }

        public void EnsureInit()
        {
            if (_methodInfosClass != null)
                return;
            
            var classAttributes = TypeAttributes.Class
                                  | TypeAttributes.Public
                                  | TypeAttributes.Abstract // abstract + sealed => static
                                  | TypeAttributes.Sealed;
            _methodInfosClass = new TypeDefinition(_cacheNamespace, _cacheClass, classAttributes)
            {
                BaseType = GetTypeReference(typeof(object))
            };
            _mainModule.Types.Add(_methodInfosClass);
        }

        public void AddMethod(MethodDefinition method)
        {
            EnsureInit();

            var typeReferenceMethodBase = GetTypeReference(typeof(MethodBase));
            _mainModule.ImportReference(typeReferenceMethodBase);

            var identifier = CreateIdentifier(method);
            var fieldAttributes = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly;
            var fd = new FieldDefinition($"_methodInfo_{identifier}", fieldAttributes, typeReferenceMethodBase);
            _methodInfosClass.Fields.Add(fd);

            _fieldsCache.Add(method, fd);
        }

        public InstructionBlock PushMethodInfoOnStack(MethodDefinition method, VariablePersistable variablePersistable)
        {
            var fieldDefinition = _fieldsCache[method];
            var getMethodFromHandle2 = ImportGetMethodFromHandleArg2();

            var instructions = new List<Instruction>();
            if (ContainsOpenTypeRecursive(method))
            {
                instructions.Add(Instruction.Create(OpCodes.Ldtoken, method));
                instructions.Add(Instruction.Create(OpCodes.Ldtoken, method.DeclaringType));
                instructions.Add(Instruction.Create(OpCodes.Call, getMethodFromHandle2));
            }
            else
                instructions.Add(Instruction.Create(OpCodes.Ldsfld, fieldDefinition));
            
            var store = variablePersistable.Store(
                new InstructionBlock("", instructions),
                variablePersistable.PersistedType);
            return new InstructionBlock($"Load method info for '{method.Name}'", store.Instructions);
        }

        public void Finish()
        {
            CreateStaticCtor();
        }

        private static bool ContainsOpenTypeRecursive(MethodDefinition method)
        {
            if (ContainsOpenType(method))
                return true;

            var parentType = method.DeclaringType;
            while (parentType != null)
            {
                if (ContainsOpenType(parentType))
                    return true;

                parentType = parentType.DeclaringType;
            }

            return false;
        }

        private static bool ContainsOpenType(IGenericParameterProvider method)
        {
            return method.GenericParameters.Any(x => x.ContainsGenericParameter);
        }

        private string CreateIdentifier(MemberReference method)
        {
            var bytes = Encoding.UTF8.GetBytes(method.FullName);
            var sha256 = new SHA256Managed();
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        private void CreateStaticCtor()
        {
            var typeReferenceVoid = GetTypeReference(typeof(void));
            var staticConstructorAttributes =
                MethodAttributes.Private |
                MethodAttributes.HideBySig |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName |
                MethodAttributes.Static;
            var cctor = new MethodDefinition(".cctor", staticConstructorAttributes, typeReferenceVoid);

            // taken from https://gist.github.com/jbevain/390902
            var getMethodFromHandle = ImportGetMethodFromHandleArg1();
            var cctorInstructions = new List<Instruction>();
            foreach (var entry in _fieldsCache)
            {
                if (ContainsOpenTypeRecursive(entry.Key))
                    continue; // method info has to be resolved during runtime so we don't need a cache entry

                cctorInstructions.Add(Instruction.Create(OpCodes.Ldtoken, entry.Key));
                cctorInstructions.Add(Instruction.Create(OpCodes.Call, getMethodFromHandle));
                cctorInstructions.Add(Instruction.Create(OpCodes.Stsfld, entry.Value));
            }

            if (!cctorInstructions.Any())
                return;

            cctorInstructions.Add(Instruction.Create(OpCodes.Ret));

            foreach (var methodInstruction in cctorInstructions)
                cctor.Body.Instructions.Add(methodInstruction);
            cctor.Body.Optimize();

            _methodInfosClass.Methods.Add(cctor);
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

        private MethodReference ImportGetMethodFromHandleArg1()
        {
            return _mainModule.ImportReference(typeof(MethodBase)
                .GetMethod("GetMethodFromHandle", new[] {typeof(RuntimeMethodHandle)}));
        }

        private MethodReference ImportGetMethodFromHandleArg2()
        {
            return _mainModule.ImportReference(typeof(MethodBase)
                .GetMethod("GetMethodFromHandle", new[] {typeof(RuntimeMethodHandle),typeof(RuntimeTypeHandle)}));
        }
    }
}
