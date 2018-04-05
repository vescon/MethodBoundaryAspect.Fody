using System;
using System.Diagnostics;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class IlSpy
    {
        private const bool ShouldRunIlSpyOnPeVerifyError = false;

        public static void Run(string methodName, string assemblyPath)
        {
            RunInternal(assemblyPath, "/navigateTo:M:" + methodName);
        }

        public static void OpenType(string typeName, string assemblyPath)
        {
            RunInternal(assemblyPath, "/navigateTo:T:" + typeName);
        }

        private static void RunInternal(string assemblyPath, string navigateTo)
        {
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