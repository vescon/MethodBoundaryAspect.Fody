using System;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.Attributes
{
    public abstract class OnMethodBoundaryAspect : Attribute
    {
        public MulticastAttributes AttributeTargetMemberAttributes { get; set; } =
            MulticastAttributes.AnyVisibility;

        public virtual string NamespaceFilter { get; set; }
        public virtual string TypeNameFilter { get; set; }
        public virtual string MethodNameFilter { get; set; }

        public virtual void OnEntry(MethodExecutionArgs arg)
        {
        }

        public virtual void OnExit(MethodExecutionArgs arg)
        {
        }

        public virtual void OnException(MethodExecutionArgs arg)
        {
        }

        public virtual bool CompileTimeValidate(MethodBase method)
        {
            throw new NotImplementedException("TODO!");
        }
    }
}