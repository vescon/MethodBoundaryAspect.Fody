using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An aspect applied to an event is woven into its accessors, like an aspect applied to a property.
    /// It was silently ignored, only aspects applied to the class hit the accessors.
    /// </summary>
    [Collection(nameof(NameRecordingAspect))]
    public class EventTests
    {
        public EventTests()
        {
            NameRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void AspectOnAFieldLikeEventShouldBeCalledForSubscribingAndUnsubscribing()
        {
            var target = new EventMethods();
            var called = 0;
            EventHandler handler = (s, e) => called++;

            target.FieldLike += handler;
            target.Raise();
            target.FieldLike -= handler;
            target.Raise();

            called.Should().Be(1, "the event still works");
            NameRecordingAspect.Calls.Should().Equal("add_FieldLike", "remove_FieldLike");
        }

        [Fact]
        public void AspectOnAnEventWithExplicitAccessorsShouldBeCalledForSubscribingAndUnsubscribing()
        {
            var target = new EventMethods();
            var called = 0;
            EventHandler handler = (s, e) => called++;

            target.Explicit += handler;
            target.Raise();
            target.Explicit -= handler;
            target.Raise();

            called.Should().Be(1);
            NameRecordingAspect.Calls.Should().Equal("add_Explicit", "remove_Explicit");
        }

        [Fact]
        public void EventWithoutAspectShouldNotBeWeaved()
        {
            new EventMethods().SubscribeWithoutAspect((s, e) => { });

            NameRecordingAspect.Calls.Should().BeEmpty();
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
