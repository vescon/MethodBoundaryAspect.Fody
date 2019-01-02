using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.UnitTests.Shared.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.UnverifiableTestAssembly.NetFramework
{
    // Ignore PEVerify's complaint about ref return types.
    [IgnorePEVerifyCode("80131870")]
    public class UnverifiableGenerics<T>: IResult where T : IDisposable, new()
    {
        public static object Result { get; set; }
        public object InstanceResult { set { Result = value; } }
        
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

        T _t = new T();
        IList<T> _list = new List<T>();
        IList<T[]> _listOfArray = new List<T[]>();
        IList<T>[] _arrayOfList = new IList<T>[1];
        IList<IList<T>> _listOfList = new List<IList<T>>();

        public void TestRefGetClassLevelParameter() => RefGetClassLevelParameter();
        public void TestRefGetListOfClassLevelParameter() => RefGetListOfClassLevelParameter();
        public void TestRefGetListOfArrayOfClassLevelParameter() => RefGetListOfArrayOfClassLevelParameter();
        public void TestRefGetArrayOfListOfClassLevelParameter() => RefGetArrayOfListOfClassLevelParameter();
        public void TestRefGetListOfListOfClassLevelParameter() => RefGetListOfListOfClassLevelParameter();

        [AddLogs] public ref T RefGetClassLevelParameter() => ref _t;
        [AddLogs] public ref IList<T> RefGetListOfClassLevelParameter() => ref _list;
        [AddLogs] public ref IList<T[]> RefGetListOfArrayOfClassLevelParameter() => ref _listOfArray;
        [AddLogs] public ref IList<T>[] RefGetArrayOfListOfClassLevelParameter() => ref _arrayOfList;
        [AddLogs] public ref IList<IList<T>> RefGetListOfListOfClassLevelParameter() => ref _listOfList;

        static class Inner<T2>
        {
            public static T2 _t = (T2)Activator.CreateInstance(typeof(T2));
            public static IList<T2> _listOfT = new List<T2>();
            public static IList<T2[]> _listOfArrayOfT = new List<T2[]>();
            public static IList<T2>[] _arrayOfListOfT = new IList<T2>[1];
            public static IList<IList<T2>> _listOfListOfT = new List<IList<T2>>();

            public static IDictionary<T, T2> _dict = new Dictionary<T, T2>();
            public static IDictionary<T[], T2[]> _dictOfArray = new Dictionary<T[], T2[]>();
            public static IDictionary<T, T2>[] _arrayOfDict = new IDictionary<T, T2>[1];
            public static IDictionary<IList<T>, IList<T2>> _dictOfLists = new Dictionary<IList<T>, IList<T2>>();
            public static IList<IDictionary<T, T2>> _listOfDicts = new List<IDictionary<T, T2>>();
        }

        public void TestRefGetMethodLevelParameter<TM>() => RefGetMethodLevelParameter<TM>();
        public void TestRefGetListOfMethodLevelParameter<TM>() => RefGetListOfMethodLevelParameter<TM>();
        public void TestRefGetListOfArrayOfMethodLevelParameter<TM>() => RefGetListOfArrayOfMethodLevelParameter<TM>();
        public void TestRefGetArrayOfListOfMethodLevelParameter<TM>() => RefGetArrayOfListOfMethodLevelParameter<TM>();
        public void TestRefGetListOfListOfMethodLevelParameter<TM>() => RefGetListOfListOfMethodLevelParameter<TM>();

        [AddLogs] public ref TM RefGetMethodLevelParameter<TM>() => ref Inner<TM>._t;
        [AddLogs] public ref IList<TM> RefGetListOfMethodLevelParameter<TM>() => ref Inner<TM>._listOfT;
        [AddLogs] public ref IList<TM[]> RefGetListOfArrayOfMethodLevelParameter<TM>() => ref Inner<TM>._listOfArrayOfT;
        [AddLogs] public ref IList<TM>[] RefGetArrayOfListOfMethodLevelParameter<TM>() => ref Inner<TM>._arrayOfListOfT;
        [AddLogs] public ref IList<IList<TM>> RefGetListOfListOfMethodLevelParameter<TM>() => ref Inner<TM>._listOfListOfT;

        public void TestRefGetDictMixedParameters<TM>() => RefGetDictMixedParameters<TM>();
        public void TestRefGetDictOfArrayOfMixedParameters<TM>() => RefGetDictOfArrayOfMixedParameters<TM>();
        public void TestRefGetArrayOfDictOfMixedParameters<TM>() => RefGetArrayOfDictOfMixedParameters<TM>();
        public void TestRefGetDictOfListsOfMixedParameters<TM>() => RefGetDictOfListsOfMixedParameters<TM>();
        public void TestRefGetListOfDictOfMixedParameters<TM>() => RefGetListOfDictOfMixedParameters<TM>();

        [AddLogs] public ref IDictionary<T, TM> RefGetDictMixedParameters<TM>() => ref Inner<TM>._dict;
        [AddLogs] public ref IDictionary<T[], TM[]> RefGetDictOfArrayOfMixedParameters<TM>() => ref Inner<TM>._dictOfArray;
        [AddLogs] public ref IDictionary<T, TM>[] RefGetArrayOfDictOfMixedParameters<TM>() => ref Inner<TM>._arrayOfDict;
        [AddLogs] public ref IDictionary<IList<T>, IList<TM>> RefGetDictOfListsOfMixedParameters<TM>() => ref Inner<TM>._dictOfLists;
        [AddLogs] public ref IList<IDictionary<T, TM>> RefGetListOfDictOfMixedParameters<TM>() => ref Inner<TM>._listOfDicts;

        static class Inner<TM, TN>
        {
            public static IDictionary<TN, TM> _dict = new Dictionary<TN, TM>();
            public static IDictionary<TN[], TM[]> _dictOfArray = new Dictionary<TN[], TM[]>();
            public static IDictionary<TN, TM>[] _arrayOfDicts = new IDictionary<TN, TM>[1];
            public static IDictionary<IList<TN>, IList<TM>> _dictOfLists = new Dictionary<IList<TN>, IList<TM>>();
            public static IList<IDictionary<TN, TM>> _listOfDicts = new List<IDictionary<TN, TM>>();
        }

        public void TestRefGetDictRelatedMethodLevelParameters<TM, TN>() => RefGetDictRelatedMethodLevelParameters<TM, TN>();
        public void TestRefGetDictOfArrayOfRelatedMethodLevelParameters<TM, TN>() => RefGetDictOfArrayOfRelatedMethodLevelParameters<TM, TN>();
        public void TestRefGetArrayOfDictOfRelatedMethodLevelParameters<TM, TN>() => RefGetArrayOfDictOfRelatedMethodLevelParameters<TM, TN>();
        public void TestRefGetDictOfListsOfRelatedMethodLevelParameters<TM, TN>() => RefGetDictOfListsOfRelatedMethodLevelParameters<TM, TN>();
        public void TestRefGetListOfDictsOfRelatedMethodLevelParameters<TM, TN>() => RefGetListOfDictsOfRelatedMethodLevelParameters<TM, TN>();

        [AddLogs] public ref IDictionary<TN, TM> RefGetDictRelatedMethodLevelParameters<TM, TN>() => ref Inner<TM, TN>._dict;
        [AddLogs] public ref IDictionary<TN[], TM[]> RefGetDictOfArrayOfRelatedMethodLevelParameters<TM, TN>() => ref Inner<TM, TN>._dictOfArray;
        [AddLogs] public ref IDictionary<TN, TM>[] RefGetArrayOfDictOfRelatedMethodLevelParameters<TM, TN>() => ref Inner<TM, TN>._arrayOfDicts;
        [AddLogs] public ref IDictionary<IList<TN>, IList<TM>> RefGetDictOfListsOfRelatedMethodLevelParameters<TM, TN>() => ref Inner<TM, TN>._dictOfLists;
        [AddLogs] public ref IList<IDictionary<TN, TM>> RefGetListOfDictsOfRelatedMethodLevelParameters<TM, TN>() => ref Inner<TM, TN>._listOfDicts;
    }
}
