using MethodBoundaryAspect.Fody.Ordering;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

namespace MethodBoundaryAspect.Fody
{
    public class AspectDataOnAsyncMethod : AspectData
    {
        TypeReference _stateMachine;
        MethodDefinition _moveNext;
        VariableDefinition _stateMachineLocal;
        FieldReference _tagField;
        ArrayType _tagArrayType;
        FieldReference _aspectField;

        public AspectDataOnAsyncMethod(
            MethodDefinition moveNext,
            AspectInfo info,
            AspectMethods methods,
            MethodDefinition method,
            ModuleDefinition module,
            MethodDefinition sharedInstanceGetter = null)
            : base(info, methods, method, module, sharedInstanceGetter)
        {
            _moveNext = moveNext;
            _stateMachineLocal = method.Body.Variables.Single(v => v.VariableType.Resolve() == moveNext.DeclaringType);
            _stateMachine = _stateMachineLocal.VariableType;
        }

        // The tag lives in an array: the state machine is a struct which is copied into a box when the method awaits for
        // the first time, so a field written after the start of the state machine (the tag set in OnExit) would be lost
        // for the copy that runs MoveNext, which reads the tag in OnException.
        public override void EnsureTagStorage()
        {
            var systemObject = _referenceFinder.GetTypeReference(typeof(object));
            _tagArrayType = new ArrayType(systemObject);
            _tagField = _module.ImportReference(_stateMachine.AddPublicInstanceField(_tagArrayType));

            TagPersistable = new ArrayElementPersistable(
                new FieldPersistable(new VariablePersistable(_stateMachineLocal), _tagField), 0, systemObject);
        }

        public override InstructionBlockChain CreateTagStorage()
        {
            var instructions = new VariablePersistable(_stateMachineLocal).Load(true, false).Instructions.ToList();
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            instructions.Add(Instruction.Create(OpCodes.Newarr, _referenceFinder.GetTypeReference(typeof(object))));
            instructions.Add(Instruction.Create(OpCodes.Stfld, _tagField));

            var chain = new InstructionBlockChain();
            chain.Add(new InstructionBlock("Create the storage of the tag", instructions));
            return chain;
        }

        public override InstructionBlockChain CreateAspectInstance()
        {
            var aspectTypeReference = _module.ImportReference(Info.AspectAttribute.AttributeType);
            _aspectField = _module.ImportReference(_stateMachine.AddPublicInstanceField(aspectTypeReference));

            var loadMachine = new VariablePersistable(_stateMachineLocal).Load(true, false);

            var newObjectAspectBlock = CreateOrGetAspect();
            var loadOnStack = new InstructionBlock("Load on stack", Instruction.Create(OpCodes.Ldloc, newObjectAspectBlock.Variable));
            var storeField = new InstructionBlock("Store Field", Instruction.Create(OpCodes.Stfld, _aspectField));

            var newObjectAspectBlockChain = new InstructionBlockChain();
            newObjectAspectBlockChain.Add(loadMachine);
            newObjectAspectBlockChain.Add(newObjectAspectBlock);
            newObjectAspectBlockChain.Add(loadOnStack);
            newObjectAspectBlockChain.Add(storeField);

            AspectPersistable = new FieldPersistable(new VariablePersistable(_stateMachineLocal), _aspectField);
            return newObjectAspectBlockChain;
        }

        public IPersistable GetMoveNextExecutionArgs(IPersistable executionArgs)
        {
            var fieldExecutionArgs = executionArgs as FieldPersistable;
            var sm = StateMachineFromMoveNext;
            return new FieldPersistable(new ThisLoadable(sm), fieldExecutionArgs.Field.AsDefinedOn(sm));
        }

        TypeReference StateMachineFromMoveNext
        {
            get
            {
                if (!_stateMachine.ContainsGenericParameter)
                    return _stateMachine;

                var smType = _stateMachine.Resolve();
                var result = smType.MakeGenericType(smType.GenericParameters.ToArray());
                return result;
            }
        }

        public InstructionBlockChain LoadTagInMoveNext(IPersistable executionArgs)
        {
            var setMethod = _referenceFinder.GetMethodReference(executionArgs.PersistedType,
                    md => md.Name == "set_MethodExecutionTag");
            var sm = StateMachineFromMoveNext;

            return _creator.CallVoidMethod(setMethod, GetMoveNextExecutionArgs(executionArgs),
                new ArrayElementPersistable(
                    new FieldPersistable(new ThisLoadable(sm), _tagField.AsDefinedOn(sm)), 0,
                    _referenceFinder.GetTypeReference(typeof(object))));
        }

        public InstructionBlockChain CallOnExceptionInMoveNext(IPersistable executionArgs, VariableDefinition exceptionLocal)
        {
            var onExceptionMethodRef = _referenceFinder.GetMethodReference(Info.AspectAttribute.AttributeType,
                AspectMethodCriteria.IsOnExceptionMethod);

            var setMethod = _referenceFinder.GetMethodReference(executionArgs.PersistedType,
                md => md.Name == "set_Exception");

            var setExceptionOnArgsBlock = _creator.CallVoidMethod(setMethod, GetMoveNextExecutionArgs(executionArgs),
                new VariablePersistable(exceptionLocal));

            var sm = StateMachineFromMoveNext;
            var smPersistable = new ThisLoadable(sm);

            var aspectPersistable = new FieldPersistable(smPersistable, _aspectField.AsDefinedOn(sm));
            var callOnExceptionBlock = _creator.CallVoidMethod(onExceptionMethodRef,
                aspectPersistable, GetMoveNextExecutionArgs(executionArgs));

            var callAspectOnExceptionBlockChain = new InstructionBlockChain();
            callAspectOnExceptionBlockChain.Add(setExceptionOnArgsBlock);
            callAspectOnExceptionBlockChain.Add(callOnExceptionBlock);
            return callAspectOnExceptionBlockChain;
        }
    }
}
