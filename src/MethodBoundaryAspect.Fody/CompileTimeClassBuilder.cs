using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// The common part of the weavers which generate a static class with a lazily filled field and a getter for each
    /// value, see <see cref="MethodInfoCompileTimeWeaver"/> and <see cref="AspectInstanceCompileTimeWeaver"/>.
    /// </summary>
    internal static class CompileTimeClassBuilder
    {
        private const string Namespace = "OnMethodBoundaryAspectCompile";

        /// <summary>
        /// Adds an internal static class to the module: only the weaved code of the same assembly uses it, it is no API
        /// of the assembly.
        /// </summary>
        public static TypeDefinition CreateStaticClass(ModuleDefinition module, string name, TypeReference baseType)
        {
            const TypeAttributes classAttributes = TypeAttributes.Class
                                                   | TypeAttributes.NotPublic
                                                   | TypeAttributes.Abstract // abstract + sealed => static
                                                   | TypeAttributes.Sealed;
            var type = new TypeDefinition(Namespace, name, classAttributes) { BaseType = baseType };
            module.Types.Add(type);
            return type;
        }

        /// <summary>
        /// Appends the body of a getter: var value = field; if (value == null) field = value = [value pushed by
        /// <paramref name="pushNewValue"/>]; return value;
        /// Concurrent first calls may both create the value, which has to be harmless.
        /// </summary>
        public static void AppendLazyGetterBody(ILProcessor processor, FieldDefinition field, Action pushNewValue)
        {
            var ret = Instruction.Create(OpCodes.Ret);
            processor.Append(Instruction.Create(OpCodes.Ldsfld, field));
            processor.Append(Instruction.Create(OpCodes.Dup));
            processor.Append(Instruction.Create(OpCodes.Brtrue, ret));
            processor.Append(Instruction.Create(OpCodes.Pop));
            pushNewValue();
            processor.Append(Instruction.Create(OpCodes.Dup));
            processor.Append(Instruction.Create(OpCodes.Stsfld, field));
            processor.Append(ret);
        }
    }
}
