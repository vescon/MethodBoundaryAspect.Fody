using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    [Serializable]
    public class Disposable : IDisposable { public void Dispose() { } }

    [Serializable]
    public class MethodDisposable : IDisposable { public void Dispose() { } }

    public class Generic<T> : IResult where T : IDisposable
    {
        public static object Result { get; set; }

        public object InstanceResult { set { Result = value; } }

        [AddLogs] public T GetClassLevelParameter() => NewT<T>();
        [AddLogs] public IList<T> GetListOfClassLevelParameter() => NewT<IList<T>>();
        [AddLogs] public IList<T[]> GetListOfArrayOfClassLevelParameter() => NewT<IList<T[]>>();
        [AddLogs] public IList<T>[] GetArrayOfListOfClassLevelParameter() => NewT<IList<T>[]>();
        [AddLogs] public IList<IList<T>> GetListOfListOfClassLevelParameter() => NewT<IList<IList<T>>>();

        [AddLogs] public void TakeClassLevelParameter(T arg) { }
        [AddLogs] public void TakeListOfClassLevelParameter(IList<T> arg) { }
        [AddLogs] public void TakeListOfArrayOfClassLevelParameter(IList<T[]> arg) { }
        [AddLogs] public void TakeArrayOfListOfClassLevelParameter(IList<T>[] arg) { }
        [AddLogs] public void TakeListOfListOfClassLevelParameter(IList<IList<T>> arg) { }

        [AddLogs] public void RefTakeClassLevelParameter(ref T arg) { }
        [AddLogs] public void RefTakeListOfClassLevelParameter(ref IList<T> arg) { }
        [AddLogs] public void RefTakeListOfArrayOfClassLevelParameter(ref IList<T[]> arg) { }
        [AddLogs] public void RefTakeArrayOfListOfClassLevelParameter(ref IList<T>[] arg) { }
        [AddLogs] public void RefTakeListOfListOfClassLevelParameter(ref IList<IList<T>> arg) { }

        [AddLogs] public TM GetMethodLevelParameter<TM>() => NewT<TM>();
        [AddLogs] public IList<TM> GetListOfMethodLevelParameter<TM>() => NewT<IList<TM>>();
        [AddLogs] public IList<TM[]> GetListOfArrayOfMethodLevelParameter<TM>() => NewT<IList<TM[]>>();
        [AddLogs] public IList<TM>[] GetArrayOfListOfMethodLevelParameter<TM>() => NewT<IList<TM>[]>();
        [AddLogs] public IList<IList<TM>> GetListOfListOfMethodLevelParameter<TM>() => NewT<IList<IList<TM>>>();

        [AddLogs] public void TakeMethodLevelParameter<TM>(TM arg) { }
        [AddLogs] public void TakeListOfMethodLevelParameter<TM>(IList<TM> arg) { }
        [AddLogs] public void TakeListOfArrayOfMethodLevelParameter<TM>(IList<TM[]> arg) { }
        [AddLogs] public void TakeArrayOfListOfMethodLevelParameter<TM>(IList<TM>[] arg) { }
        [AddLogs] public void TakeListOfListOfMethodLevelParameter<TM>(IList<IList<TM>> arg) { }

        [AddLogs] public void RefTakeMethodLevelParameter<TM>(ref TM arg) { }
        [AddLogs] public void RefTakeListOfMethodLevelParameter<TM>(ref IList<TM> arg) { }
        [AddLogs] public void RefTakeListOfArrayOfMethodLevelParameter<TM>(ref IList<TM[]> arg) { }
        [AddLogs] public void RefTakeArrayOfListOfMethodLevelParameter<TM>(ref IList<TM>[] arg) { }
        [AddLogs] public void RefTakeListOfListOfMethodLevelParameter<TM>(ref IList<IList<TM>> arg) { }

        [AddLogs] public IDictionary<T, TM> GetDictMixedParameters<TM>() => NewT<IDictionary<T, TM>>();
        [AddLogs] public IDictionary<T[], TM[]> GetDictOfArrayOfMixedParameters<TM>() => NewT<IDictionary<T[], TM[]>>();
        [AddLogs] public IDictionary<T, TM>[] GetArrayOfDictOfMixedParameters<TM>() => NewT<IDictionary<T, TM>[]>();
        [AddLogs] public IDictionary<IList<T>, IList<TM>> GetDictOfListsOfMixedParameters<TM>() => NewT<IDictionary<IList<T>, IList<TM>>>();
        [AddLogs] public IList<IDictionary<T, TM>> GetListOfDictOfMixedParameters<TM>() => NewT<IList<IDictionary<T, TM>>>();

        [AddLogs] public void TakeDictMixedParameters<TM>(IDictionary<T, TM> arg) { }
        [AddLogs] public void TakeDictOfArrayOfMixedParameters<TM>(IDictionary<T[], TM[]> arg) { }
        [AddLogs] public void TakeArrayOfDictOfMixedParameters<TM>(IDictionary<T, TM>[] arg) { }
        [AddLogs] public void TakeDictOfListsOfMixedParameters<TM>(IDictionary<IList<T>, IList<TM>> arg) { }
        [AddLogs] public void TakeListOfDictOfMixedParameters<TM>(IList<IDictionary<T, TM>> arg) { }

        [AddLogs] public void RefTakeDictMixedParameters<TM>(ref IDictionary<T, TM> arg) { }
        [AddLogs] public void RefTakeDictOfArrayOfMixedParameters<TM>(ref IDictionary<T[], TM[]> arg) { }
        [AddLogs] public void RefTakeArrayOfDictOfMixedParameters<TM>(ref IDictionary<T, TM>[] arg) { }
        [AddLogs] public void RefTakeDictOfListsOfMixedParameters<TM>(ref IDictionary<IList<T>, IList<TM>> arg) { }
        [AddLogs] public void RefTakeListOfDictOfMixedParameters<TM>(ref IList<IDictionary<T, TM>> arg) { }

        [AddLogs]
        public IDictionary<TN, TM> GetDictRelatedMethodLevelParameters<TM, TN>() where TN : IList<TM[]>
            => NewT<IDictionary<TN, TM>>();
        [AddLogs]
        public IDictionary<TN[], TM[]> GetDictOfArrayOfRelatedMethodLevelParameters<TM, TN>() where TN : IList<TM[]>
            => NewT<IDictionary<TN[], TM[]>>();
        [AddLogs]
        public IDictionary<TN, TM>[] GetArrayOfDictOfRelatedMethodLevelParameters<TM, TN>() where TN : IList<TM[]>
            => NewT<IDictionary<TN, TM>[]>();
        [AddLogs]
        public IDictionary<IList<TN>, IList<TM>> GetDictOfListsOfRelatedMethodLevelParameters<TM, TN>() where TN : IList<TM[]>
            => NewT<IDictionary<IList<TN>, IList<TM>>>();
        [AddLogs]
        public IList<IDictionary<TN, TM>> GetListOfDictsOfRelatedMethodLevelParameters<TM, TN>() where TN : IList<TM[]>
            => NewT<IList<IDictionary<TN, TM>>>();

        [AddLogs] public void TakeDictRelatedMethodLevelParameters<TM, TN>(IDictionary<TN, TM> arg) where TN : IList<TM[]> { }
        [AddLogs] public void TakeDictOfArraysOfRelatedMethodLevelParameters<TM, TN>(IDictionary<TN[], TM[]> arg) where TN : IList<TM[]> { }
        [AddLogs] public void TakeArrayOfDictsOfRelatedMethodLevelParameters<TM, TN>(IDictionary<TN, TM>[] arg) where TN : IList<TM[]> { }
        [AddLogs] public void TakeDictOfListsOfRelatedMethodLevelParameters<TM, TN>(IDictionary<IList<TN>, IList<TM>> arg) where TN : IList<TM[]> { }
        [AddLogs] public void TakeListOfDictsOfRelatedMethodLevelParameters<TM, TN>(IList<IDictionary<TN, TM>> arg) where TN : IList<TM[]> { }

        [AddLogs] public void RefTakeDictRelatedMethodLevelParameters<TM, TN>(ref IDictionary<TN, TM> arg) where TN : IList<TM[]> { }
        [AddLogs] public void RefTakeDictOfArraysOfRelatedMethodLevelParameters<TM, TN>(ref IDictionary<TN[], TM[]> arg) where TN : IList<TM[]> { }
        [AddLogs] public void RefTakeArrayOfDictsOfRelatedMethodLevelParameters<TM, TN>(ref IDictionary<TN, TM>[] arg) where TN : IList<TM[]> { }
        [AddLogs] public void RefTakeDictOfListsOfRelatedMethodLevelParameters<TM, TN>(ref IDictionary<IList<TN>, IList<TM>> arg) where TN : IList<TM[]> { }
        [AddLogs] public void RefTakeListOfDictsOfRelatedMethodLevelParameters<TM, TN>(ref IList<IDictionary<TN, TM>> arg) where TN : IList<TM[]> { }

        public static TItem NewT<TItem>() => (TItem)NewT(typeof(TItem));

        public static object NewT(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>))
                return NewT(typeof(List<>).MakeGenericType(t.GetGenericArguments()));
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                return NewT(typeof(Dictionary<,>).MakeGenericType(t.GetGenericArguments()));
            else if (t.IsArray)
                return Array.CreateInstance(t.GetElementType(), 1);
            return Activator.CreateInstance(t);
        }
    }
}
