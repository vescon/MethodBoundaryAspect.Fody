using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/85
    /// MethodExecutionArgs.Method was resolved by reflection on every call. It is cached in static fields,
    /// now also for methods of generic types and generic methods, which get their open generic definition.
    /// </summary>
    public class Issue85Tests
    {
        private const BindingFlags AllMembers =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public Issue85Tests()
        {
            MethodInfoRecordingAspect.Methods.Clear();
        }

        [Fact]
        public void NonGenericMethodShouldGetMethod()
        {
            new MethodInfoMethods().NonGeneric(1).Should().Be(1);
            new MethodInfoMethods().NonGeneric(2).Should().Be(2);

            ShouldAllBe(Method(typeof(MethodInfoMethods), nameof(MethodInfoMethods.NonGeneric)));
        }

        [Fact]
        public void GenericMethodShouldGetGenericMethodDefinition()
        {
            new MethodInfoMethods().GenericMethod(1).Should().Be(1);
            new MethodInfoMethods().GenericMethod("value").Should().Be("value");

            ShouldAllBe(Method(typeof(MethodInfoMethods), nameof(MethodInfoMethods.GenericMethod)));
        }

        [Fact]
        public async Task AsyncGenericMethodShouldGetGenericMethodDefinition()
        {
            (await new MethodInfoMethods().GenericAsync(1)).Should().Be(1);
            (await new MethodInfoMethods().GenericAsync("value")).Should().Be("value");

            ShouldAllBe(Method(typeof(MethodInfoMethods), nameof(MethodInfoMethods.GenericAsync)));
        }

        [Fact]
        public void MethodOfGenericTypeShouldGetMethodOfGenericTypeDefinition()
        {
            new MethodInfoGeneric<int>().NonGenericMethod(1).Should().Be(1);
            new MethodInfoGeneric<string>().NonGenericMethod("value").Should().Be("value");

            ShouldAllBe(Method(typeof(MethodInfoGeneric<>), nameof(MethodInfoGeneric<int>.NonGenericMethod)));
        }

        [Fact]
        public void GenericMethodOfGenericTypeShouldGetGenericMethodDefinition()
        {
            new MethodInfoGeneric<int>().GenericMethod(1, "value").Should().Be("value");
            new MethodInfoGeneric<string>().GenericMethod("value", 2.5).Should().Be(2.5);

            ShouldAllBe(Method(typeof(MethodInfoGeneric<>), nameof(MethodInfoGeneric<int>.GenericMethod)));
        }

        [Fact]
        public void StaticMethodOfGenericTypeShouldGetMethodOfGenericTypeDefinition()
        {
            MethodInfoGeneric<int>.Static(1).Should().Be(1);
            MethodInfoGeneric<string>.Static("value").Should().Be("value");

            ShouldAllBe(Method(typeof(MethodInfoGeneric<>), nameof(MethodInfoGeneric<int>.Static)));
        }

        [Fact]
        public async Task AsyncMethodOfGenericTypeShouldGetMethodOfGenericTypeDefinition()
        {
            (await new MethodInfoGeneric<int>().Async(1)).Should().Be(1);
            (await new MethodInfoGeneric<string>().Async("value")).Should().Be("value");

            ShouldAllBe(Method(typeof(MethodInfoGeneric<>), nameof(MethodInfoGeneric<int>.Async)));
        }

        [Fact]
        public void MethodOfNestedGenericTypeShouldGetMethodOfGenericTypeDefinition()
        {
            new MethodInfoGeneric<int>.Nested<string>().Method(1, "value").Should().Be("value");
            new MethodInfoGeneric<string>.Nested<int>().Method("value", 2).Should().Be(2);

            ShouldAllBe(Method(typeof(MethodInfoGeneric<>.Nested<>), nameof(MethodInfoGeneric<int>.Nested<int>.Method)));
        }

        [Fact]
        public void MethodOfPrivateGenericTypeShouldGetMethodOfGenericTypeDefinition()
        {
            MethodInfoMethods.CallPrivateGeneric(1).Should().Be(1);
            MethodInfoMethods.CallPrivateGeneric("value").Should().Be("value");

            ShouldAllBe(Method(MethodInfoMethods.PrivateGenericType, "Method"));
        }

        [Fact]
        public void GenericMethodOfConstrainedGenericTypeShouldGetGenericMethodDefinition()
        {
            new MethodInfoConstrained<int>().Method(1, new object()).Should().NotBeNull();
            new MethodInfoConstrained<double>().Method(1.5, new Exception()).Should().NotBeNull();

            ShouldAllBe(Method(typeof(MethodInfoConstrained<>), nameof(MethodInfoConstrained<int>.Method)));
        }

        private static MethodInfo Method(Type type, string name) => type.GetMethod(name, AllMembers);

        // both calls get the same cached instance
        private static void ShouldAllBe(MethodInfo expected)
        {
            expected.Should().NotBeNull();
            MethodInfoRecordingAspect.Methods.Should().HaveCount(2)
                .And.AllSatisfy(m => m.Should().BeSameAs(MethodInfoRecordingAspect.Methods[0]))
                .And.AllSatisfy(m => m.Should().Be(expected));
        }
    }
}
