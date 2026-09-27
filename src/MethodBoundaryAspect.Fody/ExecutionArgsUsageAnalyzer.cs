using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// Determines which MethodExecutionArgs properties an aspect uses by analyzing the IL of its
    /// OnEntry, OnExit and OnException methods.
    /// The analysis is conservative: if the MethodExecutionArgs instance is used in any other way than calling
    /// one of its property accessors (e.g. it is passed to a logger, stored in a field or captured by a lambda),
    /// the aspect is assumed to use all properties. Calls to methods of the aspect type hierarchy
    /// (e.g. base.OnEntry(arg) or a static helper) are followed.
    /// </summary>
    public class ExecutionArgsUsageAnalyzer
    {
        private const string ExecutionArgsTypeName = "MethodBoundaryAspect.Fody.Attributes.MethodExecutionArgs";
        private const string ReferenceAssemblyAttributeName = "System.Runtime.CompilerServices.ReferenceAssemblyAttribute";

        private readonly Dictionary<TypeDefinition, ExecutionArgsUsage> _cache = new Dictionary<TypeDefinition, ExecutionArgsUsage>();
        private readonly HashSet<string> _disabledAspects;
        private readonly bool _disabled;

        public ExecutionArgsUsageAnalyzer(bool disabled, IEnumerable<string> disabledAspects)
        {
            _disabled = disabled;
            _disabledAspects = new HashSet<string>(disabledAspects.Select(NormalizeTypeName));
        }

        /// <summary>
        /// The configured aspect names which matched at least one weaved aspect.
        /// </summary>
        public HashSet<string> MatchedDisabledAspects { get; } = new HashSet<string>();

        public ExecutionArgsUsage GetUsage(TypeDefinition aspectType)
        {
            if (aspectType == null)
                return ExecutionArgsUsage.All;

            var aspectTypeName = NormalizeTypeName(aspectType.FullName);
            if (_disabledAspects.Contains(aspectTypeName))
            {
                MatchedDisabledAspects.Add(aspectTypeName);
                return ExecutionArgsUsage.All;
            }

            if (_disabled)
                return ExecutionArgsUsage.All;

            if (!_cache.TryGetValue(aspectType, out var usage))
            {
                usage = Analyze(aspectType);
                _cache.Add(aspectType, usage);
            }

            return usage;
        }

        public static string NormalizeTypeName(string typeName) => typeName.Trim().Replace('+', '/');

        private static ExecutionArgsUsage Analyze(TypeDefinition aspectType)
        {
            var hierarchy = GetHierarchy(aspectType);
            if (hierarchy == null)
                return ExecutionArgsUsage.All;

            var visited = new HashSet<(MethodDefinition, int)>();
            var usage = ExecutionArgsUsage.None;
            foreach (var method in GetAspectMethods(hierarchy))
            {
                usage |= AnalyzeParameter(hierarchy, method, method.Parameters[0], visited);
                if (usage == ExecutionArgsUsage.All)
                    break;
            }

            return usage;
        }

        /// <summary>
        /// The aspect type and its base types up to OnMethodBoundaryAspect, null if a type can't be resolved.
        /// </summary>
        private static List<TypeDefinition> GetHierarchy(TypeDefinition aspectType)
        {
            var hierarchy = new List<TypeDefinition>();
            var currentType = aspectType;
            while (true)
            {
                hierarchy.Add(currentType);
                if (currentType.FullName == AttributeFullNames.OnMethodBoundaryAspect)
                    return hierarchy;

                currentType = currentType.BaseType?.Resolve();
                if (currentType == null)
                    return null;
            }
        }

        /// <summary>
        /// The most derived OnEntry, OnExit and OnException methods, like they are called at runtime.
        /// </summary>
        private static IEnumerable<MethodDefinition> GetAspectMethods(IEnumerable<TypeDefinition> hierarchy)
        {
            var methods = new Dictionary<string, MethodDefinition>();
            foreach (var type in hierarchy)
            {
                foreach (var method in type.Methods.Where(AspectMethodCriteria.MatchesSignature))
                {
                    if (!methods.ContainsKey(method.Name))
                        methods.Add(method.Name, method);
                }
            }

            return new[]
                {
                    AspectMethodCriteria.OnEntryMethodName,
                    AspectMethodCriteria.OnExitMethodName,
                    AspectMethodCriteria.OnExceptionMethodName
                }
                .Where(methods.ContainsKey)
                .Select(name => methods[name]);
        }

        private static ExecutionArgsUsage AnalyzeParameter(
            List<TypeDefinition> hierarchy,
            MethodDefinition method,
            ParameterDefinition parameter,
            HashSet<(MethodDefinition, int)> visited)
        {
            if (!visited.Add((method, parameter.Index)))
                return ExecutionArgsUsage.None;

            // no IL to analyze, e.g. the aspect was resolved from a reference assembly
            if (!method.HasBody || IsReferenceAssembly(method.Module) || IsThrowOnly(method.Body))
                return ExecutionArgsUsage.All;

            var usage = ExecutionArgsUsage.None;
            foreach (var instruction in method.Body.Instructions)
            {
                if (IsParameterStoreOrAddress(instruction, parameter))
                    return ExecutionArgsUsage.All;

                if (!IsParameterLoad(instruction, method, parameter))
                    continue;

                var consumer = FindConsumer(instruction, out var argumentIndex);
                if (consumer != null && IsNullCheck(consumer))
                    continue;

                if (!(consumer?.Operand is MethodReference calledMethod)
                    || (consumer.OpCode.Code != Code.Call && consumer.OpCode.Code != Code.Callvirt))
                    return ExecutionArgsUsage.All;

                if (calledMethod.DeclaringType.FullName == ExecutionArgsTypeName)
                {
                    if (argumentIndex != 0 || !calledMethod.HasThis)
                        return ExecutionArgsUsage.All;

                    var accessorUsage = GetAccessorUsage(calledMethod.Name);
                    if (accessorUsage == null)
                        return ExecutionArgsUsage.All;

                    usage |= accessorUsage.Value;
                    continue;
                }

                // follow calls into the aspect type hierarchy, like base.OnEntry(arg) or a helper method.
                // Virtual calls are only followed if they are not dispatched (base calls), otherwise the called method is unknown.
                var calledDefinition = calledMethod.Resolve();
                if (calledDefinition == null
                    || !hierarchy.Any(t => t.FullName == calledDefinition.DeclaringType.FullName)
                    || (calledDefinition.IsVirtual && consumer.OpCode.Code == Code.Callvirt))
                    return ExecutionArgsUsage.All;

                var parameterIndex = calledDefinition.HasThis ? argumentIndex - 1 : argumentIndex;
                if (parameterIndex < 0)
                    return ExecutionArgsUsage.All;

                usage |= AnalyzeParameter(hierarchy, calledDefinition, calledDefinition.Parameters[parameterIndex], visited);
                if (usage == ExecutionArgsUsage.All)
                    return usage;
            }

            return usage;
        }

        private static ExecutionArgsUsage? GetAccessorUsage(string methodName)
        {
            switch (methodName)
            {
                case "get_Arguments":
                    return ExecutionArgsUsage.Arguments;
                case "get_ReturnValue":
                    return ExecutionArgsUsage.ReadReturnValue;
                case "set_ReturnValue":
                    return ExecutionArgsUsage.WriteReturnValue;
                case "get_Method":
                    return ExecutionArgsUsage.Method;
                case "get_Instance":
                    return ExecutionArgsUsage.Instance;
                case "set_Arguments":
                case "set_Method":
                case "set_Instance":
                case "get_Exception":
                case "set_Exception":
                case "get_FlowBehavior":
                case "set_FlowBehavior":
                case "get_MethodExecutionTag":
                case "set_MethodExecutionTag":
                    return ExecutionArgsUsage.None;
                default:
                    return null; // unknown method, e.g. ToString()
            }
        }

        /// <summary>
        /// Finds the instruction which pops the value pushed by <paramref name="load"/> from the stack.
        /// Returns null if the control flow is left before or the stack behavior is unknown.
        /// </summary>
        private static Instruction FindConsumer(Instruction load, out int argumentIndex)
        {
            argumentIndex = -1;

            // number of stack entries from the loaded value (inclusive) to the top of the stack
            var depth = 1;
            for (var instruction = load.Next; instruction != null; instruction = instruction.Next)
            {
                switch (instruction.OpCode.FlowControl)
                {
                    case FlowControl.Branch:
                    case FlowControl.Return:
                    case FlowControl.Throw:
                    case FlowControl.Break:
                        return null;
                }

                if (!TryGetStackBehavior(instruction, out var pops, out var pushes))
                    return null;

                if (pops >= depth)
                {
                    // index within the popped values, 0 is the deepest one (e.g. "this" for an instance call)
                    argumentIndex = pops - depth;
                    return instruction;
                }

                // the value is still on the stack when the control flow continues somewhere else
                if (instruction.OpCode.FlowControl == FlowControl.Cond_Branch)
                    return null;

                depth += pushes - pops;
            }

            return null;
        }

        private static bool TryGetStackBehavior(Instruction instruction, out int pops, out int pushes)
        {
            pops = 0;
            pushes = 0;

            var opCode = instruction.OpCode;
            var method = instruction.Operand as MethodReference;
            if (opCode.StackBehaviourPop == StackBehaviour.Varpop || opCode.StackBehaviourPush == StackBehaviour.Varpush)
            {
                if (method == null || (opCode.Code != Code.Call && opCode.Code != Code.Callvirt && opCode.Code != Code.Newobj))
                    return false;
            }

            switch (opCode.StackBehaviourPop)
            {
                case StackBehaviour.Pop0:
                    pops = 0;
                    break;
                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                    pops = 1;
                    break;
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    pops = 2;
                    break;
                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    pops = 3;
                    break;
                case StackBehaviour.Varpop:
                    pops = method.Parameters.Count;
                    if (opCode.Code != Code.Newobj && method.HasThis && !method.ExplicitThis)
                        pops++;
                    break;
                default:
                    return false;
            }

            switch (opCode.StackBehaviourPush)
            {
                case StackBehaviour.Push0:
                    pushes = 0;
                    break;
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                    pushes = 1;
                    break;
                case StackBehaviour.Push1_push1:
                    pushes = 2;
                    break;
                case StackBehaviour.Varpush:
                    pushes = opCode.Code == Code.Newobj || method.ReturnType.MetadataType != MetadataType.Void ? 1 : 0;
                    break;
                default:
                    return false;
            }

            return true;
        }

        // e.g. arg?.Method
        private static bool IsNullCheck(Instruction instruction)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Brtrue:
                case Code.Brtrue_S:
                case Code.Brfalse:
                case Code.Brfalse_S:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsParameterLoad(Instruction instruction, MethodDefinition method, ParameterDefinition parameter)
        {
            var sequence = method.HasThis ? parameter.Index + 1 : parameter.Index;
            switch (instruction.OpCode.Code)
            {
                case Code.Ldarg_0:
                    return sequence == 0;
                case Code.Ldarg_1:
                    return sequence == 1;
                case Code.Ldarg_2:
                    return sequence == 2;
                case Code.Ldarg_3:
                    return sequence == 3;
                case Code.Ldarg:
                case Code.Ldarg_S:
                    return instruction.Operand == parameter;
                default:
                    return false;
            }
        }

        private static bool IsParameterStoreOrAddress(Instruction instruction, ParameterDefinition parameter)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Starg:
                case Code.Starg_S:
                case Code.Ldarga:
                case Code.Ldarga_S:
                    return instruction.Operand == parameter;
                default:
                    return false;
            }
        }

        private static bool IsReferenceAssembly(ModuleDefinition module)
        {
            return module.Assembly.CustomAttributes.Any(a => a.AttributeType.FullName == ReferenceAssemblyAttributeName);
        }

        /// <summary>
        /// Method bodies of reference assemblies are "throw null".
        /// </summary>
        private static bool IsThrowOnly(MethodBody body)
        {
            var instructions = body.Instructions.Where(i => i.OpCode.Code != Code.Nop).ToList();
            return instructions.Count == 2
                   && instructions[0].OpCode.Code == Code.Ldnull
                   && instructions[1].OpCode.Code == Code.Throw;
        }
    }
}
