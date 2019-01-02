using System;

namespace MethodBoundaryAspect.Fody.UnitTests.Shared.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class IgnorePEVerifyCode : Attribute
    {
        public string ErrorCode { get; private set; }

        public IgnorePEVerifyCode(string code) => ErrorCode = code;
    }
}
