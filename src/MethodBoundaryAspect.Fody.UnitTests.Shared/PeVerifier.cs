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
            // <ProgramFiles (x86)>/Microsoft SDKs/Windows/<sdk>/bin/NETFX <version> Tools/peverify.exe, the newest one
            var sdksPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft SDKs", "Windows");
            var peVerifyPath = !Directory.Exists(sdksPath)
                ? null
                : Directory.EnumerateDirectories(sdksPath)
                    .Select(sdk => Path.Combine(sdk, "bin"))
                    .Where(Directory.Exists)
                    .SelectMany(bin => Directory.EnumerateDirectories(bin, "NETFX * Tools"))
                    .Select(tools => new { Path = Path.Combine(tools, "peverify.exe"), Version = ParseNetFxVersion(tools) })
                    .Where(x => File.Exists(x.Path))
                    .OrderByDescending(x => x.Version)
                    .Select(x => x.Path)
                    .FirstOrDefault();
            if (peVerifyPath == null)
                throw new InvalidOperationException("PeVerify.exe could not be found. Please install it with the Windows SDK.");

            return peVerifyPath;
        }

        // "NETFX 4.8.1 Tools" -> 4.8.1
        private static Version ParseNetFxVersion(string toolsFolder)
        {
            var name = Path.GetFileName(toolsFolder).Split(' ');
            return name.Length > 1 && Version.TryParse(name[1], out var version) ? version : new Version(0, 0);
        }
    }
}