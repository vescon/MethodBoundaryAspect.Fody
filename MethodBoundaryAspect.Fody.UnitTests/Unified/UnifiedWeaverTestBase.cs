using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Mono.Cecil.Cil;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests.Unified
{
    public class UnifiedWeaverTestBase
    {
        protected static readonly List<OpCode> AllStLocOpCodes = new List<OpCode>
                {
                    OpCodes.Stloc_S,
                    OpCodes.Stloc_0,
                    OpCodes.Stloc_1,
                    OpCodes.Stloc_2,
                    OpCodes.Stloc_3,
                };
        protected static readonly List<OpCode> AllLdLocOpCodes = new List<OpCode>
                {
                    OpCodes.Ldloc_S,
                    OpCodes.Ldloc_0,
                    OpCodes.Ldloc_1,
                    OpCodes.Ldloc_2,
                    OpCodes.Ldloc_3
                };

        protected string WeaveDll { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            var sourceDll = Assembly.GetExecutingAssembly().CodeBase.Replace(@"file:///", string.Empty);
            var targetDll = sourceDll.Replace(".DLL", "._Weaved.dll");
            File.Copy(sourceDll, targetDll, true);
            WeaveDll = targetDll;

            var sourcePdb = sourceDll.Replace(@".DLL", ".Pdb");
            var targetPdb = sourcePdb.Replace(".Pdb", "._Weaved.Pdb");
            File.Copy(sourcePdb, targetPdb, true);
        }

        protected void AssertRunPeVerify()
        {
            Action action = () => PeVerifier.Verify(WeaveDll);
            action.ShouldNotThrow();
        }
    }
}