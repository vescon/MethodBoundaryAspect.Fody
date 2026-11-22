using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// What the compilers of C# 12 to 14 emit has to be woven like other code: extension blocks, primary constructors,
    /// the "field" keyword, "params" spans, static interface members, inline arrays and others.
    /// </summary>
    public class NewerLanguageFeaturesTests
    {
        private const BindingFlags AllDeclared =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        public NewerLanguageFeaturesTests()
        {
            FeatureRecordingAttribute.Calls.Clear();
        }

        [Fact]
        public void TheMembersOfAnExtensionBlockShouldBeWoven()
        {
            "abc".LengthPlus(2).Should().Be(5);
            "abc".DoubleLength.Should().Be(6);
            string.Twice("x").Should().Be("xx");

            FeatureRecordingAttribute.Calls.Should().Equal(
                "OnEntry LengthPlus(abc, 2)",
                "OnExit LengthPlus 5",
                "OnEntry get_DoubleLength(abc)",
                "OnExit get_DoubleLength 6",
                "OnEntry Twice(x)",
                "OnExit Twice xx");
        }

        [Fact]
        public void TheAspectOfAnExtensionPropertyShouldOnlyBeWovenIntoThatProperty()
        {
            var list = new List<int> { Capacity2 = 8 };

            list.Capacity2.Should().Be(8);
            "abc".Unwoven.Should().Be(3);
            4.DoubleLength.Should().Be(8);

            FeatureRecordingAttribute.Calls.Should().Equal(
                $"OnEntry set_Capacity2({list}, 8)",
                "OnExit set_Capacity2",
                $"OnEntry get_Capacity2({list})",
                "OnExit get_Capacity2 8");
        }

        [Fact]
        public void TheGroupingTypeOfAnExtensionBlockShouldNotBeWoven()
        {
            var executors = typeof(FeatureExtensions).GetMethods(AllDeclared)
                .Where(x => x.Name.StartsWith("$_executor_"))
                .Select(x => x.Name);
            var nestedExecutors = typeof(FeatureExtensions).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .SelectMany(x => x.GetMethods(AllDeclared))
                .Where(x => x.Name.StartsWith("$_executor_"));

            executors.Should().BeEquivalentTo(
                "$_executor_LengthPlus",
                "$_executor_get_DoubleLength",
                "$_executor_Twice",
                "$_executor_get_Capacity2",
                "$_executor_set_Capacity2");
            nestedExecutors.Should().BeEmpty();
        }

        [Fact]
        public void AClassWithAPrimaryConstructorShouldBeWoven()
        {
            new FeatureCalculator(3).Multiply(4).Should().Be(12);

            FeatureRecordingAttribute.Calls.Should().Equal("OnEntry Multiply(4)", "OnExit Multiply 12");
        }

        [Fact]
        public void APropertyWithTheFieldKeywordShouldBeWoven()
        {
            var target = new FeatureCalculator(1) { NotNegative = -5 };

            target.NotNegative.Should().Be(0);

            FeatureRecordingAttribute.Calls.Should().Equal(
                "OnEntry set_NotNegative(-5)",
                "OnExit set_NotNegative",
                "OnEntry get_NotNegative()",
                "OnExit get_NotNegative 0");
        }

        [Fact]
        public void AParamsSpanShouldBePassedToTheMethodAndAsNullToTheAspect()
        {
            FeatureCalculator.Sum(1, 2, 3).Should().Be(6);

            FeatureRecordingAttribute.Calls.Should().Equal("OnEntry Sum(null)", "OnExit Sum 6");
        }

        [Fact]
        public void ARefReadonlyParameterShouldBeWoven()
        {
            var value = 4;

            FeatureCalculator.ReadOnlyReference(ref value).Should().Be(5);

            FeatureRecordingAttribute.Calls.Should().Equal("OnEntry ReadOnlyReference(4)", "OnExit ReadOnlyReference 5");
        }

        [Fact]
        public void AFileLocalTypeShouldBeWoven()
        {
            FeatureCalculator.CallFileLocalType(1).Should().Be(2);

            FeatureRecordingAttribute.Calls.Should().Equal("OnEntry AddOne(1)", "OnExit AddOne 2");
        }

        [Fact]
        public void ACheckedOperatorShouldBeWoven()
        {
            var sum = new FeatureMoney(1) + new FeatureMoney(2);
            var checkedSum = checked(new FeatureMoney(3) + new FeatureMoney(4));
            Action overflow = () => _ = checked(new FeatureMoney(int.MaxValue) + new FeatureMoney(1));

            sum.Amount.Should().Be(3);
            checkedSum.Amount.Should().Be(7);
            overflow.Should().Throw<OverflowException>();
            FeatureRecordingAttribute.Calls.Take(4).Should().Equal(
                "OnEntry op_Addition(1, 2)",
                "OnExit op_Addition",
                "OnEntry op_CheckedAddition(3, 4)",
                "OnExit op_CheckedAddition");
            FeatureRecordingAttribute.Calls.Last().Should().StartWith("OnException op_CheckedAddition");
        }

#if NETCOREAPP
        [Fact]
        public void StaticInterfaceMembersShouldBeWoven()
        {
            FeatureStatic.CallDefault<FeatureStatic>(1).Should().Be(2);
            FeatureStatic.CallAbstract<FeatureStatic>(1).Should().Be(3);

            FeatureRecordingAttribute.Calls.Should().Equal(
                "OnEntry Default(1)",
                "OnExit Default 2",
                "OnEntry Abstract(1)",
                "OnExit Abstract 3");
        }

        [Fact]
        public async Task AnAsyncMethodWithThePoolingBuilderShouldBeWoven()
        {
            var result = await FeatureRuntimeMethods.Pooled(false);

            result.Should().Be(5);
            FeatureRecordingAttribute.Calls.Should().Equal("OnEntry Pooled(False)", "OnExit Pooled");
        }

        [Fact]
        public async Task AnExceptionOfAnAsyncMethodWithThePoolingBuilderShouldBePassedToTheAspect()
        {
            Func<Task> action = async () => await FeatureRuntimeMethods.Pooled(true);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("failed");
            FeatureRecordingAttribute.Calls.Should().BeEquivalentTo("OnEntry Pooled(True)", "OnExit Pooled", "OnException Pooled failed");
        }

        [Fact]
        public async Task AnExceptionOfAnAsyncMethodWithThePoolingBuilderShouldBeSwallowed()
        {
            var result = await FeatureRuntimeMethods.PooledSwallowed();

            result.Should().Be(-1);
        }

        [Fact]
        public void AnInlineArrayShouldBePassedToTheMethodAndTheAspect()
        {
            var buffer = new FeatureBuffer();
            buffer[0] = 1;
            buffer[2] = 5;

            FeatureRuntimeMethods.SumBuffer(buffer).Should().Be(6);

            FeatureRecordingAttribute.Calls.Should().Equal($"OnEntry SumBuffer({typeof(FeatureBuffer).FullName})", "OnExit SumBuffer 6");
        }

        [Fact]
        public void AMethodForUnmanagedCallersShouldBeWoven()
        {
            FeatureRuntimeMethods.CallNativeCallback(1).Should().Be(2);

            FeatureRecordingAttribute.Calls.Should().Equal("OnEntry NativeCallback(1)", "OnExit NativeCallback 2");
        }
#endif

#if NET9_0_OR_GREATER
        [Fact]
        public void AnArgumentWhichAllowsRefStructsShouldBeNullForTheAspect()
        {
            FeatureAllowsRefStruct.Count(new Span<int>(new int[2]), 1).Should().Be(1);
            FeatureAllowsRefStruct.Count(7, 2).Should().Be(2);

            // also if the type argument is no ref struct: the weaver does not know it at build time
            FeatureRecordingAttribute.Calls.Should().Equal(
                "OnEntry Count(null, 1)",
                "OnExit Count 1",
                "OnEntry Count(null, 2)",
                "OnExit Count 2");
        }
#endif
    }
}
