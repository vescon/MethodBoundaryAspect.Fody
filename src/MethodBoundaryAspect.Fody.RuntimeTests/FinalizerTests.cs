using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System.Reflection;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// The aspect of a class is not called by the finalizer, which runs on the finalizer thread: an exception of the
    /// aspect would end the process.
    /// </summary>
    [Collection(nameof(FinalizerCountAttribute))]
    public class FinalizerTests
    {
        [Fact]
        public void TheFinalizerShouldNotBeWoven()
        {
            FinalizerCountAttribute.Entries = 0;
            var target = new FinalizerMethods();

            target.Method();
            typeof(FinalizerMethods).GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(target, null);

            FinalizerCountAttribute.Entries.Should().Be(1, "only Method is woven");
        }
    }
}
