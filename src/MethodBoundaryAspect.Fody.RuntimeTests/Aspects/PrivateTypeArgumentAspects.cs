using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Reads the arguments, so the weaver has to put them into MethodExecutionArgs.
    /// </summary>
    public class ReadArgumentsAttribute : OnMethodBoundaryAspect
    {
        public static int ArgumentCount;
        public static string LastType;

        public override void OnEntry(MethodExecutionArgs arg) => ArgumentCount = arg.Arguments.Length;
    }

#if NETCOREAPP
    /// <summary>
    /// Holds no state, so one instance would be shared; applied with a type argument which is private.
    /// </summary>
    public class TypeArgumentAspectAttribute<T> : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) => ReadArgumentsAttribute.LastType = typeof(T).Name;
    }
#endif
}
