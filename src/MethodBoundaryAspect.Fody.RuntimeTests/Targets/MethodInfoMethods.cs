using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class MethodInfoMethods
    {
        public static Type PrivateGenericType => typeof(PrivateGeneric<>);

        [MethodInfoRecordingAspect]
        public int NonGeneric(int value) => value;

        [MethodInfoRecordingAspect]
        public T GenericMethod<T>(T value) => value;

        [MethodInfoRecordingAspect]
        public async Task<T> GenericAsync<T>(T value)
        {
            await Task.Yield();
            return value;
        }

        public static T CallPrivateGeneric<T>(T value) => new PrivateGeneric<T>().Method(value);

        private class PrivateGeneric<T>
        {
            [MethodInfoRecordingAspect]
            public T Method(T value) => value;
        }
    }

    public class MethodInfoGeneric<TClass>
    {
        [MethodInfoRecordingAspect]
        public TClass NonGenericMethod(TClass value) => value;

        [MethodInfoRecordingAspect]
        public TMethod GenericMethod<TMethod>(TClass value, TMethod other) => other;

        [MethodInfoRecordingAspect]
        public static TClass Static(TClass value) => value;

        [MethodInfoRecordingAspect]
        public async Task<TClass> Async(TClass value)
        {
            await Task.Yield();
            return value;
        }

        public class Nested<TNested>
        {
            [MethodInfoRecordingAspect]
            public TNested Method(TClass value, TNested other) => other;
        }
    }

    public class MethodInfoConstrained<TClass> where TClass : struct, IComparable<TClass>
    {
        [MethodInfoRecordingAspect]
        public TMethod Method<TMethod>(TClass value, TMethod other) where TMethod : class, new() => other;
    }
}
