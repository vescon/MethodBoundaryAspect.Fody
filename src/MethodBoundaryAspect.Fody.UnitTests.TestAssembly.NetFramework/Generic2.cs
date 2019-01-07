using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class Generic<T1, T2> : IResult where T1 : IList<T2[]>
    {
        public static object Result { get; set; }

        public object InstanceResult { set { Result = value; } }

        [AddLogs] public IDictionary<T1, T2> GetDictClassLevelParameters() => NewT<IDictionary<T1, T2>>();
        [AddLogs] public IDictionary<T1[], T2[]> GetDictOfArrayOfClassLevelParameters() => NewT<IDictionary<T1[], T2[]>>();
        [AddLogs] public IDictionary<T1, T2>[] GetArrayOfDictOfClassLevelParameters() => NewT<IDictionary<T1, T2>[]>();
        [AddLogs] public IDictionary<IList<T1>, IList<T2>> GetDictOfListOfClassLevelParameters() => NewT<IDictionary<IList<T1>, IList<T2>>>();
        [AddLogs] public IList<IDictionary<T1, T2>> GetListOfDictOfClassLevelParameters() => NewT<IList<IDictionary<T1, T2>>>();

        [AddLogs] public void TakeDictClassLevelParameters(IDictionary<T1, T2> arg) { }
        [AddLogs] public void TakeDictOfArraysOfClassLevelParameters(IDictionary<T1[], T2[]> arg) { }
        [AddLogs] public void TakeArrayOfDictOfClassLevelParameters(IDictionary<T1, T2>[] arg) { }
        [AddLogs] public void TakeDictOfListOfClassLevelParameters(IDictionary<IList<T1>, IList<T2>> arg) { }
        [AddLogs] public void TakeListOfDictOfClassLevelParameters(IList<IDictionary<T1, T2>> arg) { }

        [AddLogs] public void RefTakeDictClassLevelParameters(ref IDictionary<T1, T2> arg) { }
        [AddLogs] public void RefTakeDictOfArraysOfClassLevelParameters(ref IDictionary<T1[], T2[]> arg) { }
        [AddLogs] public void RefTakeArrayOfDictOfClassLevelParameters(ref IDictionary<T1, T2>[] arg) { }
        [AddLogs] public void RefTakeDictOfListOfClassLevelParameters(ref IDictionary<IList<T1>, IList<T2>> arg) { }
        [AddLogs] public void RefTakeListOfDictOfClassLevelParameters(ref IList<IDictionary<T1, T2>> arg) { }

        public class Inner<T3> : IResult where T3 : IList<T1>
        {
            public static object Result { get; set; }

            public object InstanceResult { set => Result = value; }

            [AddLogs] public IDictionary<T3, T2> GetDictOuterAndInnerParameters() => NewT<IDictionary<T3, T2>>();
            [AddLogs] public IDictionary<T3[], T2[]> GetDictOfArrayOfOuterAndInnerParameters() => NewT<IDictionary<T3[], T2[]>>();
            [AddLogs] public IDictionary<T3, T2>[] GetArrayOfDictOfOuterAndInnerParameters() => NewT<IDictionary<T3, T2>[]>();
            [AddLogs] public IDictionary<IList<T3>, IList<T2>> GetDictOfListOfOuterAndInnerParameters() => NewT<IDictionary<IList<T3>, IList<T2>>>();
            [AddLogs] public IList<IDictionary<T3, T2>> GetListOfDictOfOuterAndInnerParameters() => NewT<IList<IDictionary<T3, T2>>>();

            [AddLogs] public void TakeDictOuterAndInnerParameters(IDictionary<T3, T2> arg) { }
            [AddLogs] public void TakeDictOfArrayOfOuterAndInnerParameters(IDictionary<T3[], T2[]> arg) { }
            [AddLogs] public void TakeArrayOfDictOfOuterAndInnerParameters(IDictionary<T3, T2>[] arg) { }
            [AddLogs] public void TakeDictOfListOfOuterAndInnerParameters(IDictionary<IList<T3>, IList<T2>> arg) { }
            [AddLogs] public void TakeListOfDictOfOuterAndInnerParameters(IList<IDictionary<T3, T2>> arg) { }

            [AddLogs] public void RefTakeDictOuterAndInnerParameters(ref IDictionary<T3, T2> arg) { }
            [AddLogs] public void RefTakeDictOfArrayOfOuterAndInnerParameters(ref IDictionary<T3[], T2[]> arg) { }
            [AddLogs] public void RefTakeArrayOfDictOfOuterAndInnerParameters(ref IDictionary<T3, T2>[] arg) { }
            [AddLogs] public void RefTakeDictOfListOfOuterAndInnerParameters(ref IDictionary<IList<T3>, IList<T2>> arg) { }
            [AddLogs] public void RefTakeListOfDictOfOuterAndInnerParameters(ref IList<IDictionary<T3, T2>> arg) { }
        }

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
