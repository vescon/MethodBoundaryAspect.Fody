using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System.Runtime.InteropServices;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class ReadOnlyParameterBase
    {
        public virtual int ReadInVirtual(in int value) => value;
    }

    public class ReadOnlyParameterMethods : ReadOnlyParameterBase
    {
        private readonly int _field = 10;

        public int Field => _field;

        [NativeValueArgumentsAspect]
        public int ReadIn(in int value) => value;

        // the type of the in parameter is "int& modreq(InAttribute)"
        [NativeValueArgumentsAspect]
        public override int ReadInVirtual(in int value) => value;

        // [In] on a ref parameter does not make it read only (an in parameter has the attribute IsReadOnly)
        [NativeValueArgumentsAspect]
        public void Increment([In] ref int value) => value++;

        // passes the address of the readonly field
        public int ReadField() => ReadIn(in _field);
    }
}
