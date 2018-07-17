using MethodBoundaryAspect.Fody.Ordering;
using Mono.Cecil;

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
        
        public AspectData(AspectInfo info, AspectMethods methods, MethodDefinition method, ModuleDefinition module)
        {
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
            var chain = _creator.CreateAndNewUpAspect(Info.AspectAttribute);
            AspectPersistable = chain;
            return chain;
        }
    }
}
