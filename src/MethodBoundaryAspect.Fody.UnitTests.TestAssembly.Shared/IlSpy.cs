using System;
using System.Diagnostics;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared
{
    public class IlSpy
    {
        public const bool AlwaysRunIlSpy = false;
        private const bool ShouldRunIlSpyOnPeVerifyError = false;

        public static void ShowMethod(string methodName, string assemblyPath)
        {
            RunInternal(assemblyPath, "/navigateTo:M:" + methodName);
        }

        public static void ShowType(string typeName, string assemblyPath)
        {
            RunInternal(assemblyPath, "/navigateTo:T:" + typeName);
        }

        private static void RunInternal(string assemblyPath, string navigateTo)
        {
            if(!AlwaysRunIlSpy)
                if (!ShouldRunIlSpyOnPeVerifyError)
                    return;
            
            var args = new[]
            {
                assemblyPath,
                "/separate",
                "/clearList",
                "/language:IL",
                navigateTo
            };
            var arg = string.Join(" ", args);

            var currentDirectory = Environment.CurrentDirectory+ @"\..\..\..\..\Tools\ILSpy";
            var psi = new ProcessStartInfo
            {
                WorkingDirectory = currentDirectory ,
                FileName = currentDirectory + @"\ILSpy.exe",
                Arguments = arg
            };

            Process.Start(psi);
        }
    }
}