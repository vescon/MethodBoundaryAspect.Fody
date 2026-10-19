using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.NetCore.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests.NetCore
{
    /// <summary>
    /// Types with a required modifier (modreq) hide the type behind it, so the weaver didn't recognize them:
    /// - init accessors (also of records) return "void modreq(IsExternalInit)": a local variable of type void was
    ///   woven, InvalidProgramException,
    /// - in parameters and ref readonly return values of virtual methods (also overrides and interface
    ///   implementations) are "int&amp; modreq(InAttribute)": the pointer was stored in MethodExecutionArgs instead of
    ///   the value, AccessViolationException (in parameter) or InvalidProgramException (ref readonly return value).
    /// </summary>
    [Collection(nameof(ModifierTypesRecordingAspect))]
    public class ModifierTypesTests
    {
        public ModifierTypesTests()
        {
            ModifierTypesRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void InParameterOfOverrideShouldBeProvided()
        {
            InParameterBase target = new InParameterMethods();

            target.Twice(21).Should().Be(42);

            ModifierTypesRecordingAspect.Calls.Should().Equal("Twice(21) returned 42");
        }

        [Fact]
        public void InParameterOfInterfaceImplementationShouldBeProvided()
        {
            IInParameterService target = new InParameterMethods();

            target.Twice(21).Should().Be(63);

            ModifierTypesRecordingAspect.Calls.Should().Equal(
                $"{typeof(IInParameterService).FullName}.Twice(21) returned 63");
        }

        [Fact]
        public void RefReadonlyReturnValueOfOverrideShouldBeProvided()
        {
            var target = new InParameterMethods();

            target.GetValueRef().Should().Be(7);

            ModifierTypesRecordingAspect.Calls.Should().Equal("GetValueRef() returned 7");
        }

        [Fact]
        public void InitAccessorShouldBeWoven()
        {
            var target = new InitAccessorMethods { Value = 5 };

            target.Value.Should().Be(5);

            ModifierTypesRecordingAspect.Calls.Should().Equal(
                "set_Value(5) returned null",
                "get_Value() returned 5");
        }

        [Fact]
        public void InitAccessorOfRecordShouldBeWoven()
        {
            var target = new RecordWithAspect("Ann", 30) with { Age = 31 };

            target.Age.Should().Be(31);
            target.Name.Should().Be("Ann");

            ModifierTypesRecordingAspect.Calls.Should().Equal(
                "set_Age(31) returned null",
                "get_Age() returned 31",
                "get_Name() returned Ann");
        }
    }
}
