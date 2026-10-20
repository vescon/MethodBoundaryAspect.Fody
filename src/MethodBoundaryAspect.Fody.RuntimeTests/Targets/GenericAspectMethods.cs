using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class GenericAspectMethods
    {
        [GenericRecordingAspect<int>("A", Label = "label", Number = 3)]
        public int Sync(int value) => value;

        [GenericRecordingAspect<List<string>>("B")]
        public int Throwing() => throw new InvalidOperationException();

        [GenericRecordingAspect<Guid>("C")]
        public async Task<int> Async()
        {
            await Task.Yield();
            return 7;
        }

        [GenericRecordingAspect<int>("D")]
        public async Task AsyncThrowing()
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }

        [DerivedFromGenericAspect]
        public int FromGenericBase() => 1;
    }

    [GenericRecordingAspect<byte>("E")]
    public class GenericAspectOnGenericClass<TClass>
    {
        public TClass Method<TMethod>(TClass value, TMethod other) => value;
    }
}
