using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MethodBoundaryAspect.Fody.UnitTests.Shared
{
    public static class PeVerifier
    {
        public static void Verify(string assemblyPath, IEnumerable<string> ignoreErrorCodes = null)
        {
            var peVerifyPath = GetPeVerifyPath();

            string path = $"\"{assemblyPath}\"";
            string joinedErrorCodes = String.Empty;
            if (ignoreErrorCodes != null)
                joinedErrorCodes = String.Join(",", ignoreErrorCodes);
            if (!string.IsNullOrEmpty(joinedErrorCodes))
                path = $"{path} /ignore={joinedErrorCodes}";

            var psi = new ProcessStartInfo
            {
                FileName = peVerifyPath,
                Arguments = path,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(psi))
            {
                // read both streams while waiting: a full pipe blocks the process, and the wait would never end
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit((int)VerifyTimeout.TotalMilliseconds))
                {
                    process.Kill();
                    throw new TimeoutException($"PEVerify did not finish within {VerifyTimeout.TotalSeconds} seconds for '{assemblyPath}'.");
                }

                // the exit code is only reliable after the process (and the reading of its output) has ended
                process.WaitForExit();
                var output = outputTask.Result + errorTask.Result;
                Trace.WriteLine("PEVerify output: " + Environment.NewLine + output);

                if (process.ExitCode != 0)
                    throw new PeVerifyException(process.ExitCode, output);
            }
        }

        private static readonly TimeSpan VerifyTimeout = TimeSpan.FromMinutes(2);

        private static string GetPeVerifyPath()
        {
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var possiblePeVerifyPaths = new[]
            {
                $@"{folderPath}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\peverify.exe",
                $@"{folderPath}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\peverify.exe",
                $@"{folderPath}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.1 Tools\peverify.exe",
                $@"{folderPath}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7 Tools\peverify.exe",
                $@"{folderPath}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\peverify.exe",
                $@"{folderPath}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\peverify.exe",
                $@"{folderPath}\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\peverify.exe"
            };

            var peVerifyPath = possiblePeVerifyPaths.FirstOrDefault(File.Exists);
            if (peVerifyPath == null)
                throw new InvalidOperationException("PeVerify.exe could not be found. Please install it with the Windows SDK.");

            return peVerifyPath;
        }
    }
}