using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class AsyncChangedArgumentMethods
    {
        private readonly int _offset = 100;

        [ChangingInput]
        public async Task<int> Changed(int value)
        {
            await Task.Yield();
            return value;
        }

        [ChangingInput]
        public async Task<int> ChangedOfInstance(int value)
        {
            await Task.Yield();
            return value + _offset;
        }

        [NullArgument]
        public async Task<int> NullForInt(int value)
        {
            await Task.Yield();
            return value;
        }

        [NullArgument(Index = 1)]
        public async Task<string> NullForSecondArgument(string text, Guid id)
        {
            await Task.Yield();
            return text + id;
        }

        [NullArgument]
        public async Task<T> NullForGeneric<T>(T value)
        {
            await Task.Yield();
            return value;
        }

        [ChangingInput]
        public async Task ChangedWithoutResult(int value)
        {
            await Task.Yield();
            Result = value;
        }

        public static int Result;

        // the method does not change the arguments, so it doesn't need the attribute
        [NameRecordingAspect]
        public async Task<int> Unchanged(int value)
        {
            await Task.Yield();
            return value;
        }
    }
}
