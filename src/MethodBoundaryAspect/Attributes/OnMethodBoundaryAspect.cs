using System;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.Attributes
{
    public abstract class OnMethodBoundaryAspect : Attribute
    {
        /// <summary>
        /// The visibilities of the methods the aspect is woven into, by default all.
        /// The weaver only sees a value set where the aspect is applied, e.g.
        /// [Log(AttributeTargetMemberAttributes = MulticastAttributes.Public)], not a value set in the
        /// constructor of the aspect. Without such a value methods of all visibilities are woven, so the
        /// default value here has to be AnyVisibility as well.
        /// </summary>
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