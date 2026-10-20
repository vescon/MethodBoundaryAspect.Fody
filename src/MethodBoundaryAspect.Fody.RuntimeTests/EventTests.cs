using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An aspect applied directly to an event is ignored until version 4 (only aspects of the class or assembly
    /// hit the accessors), but [DisableWeaving] on an event is honored like on a property.
    /// </summary>
    [Collection(nameof(NameRecordingAspect))]
    public class EventTests
    {
        public EventTests()
        {
            NameRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void AspectAppliedToAnEventShouldNotBeWovenBeforeVersion4()
        {
            var target = new EventMethods();
            var called = 0;
            EventHandler handler = (s, e) => called++;

            target.FieldLike += handler;
            target.Explicit += handler;
            target.Raise();
            target.FieldLike -= handler;
            target.Explicit -= handler;

            called.Should().Be(2, "the events still work");
            NameRecordingAspect.Calls.Should().BeEmpty();
        }

        [Fact]
        public void AspectOfTheClassShouldHitTheAccessorsOfAnEvent()
        {
            var target = new ClassAspectEventMethods();

            target.Changed += (s, e) => { };

            NameRecordingAspect.Calls.Should().Equal("add_Changed");
        }

        [Fact]
        public void DisableWeavingOnAnEventShouldExcludeItsAccessors()
        {
            var target = new DisableWeavingOnEventMethods();

            target.Disabled += (s, e) => { };
            target.Enabled += (s, e) => { };

            NameRecordingAspect.Calls.Should().Equal("add_Enabled");
        }
    }
}
