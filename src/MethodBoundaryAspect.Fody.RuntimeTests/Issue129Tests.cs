using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/129
    /// "Common Language Runtime detected an invalid program" for methods hit by a (class level) aspect
    /// which use ref structs (e.g. Span&lt;T&gt;), because their values were boxed into MethodExecutionArgs.
    /// Ref struct values are now passed as null and changes to them by the aspect are ignored.
    /// Using the VerifyAssembly directive _does not_ catch these defects.
    /// </summary>
    public class Issue129Tests
    {
        public Issue129Tests()
        {
            RecordingMethodBoundaryAspect.Calls.Clear();
        }

        [Fact]
        public void SpanParameterShouldBePassedAsNull()
        {
            var target = new RefStructMethods();

            target.SpanParameter("abc".AsSpan(), 3).Should().BeTrue();

            RecordingMethodBoundaryAspect.Calls.Should().Equal(
                "OnEntry SpanParameter [null, 3] instance: RefStructMethods",
                "OnExit SpanParameter returned True");
        }

        [Fact]
        public void SpanReturnValueShouldBePassedAsNull()
        {
            var target = new RefStructMethods();

            target.SpanReturn(new[] { 1, 2, 3 }).ToArray().Should().Equal(2, 3);

            RecordingMethodBoundaryAspect.Calls.Should().Equal(
                "OnEntry SpanReturn [System.Int32[]] instance: RefStructMethods",
                "OnExit SpanReturn returned null");
        }

        [Fact]
        public void RefSpanParameterShouldBePassedAsNull()
        {
            var target = new RefStructMethods();
            var values = new[] { 1, 2, 3 }.AsSpan();

            target.RefSpanParameter(ref values);

            values.ToArray().Should().Equal(2, 3);
            RecordingMethodBoundaryAspect.Calls.Should().Equal(
                "OnEntry RefSpanParameter [null] instance: RefStructMethods",
                "OnExit RefSpanParameter returned null");
        }

        [Fact]
        public void MethodWithoutRefStructShouldBeWeavedAsBefore()
        {
            var target = new RefStructMethods();

            target.NoRefStruct(21).Should().Be(42);

            RecordingMethodBoundaryAspect.Calls.Should().Equal(
                "OnEntry NoRefStruct [21] instance: RefStructMethods",
                "OnExit NoRefStruct returned 42");
        }

        [Fact]
        public void InstanceOfRefStructShouldBePassedAsNull()
        {
            var target = new RefStructWithAspect(21);

            target.InstanceMethod().Should().Be(42);

            RecordingMethodBoundaryAspect.Calls.Should().Equal(
                "OnEntry InstanceMethod [] instance: null",
                "OnExit InstanceMethod returned 42");
        }

        [Fact]
        public void StaticMethodOfRefStructShouldBeWeavedAsBefore()
        {
            RefStructWithAspect.StaticMethod(21).Should().Be(42);

            RecordingMethodBoundaryAspect.Calls.Should().Equal(
                "OnEntry StaticMethod [21] instance: null",
                "OnExit StaticMethod returned 42");
        }

        [Fact]
        public void ChangedSpanArgumentShouldBeIgnored()
        {
            var target = new OverwritingRefStructMethods();

            // factor 2 is changed to 20 by the aspect, the span is kept
            target.ChangeArguments(new[] { 3 }.AsSpan(), 2).Should().Be(60);
        }

        [Fact]
        public void ChangedRefSpanArgumentShouldBeIgnored()
        {
            var target = new OverwritingRefStructMethods();
            var values = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }.AsSpan();

            // start 1 is changed to 10 by the aspect, the span is kept
            target.ChangeRefArgument(ref values, 1);

            values.ToArray().Should().Equal(11, 12);
        }

        [Fact]
        public void OverwrittenSpanReturnValueShouldBeIgnored()
        {
            var target = new OverwritingRefStructMethods();

            target.OverwriteReturnValue(new[] { 1, 2, 3 }).ToArray().Should().Equal(2, 3);
        }

        [Fact]
        public void SkippedMethodWithSpanReturnValueShouldReturnDefault()
        {
            var target = new OverwritingRefStructMethods();

            target.SkipBody(new[] { 1, 2, 3 }).IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void SkippedMethodWithRefSpanShouldKeepSpanAndCopyBackOtherRefArguments()
        {
            var target = new OverwritingRefStructMethods();
            var values = new[] { 1, 2, 3 }.AsSpan();
            var count = 3;

            target.SkipBodyWithRefSpan(ref values, ref count);

            // body is skipped: span is untouched, the ref int is copied back as changed by the aspect (3 * 10)
            values.ToArray().Should().Equal(1, 2, 3);
            count.Should().Be(30);
        }

        [Fact]
        public void SwallowedExceptionWithSpanReturnValueShouldReturnDefault()
        {
            var target = new OverwritingRefStructMethods();

            target.SwallowException(new[] { 1, 2, 3 }).IsEmpty.Should().BeTrue();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AsyncServiceMethodFromIssueShouldNotThrow(bool found)
        {
            var target = new Issue129Service(new Issue129Dal(found));

            Func<Task<Issue129Dto>> action = () => target.GetPlaceDetails("id");

            (await action.Should().NotThrowAsync()).Subject.Id.Should().Be("id");
        }
    }
}
