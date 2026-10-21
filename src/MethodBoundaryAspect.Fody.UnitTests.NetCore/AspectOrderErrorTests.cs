#pragma warning disable 0618 // the obsolete aspect roles are the subject of these tests
using FluentAssertions;
using MethodBoundaryAspect.Fody.Ordering;
using System;
using System.Collections.Generic;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// An invalid order of aspect roles is an error of the user, it is reported with a message
    /// (not as an empty InvalidOperationException).
    /// </summary>
    public class AspectOrderErrorTests
    {
        [Fact]
        public void IfRolesCannotBeOrdered_ThenTheExceptionHasAMessage()
        {
            var order = new AspectOrder(null);
            order.AddRole("First", 1); // after First
            order.AddRole("Second", -1); // before Second, which is the next one after First

            Action getIndex = () => order.GetOrderIndex(new Dictionary<string, int> { ["First"] = 0, ["Second"] = 1 });

            getIndex.Should().Throw<InvalidAspectConfigurationException>()
                .WithMessage("*First*Second*");
        }

        [Fact]
        public void IfARoleIsDefinedTwice_ThenTheExceptionHasAMessage()
        {
            var order = new AspectOrder(null);
            order.AddRole("First", 1);

            Action addRole = () => order.AddRole("First", -1);

            addRole.Should().Throw<InvalidAspectConfigurationException>().WithMessage("*First*");
        }
    }
}
