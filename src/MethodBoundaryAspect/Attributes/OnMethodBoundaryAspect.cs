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

        /// <summary>
        /// A regular expression for the namespace of the types the aspect is woven into.
        /// Like AttributeTargetMemberAttributes it is only read where the aspect is applied,
        /// e.g. [Log(NamespaceFilter = "...")]. A value set in the constructor of the aspect or by
        /// overriding the property is ignored (the weaver warns about it).
        /// </summary>
        public virtual string NamespaceFilter { get; set; }

        /// <summary>
        /// A regular expression for the names of the types the aspect is woven into.
        /// Only read where the aspect is applied, see <see cref="NamespaceFilter"/>.
        /// </summary>
        public virtual string TypeNameFilter { get; set; }

        /// <summary>
        /// A regular expression for the names of the methods the aspect is woven into.
        /// Only read where the aspect is applied, see <see cref="NamespaceFilter"/>.
        /// </summary>
        public virtual string MethodNameFilter { get; set; }

        /// <summary>
        /// Called before the method body. For async methods when the method is called.
        /// </summary>
        public virtual void OnEntry(MethodExecutionArgs arg)
        {
        }

        /// <summary>
        /// Called after the method body completed successfully, not after an exception. For async methods when
        /// the method returns its task to the caller, usually before the asynchronous work is done.
        /// </summary>
        public virtual void OnExit(MethodExecutionArgs arg)
        {
        }

        /// <summary>
        /// Called when the method body throws. For async methods when the asynchronous work fails, possibly
        /// long after <see cref="OnExit"/>. The exception is rethrown unless
        /// <see cref="MethodExecutionArgs.FlowBehavior"/> is changed.
        /// </summary>
        public virtual void OnException(MethodExecutionArgs arg)
        {
        }

        [Obsolete("CompileTimeValidate is never called by the weaver, which does not run the code of aspects. It will be removed in version 4.")]
        public virtual bool CompileTimeValidate(MethodBase method)
        {
            throw new NotImplementedException("TODO!");
        }
    }
}