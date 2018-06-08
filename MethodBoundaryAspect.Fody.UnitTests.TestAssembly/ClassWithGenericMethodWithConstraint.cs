using System;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class ClassWithGenericMethodWithConstraint
    {
        [SetInstanceValueAspect]
        public string DoItWithImplementedConstraint<T>(T test) where T : ITestClass
        {
            return test.Test();
        }

        [SetInstanceValueAspect]
        public void DoItWithClassConstraint<T>(T test) where T : class
        {
            T t = default(T);
        }

        [SetInstanceValueAspect]
        public void DoItWithStructConstraint<T>(T test) where T : struct
        {
            T t = default(T);
        }

        [SetInstanceValueAspect]
        public void DoItWithNewConstraint<T>(T test) where T : new()
        {
            T t = new T();
        }
    }

    public interface ITestClass
    {
        string Test();
    }

    [Serializable]
    public class TestClass : ITestClass
    {
        public string Test()
        {
            return "Test succeeded";
        }
    }

    [Serializable]
    public struct TestStruct
    {
        public string Test;
    }

    [Serializable]
    public class InvalidTestClass
    {
        public InvalidTestClass(string test)
        {

        }
    }
}