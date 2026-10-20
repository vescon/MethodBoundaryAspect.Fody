using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// The original method body is moved into a private executor method, which has to keep the compiler and JIT
    /// settings of the original method.
    /// </summary>
    [Collection(nameof(NameRecordingAspect))]
    public class ExecutorMethodTests
    {
        public ExecutorMethodTests()
        {
            NameRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void ExecutorShouldNotInitializeLocalsIfTheMethodHasSkipLocalsInit()
        {
            new ExecutorMethods().SkipLocalsInitMethod().Should().Be(6);

            GetExecutor(nameof(ExecutorMethods.SkipLocalsInitMethod)).GetMethodBody().InitLocals.Should().BeFalse();
        }

        [Fact]
        public void ExecutorShouldInitializeLocalsByDefault()
        {
            new ExecutorMethods().InitLocalsMethod().Should().Be(6);

            GetExecutor(nameof(ExecutorMethods.InitLocalsMethod)).GetMethodBody().InitLocals.Should().BeTrue();
        }

        [Fact]
        public void ExecutorShouldKeepNoOptimization()
        {
            new ExecutorMethods().NoOptimizationMethod().Should().Be(1);

            GetExecutor(nameof(ExecutorMethods.NoOptimizationMethod)).GetMethodImplementationFlags()
                .Should().HaveFlag(MethodImplAttributes.NoOptimization);
            typeof(ExecutorMethods).GetMethod(nameof(ExecutorMethods.NoOptimizationMethod)).GetMethodImplementationFlags()
                .Should().HaveFlag(MethodImplAttributes.NoOptimization);
        }

        [Fact]
        public void ExecutorShouldNotBeOptimizedWithoutNoOptimization()
        {
            new ExecutorMethods().PlainMethod().Should().Be(1);

            GetExecutor(nameof(ExecutorMethods.PlainMethod)).GetMethodImplementationFlags()
                .Should().NotHaveFlag(MethodImplAttributes.NoOptimization);
        }

        [Fact]
        public void NoInliningShouldStayOnTheOriginalMethodOnly()
        {
            new ExecutorMethods().NoInliningMethod().Should().Be(1);

            typeof(ExecutorMethods).GetMethod(nameof(ExecutorMethods.NoInliningMethod)).GetMethodImplementationFlags()
                .Should().HaveFlag(MethodImplAttributes.NoInlining);
            GetExecutor(nameof(ExecutorMethods.NoInliningMethod)).GetMethodImplementationFlags()
                .Should().NotHaveFlag(MethodImplAttributes.NoInlining);
        }

        private static MethodInfo GetExecutor(string methodName) =>
            typeof(ExecutorMethods).GetMethod("$_executor_" + methodName, BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
