using System;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.Shared;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    /// <summary>
    /// Fody's assembly resolver returns null for assemblies it cannot find, so resolving an
    /// attribute (or one of its base types) can fail while checking whether it is an aspect,
    /// see https://github.com/vescon/MethodBoundaryAspect.Fody/issues/119
    /// </summary>
    public class UnresolvableAttributeBaseTypeTests
    {
        private const string MissingAssemblyName = "MissingAssembly";

        [Fact]
        public void IfBaseTypeOfAttributeCannotBeResolved_ThenWeavingDoesNotThrow()
        {
            // Arrange
            var module = CreateModuleWithAttributeDerivingFromUnresolvableType(out var targetMethod);
            var weaver = new ModuleWeaver { ModuleDefinition = module };

            // Act
            Action call = () => weaver.Execute();

            // Assert
            call.Should().NotThrow();
            weaver.TotalWeavedMethods.Should().Be(0);
            targetMethod.Body.Instructions.Should().HaveCount(1);
        }

        [Fact]
        public void IfAttributeTypeCannotBeResolved_ThenWeavingDoesNotThrow()
        {
            // Arrange
            var module = CreateModuleWithUnresolvableAttribute(out var targetMethod);
            var weaver = new ModuleWeaver { ModuleDefinition = module };

            // Act
            Action call = () => weaver.Execute();

            // Assert
            call.Should().NotThrow();
            weaver.TotalWeavedMethods.Should().Be(0);
            targetMethod.Body.Instructions.Should().HaveCount(1);
        }

        private static ModuleDefinition CreateModuleWithAttributeDerivingFromUnresolvableType(out MethodDefinition targetMethod)
        {
            var module = CreateModule();

            // class AttributeWithUnresolvableBase : MissingAssembly.MissingBaseAttribute
            var missingBaseType = new TypeReference(MissingAssemblyName, "MissingBaseAttribute", module, CreateMissingAssemblyReference(module));
            var attributeType = new TypeDefinition("Target", "AttributeWithUnresolvableBase", TypeAttributes.Public | TypeAttributes.Class, missingBaseType);
            var attributeCtor = new MethodDefinition(".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);
            attributeCtor.Body.GetILProcessor().Emit(OpCodes.Ret);
            attributeType.Methods.Add(attributeCtor);
            module.Types.Add(attributeType);

            targetMethod = AddTargetMethod(module, attributeCtor);
            return module;
        }

        private static ModuleDefinition CreateModuleWithUnresolvableAttribute(out MethodDefinition targetMethod)
        {
            var module = CreateModule();

            var missingAttributeType = new TypeReference(MissingAssemblyName, "MissingAttribute", module, CreateMissingAssemblyReference(module));
            var missingAttributeCtor = new MethodReference(".ctor", module.TypeSystem.Void, missingAttributeType) { HasThis = true };

            targetMethod = AddTargetMethod(module, missingAttributeCtor);
            return module;
        }

        private static ModuleDefinition CreateModule()
        {
            var assemblyName = new AssemblyNameDefinition("Target", new Version(1, 0));
            var parameters = new ModuleParameters
            {
                Kind = ModuleKind.Dll,
                AssemblyResolver = new MissingAssemblyResolver()
            };
            return AssemblyDefinition.CreateAssembly(assemblyName, "Target", parameters).MainModule;
        }

        private static AssemblyNameReference CreateMissingAssemblyReference(ModuleDefinition module)
        {
            var reference = new AssemblyNameReference(MissingAssemblyName, new Version(1, 0));
            module.AssemblyReferences.Add(reference);
            return reference;
        }

        private static MethodDefinition AddTargetMethod(ModuleDefinition module, MethodReference attributeCtor)
        {
            var targetType = new TypeDefinition("Target", "TargetClass", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
            var targetMethod = new MethodDefinition("TargetMethod", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            targetMethod.Body.GetILProcessor().Emit(OpCodes.Ret);
            targetMethod.CustomAttributes.Add(new CustomAttribute(attributeCtor));
            targetType.Methods.Add(targetMethod);
            module.Types.Add(targetType);
            return targetMethod;
        }

        /// <summary>
        /// Behaves like Fody's assembly resolver: returns null instead of throwing for unknown assemblies.
        /// </summary>
        private class MissingAssemblyResolver : IAssemblyResolver
        {
            private readonly IAssemblyResolver _resolver = ModuleHelper.AssemblyResolver;

            public AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                return name.Name == MissingAssemblyName ? null : _resolver.Resolve(name);
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                return name.Name == MissingAssemblyName ? null : _resolver.Resolve(name, parameters);
            }

            public void Dispose()
            {
                _resolver.Dispose();
            }
        }
    }
}
