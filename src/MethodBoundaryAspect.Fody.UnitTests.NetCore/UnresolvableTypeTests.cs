using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// Resolve() returns null for a type which is not in its assembly (or whose assembly is missing).
    /// The helpers for the IL of values must not fail with a NullReferenceException then.
    /// </summary>
    public class UnresolvableTypeTests
    {
        private readonly TypeReference _unresolvable;

        public UnresolvableTypeTests()
        {
            var module = ModuleDefinition.ReadModule(typeof(VarArgMethods).Assembly.Location,
                new ReaderParameters { AssemblyResolver = ModuleHelper.AssemblyResolver });
            _unresolvable = new TypeReference("System", "DoesNotExist", module, module.TypeSystem.CoreLibrary, true);
        }

        [Fact]
        public void TheTypeShouldNotBeResolvable()
        {
            _unresolvable.Resolve().Should().BeNull();
        }

        [Fact]
        public void IsEnumShouldBeFalse()
        {
            _unresolvable.IsEnum(out var underlyingType).Should().BeFalse();
            underlyingType.Should().BeNull();
        }

        [Fact]
        public void AValueTypeShouldBeStoredWithStobj()
        {
            _unresolvable.GetStIndInstruction().OpCode.Should().Be(OpCodes.Stobj);
        }

        [Fact]
        public void AValueTypeShouldBeStoredInAnArrayWithStelemRef()
        {
            Action getCode = () => _unresolvable.GetStElemCode();

            getCode.Should().NotThrow();
        }
    }
}
