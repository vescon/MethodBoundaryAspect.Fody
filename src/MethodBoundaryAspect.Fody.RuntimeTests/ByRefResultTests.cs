using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// OnExit sees the values of ref and out arguments after the call, also if no aspect changes arguments.
    /// </summary>
    public class ByRefResultTests
    {
        private readonly ByRefResultMethods _target = new ByRefResultMethods();

        public ByRefResultTests()
        {
            lock (RecordFirstArgumentAttribute.InOnEntry)
                RecordFirstArgumentAttribute.InOnEntry.Clear();
            lock (RecordFirstArgumentAttribute.InOnExit)
                RecordFirstArgumentAttribute.InOnExit.Clear();
        }

        [Fact]
        public void OnExitShouldSeeTheOutValue()
        {
            _target.Out(out var value);

            value.Should().Be(42);
            RecordFirstArgumentAttribute.InOnEntry.Should().Equal("0");
            RecordFirstArgumentAttribute.InOnExit.Should().Equal("42");
        }

        [Fact]
        public void OnExitShouldSeeTheRefValue()
        {
            var value = 5;

            _target.Ref(ref value);

            value.Should().Be(6);
            RecordFirstArgumentAttribute.InOnEntry.Should().Equal("5");
            RecordFirstArgumentAttribute.InOnExit.Should().Equal("6");
        }
    }
}
