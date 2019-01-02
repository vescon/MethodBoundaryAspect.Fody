using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class GenericReferencingForeignAssembly<T> : IResult
    {
        public static object Result { get; set; }
        public object InstanceResult { set { Result = value; } }

        [AddLogs]
        public ForeignGeneric<T1, T>.Inner<T3> GetInnerMixed<T1, T3>() where T1 : IList<T>
            => NewT<ForeignGeneric<T1, T>.Inner<T3>>();
        [AddLogs]
        public ForeignGeneric<T1, T2>.Inner<T> GetInnerOuterMixed<T1, T2>() where T1 : IList<T2>
            => NewT<ForeignGeneric<T1, T2>.Inner<T>>();

        [AddLogs] public void TakeInnerMixed<T1, T3>(ForeignGeneric<T1, T>.Inner<T3> arg) where T1 : IList<T> { }
        [AddLogs] public void TakeInnerOuterMixed<T1, T2>(ForeignGeneric<T1, T2>.Inner<T> arg) where T1 : IList<T2> { }

        [AddLogs] public void RefTakeInnerMixed<T1, T3>(ref ForeignGeneric<T1, T>.Inner<T3> arg) where T1 : IList<T> { }
        [AddLogs] public void RefTakeInnerOuterMixed<T1, T2>(ref ForeignGeneric<T1, T2>.Inner<T> arg) where T1 : IList<T2> { }

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
