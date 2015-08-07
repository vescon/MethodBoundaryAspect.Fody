using System;
using System.Diagnostics;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified
{
    public class PeVerifier
    {
        public static int Verify(string assemblyPath)
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
            Trace.WriteLine("PEVerify output: " + Environment.NewLine + output);
            return process.ExitCode;
        }
    }
}