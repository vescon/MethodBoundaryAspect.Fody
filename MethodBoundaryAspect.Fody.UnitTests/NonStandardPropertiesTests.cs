using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class NonStandardPropertiesTests : MethodBoundaryAspectTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [Test]
        public void IfPropertyWithDoubleUnderscoreIsWeaved_ThenTheAssemblyShouldBeValid()
        {
            // Arrange
            var testClassType = typeof(NonStandardProperties);

            // Act
            Action call = () => WeaveAssemblyClassAndLoad(testClassType);

            // Assert
            call.ShouldNotThrow();
        }
    }
}