using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;
using System;
using System.Linq;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// The weaver compares the FlowBehavior in the woven code with numbers, which have to be the values of the enum.
    /// </summary>
    public class FlowBehaviorValuesTests
    {
        [Fact]
        public void TheValuesShouldBeTheOnesOfTheRuntimeEnum()
        {
            // the runtime library is no direct reference of this project, get it via the base class of an aspect
            var flowBehavior = typeof(RoleOnlyAspect).BaseType.Assembly
                .GetType("MethodBoundaryAspect.Fody.Attributes.FlowBehavior", throwOnError: true);

            Enum.GetNames(flowBehavior).Should().BeEquivalentTo(
                typeof(FlowBehaviorValues).GetFields().Select(f => f.Name));
            foreach (var field in typeof(FlowBehaviorValues).GetFields())
                ((int)Enum.Parse(flowBehavior, field.Name)).Should().Be((int)field.GetRawConstantValue(), field.Name);
        }
    }
}
