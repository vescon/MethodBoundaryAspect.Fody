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
}
