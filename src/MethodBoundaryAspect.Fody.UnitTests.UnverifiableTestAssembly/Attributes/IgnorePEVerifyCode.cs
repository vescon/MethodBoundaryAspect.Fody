using System;

// Ignore PEVerify's complaint about ref return types.
[assembly: MethodBoundaryAspect.Fody.UnitTests.UnverifiableTestAssembly.Attributes.IgnorePEVerifyCode("80131870")]

namespace MethodBoundaryAspect.Fody.UnitTests.UnverifiableTestAssembly.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple =true)]
    public class IgnorePEVerifyCode : Attribute
    {
        public string ErrorCode { get; private set; }

        public IgnorePEVerifyCode(string code) => ErrorCode = code;
    }
}
