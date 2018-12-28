using System;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared.Attributes
{
    public class IgnorePEVerifyCode : Attribute
    {
        public string ErrorCode { get; private set; }

        public IgnorePEVerifyCode(string code) => ErrorCode = code;
    }
}
