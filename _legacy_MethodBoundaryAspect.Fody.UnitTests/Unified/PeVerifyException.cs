using System;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified
{
    public class PeVerifyException : Exception
    {
        public PeVerifyException(int exitCode, string errorText)
            : base($"PE Verify failed with code '{exitCode}' and error: ${errorText}")
        {
            ExitCode = exitCode;
            ErrorText = errorText;
        }

        public int ExitCode { get; }
        public string ErrorText { get; }
    }
}