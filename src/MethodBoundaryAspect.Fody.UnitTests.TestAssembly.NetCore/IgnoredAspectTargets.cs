using System;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    // the aspect has no AttributeUsage, the compiler accepts it everywhere
    public class IgnoredAspectTargets
    {
        [OnlyOnEntryAspect]
        private int _field = 1;

        [OnlyOnEntryAspect]
        public IgnoredAspectTargets()
        {
        }

        [OnlyOnEntryAspect]
        public event Action Changed;

        [return: OnlyOnEntryAspect]
        public int Method([OnlyOnEntryAspect] int parameter) => parameter + _field;

        [OnlyOnEntryAspect]
        public int Property { get; set; }

        public void Raise() => Changed?.Invoke();
    }
}
