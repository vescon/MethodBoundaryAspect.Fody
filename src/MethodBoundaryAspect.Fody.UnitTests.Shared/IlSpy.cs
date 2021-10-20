using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MethodBoundaryAspect.Fody.UnitTests.Shared
{
    public static class IlSpy
    {
        public const bool AlwaysRunIlSpy = false;
        private const bool ShouldRunIlSpyOnPeVerifyError = false;

        public static void ShowMethod(string methodName, string assemblyPath)
        {
            var navigateTo = $"/navigateTo:M:\"{methodName}\"";
            RunInternal(assemblyPath, navigateTo);
        }

        public static void ShowType(string typeName, string assemblyPath)
        {
            var navigateTo = $"/navigateTo:T:\"{typeName}\"";
            RunInternal(assemblyPath, navigateTo);
        }

#pragma warning disable CS0162 // Unreachable code detected
        private static void RunInternal(string assemblyPath, string navigateTo)
        {
            if(!AlwaysRunIlSpy)
                if (!ShouldRunIlSpyOnPeVerifyError)
                    return;

            var args = new List<string>
            {
                assemblyPath,
                "/separate",
                "/clearList",
                "/language:IL",
                navigateTo
            };

            var arg = string.Join(" ", args);

            var currentDirectory = Environment.CurrentDirectory + @"\..\..\..\..\..\Tools\ILSpy";
            var psi = new ProcessStartInfo
            {
                WorkingDirectory = currentDirectory ,
                FileName = currentDirectory + @"\ILSpy.exe",
                Arguments = arg
            };

            Process.Start(psi);
        }
#pragma warning restore CS0162 // Unreachable code detected
    }
}