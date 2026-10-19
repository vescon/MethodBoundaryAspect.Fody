using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public interface IInParameterService
    {
        int Twice(in int value);
    }

    public class InParameterBase
    {
        public virtual int Twice(in int value) => value;

        public virtual ref readonly int GetValueRef() => throw new System.NotSupportedException();
    }

    /// <summary>
    /// The in parameters and the ref readonly return value of virtual methods have the type "int&amp; modreq(InAttribute)".
    /// </summary>
    [ModifierTypesRecordingAspect]
    public class InParameterMethods : InParameterBase, IInParameterService
    {
        private readonly int _value = 7;

        public override int Twice(in int value) => value * 2;

        int IInParameterService.Twice(in int value) => value * 3;

        public override ref readonly int GetValueRef() => ref _value;
    }

    /// <summary>
    /// The return type of an init accessor is "void modreq(IsExternalInit)".
    /// </summary>
    [ModifierTypesRecordingAspect]
    public class InitAccessorMethods
    {
        public int Value { get; init; }
    }

    [ModifierTypesRecordingAspect]
    public record RecordWithAspect(string Name, int Age);
}
