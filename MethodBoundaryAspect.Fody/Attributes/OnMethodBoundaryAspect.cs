using System;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.Attributes
{
    [Serializable]
    public abstract class OnMethodBoundaryAspect : Attribute
    {
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