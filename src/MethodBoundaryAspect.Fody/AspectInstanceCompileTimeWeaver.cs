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
                _aspectsClass = CompileTimeClassBuilder.CreateStaticClass(_module, CacheClass, _module.TypeSystem.Object);

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
            CompileTimeClassBuilder.AppendLazyGetterBody(processor, field, () =>
            {
                var newAspect = new InstructionBlockChainCreator(getter, _module).CreateAndNewUpAspect(attribute);
                newAspect.Append(processor);
                processor.Append(Instruction.Create(OpCodes.Ldloc, newAspect.Variable));
            });

            _getters[attribute] = getter;
            return getter;
        }
    }
}
