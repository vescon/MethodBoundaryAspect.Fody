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
            var instruction = Instruction.Create(OpCodes.Ldsfld, fieldDefinition);
            var store = variablePersistable.Store(
                new InstructionBlock("", instruction),
                variablePersistable.PersistedType);
            return new InstructionBlock($"Load method info for '{method.Name}'", store.Instructions);
        }

        public void Finish()
        {
            if (_fieldsCache.Any())
                CreateStaticCtor();
        }
        
        public bool CanWeave(MethodDefinition method)
        {
            // no support for open generic types
            if (IsOpenType(method))
                return false;

            var parentType = method.DeclaringType;
            while (parentType != null)
            {
                if (IsOpenType(parentType))
                    return false;
                parentType = parentType.DeclaringType;
            }

            return true;
        }

        private static bool IsOpenType(IGenericParameterProvider method)
        {
            return method.GenericParameters.Any(x => x.IsGenericParameter);
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
            var getMethodFromHandle = ImportGetMethodFromHandle();
            var cctorInstructions = new List<Instruction>();
            foreach (var entry in _fieldsCache)
            {
                cctorInstructions.Add(Instruction.Create(OpCodes.Ldtoken, entry.Key));
                cctorInstructions.Add(Instruction.Create(OpCodes.Call, getMethodFromHandle));
                cctorInstructions.Add(Instruction.Create(OpCodes.Stsfld, entry.Value));
            }

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
                    var splitted = name.Split('.');
                    var namespaceName = string.Join(".", splitted.Take(splitted.Length - 1));
                    var typeName = splitted.Last();
                    return new TypeReference(namespaceName, typeName, _mainModule, _mainModule.TypeSystem.CoreLibrary);
                }
            }
        }

        private MethodReference ImportGetMethodFromHandle()
        {
            return _mainModule.ImportReference(typeof(MethodBase)
                .GetMethod("GetMethodFromHandle", new[] {typeof(RuntimeMethodHandle)}));
        }
    }
}
