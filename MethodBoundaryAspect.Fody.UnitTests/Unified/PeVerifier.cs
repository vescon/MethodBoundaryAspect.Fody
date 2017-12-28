using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified
{
    public static class PeVerifier
    {
        public static void Verify(string assemblyPath)
        {
            var peVerifyPath = GetPeVerifyPath();

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

        private static string GetPeVerifyPath()
        {
            var possiblePeVerifyPaths = new[]
            {
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\peverify.exe",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\peverify.exe"
            };

            var peVerifyPath = possiblePeVerifyPaths.FirstOrDefault(File.Exists);

            if (peVerifyPath == null)
                throw new InvalidOperationException("PeVerify.exe could not be found. Please install it with the Windows SDK.");

            return peVerifyPath;
        }
    }
}