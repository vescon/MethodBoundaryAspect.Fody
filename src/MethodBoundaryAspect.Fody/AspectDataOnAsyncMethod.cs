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
        FieldReference _aspectField;

        public AspectDataOnAsyncMethod(MethodDefinition moveNext, AspectInfo info, AspectMethods methods, MethodDefinition method, ModuleDefinition module)
            : base(info, methods, method, module)
        {
            _moveNext = moveNext;
            _stateMachineLocal = method.Body.Variables.Single(v => v.VariableType.Resolve() == moveNext.DeclaringType);
            _stateMachine = _stateMachineLocal.VariableType;
        }

        public override void EnsureTagStorage()
        {
            var systemObject = _referenceFinder.GetTypeReference(typeof(object));
            _tagField = _module.ImportReference(_stateMachine.AddPublicInstanceField(systemObject));

            TagPersistable = new FieldPersistable(new VariablePersistable(_stateMachineLocal), _tagField);
        }

        public override InstructionBlockChain CreateAspectInstance()
        {
            var aspectTypeReference = _module.ImportReference(Info.AspectAttribute.AttributeType);
            _aspectField = _module.ImportReference(_stateMachine.AddPublicInstanceField(aspectTypeReference));

            var loadMachine = new VariablePersistable(_stateMachineLocal).Load(true, false);

            var newObjectAspectBlock = _creator.CreateAndNewUpAspect(Info.AspectAttribute);
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
                new FieldPersistable(new ThisLoadable(sm), _tagField.AsDefinedOn(sm)));
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
