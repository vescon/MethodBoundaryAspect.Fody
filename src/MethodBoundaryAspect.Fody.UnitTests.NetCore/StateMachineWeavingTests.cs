using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// Class aspects are not woven into the compiler generated nested types of the class, e.g. the state machines
    /// of async and iterator methods. Weaving the MoveNext method of an async state machine a second time called
    /// the aspects for each step of the state machine, see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/66
    /// </summary>
    [Collection(RefStructWarningTests.TestAssemblyNetCoreWeaving)]
    public class StateMachineWeavingTests : IDisposable
    {
        private readonly List<string> _weavedAssemblyPaths = new List<string>();

        [Fact]
        public void IfClassAspectIsWeaved_ThenCompilerGeneratedNestedTypesAreNotWeaved()
        {
            var weaver = new ModuleWeaver();
            var type = typeof(StateMachineMethods);
            var nestedTypes = type.GetNestedTypes(BindingFlags.NonPublic);
            nestedTypes.Should().HaveCount(2, "the async and the iterator method have a state machine");
            weaver.AddClassFilter(type.FullName);
            foreach (var nestedType in nestedTypes)
                weaver.AddClassFilter(nestedType.FullName.Replace('+', '/'));

            var weavedAssemblyPath = weaver.WeaveToShadowFile(type.Assembly.Location, ModuleHelper.AssemblyResolver);
            _weavedAssemblyPaths.Add(weavedAssemblyPath);

            weaver.TotalWeavedMethods.Should().Be(2);
            weaver.TotalWeavedTypes.Should().Be(1);
            var weavedType = ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(weavedAssemblyPath)),
                    new ReaderParameters { AssemblyResolver = ModuleHelper.AssemblyResolver })
                .GetType(type.FullName);
            weavedType.NestedTypes.Should().HaveCount(2);
            weavedType.NestedTypes.SelectMany(t => t.Methods).Should().NotContain(m => m.Name.StartsWith("$_executor_"));
        }

        public void Dispose()
        {
            foreach (var path in _weavedAssemblyPaths)
            {
                File.Delete(path);
                File.Delete(Path.ChangeExtension(path, "pdb"));
            }
        }
    }
}
