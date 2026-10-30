using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    /// <summary>
    /// The IL of an explicit interface implementation is private and its name includes the interface.
    /// </summary>
    public class ExplicitInterfaceMethods : IDisposable, IComparable, IFormattable
    {
        [NameRecordingAspect(MethodNameFilter = "^Dispose$")]
        void IDisposable.Dispose()
        {
        }

        [NameRecordingAspect(AttributeTargetMemberAttributes = MulticastAttributes.Public)]
        int IComparable.CompareTo(object obj) => 0;

        [NameRecordingAspect]
        string IFormattable.ToString(string format, IFormatProvider formatProvider) => "";
    }
}
