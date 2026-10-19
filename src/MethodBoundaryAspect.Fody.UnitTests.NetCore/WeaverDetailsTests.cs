using System;
using System.Linq;
using System.Xml.Linq;
using Fody;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// Details of the weaver found in a review: statistics, configuration, filters and cloned generic parameters.
    /// The module is woven in memory.
    /// </summary>
    public class WeaverDetailsTests
    {
        [Fact]
        public void IfTheLastMethodOfATypeIsNotWoven_ThenTheTypeIsCountedAsWoven()
        {
            var weaver = CreateWeaver(typeof(LastMethodNotWovenMethods));

            weaver.Execute();

            weaver.TotalWeavedMethods.Should().Be(1);
            weaver.TotalWeavedTypes.Should().Be(1);
        }

        private static ModuleWeaver CreateWeaver(Type type)
        {
            var weaver = new ModuleWeaver
            {
                ModuleDefinition = ModuleDefinition.ReadModule(type.Assembly.Location,
                    new ReaderParameters { AssemblyResolver = ModuleHelper.AssemblyResolver })
            };
            weaver.AddClassFilter(type.FullName);
            return weaver;
        }
    }
}
