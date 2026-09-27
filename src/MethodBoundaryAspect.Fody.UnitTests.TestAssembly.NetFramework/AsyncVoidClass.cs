using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class AsyncVoidClass
    {
        public static string Result { [DisableWeaving] get; [DisableWeaving] set; }

        [DisableWeaving]
        public void AttemptThrowWithExceptionOnlyAspect() => RunAsyncVoid(ThrowWithExceptionOnlyAspect);

        [AsyncVoidExceptionAspect]
        public async void ThrowWithExceptionOnlyAspect()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

        [DisableWeaving]
        public void AttemptThrowAndSwallow() => RunAsyncVoid(ThrowAndSwallow);

        [AsyncVoidExceptionAspect(SwallowException = true)]
        public async void ThrowAndSwallow()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

        [DisableWeaving]
        public void AttemptThrowWithFullAspect() => RunAsyncVoid(ThrowWithFullAspect);

        [AsyncVoidAspect]
        public async void ThrowWithFullAspect()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("An exception");
        }

        [DisableWeaving]
        public void AttemptReturnWithFullAspect() => RunAsyncVoid(ReturnWithFullAspect);

        [AsyncVoidAspect]
        public async void ReturnWithFullAspect()
        {
            await Task.Delay(10);
        }

        /// <summary>
        /// Runs an async void method with a synchronization context which captures
        /// unhandled exceptions (which would otherwise crash the process) and waits
        /// until the async void method has completed.
        /// </summary>
        [DisableWeaving]
        private static void RunAsyncVoid(Action asyncVoidMethod)
        {
            Result = string.Empty;
            var previousContext = SynchronizationContext.Current;
            var context = new AsyncVoidSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                asyncVoidMethod();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }

            if (!context.Completed.Wait(TimeSpan.FromSeconds(10)))
                Result += "[Timeout]";

            foreach (var exception in context.UnhandledExceptions)
                Result += $"[Unhandled: {exception.Message}]";
        }

        private class AsyncVoidSynchronizationContext : SynchronizationContext
        {
            public ManualResetEventSlim Completed { get; } = new ManualResetEventSlim();

            public List<Exception> UnhandledExceptions { get; } = new List<Exception>();

            public override void OperationCompleted() => Completed.Set();

            public override void Post(SendOrPostCallback d, object state)
            {
                // AsyncVoidMethodBuilder.SetException posts a callback which rethrows the exception
                try
                {
                    d(state);
                }
                catch (Exception e)
                {
                    lock (UnhandledExceptions)
                        UnhandledExceptions.Add(e);
                }
            }
        }
    }
}
