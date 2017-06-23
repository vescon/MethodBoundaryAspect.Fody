using System;
using System.Diagnostics;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified
{
    public static class PeVerifier
    {
        public static void Verify(string assemblyPath)
        {
            const string peVerifyPath =
                @"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\peverify.exe";
            var psi = new ProcessStartInfo
            {
                FileName = peVerifyPath,
                Arguments = assemblyPath,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var process = Process.Start(psi);
            var output = process.StandardOutput.ReadToEnd();
            var processExitCode = process.ExitCode;
            Trace.WriteLine("PEVerify output: " + Environment.NewLine + output);

            if (processExitCode != 0)
                throw new PeVerifyException(processExitCode, output);
        }
    }
}