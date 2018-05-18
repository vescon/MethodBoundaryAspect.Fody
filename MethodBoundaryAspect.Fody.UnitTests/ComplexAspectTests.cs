using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly;
using MethodBoundaryAspect.Fody.UnitTests.TestAssemblyAspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class ComplexAspectTests : MethodBoundaryAspectTestBase
    {
        static readonly Type TestClassType = typeof(ComplexAspectTestClass);

        static string GetExpectedResult(string testMethodName) => TestClassType.GetMethod(testMethodName, BindingFlags.Public | BindingFlags.Instance)
                                                                    .GetCustomAttributes<ComplexAspect>()
                                                                    .FirstOrDefault()
                                                                    .GetResult();

        public static IEnumerable<object[]> Methods => from m in TestClassType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                                       where m.GetCustomAttributes<ComplexAspect>().Any()
                                                       select new object[] { m.Name };

        [Theory]
        [MemberData(nameof(Methods))]
        public void IfAttributeHasComplexCtorParameters_ThenParametersAreGivenCorrectly(string testMethodName)
        {
            // Arrange
            WeaveAssemblyMethodAndLoad(TestClassType, testMethodName);

            // Act
            var result = AssemblyLoader.InvokeMethod(TestClassType.FullName, testMethodName);

            // Assert
            result.Should().Be(GetExpectedResult(testMethodName));
        }
    }
}