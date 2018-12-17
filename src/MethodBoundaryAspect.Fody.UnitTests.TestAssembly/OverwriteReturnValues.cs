using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;
using System;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    public class OverwriteReturnValues
    {
        public static string Result { get; set; }

        public void TestReturnString()
        {
            Result = ReturnString();
        }

        [OverwriteReturnValueAspect("Overwritten")]
        string ReturnString()
        {
            return "not overwritten";
        }

        public void ExternTestReturnString()
        {
            Result = ExternReturnString();
        }

        [TestAssemblyAspects.OverwriteReturnValueAspect("Overwritten")]
        string ExternReturnString()
        {
            return "not overwritten";
        }

        public void TestReturnInt()
        {
            Result = ReturnInt().ToString();
        }

        [OverwriteReturnValueAspect(42)]
        int ReturnInt()
        {
            return -1;
        }

        public void ExternTestReturnInt()
        {
            Result = ExternReturnInt().ToString();
        }

        [TestAssemblyAspects.OverwriteReturnValueAspect(42)]
        int ExternReturnInt()
        {
            return -1;
        }

        public void TestReturnEnum()
        {
            Result = ReturnEnum().ToString();
        }

        public enum TestValues
        {
            NotOverwritten,
            Overwritten
        }

        [OverwriteReturnValueAspect(TestValues.Overwritten)]
        TestValues ReturnEnum()
        {
            return TestValues.NotOverwritten;
        }
        
        public void ExternTestReturnEnum()
        {
            Result = ExternReturnEnum().ToString();
        }

        [TestAssemblyAspects.OverwriteReturnValueAspect(TestValues.Overwritten)]
        TestValues ExternReturnEnum()
        {
            return TestValues.NotOverwritten;
        }
        
        public void TestWrongType()
        {
            ReturnWrongType();
        }

        [OverwriteReturnValueAspect("not an integer")]
        int ReturnWrongType()
        {
            return -1;
        }

        public void ExternTestWrongType()
        {
            ExternReturnWrongType();
        }

        [TestAssemblyAspects.OverwriteReturnValueAspect("not an integer")]
        int ExternReturnWrongType()
        {
            return -1;
        }

        public void TestTry()
        {
            Result = ReturnFromTry();
        }

        [OverwriteReturnValueAspect("Overwritten")]
        string ReturnFromTry()
        {
            try
            {
                return "not overwritten";
            }
            catch (ArgumentNullException)
            {
                throw;
            }
        }
        
        public void ExternTestTry()
        {
            Result = ExternReturnFromTry();
        }

        [TestAssemblyAspects.OverwriteReturnValueAspect("Overwritten")]
        string ExternReturnFromTry()
        {
            try
            {
                return "not overwritten";
            }
            catch (ArgumentNullException)
            {
                throw;
            }
        }

        public void TestCatch()
        {
            Result = ReturnFromCatch();
        }

        [OverwriteReturnValueAspect("Overwritten")]
        string ReturnFromCatch()
        {
            try
            {
                throw new ArgumentNullException();
            }
            catch (ArgumentNullException)
            {
                return "not overwritten";
            }
        }
        
        public void ExternTestCatch()
        {
            Result = ExternReturnFromCatch();
        }

        [TestAssemblyAspects.OverwriteReturnValueAspect("Overwritten")]
        string ExternReturnFromCatch()
        {
            try
            {
                throw new ArgumentNullException();
            }
            catch (ArgumentNullException)
            {
                return "not overwritten";
            }
        }

        public void TestCovariantGeneric()
        {
            Result = ReturnCovariantGeneric().GetType().FullName;
        }

        [OverwriteReturnValueAspect(new string[] { "one", "two" })]
        IEnumerable<object> ReturnCovariantGeneric()
        {
            return new object[0];
        }

        public void ExternTestCovariantGeneric()
        {
            Result = ExternReturnCovariantGeneric().GetType().FullName;
        }

        [TestAssemblyAspects.OverwriteReturnValueAspect(new string[] { "one", "two" })]
        IEnumerable<object> ExternReturnCovariantGeneric()
        {
            return new object[0];
        }

        public void TestCustomStruct()
        {
            Result = ReturnCustomStruct().ToString();
        }

        [OverwriteReturnValueWithTestDataAspect(42)]
        TestAssemblyAspects.TestData ReturnCustomStruct()
        {
            return new TestAssemblyAspects.TestData(0);
        }

        public void ExternTestCustomStruct()
        {
            Result = ExternReturnCustomStruct().ToString();
        }

        [TestAssemblyAspects.OverwriteReturnValueWithTestDataAspect(42)]
        TestAssemblyAspects.TestData ExternReturnCustomStruct()
        {
            return new TestAssemblyAspects.TestData(0);
        }
        
        public void TestMultipleSimultaneousAspects()
        {
            Result = ReturnMultipleAspects(4).ToString();
        }

        [TestAssemblyAspects.ReturnValueTimesN(2)]  // Has order First, so its OnExit should be run last.
        [TestAssemblyAspects.ReturnValuePlusN(5)]
        int ReturnMultipleAspects(int n)
        {
            // Should be rewoven to:
            //      ((n * 4) + 5) * 2
            //      that is,
            //      8n + 10
            return n* 4;
        }
        
        public void TestGenericReturnOverwriteInt()
        {
            Result = ReturnGenericInt<int>().ToString();
        }

        [OverwriteReturnValueAspect(42)]
        T ReturnGenericInt<T>() => default(T);

        public void ExternTestGenericReturnOverwriteInt()
        {
            Result = ExternReturnGenericInt<int>().ToString();
        }

        [TestAssemblyAspects.OverwriteReturnValueAspect(42)]
        T ExternReturnGenericInt<T>() => default(T);

        public void TestGenericReturnOverwriteString()
        {
            Result = ReturnGenericString<string>().ToString();
        }

        [OverwriteReturnValueAspect("Overwritten")]
        T ReturnGenericString<T>() => default(T);

        public void ExternTestGenericReturnOverwriteString()
        {
            Result = ExternReturnGenericString<string>().ToString();
        }

        [TestAssemblyAspects.OverwriteReturnValueAspect("Overwritten")]
        T ExternReturnGenericString<T>() => default(T);

        public void TestGenericReturnOverwriteEnum()
        {
            Result = ReturnGenericEnum<TestAssemblyAspects.LongExampleEnum>().ToString();
        }

        [OverwriteReturnValueAspect(TestAssemblyAspects.LongExampleEnum.LongFifth)]
        T ReturnGenericEnum<T>() => default(T);

        public void ExternTestGenericReturnOverwriteEnum()
        {
            Result = ExternReturnGenericEnum<TestAssemblyAspects.LongExampleEnum>().ToString();
        }

        [TestAssemblyAspects.OverwriteReturnValueAspect(TestAssemblyAspects.LongExampleEnum.LongFifth)]
        T ExternReturnGenericEnum<T>() => default(T);

        public void TestGenericReturnOverwriteStruct()
        {
            Result = ReturnGenericStruct<TestAssemblyAspects.TestData>().ToString();
        }

        [OverwriteReturnValueWithTestDataAspect(42)]
        T ReturnGenericStruct<T>() => default(T);

        public void ExternTestGenericReturnOverwriteStruct()
        {
            Result = ExternReturnGenericStruct<TestAssemblyAspects.TestData>().ToString();
        }

        [TestAssemblyAspects.OverwriteReturnValueWithTestDataAspect(42)]
        T ExternReturnGenericStruct<T>() => default(T);
    }
}