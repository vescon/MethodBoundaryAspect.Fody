using System;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody
{
    internal class MethodWeaver : IDisposable
    {
        private NamedInstructionBlockChain _createArgumentsArray;
        private MethodBodyPatcher _methodBodyChanger;
        private bool _finished;
        
        public int WeaveCounter { get; private set; }

        public void Weave(
            MethodDefinition method,
            CustomAttribute aspect,
            AspectMethods overriddenAspectMethods,
            ModuleDefinition moduleDefinition)
        {
            if (overriddenAspectMethods == AspectMethods.None)
                return;
            
            var creator = new InstructionBlockChainCreator(method, aspect.AttributeType, moduleDefinition, WeaveCounter);

            _methodBodyChanger = new MethodBodyPatcher(method.Name, method);
            var saveReturnValue = creator.SaveReturnValue();
            var loadReturnValue = creator.LoadValueOnStack(saveReturnValue);
            _methodBodyChanger.Unify(saveReturnValue, loadReturnValue);

            if (WeaveCounter == 0)
                _createArgumentsArray = creator.CreateMethodArgumentsArray();

            var createMethodExecutionArgsInstance = creator.CreateMethodExecutionArgsInstance(_createArgumentsArray);
            _methodBodyChanger.AddCreateMethodExecutionArgs(createMethodExecutionArgsInstance);

            var createAspectInstance = creator.CreateAspectInstance(aspect);
            if (overriddenAspectMethods.HasFlag(AspectMethods.OnEntry))
            {
                var callAspectOnEntry = creator.CallAspectOnEntry(createAspectInstance,
                    createMethodExecutionArgsInstance);
                _methodBodyChanger.AddOnEntryCall(createAspectInstance, callAspectOnEntry);
            }

            if (overriddenAspectMethods.HasFlag(AspectMethods.OnExit))
            {
                var setMethodExecutionArgsReturnValue =
                    creator.SetMethodExecutionArgsReturnValue(createMethodExecutionArgsInstance, loadReturnValue);
                var callAspectOnExit = creator.CallAspectOnExit(createAspectInstance,
                    createMethodExecutionArgsInstance);
                _methodBodyChanger.AddOnExitCall(createAspectInstance, callAspectOnExit,
                    setMethodExecutionArgsReturnValue);
            }

            if (overriddenAspectMethods.HasFlag(AspectMethods.OnException))
            {
                var setMethodExecutionArgsExceptionFromStack =
                    creator.SetMethodExecutionArgsExceptionFromStack(createMethodExecutionArgsInstance);

                var callAspectOnException = creator.CallAspectOnException(createAspectInstance,
                    createMethodExecutionArgsInstance);
                _methodBodyChanger.AddOnExceptionCall(createAspectInstance, callAspectOnException,
                    setMethodExecutionArgsExceptionFromStack);
            }

            if (_methodBodyChanger.EndsWithThrow)
            {
                var saveThrownException = creator.SaveThrownException();
                var loadThrownException = creator.LoadValueOnStack(saveThrownException);
                var loadThrownException2 = creator.LoadValueOnStack(saveThrownException);
                _methodBodyChanger.FixThrowAtEndOfRealBody(
                    saveThrownException, 
                    loadThrownException,
                    loadThrownException2);
            }

            _methodBodyChanger.OptimizeBody();

            Catel.Fody.CecilExtensions.UpdateDebugInfo(method);

            WeaveCounter++;
        }

        public void Finish()
        {
            if (_finished)
                return;

            if (_methodBodyChanger != null)
                _methodBodyChanger.AddCreateArgumentsArray(_createArgumentsArray);
            _finished = true;
        }

        public void Dispose()
        {
            Finish();
        }
    }
}