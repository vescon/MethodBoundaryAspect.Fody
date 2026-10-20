using System.Collections.Generic;
using System.Linq;
using MethodBoundaryAspect.Fody.Ordering;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// Generates the static class <c>OnMethodBoundaryAspectCompile.Aspects</c> with a field and a getter for each
    /// aspect which is shared by all calls, see <see cref="AspectInstanceReuseAnalyzer"/>. The aspect is created
    /// on the first call, like the method infos of <see cref="MethodInfoCompileTimeWeaver"/>.
    /// </summary>
    public class AspectInstanceCompileTimeWeaver
    {
        private const string CacheNamespace = "OnMethodBoundaryAspectCompile";
        private const string CacheClass = "Aspects";

        private readonly ModuleDefinition _module;
        private readonly AspectInstanceReuseAnalyzer _analyzer;
        private TypeDefinition _aspectsClass;

        // one instance for all methods an attribute (e.g. of a class) is applied to
        private readonly Dictionary<CustomAttribute, MethodDefinition> _getters = new Dictionary<CustomAttribute, MethodDefinition>();

        public AspectInstanceCompileTimeWeaver(ModuleDefinition module, AspectInstanceReuseAnalyzer analyzer)
        {
            _module = module;
            _analyzer = analyzer;
        }

        /// <summary>
        /// The method returning the shared instance of the aspect, null if a new instance has to be created for every call.
        /// </summary>
        public MethodDefinition GetGetter(AspectInfo aspect)
        {
            var attribute = aspect.AspectAttribute;
            if (_getters.TryGetValue(attribute, out var getter))
                return getter;

            if (!_analyzer.CanReuse(aspect.AspectTypeDefinition)
                || attribute.AttributeType.ContainsGenericParameter
                || !IsAccessible(aspect.AspectTypeDefinition))
                return null;

            return AddGetter(attribute);
        }

        // the aspect is created in another class than the one of the woven method, e.g. not possible for a private nested aspect
        private bool IsAccessible(TypeDefinition aspectType)
        {
            for (var type = aspectType; type != null; type = type.DeclaringType)
            {
                var visible = type.IsPublic || type.IsNestedPublic
                              || (type.Module == _module && (type.IsNotPublic || type.IsNestedAssembly || type.IsNestedFamilyOrAssembly));
                if (!visible)
                    return false;
            }

            return aspectType.Methods
                .Where(m => m.IsConstructor && !m.IsStatic)
                .All(m => m.IsPublic || (aspectType.Module == _module && (m.IsAssembly || m.IsFamilyOrAssembly)));
        }

        private MethodDefinition AddGetter(CustomAttribute attribute)
        {
            if (_aspectsClass == null)
            {
                // internal: only the weaved code of the same assembly uses it, it is no API of the assembly
                var classAttributes = TypeAttributes.Class
                                      | TypeAttributes.NotPublic
                                      | TypeAttributes.Abstract // abstract + sealed => static
                                      | TypeAttributes.Sealed;
                _aspectsClass = new TypeDefinition(CacheNamespace, CacheClass, classAttributes)
                {
                    BaseType = _module.TypeSystem.Object
                };
                _module.Types.Add(_aspectsClass);
            }

            var aspectType = _module.ImportReference(attribute.AttributeType);
            var identifier = $"{_getters.Count}_{attribute.AttributeType.Name}";

            var field = new FieldDefinition($"_aspect_{identifier}", FieldAttributes.Private | FieldAttributes.Static, aspectType);
            _aspectsClass.Fields.Add(field);

            var getter = new MethodDefinition($"GetAspect_{identifier}",
                MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig,
                aspectType);
            // PEVerify wants initlocals for a method with a local variable
            getter.Body.InitLocals = true;
            _aspectsClass.Methods.Add(getter);

            // var aspect = _aspect; if (aspect == null) _aspect = aspect = new Aspect(...);
            // concurrent first calls may both create it, which is harmless for a reusable aspect
            var processor = getter.Body.GetILProcessor();
            var ret = Instruction.Create(OpCodes.Ret);
            processor.Append(Instruction.Create(OpCodes.Ldsfld, field));
            processor.Append(Instruction.Create(OpCodes.Dup));
            processor.Append(Instruction.Create(OpCodes.Brtrue, ret));
            processor.Append(Instruction.Create(OpCodes.Pop));

            var newAspect = new InstructionBlockChainCreator(getter, _module).CreateAndNewUpAspect(attribute);
            newAspect.Append(processor);
            processor.Append(Instruction.Create(OpCodes.Ldloc, newAspect.Variable));
            processor.Append(Instruction.Create(OpCodes.Dup));
            processor.Append(Instruction.Create(OpCodes.Stsfld, field));
            processor.Append(ret);

            _getters[attribute] = getter;
            return getter;
        }
    }
}
