using System.Collections.Generic;
using System.Linq;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.Ordering;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace MethodBoundaryAspect.Fody
{
    public class MethodWeaver
    {
        public int WeaveCounter { get; private set; }

        public void Weave(
            ModuleDefinition module,
            MethodDefinition method,
            IEnumerable<AspectInfo> aspectInfos)
        {
            var usableAspects = aspectInfos
                .Select(x => new
                {
                    Aspect = x,
                    AspectMethods = GetUsedAspectMethods(x.AspectAttribute.AttributeType)
                })
                .Where(x => x.AspectMethods != AspectMethods.None)
                .ToList();
            if (usableAspects.Count == 0)
                return;

            var hasMultipleAspects = usableAspects.Count > 1;

            var clonedMethod = CloneMethod(method);
            method.DeclaringType.Methods.Add(clonedMethod);

            // redirect source call
            ClearMethod(method);

            var ilProcessor = method.Body.GetILProcessor();
            var creator = new InstructionBlockChainCreator(method, module);
            var arguments = creator.CreateMethodArgumentsArray();
            arguments.Append(ilProcessor);

            var executionArgs = creator.CreateMethodExecutionArgsInstance(
                arguments,
                usableAspects.First().Aspect.AspectAttribute.AttributeType);
            executionArgs.Append(ilProcessor);

            // create aspect instance
            var aspectInstances = new Dictionary<AspectInfo, NamedInstructionBlockChain>();
            var tagVariables = new Dictionary<AspectInfo, VariableDefinition>();
            foreach (var aspect in usableAspects)
            {
                var instance = creator.CreateAspectInstance(aspect.Aspect.AspectAttribute);
                instance.Append(ilProcessor);

                if (hasMultipleAspects)
                {
                    var tagVariable = creator.CreateObjectVariable();
                    method.Body.Variables.Add(tagVariable.Variable);
                    tagVariables.Add(aspect.Aspect, tagVariable.Variable);
                }

                aspectInstances.Add(aspect.Aspect, instance);
            }

            //OnEntries
            foreach (var aspect in usableAspects.Where(x => x.AspectMethods.HasFlag(AspectMethods.OnEntry)))
            {
                var aspectInstance = aspectInstances[aspect.Aspect];
                var call = creator.CallAspectOnEntry(aspectInstance, executionArgs);
                call.Append(ilProcessor);

                if (hasMultipleAspects)
                {
                    var tagVariable = tagVariables[aspect.Aspect];
                    var save = creator.SaveMethodExecutionArgsTagToVariable(executionArgs, tagVariable);
                    save.Append(ilProcessor);
                }
            }

            // call original method
            VariableDefinition thisVariable = null;
            if (!method.IsStatic)
            {
                var thisVariableBlock = creator.CreateThisVariable(method.DeclaringType);
                thisVariableBlock.Append(ilProcessor);
                thisVariable = thisVariableBlock.Variable;
            }

            var hasReturnValue = !InstructionBlockCreator.IsVoid(method.ReturnType);
            var returnValue = hasReturnValue
                ? creator.CreateVariable(method.ReturnType)
                : null;
            var callSourceMethod = creator.CallMethodWithLocalParameters(
                method,
                clonedMethod,
                thisVariable,
                returnValue?.Variable);
            callSourceMethod.Append(ilProcessor);
            var instructionCallStart = callSourceMethod.First;
            var instructionCallEnd = callSourceMethod.Last;

            var onExitAspects = usableAspects
                .Where(x => x.AspectMethods.HasFlag(AspectMethods.OnExit))
                .Reverse()
                .ToList();

            Instruction instructionAfterCall = null;
            if (hasReturnValue && onExitAspects.Any())
            {
                var loadReturnValue = creator.LoadValueOnStack(returnValue);

                var setMethodExecutionArgsReturnValue = creator.SetMethodExecutionArgsReturnValue(
                    executionArgs,
                    loadReturnValue);
                setMethodExecutionArgsReturnValue.Append(ilProcessor);

                instructionAfterCall = setMethodExecutionArgsReturnValue.First;
            }

            // OnExit
            foreach (var aspect in onExitAspects)
            {
                if (hasMultipleAspects)
                {
                    var tagVariable = tagVariables[aspect.Aspect];
                    var load = creator.LoadMethodExecutionArgsTagFromVariable(executionArgs,
                        tagVariable);
                    load.Append(ilProcessor);
                }

                var aspectInstance = aspectInstances[aspect.Aspect];
                var call = creator.CallAspectOnExit(aspectInstance, executionArgs);
                call.Append(ilProcessor);
            }

            // return
            if (hasReturnValue)
            {
                var load = creator.LoadValueOnStack(returnValue);
                load.Append(ilProcessor);
            }

            var returnCall = creator.CreateReturn();
            returnCall.Append(ilProcessor);

            // add exception handling
            var onExceptionAspects = usableAspects
                .Where(x => x.AspectMethods.HasFlag(AspectMethods.OnException))
                .Reverse()
                .ToList();

            if (onExceptionAspects.Any())
            {
                var realInstructionAfterCall = instructionAfterCall ?? instructionCallEnd.Next;
                var tryLeaveInstruction = Instruction.Create(OpCodes.Leave, realInstructionAfterCall);
                ilProcessor.InsertAfter(instructionCallEnd, tryLeaveInstruction);

                var exception = creator.SaveThrownException();
                var exceptionHandlerCurrent = exception.InsertAfter(tryLeaveInstruction, ilProcessor);

                var pushException = creator.LoadValueOnStack(exception);
                exceptionHandlerCurrent = pushException.InsertAfter(exceptionHandlerCurrent, ilProcessor);

                var setExceptionFromStack =
                    creator.SetMethodExecutionArgsExceptionFromStack(executionArgs);
                exceptionHandlerCurrent = setExceptionFromStack.InsertAfter(exceptionHandlerCurrent, ilProcessor);

                foreach (var onExceptionAspect in onExceptionAspects)
                {
                    if (hasMultipleAspects)
                    {
                        var tagVariable = tagVariables[onExceptionAspect.Aspect];
                        var load = creator.LoadMethodExecutionArgsTagFromVariable(executionArgs,
                            tagVariable);
                        exceptionHandlerCurrent = load.InsertAfter(exceptionHandlerCurrent, ilProcessor);
                    }

                    var aspectInstance = aspectInstances[onExceptionAspect.Aspect];
                    var callAspectOnException =
                        creator.CallAspectOnException(aspectInstance, executionArgs);
                    callAspectOnException.InsertAfter(exceptionHandlerCurrent, ilProcessor);
                    exceptionHandlerCurrent = callAspectOnException.Last;
                }

                var pushException2 = creator.LoadValueOnStack(exception);
                exceptionHandlerCurrent = pushException2.InsertAfter(exceptionHandlerCurrent, ilProcessor);

                ////var catchLeaveInstruction = Instruction.Create(OpCodes.Leave, realInstructionAfterCall);
                var catchLastInstruction = Instruction.Create(OpCodes.Throw);
                ilProcessor.InsertAfter(exceptionHandlerCurrent, catchLastInstruction);

                method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                {
                    CatchType = creator.GetExceptionTypeReference(),
                    TryStart = instructionCallStart,
                    TryEnd = tryLeaveInstruction.Next,
                    HandlerStart = tryLeaveInstruction.Next,
                    HandlerEnd = catchLastInstruction.Next
                });
            }

            // add DebuggerStepThrough attribute to avoid compiler searching for
            // non existing soure code for weaved il code
            var attributeCtor = creator.GetDebuggerStepThroughAttributeCtorReference();
            method.CustomAttributes.Add(new CustomAttribute(attributeCtor));

            method.Body.InitLocals = true;
            method.Body.Optimize();
            Catel.Fody.CecilExtensions.UpdateDebugInfo(method);

            clonedMethod.Body.InitLocals = true;
            clonedMethod.Body.Optimize();
            Catel.Fody.CecilExtensions.UpdateDebugInfo(clonedMethod);

            WeaveCounter++;
        }

        private static MethodDefinition CloneMethod(MethodDefinition method)
        {
            var targetMethodName = "$_executor_" + method.Name;
            var isStaticMethod = method.IsStatic;
            var methodAttributes = MethodAttributes.Private;
            if (isStaticMethod)
                methodAttributes |= MethodAttributes.Static;

            var clonedMethod = new MethodDefinition(targetMethodName, methodAttributes, method.ReturnType)
            {
                AggressiveInlining = true, // try to get rid of additional stack frame
                HasThis = method.HasThis,
                ExplicitThis = method.ExplicitThis,
                CallingConvention = method.CallingConvention
            };

            foreach (var parameter in method.Parameters)
                clonedMethod.Parameters.Add(parameter);

            foreach (var variable in method.Body.Variables)
                clonedMethod.Body.Variables.Add(variable);

            foreach (var variable in method.Body.ExceptionHandlers)
                clonedMethod.Body.ExceptionHandlers.Add(variable);

            var targetProcessor = clonedMethod.Body.GetILProcessor();
            foreach (var instruction in method.Body.Instructions)
                targetProcessor.Append(instruction);

            if (method.HasGenericParameters)
            {
                foreach (var parameter in method.GenericParameters)
                    clonedMethod.GenericParameters.Add(new GenericParameter(parameter.Name, clonedMethod));
            }

            if (method.DebugInformation.HasSequencePoints)
            {
                foreach (var sequencePoint in method.DebugInformation.SequencePoints)
                    clonedMethod.DebugInformation.SequencePoints.Add(sequencePoint);
            }

            clonedMethod.DebugInformation.Scope = new ScopeDebugInformation(method.Body.Instructions.First(), method.Body.Instructions.Last());
            return clonedMethod;
        }

        private static void ClearMethod(MethodDefinition method)
        {
            var body = method.Body;
            body.Variables.Clear();
            body.Instructions.Clear();
            body.ExceptionHandlers.Clear();
        }

        private static AspectMethods GetUsedAspectMethods(TypeReference aspectTypeDefinition)
        {
            var overloadedMethods = new Dictionary<string, MethodDefinition>();

            var currentType = aspectTypeDefinition;
            do
            {
                var typeDefinition = currentType.Resolve();
                var methods = typeDefinition.Methods
                    .Where(x => x.IsVirtual)
                    .ToList();
                foreach (var method in methods)
                {
                    if (overloadedMethods.ContainsKey(method.Name))
                        continue;

                    overloadedMethods.Add(method.Name, method);
                }

                currentType = typeDefinition.BaseType;
            } while (currentType.FullName != typeof(OnMethodBoundaryAspect).FullName);

            var aspectMethods = AspectMethods.None;
            if (overloadedMethods.ContainsKey("OnEntry"))
                aspectMethods |= AspectMethods.OnEntry;
            if (overloadedMethods.ContainsKey("OnExit"))
                aspectMethods |= AspectMethods.OnExit;
            if (overloadedMethods.ContainsKey("OnException"))
                aspectMethods |= AspectMethods.OnException;
            return aspectMethods;
        }
    }
}
