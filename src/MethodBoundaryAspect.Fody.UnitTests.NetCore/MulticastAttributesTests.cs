using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;
using System;
using System.Linq;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// The weaver reads the numeric value of AttributeTargetMemberAttributes from the aspect attribute,
    /// its copy of the enum has to be the one of the runtime library.
    /// </summary>
    public class MulticastAttributesTests
    {
        [Fact]
        public void TheValuesShouldBeTheOnesOfTheRuntimeEnum()
        {
            // the runtime library is no direct reference of this project, get it via the base class of an aspect
            var runtimeEnum = typeof(RoleOnlyAspect).BaseType.Assembly
                .GetType("MethodBoundaryAspect.Fody.Attributes.MulticastAttributes", throwOnError: true);

            Enum.GetNames(typeof(MulticastAttributes)).Should().BeEquivalentTo(Enum.GetNames(runtimeEnum));
            foreach (var name in Enum.GetNames(runtimeEnum))
            {
                ((int)Enum.Parse(typeof(MulticastAttributes), name)).Should().Be((int)Enum.Parse(runtimeEnum, name), name);
            }
        }
    }
}
