using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An explicit interface implementation is a private method named like <c>System.IDisposable.Dispose</c>, so
    /// the filters see this: <c>^Dispose$</c> does not match it and <c>Public</c> excludes it.
    /// </summary>
    [Collection(nameof(NameRecordingAspect))]
    public class ExplicitInterfaceFilterTests
    {
        public ExplicitInterfaceFilterTests()
        {
            NameRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void TheNameFilterShouldSeeTheNameWithTheInterface()
        {
            ((IDisposable)new ExplicitInterfaceMethods()).Dispose();

            NameRecordingAspect.Calls.Should().BeEmpty();
        }

        [Fact]
        public void TheVisibilityFilterShouldSeeAPrivateMethod()
        {
            ((IComparable)new ExplicitInterfaceMethods()).CompareTo(null);

            NameRecordingAspect.Calls.Should().BeEmpty();
        }

        [Fact]
        public void WithoutAFilterTheMethodShouldBeWoven()
        {
            ((IFormattable)new ExplicitInterfaceMethods()).ToString(null, null);

            NameRecordingAspect.Calls.Should().Equal("System.IFormattable.ToString");
        }
    }
}
