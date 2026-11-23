using System.Runtime.InteropServices;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    // methods without a body are never woven, and aspects are not inherited by overrides and implementations
    public interface IIgnoredAspectTargets
    {
        [OnlyOnEntryAspect]
        void InterfaceMethod();

        [OnlyOnEntryAspect]
        int InterfaceProperty { get; }
    }

    public abstract class IgnoredAspectTargetsWithoutBody
    {
        [OnlyOnEntryAspect]
        public abstract void AbstractMethod();

        [OnlyOnEntryAspect]
        public abstract int AbstractProperty { get; set; }

        [OnlyOnEntryAspect]
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [OnlyOnEntryAspect]
        public virtual void MethodWithBody()
        {
        }
    }

    // an aspect on a type hits its methods with a body: these have none
    [OnlyOnEntryAspect]
    public interface IIgnoredAspectOnInterface
    {
        void Method();
    }

    [OnlyOnEntryAspect]
    public abstract class IgnoredAspectOnAbstractClass
    {
        public abstract void Method();
    }

    [OnlyOnEntryAspect]
    public delegate void IgnoredAspectOnDelegate();

    [OnlyOnEntryAspect]
    public enum IgnoredAspectOnEnum
    {
        Value
    }

    // woven: a method with a body, a method of a nested class (no interface with a default method here, PEVerify
    // rejects the whole assembly for it)
    [OnlyOnEntryAspect]
    public abstract class AspectOnAbstractClassWithMethodBody
    {
        public abstract void Method();

        public int MethodWithBody() => 1;
    }

    [OnlyOnEntryAspect]
    public abstract class AspectOnAbstractClassWithNestedClass
    {
        public abstract void Method();

        public class Nested
        {
            public int Method() => 1;
        }
    }

    public class IgnoredAspectOnFinalizer
    {
        [OnlyOnEntryAspect]
        ~IgnoredAspectOnFinalizer()
        {
        }
    }
}
