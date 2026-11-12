using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MethodBoundaryAspect.Fody
{
    public class MethodWeaver
    {
        private MethodDefinition _clonedMethod;

        protected readonly ModuleDefinition _module;
        protected readonly MethodDefinition _method;
        protected readonly InstructionBlockChainCreator _creator;
        protected readonly ILProcessor _ilProcessor;
        protected readonly IList<AspectData> _aspects;
        protected readonly MethodInfoCompileTimeWeaver _methodInfoCompileTimeWeaver;
        protected readonly ExecutionArgsUsage _executionArgsUsage;

        protected bool HasMultipleAspects => _aspects.Count > 1;

        // the tag of each aspect is saved and restored, but only if an aspect uses a tag at all
        protected bool HandlesTags => HasMultipleAspects && (_executionArgsUsage & ExecutionArgsUsage.Tag) != 0;

        // the FlowBehavior is only checked if an aspect can change it
        protected bool WritesFlowBehavior => (_executionArgsUsage & ExecutionArgsUsage.WritesFlowBehavior) != 0;
        protected bool AllowChangingInputArguments => _aspects.Any(x => x.Info.AllowChangingInputArguments);

        // a return value not used by any aspect doesn't have to be boxed into MethodExecutionArgs.
        // It is also needed if it's only written: it's read back after OnExit, also if the aspect didn't overwrite it
        protected bool ProvidesReturnValue =>
            (_executionArgsUsage & (ExecutionArgsUsage.ReadReturnValue | ExecutionArgsUsage.WriteReturnValue)) != 0;

        // a return value not written by any aspect doesn't have to be read back from MethodExecutionArgs,
        // a skipped method body (FlowBehavior.Return, Continue) returns the default value
        protected bool ReadsBackReturnValue => (_executionArgsUsage & ExecutionArgsUsage.WriteReturnValue) != 0;

        protected IPersistable ExecutionArgs { get; set; }

        public int WeaveCounter { get; private set; }

        public MethodWeaver(
            ModuleDefinition module,
            MethodDefinition method,
            IList<AspectData> aspects,
            MethodInfoCompileTimeWeaver methodInfoCompileTimeWeaver,
            ExecutionArgsUsage executionArgsUsage = ExecutionArgsUsage.All)
        {
            _module = module;
            _method = method;
            _creator = new InstructionBlockChainCreator(method, module);
            _ilProcessor = _method.Body.GetILProcessor();
            _aspects = aspects;
            _methodInfoCompileTimeWeaver = methodInfoCompileTimeWeaver;
            _executionArgsUsage = executionArgsUsage;
        }

        public void Weave()
        {
            if (_aspects.Count == 0)
                return;

            Setup();

            // the arguments array is only created if an aspect reads it or changed arguments are passed to the method
            NamedInstructionBlockChain arguments = null;
            if (AllowChangingInputArguments || (_executionArgsUsage & ExecutionArgsUsage.Arguments) != 0)
            {
                arguments = _creator.CreateMethodArgumentsArray();
                AddToSetup(arguments);

                if (AllowChangingInputArguments)
                    RememberOriginalArgumentElements(arguments);
            }

            _argumentsArray = arguments;
            WeaveMethodExecutionArgs(arguments);

            SetupAspects();

            var hasReturnValue = !InstructionBlockCreator.IsVoid(_method.ReturnType);
            var returnValue = hasReturnValue
                    ? _creator.CreateVariable(_method.ReturnType.WithoutModifiers())
                    : null;

            WeaveOnEntry(arguments, returnValue);
            
            HandleBody(arguments, returnValue?.Variable, out var instructionCallStart, out var instructionCallEnd);

            WeaveOnExit(hasReturnValue, returnValue);

            HandleReturnValue(hasReturnValue, returnValue);

            if (_aspects.Any(x => (x.AspectMethods & AspectMethods.OnException) != 0))
                WeaveOnException(_aspects, instructionCallStart, instructionCallEnd, returnValue);

            Optimize();

            Finish();

            WeaveCounter++;
        }

        protected virtual void Setup()
        {
            _hasDebugScope = _method.DebugInformation.Scope != null;
            _importOfDebugScope = _method.DebugInformation.Scope?.Import;
            _clonedMethod = CloneMethod(_method);
            _method.DeclaringType.Methods.Add(_clonedMethod);
            ClearMethod(_method);
        }

        private NamedInstructionBlockChain _argumentsArray;

        // the executor takes over the scope of the method, the method gets a new one with the same imports
        private bool _hasDebugScope;
        private ImportDebugInformation _importOfDebugScope;

        private readonly Dictionary<ParameterDefinition, VariableDefinition> _originalArgumentElements =
            new Dictionary<ParameterDefinition, VariableDefinition>();

        private static readonly HashSet<string> ExecutorAttributeTypes = new HashSet<string>
        {
            "System.Diagnostics.DebuggerHiddenAttribute",
            "System.Diagnostics.DebuggerStepThroughAttribute",
            "System.Diagnostics.DebuggerNonUserCodeAttribute",
            "System.Diagnostics.StackTraceHiddenAttribute",
            "System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptionsAttribute",
            "System.Security.SecurityCriticalAttribute",
            "System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute",
            "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute",
            "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute"
        };

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

            // the settings of the original method stay with its body (NoInlining stays on the original method
            // which calls the executor), e.g. [SkipLocalsInit] and [MethodImpl(MethodImplOptions.NoOptimization)]
            clonedMethod.Body.InitLocals = method.Body.InitLocals;
            clonedMethod.ImplAttributes |= method.ImplAttributes
                                           & (MethodImplAttributes.NoOptimization | MethodImplAttributes.AggressiveOptimization);

            // the executor runs the code of the original method, so it needs the attributes which change how that code is
            // debugged, trimmed or run (the original keeps them as well, it is the wrapper then)
            foreach (var attribute in method.CustomAttributes)
            {
                if (!ExecutorAttributeTypes.Contains(attribute.AttributeType.FullName))
                    continue;

                var clonedAttribute = new CustomAttribute(attribute.Constructor);
                foreach (var argument in attribute.ConstructorArguments)
                    clonedAttribute.ConstructorArguments.Add(argument);
                foreach (var property in attribute.Properties)
                    clonedAttribute.Properties.Add(property);
                foreach (var field in attribute.Fields)
                    clonedAttribute.Fields.Add(field);
                clonedMethod.CustomAttributes.Add(clonedAttribute);
            }

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
                //Contravariant:
                //  The generic type parameter is contravariant. A contravariant type parameter can appear as a parameter type in method signatures.  
                //Covariant:
                //  The generic type parameter is covariant. A covariant type parameter can appear as the result type of a method, the type of a read-only field, a declared base type, or an implemented interface. 
                //DefaultConstructorConstraint:
                //  A type can be substituted for the generic type parameter only if it has a parameterless constructor. 
                //None:
                //  There are no special flags. 
                //NotNullableValueTypeConstraint:
                //  A type can be substituted for the generic type parameter only if it is a value type and is not nullable. 
                //ReferenceTypeConstraint:
                //  A type can be substituted for the generic type parameter only if it is a reference type. 
                //SpecialConstraintMask:
                //  Selects the combination of all special constraint flags. This value is the result of using logical OR to combine the following flags: DefaultConstructorConstraint, ReferenceTypeConstraint, and NotNullableValueTypeConstraint. 
                //VarianceMask:
                //  Selects the combination of all variance flags. This value is the result of using logical OR to combine the following flags: Contravariant and Covariant. 
                foreach (var parameter in method.GenericParameters)
                {
                    // all flags, also those without a type constraint, e.g. "allows ref struct"
                    var clonedParameter = new GenericParameter(parameter.Name, clonedMethod)
                    {
                        Attributes = parameter.Attributes
                    };
                    foreach (var parameterConstraint in parameter.Constraints)
                        clonedParameter.Constraints.Add(parameterConstraint);

                    clonedMethod.GenericParameters.Add(clonedParameter);
                }
            }

            if (method.DebugInformation.HasSequencePoints)
            {
                foreach (var sequencePoint in method.DebugInformation.SequencePoints)
                    clonedMethod.DebugInformation.SequencePoints.Add(sequencePoint);
            }

            // the executor takes over the scopes with the variable names, the imports (namespaces of the watch window)
            // and the constants; the scope covers the whole body, also its last instruction
            var scope = method.DebugInformation.Scope;
            if (scope != null)
            {
                clonedMethod.DebugInformation.Scope = scope;
                scope.Start = new InstructionOffset(method.Body.Instructions.First());
                scope.End = new InstructionOffset();
            }

            return clonedMethod;
        }

        private static void ClearMethod(MethodDefinition method)
        {
            var body = method.Body;
            body.Variables.Clear();
            body.Instructions.Clear();
            body.ExceptionHandlers.Clear();

            // the instructions moved to the executor method, which got the sequence points and the scopes
            method.DebugInformation.SequencePoints.Clear();
            method.DebugInformation.Scope = null;
        }

        protected virtual void AddToSetup(InstructionBlockChain chain)
        {
            chain.Append(_ilProcessor);
        }

        protected virtual void WeaveMethodExecutionArgs(NamedInstructionBlockChain arguments)
        {
            var executionArgs = CreateExecutionArgsInstance(arguments);
            AddToSetup(executionArgs);
            ExecutionArgs = executionArgs;
        }

        protected NamedInstructionBlockChain CreateExecutionArgsInstance(NamedInstructionBlockChain arguments)
        {
            return _creator.CreateMethodExecutionArgsInstance(
                arguments,
                _aspects[0].Info.AspectAttribute.AttributeType,
                _method,
                _methodInfoCompileTimeWeaver,
                _executionArgsUsage);
        }

        private void SetupAspects()
        {
            foreach (var aspect in _aspects)
            {
                var instance = aspect.CreateAspectInstance();
                AddToSetup(instance);

                if (HandlesTags)
                {
                    aspect.EnsureTagStorage();
                    var tagStorage = aspect.CreateTagStorage();
                    if (tagStorage != null)
                        AddToSetup(tagStorage);
                }
            }
        }

        private void WeaveOnEntry(NamedInstructionBlockChain arguments, NamedInstructionBlockChain returnValue)
        {
            var allowChangingInputArguments = AllowChangingInputArguments;

            var aspectsWithOnEntry = _aspects
                .Select((asp, index)=> new { aspect = asp, index })
                .Where(x => (x.aspect.AspectMethods & AspectMethods.OnEntry) != 0)
                .ToList();
            foreach (var entry in aspectsWithOnEntry)
            {
                var aspect = entry.aspect;

                if (HandlesTags)
                    AddToSetup(_creator.ClearMethodExecutionArgsTag(ExecutionArgs));

                var call = _creator.CallAspectOnEntry(aspect, ExecutionArgs);
                AddToSetup(call);

                if (HandlesTags)
                    AddToSetup(_creator.SaveMethodExecutionArgsTagToPersistable(ExecutionArgs, aspect.TagPersistable));

                if (!WritesFlowBehavior)
                    continue;

                var nopChain = new InstructionBlockChain();
                nopChain.Add(new InstructionBlock(null, Instruction.Create(OpCodes.Nop)));

                var flowChain = new InstructionBlockChain();
                var onExitChain = new InstructionBlockChain();

                // the aspects which have run OnEntry get their OnExit also if the method with a ref return throws
                // instead of returning, like they do if an exception is swallowed
                for (var i = entry.index-1; i >= 0; --i)
                {
                    var onExitAspect = _aspects[i];
                    if ((onExitAspect.AspectMethods & AspectMethods.OnExit) == 0) 
                        continue;

                    if (HandlesTags)
                        onExitChain.Add(_creator.LoadMethodExecutionArgsTagFromPersistable(ExecutionArgs, onExitAspect.TagPersistable));
                    onExitChain.Add(_creator.CallAspectOnExit(onExitAspect, ExecutionArgs));
                }

                if (_method.ReturnType.WithoutModifiers().IsByReference)
                {
                    onExitChain.Add(CreateThrowNotSupported(
                        "Weaving early return from a method with a byref return type is not supported."));
                }
                else
                {
                    if (allowChangingInputArguments)
                    {
                        // method body is skipped: write ref/out values set by the aspect back to the caller
                        var copyBackBlock = CreateCopyBackByRefArgumentsFromArray(arguments);
                        if (copyBackBlock != null)
                            onExitChain.Add(copyBackBlock);
                    }
                }

                if (returnValue != null)
                {
                    if (ReadsBackReturnValue)
                        onExitChain.Add(_creator.ReadReturnValue(ExecutionArgs, returnValue.Variable));
                    onExitChain.Add(CreateReturnCompletedTaskIfNull(returnValue.Variable));
                    onExitChain.Add(_creator.LoadValueOnStack(returnValue));
                }

                onExitChain.Add(new InstructionBlock("Return", Instruction.Create(OpCodes.Ret)));

                flowChain.Add(_creator.IfFlowBehaviorIsAnyOf(ExecutionArgs, nopChain.First, onExitChain, FlowBehaviorValues.Return));

                flowChain.Add(nopChain);
                AddToSetup(flowChain);
            }
        }
        
        /// <summary>
        /// A Task or Task&lt;T&gt; is never null for the caller: if the method body is skipped or an exception is
        /// swallowed and no aspect set a ReturnValue, the method returns a completed task with the default result.
        /// </summary>
        private InstructionBlockChain CreateReturnCompletedTaskIfNull(VariableDefinition returnValue)
        {
            var chain = new InstructionBlockChain();
            var returnType = _method.ReturnType.WithoutModifiers();

            MethodReference createTask;
            VariableDefinition defaultResult = null;
            if (returnType.FullName == "System.Threading.Tasks.Task")
            {
                var getCompletedTask = returnType.Resolve()?.Methods
                    .FirstOrDefault(m => m.Name == "get_CompletedTask" && m.IsStatic && m.Parameters.Count == 0);
                if (getCompletedTask == null)
                    return chain;

                createTask = _module.ImportReference(getCompletedTask);
            }
            else if (returnType is GenericInstanceType taskOfResult
                     && taskOfResult.ElementType.FullName == "System.Threading.Tasks.Task`1")
            {
                var fromResult = taskOfResult.ElementType.Resolve()?.Module.GetType("System.Threading.Tasks.Task")?.Methods
                    .FirstOrDefault(m => m.Name == "FromResult" && m.IsStatic && m.HasGenericParameters && m.Parameters.Count == 1);
                if (fromResult == null)
                    return chain;

                var resultType = taskOfResult.GenericArguments[0];
                var fromResultOfResult = new GenericInstanceMethod(_module.ImportReference(fromResult));
                fromResultOfResult.GenericArguments.Add(resultType);
                createTask = fromResultOfResult;
                defaultResult = _creator.CreateVariable(resultType).Variable;
            }
            else
                return chain;

            var end = Instruction.Create(OpCodes.Nop);
            var instructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldloc, returnValue),
                Instruction.Create(OpCodes.Brtrue, end)
            };
            if (defaultResult != null)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldloca, defaultResult));
                instructions.Add(Instruction.Create(OpCodes.Initobj, defaultResult.VariableType));
                instructions.Add(Instruction.Create(OpCodes.Ldloc, defaultResult));
            }
            instructions.Add(Instruction.Create(OpCodes.Call, createTask));
            instructions.Add(Instruction.Create(OpCodes.Stloc, returnValue));

            chain.Add(new InstructionBlock("Return a completed task if there is none", instructions));
            chain.Add(new InstructionBlock("End", end));
            return chain;
        }

        // a method with a ref return cannot return a default value (a null reference fails in the caller)
        private InstructionBlock CreateThrowNotSupported(string message, VariableDefinition innerException = null)
        {
            var parameterCount = innerException == null ? 1 : 2;
            var constructor = _module.ImportReference(
                _creator.GetExceptionTypeReference<NotSupportedException>()
                    .Resolve().Methods
                    .First(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == parameterCount
                                && m.Parameters[0].ParameterType.FullName == "System.String"
                                && (parameterCount == 1 || m.Parameters[1].ParameterType.FullName == "System.Exception")));

            var instructions = new List<Instruction> { Instruction.Create(OpCodes.Ldstr, message) };
            if (innerException != null)
                instructions.Add(Instruction.Create(OpCodes.Ldloc, innerException));
            instructions.Add(Instruction.Create(OpCodes.Newobj, constructor));
            instructions.Add(Instruction.Create(OpCodes.Throw));
            return new InstructionBlock("Throw NotSupported", instructions);
        }

        protected virtual void HandleBody(
            NamedInstructionBlockChain arguments,
            VariableDefinition returnValue,
            out Instruction instructionCallStart,
            out Instruction instructionCallEnd)
        {
            VariableDefinition thisVariable = null;
            if (!_method.IsStatic)
            {
                var thisVariableBlock = _creator.CreateThisVariable(_method.DeclaringType);
                thisVariableBlock.Append(_ilProcessor);
                thisVariable = thisVariableBlock.Variable;
            }

            InstructionBlockChain callSourceMethod;

            ILoadable[] args = null;
            var allowChangingInputArguments = AllowChangingInputArguments;
            if (allowChangingInputArguments)
            {
                // get arguments from ExecutionArgs because they could have been changed in aspect code
                // (except ref structs and pointers which cannot be passed via ExecutionArgs)
                var instanceOffset = _method.IsStatic ? 0 : 1;
                var byRefPassedViaArguments = GetByRefParametersPassedViaArguments().ToList();
                // the elements are cast before the try block: an invalid value set by an aspect is not an exception of the method
                args = new ILoadable[_method.Parameters.Count];
                for (var i = 0; i < args.Length; i++)
                {
                    var parameter = _method.Parameters[i];
                    if (parameter.ParameterType.CannotBeBoxed())
                    {
                        args[i] = new ArgumentLoadable(i + instanceOffset, parameter, _method.Body.GetILProcessor());
                        continue;
                    }

                    _originalArgumentElements.TryGetValue(parameter, out var originalElement);
                    var element = new ArrayElementLoadable(arguments.Variable, i, parameter, _method.Body.GetILProcessor(), _creator,
                        passOriginalReference: byRefPassedViaArguments.Contains(parameter), originalElement: originalElement);
                    var prepared = element.Prepare();
                    var prepareChain = new InstructionBlockChain();
                    prepareChain.Add(prepared.Prepare);
                    prepareChain.Append(_ilProcessor);
                    args[i] = prepared.Loadable;
                }

                callSourceMethod = _creator.CallMethodWithReturn(
                    _clonedMethod,
                    thisVariable == null ? null : new VariablePersistable(thisVariable),
                    returnValue == null ? null : new VariablePersistable(returnValue),
                    args);
            }
            else
            {
                callSourceMethod = _creator.CallMethodWithLocalParameters(
                    _method,
                    _clonedMethod,
                    thisVariable == null ? null : new VariablePersistable(thisVariable),
                    returnValue == null ? null : new VariablePersistable(returnValue));
            }
            
            if (arguments != null)
            {
                // the called method has written ref/out values into the variables of the caller (it got their addresses),
                // update ExecutionArgs.Arguments with them so OnExit sees them
                var updateArgumentsBlock = CreateUpdateArgumentsArrayFromByRefParameters(arguments);
                if (updateArgumentsBlock != null)
                    callSourceMethod.Add(updateArgumentsBlock);
            }

            callSourceMethod.Append(_ilProcessor);
            instructionCallStart = callSourceMethod.First;
            instructionCallEnd = callSourceMethod.Last;
        }

        // the boxes which were created for the arguments: an element which is still the same box was not changed by an aspect
        private void RememberOriginalArgumentElements(NamedInstructionBlockChain arguments)
        {
            var chain = new InstructionBlockChain();
            foreach (var parameter in GetByRefParametersPassedViaArguments())
            {
                var original = _creator.CreateVariable(_method.Module.TypeSystem.Object).Variable;
                _originalArgumentElements.Add(parameter, original);
                chain.Add(new InstructionBlock("Remember the original argument " + parameter.Index, new List<Instruction>
                {
                    _ilProcessor.Create(OpCodes.Ldloc, arguments.Variable),
                    _ilProcessor.Create(OpCodes.Ldc_I4, parameter.Index),
                    _ilProcessor.Create(OpCodes.Ldelem_Ref),
                    _ilProcessor.Create(OpCodes.Stloc, original)
                }));
            }

            if (chain.InstructionBlocks.Count > 0)
                AddToSetup(chain);
        }

        private InstructionBlock CreateUpdateArgumentsArrayFromByRefParameters(NamedInstructionBlockChain arguments)
        {
            var instructions = new List<Instruction>();
            foreach (var parameter in GetByRefParametersPassedViaArguments())
            {
                instructions.Add(_ilProcessor.Create(OpCodes.Ldloc, arguments.Variable));
                instructions.Add(_ilProcessor.Create(OpCodes.Ldc_I4, parameter.Index));

                var elementType = ((ByReferenceType)parameter.ParameterType.WithoutModifiers()).ElementType;
                instructions.Add(_ilProcessor.Create(OpCodes.Ldarg, parameter));
                instructions.Add(_ilProcessor.Create(OpCodes.Ldobj, elementType));
                // Resolve() returns the element type of an array
                if (elementType.IsValueTypeOrGenericParameter() || (!elementType.IsArray && elementType.Resolve()?.IsValueType == true))
                    instructions.Add(_ilProcessor.Create(OpCodes.Box, elementType));

                instructions.Add(_ilProcessor.Create(OpCodes.Stelem_Ref));
            }

            return instructions.Any()
                ? new InstructionBlock("Update arguments with ref values", instructions)
                : null;
        }

        private InstructionBlock CreateCopyBackByRefArgumentsFromArray(NamedInstructionBlockChain arguments)
        {
            return CreateCopyBackByRefArguments(parameter =>
            {
                var arg = new ArrayElementLoadable(arguments.Variable, parameter.Index, parameter, _ilProcessor, _creator);
                return arg.Load(false, true).Instructions;
            });
        }

        private InstructionBlock CreateCopyBackByRefArguments(Func<ParameterDefinition, IEnumerable<Instruction>> loadValue)
        {
            var copyBackInstructions = new List<Instruction>();
            foreach (var parameter in GetByRefParametersPassedViaArguments())
            {
                copyBackInstructions.Add(_ilProcessor.Create(OpCodes.Ldarg, parameter));
                copyBackInstructions.AddRange(loadValue(parameter));

                var elementType = ((ByReferenceType)parameter.ParameterType.WithoutModifiers()).ElementType;
                copyBackInstructions.Add(elementType.GetStIndInstruction());
            }

            return copyBackInstructions.Any()
                ? new InstructionBlock("Copy back ref values", copyBackInstructions)
                : null;
        }

        // ref/out parameters except ref structs and pointers, which cannot be boxed into ExecutionArgs.Arguments
        // and are passed directly to the called method instead, and except read only parameters
        private IEnumerable<ParameterDefinition> GetByRefParametersPassedViaArguments()
        {
            return _method.Parameters.Where(x => x.ParameterType.WithoutModifiers().IsByReference
                                                 && !x.ParameterType.CannotBeBoxed()
                                                 && !IsReadOnly(x));
        }

        // in and ref readonly parameters: a changed argument is passed to the method, but not written back to the
        // caller, whose variable (e.g. a readonly field) is read only for the method. The compilers mark them with
        // an attribute, the [In] flag alone is no hint: it is also allowed on a ref parameter.
        private static bool IsReadOnly(ParameterDefinition parameter)
        {
            return parameter.CustomAttributes.Any(a =>
                a.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute"
                || a.AttributeType.FullName == "System.Runtime.CompilerServices.RequiresLocationAttribute");
        }

        private void WeaveOnExit(bool hasReturnValue, NamedInstructionBlockChain returnValue)
        {
            var onExitAspects = _aspects
                            .Where(x => x.AspectMethods.HasFlag(AspectMethods.OnExit))
                            .Reverse()
                            .ToList();

            // ref structs and pointers cannot be boxed, ReturnValue stays null
            if (hasReturnValue && onExitAspects.Any() && ProvidesReturnValue && !_method.ReturnType.CannotBeBoxed())
            {
                var loadReturnValue = _creator.LoadValueOnStack(returnValue);

                var setMethodExecutionArgsReturnValue = _creator.SetMethodExecutionArgsReturnValue(
                    ExecutionArgs,
                    loadReturnValue);
                AddToEnd(setMethodExecutionArgsReturnValue);
            }

            foreach (var aspect in onExitAspects)
            {
                if (HandlesTags)
                    AddToEnd(_creator.LoadMethodExecutionArgsTagFromPersistable(ExecutionArgs, aspect.TagPersistable));
                
                AddToEnd(_creator.CallAspectOnExit(aspect, ExecutionArgs));

                // the tag of an aspect of an async method is read again in OnException, which runs later
                if (HandlesTags && SavesTagAfterOnExit)
                    AddToEnd(_creator.SaveMethodExecutionArgsTagToPersistable(ExecutionArgs, aspect.TagPersistable));
            }

            if (hasReturnValue && onExitAspects.Any() && ReadsBackReturnValue)
                ReadReturnValueAfterOnExit(returnValue.Variable).Append(_ilProcessor);

        }

        /// <summary>
        /// OnException of async methods runs after OnExit, the tag set in OnExit has to be kept for it.
        /// <para>
        /// Known issue: OnExit runs in the start of the method (on the thread of the caller, when the task is returned)
        /// and OnException in MoveNext (another thread, while the method is running). They are not synchronized and
        /// share one MethodExecutionArgs, so with several aspects a failure on another thread while OnExit runs can
        /// swap the MethodExecutionTag (and Exception, ReturnValue) under the aspect which is called. The aspect
        /// methods are not the cause (they are synchronous), the two phases of an async method overlap. Fixing it
        /// needs a lock around every swap and call, with a try/finally on every exit of the start of the method.
        /// </para>
        /// </summary>
        protected virtual bool SavesTagAfterOnExit => false;

        /// <summary>
        /// Takes the ReturnValue an aspect set in OnExit as the value the method returns.
        /// </summary>
        protected virtual InstructionBlockChain ReadReturnValueAfterOnExit(VariableDefinition returnValue)
        {
            return _creator.ReadReturnValue(ExecutionArgs, returnValue);
        }

        private void HandleReturnValue(bool hasReturnValue, NamedInstructionBlockChain returnValue)
        {
            if (hasReturnValue)
                AddToEnd(_creator.LoadValueOnStack(returnValue));
            
            AddToEnd(_creator.CreateReturn());
        }

        protected virtual void WeaveOnException(IList<AspectData> allAspects, Instruction instructionCallStart, Instruction instructionCallEnd, NamedInstructionBlockChain returnValue)
        {
            var realInstructionAfterCall = instructionCallEnd.Next;
            var tryLeaveInstruction = Instruction.Create(OpCodes.Leave, realInstructionAfterCall);
            _ilProcessor.InsertAfter(instructionCallEnd, tryLeaveInstruction);

            var exception = _creator.SaveThrownException();
            var exceptionHandlerCurrent = exception.InsertAfter(tryLeaveInstruction, _ilProcessor);

            var pushException = _creator.LoadValueOnStack(exception);
            exceptionHandlerCurrent = pushException.InsertAfter(exceptionHandlerCurrent, _ilProcessor);

            var setExceptionFromStack =
                _creator.SetMethodExecutionArgsExceptionFromStack(ExecutionArgs);
            exceptionHandlerCurrent = setExceptionFromStack.InsertAfter(exceptionHandlerCurrent, _ilProcessor);

            // the method has written ref/out values into the variables of the caller before it threw: OnException (and
            // OnExit, if the exception is swallowed) see them in Arguments
            if (_argumentsArray != null)
            {
                var updateArguments = CreateUpdateArgumentsArrayFromByRefParameters(_argumentsArray);
                if (updateArguments != null)
                {
                    var updateChain = new InstructionBlockChain();
                    updateChain.Add(updateArguments);
                    exceptionHandlerCurrent = updateChain.InsertAfter(exceptionHandlerCurrent, _ilProcessor);
                }
            }

            if (WritesFlowBehavior)
                exceptionHandlerCurrent = _creator.ResetMethodExecutionArgsFlowBehavior(ExecutionArgs).InsertAfter(exceptionHandlerCurrent, _ilProcessor);
            
            var returnsByRef = _method.ReturnType.WithoutModifiers().IsByReference;
            var returnAfterHandling = new InstructionBlockChain();
            if (returnsByRef)
            {
                returnAfterHandling.Add(CreateThrowNotSupported(
                    "Swallowing an exception of a method with a ref return is not supported, the original exception is the inner exception.",
                    exception.Variable));
            }
            else
            {
                if (returnValue != null)
                    returnAfterHandling.Add(_creator.LoadValueOnStack(returnValue));
                returnAfterHandling.Add(new InstructionBlock("Return", Instruction.Create(OpCodes.Ret)));
            }

            var indices = allAspects
                .Select((asp, index) => new { asp, index })
                .Where(x => (x.asp.AspectMethods & AspectMethods.OnException) != 0)
                .Select(x => x.index)
                .Reverse()
                .ToList();
            foreach (var index in indices)
            {
                var onExceptionAspect = allAspects[index];
                if (HandlesTags)
                {
                    var load = _creator.LoadMethodExecutionArgsTagFromPersistable(ExecutionArgs,
                        onExceptionAspect.TagPersistable);
                    exceptionHandlerCurrent = load.InsertAfter(exceptionHandlerCurrent, _ilProcessor);
                }

                var callAspectOnException =
                    _creator.CallAspectOnException(onExceptionAspect, ExecutionArgs);

                // without a FlowBehavior set by an aspect the exception is always rethrown
                if (!WritesFlowBehavior)
                {
                    callAspectOnException.InsertAfter(exceptionHandlerCurrent, _ilProcessor);
                    exceptionHandlerCurrent = callAspectOnException.Last;
                    continue;
                }

                var nop = new InstructionBlock("Nop", Instruction.Create(OpCodes.Nop));

                var callOnExitsAndReturn = new InstructionBlockChain();
                for (var i = index-1; i >= 0; --i)
                {
                    var jthAspect = allAspects[i];
                    if ((jthAspect.AspectMethods & AspectMethods.OnExit) == 0) 
                        continue;
                    if (HandlesTags)
                        callOnExitsAndReturn.Add(_creator.LoadMethodExecutionArgsTagFromPersistable(ExecutionArgs, jthAspect.TagPersistable));
                    callOnExitsAndReturn.Add(_creator.CallAspectOnExit(jthAspect, ExecutionArgs));
                }

                if (returnValue != null && !returnsByRef)
                {
                    if (ReadsBackReturnValue)
                        callOnExitsAndReturn.Add(_creator.ReadReturnValue(ExecutionArgs, returnValue.Variable));
                    callOnExitsAndReturn.Add(CreateReturnCompletedTaskIfNull(returnValue.Variable));
                }

                // long form: the handlers of the remaining aspects lie between here and the target,
                // Optimize() shortens the branch if it fits
                callOnExitsAndReturn.Add(new InstructionBlock("Leave", Instruction.Create(OpCodes.Leave, returnAfterHandling.First)));

                callAspectOnException.Add(_creator.IfFlowBehaviorIsAnyOf(ExecutionArgs, nop.First, callOnExitsAndReturn, FlowBehaviorValues.Continue, FlowBehaviorValues.Return));

                callAspectOnException.Add(nop);
                callAspectOnException.InsertAfter(exceptionHandlerCurrent, _ilProcessor);
                exceptionHandlerCurrent = callAspectOnException.Last;
            }
            
            var flowBehaviorHandler = new InstructionBlockChain();
            flowBehaviorHandler.Add(new InstructionBlock("throw", Instruction.Create(OpCodes.Rethrow)));
            flowBehaviorHandler.InsertAfter(exceptionHandlerCurrent, _ilProcessor);

            if (WritesFlowBehavior)
                returnAfterHandling.InsertAfter(flowBehaviorHandler.Last, _ilProcessor);

            _method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                CatchType = _creator.GetExceptionTypeReference(),
                TryStart = instructionCallStart,
                TryEnd = tryLeaveInstruction.Next,
                HandlerStart = tryLeaveInstruction.Next,
                HandlerEnd = flowBehaviorHandler.Last.Next
            });
        }

        private void Optimize()
        {
            // add DebuggerStepThrough attribute to avoid compiler searching for
            // non existing source code for woven il code
            var attributeCtor = _creator.GetDebuggerStepThroughAttributeCtorReference();

            // Only add DebuggerStepThrough if not already present.
            if (_method.CustomAttributes.All(a => a.AttributeType.FullName != attributeCtor.DeclaringType.FullName))
                _method.CustomAttributes.Add(new CustomAttribute(attributeCtor));

            _method.Body.InitLocals = true;
            _method.Body.Optimize();
            if (_hasDebugScope)
            {
                _method.DebugInformation.Scope = new ScopeDebugInformation(_method.Body.Instructions.First(), _method.Body.Instructions.Last())
                {
                    Import = _importOfDebugScope
                };
            }

            Catel.Fody.CecilExtensions.UpdateDebugInfo(_method);
        }

        protected virtual void Finish()
        {
            _clonedMethod.Body.Optimize();
        }
        
        private void AddToEnd(InstructionBlockChain chain)
        {
            chain.Append(_ilProcessor);
        }
    }
}
