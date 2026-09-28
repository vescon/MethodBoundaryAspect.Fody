using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    /// <summary>
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/70: an aspect applied to the assembly
    /// was woven into the aspects of the same assembly, so OnEntry called itself until a StackOverflowException.
    /// The aspects of a class are applied to its nested types like the aspects of an assembly to all types,
    /// so the aspects are declared as nested types of the class they are applied to.
    /// </summary>
    public static class Issue70Methods
    {
        /// <summary>
        /// Two aspects which would be woven into each other.
        /// </summary>
        [First]
        [Second]
        public class TwoAspects
        {
            public int Method() => 42;

            public class First : OnMethodBoundaryAspect
            {
                public override void OnEntry(MethodExecutionArgs arg) =>
                    Issue70Log.Record($"{nameof(First)} {arg.Method.Name}");
            }

            public class Second : OnMethodBoundaryAspect
            {
                public override void OnEntry(MethodExecutionArgs arg) =>
                    Issue70Log.Record($"{nameof(Second)} {arg.Method.Name}");
            }
        }

        /// <summary>
        /// The derived aspect would be woven into OnEntry of its base aspect.
        /// </summary>
        [Derived]
        public class DerivedAspect
        {
            public int Method() => 42;

            public abstract class Base : OnMethodBoundaryAspect
            {
                public override void OnEntry(MethodExecutionArgs arg) =>
                    Issue70Log.Record($"{Name} {arg.Method.Name}");

                protected abstract string Name { get; }
            }

            public class Derived : Base
            {
                protected override string Name => nameof(Derived);
            }
        }

        /// <summary>
        /// The aspect would be woven into the helper class it calls.
        /// </summary>
        [WithHelper]
        public class AspectWithHelper
        {
            public int Method() => 42;

            public async Task<int> MethodAsync()
            {
                await Task.Yield();
                return 42;
            }

            public class WithHelper : OnMethodBoundaryAspect
            {
                public override void OnEntry(MethodExecutionArgs arg) =>
                    Helper.Record(arg.Method.Name);

                public class Helper
                {
                    public static void Record(string methodName) =>
                        Issue70Log.Record($"{nameof(WithHelper)} {methodName}");
                }
            }
        }
    }
}
