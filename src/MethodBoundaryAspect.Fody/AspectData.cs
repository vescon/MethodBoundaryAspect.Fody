using MethodBoundaryAspect.Fody.Ordering;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    public class AspectData
    {
        protected readonly MethodDefinition _method;
        protected readonly ModuleDefinition _module;

        protected readonly ReferenceFinder _referenceFinder;
        protected readonly InstructionBlockChainCreator _creator;

        public IPersistable AspectPersistable { get; protected set; }

        public IPersistable TagPersistable { get; protected set; }
        
        // returns the shared instance of the aspect, null if a new instance is created for every call
        private readonly MethodDefinition _sharedInstanceGetter;

        public AspectData(
            AspectInfo info,
            AspectMethods methods,
            MethodDefinition method,
            ModuleDefinition module,
            MethodDefinition sharedInstanceGetter = null)
        {
            _sharedInstanceGetter = sharedInstanceGetter;
            Info = info;
            AspectMethods = methods;
            _method = method;
            _module = module;
            _referenceFinder = new ReferenceFinder(module);
            _creator = new InstructionBlockChainCreator(method, module);
        }

        public AspectInfo Info { get; private set; }
        public AspectMethods AspectMethods { get; private set; }
        
        public virtual void EnsureTagStorage()
        {
            TagPersistable = _creator.CreateObjectVariable();
        }

        public virtual InstructionBlockChain CreateAspectInstance()
        {
            var chain = CreateOrGetAspect();
            AspectPersistable = chain;
            return chain;
        }

        protected NamedInstructionBlockChain CreateOrGetAspect()
        {
            if (_sharedInstanceGetter == null)
                return _creator.CreateAndNewUpAspect(Info.AspectAttribute);

            var aspect = _creator.CreateVariable(_module.ImportReference(Info.AspectAttribute.AttributeType));
            aspect.Add(new InstructionBlock(
                "Get the shared aspect",
                Instruction.Create(OpCodes.Call, _sharedInstanceGetter),
                Instruction.Create(OpCodes.Stloc, aspect.Variable)));
            return aspect;
        }
    }
}
