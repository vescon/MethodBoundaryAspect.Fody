using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class Issue5Methods
    {
        [Issue5WrappingAspect]
        public async Task ThrowAfterAwait()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

        [Issue5WrappingAspect]
        public async Task<int> ThrowAfterAwaitWithResult()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

#pragma warning disable 1998 // async method without await
        [Issue5WrappingAspect]
        public async Task ThrowBeforeAwait()
        {
            throw new InvalidOperationException("An exception");
        }
#pragma warning restore 1998

        [Issue5WrappingAspect]
        public async ValueTask<int> ThrowInValueTask()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

        [Issue5WrappingAspect]
        public static async Task ThrowInStaticMethod()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

        [Issue5WrappingAspect]
        public async Task<string> ThrowInTryCatch()
        {
            try
            {
                await Task.Delay(10);
                throw new InvalidOperationException("An exception");
            }
            catch (ArgumentException)
            {
                return "Not expected";
            }
        }

        // OnException is called in reverse order: Issue5RecordingAspect, then Issue5WrappingAspect.
        [Issue5WrappingAspect]
        [Issue5RecordingAspect]
        public async Task ThrowWithWrappingAspectCalledLast()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

        // OnException is called in reverse order: Issue5WrappingAspect, then Issue5RecordingAspect is skipped.
        [Issue5RecordingAspect]
        [Issue5WrappingAspect]
        public async Task ThrowWithWrappingAspectCalledFirst()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }
    }
}
