using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.External.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class OverwriteReturnValues
    {
        public static string Result { get; set; }

        public void TestReturnString()
        {
            Result = ReturnString();
        }

        [Aspects.OverwriteReturnValueAspect("Overwritten")]
        string ReturnString()
        {
            return "not overwritten";
        }

        public void ExternTestReturnString()
        {
            Result = ExternReturnString();
        }

        [OverwriteReturnValueAspect("Overwritten")]
        string ExternReturnString()
        {
            return "not overwritten";
        }

        public void TestReturnInt()
        {
            Result = ReturnInt().ToString();
        }

        [Aspects.OverwriteReturnValueAspect(42)]
        int ReturnInt()
        {
            return -1;
        }

        public void ExternTestReturnInt()
        {
            Result = ExternReturnInt().ToString();
        }

        [OverwriteReturnValueAspect(42)]
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

        [Aspects.OverwriteReturnValueAspect(TestValues.Overwritten)]
        TestValues ReturnEnum()
        {
            return TestValues.NotOverwritten;
        }
        
        public void ExternTestReturnEnum()
        {
            Result = ExternReturnEnum().ToString();
        }

        [OverwriteReturnValueAspect(TestValues.Overwritten)]
        TestValues ExternReturnEnum()
        {
            return TestValues.NotOverwritten;
        }
        
        public void TestWrongType()
        {
            ReturnWrongType();
        }

        [Aspects.OverwriteReturnValueAspect("not an integer")]
        int ReturnWrongType()
        {
            return -1;
        }

        public void ExternTestWrongType()
        {
            ExternReturnWrongType();
        }

        [OverwriteReturnValueAspect("not an integer")]
        int ExternReturnWrongType()
        {
            return -1;
        }

        public void TestTry()
        {
            Result = ReturnFromTry();
        }

        [Aspects.OverwriteReturnValueAspect("Overwritten")]
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

        [OverwriteReturnValueAspect("Overwritten")]
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

        [Aspects.OverwriteReturnValueAspect("Overwritten")]
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

        [OverwriteReturnValueAspect("Overwritten")]
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

        [Aspects.OverwriteReturnValueAspect(new string[] { "one", "two" })]
        IEnumerable<object> ReturnCovariantGeneric()
        {
            return new object[0];
        }

        public void ExternTestCovariantGeneric()
        {
            Result = ExternReturnCovariantGeneric().GetType().FullName;
        }

        [OverwriteReturnValueAspect(new string[] { "one", "two" })]
        IEnumerable<object> ExternReturnCovariantGeneric()
        {
            return new object[0];
        }

        public void TestCustomStruct()
        {
            Result = ReturnCustomStruct().ToString();
        }

        [Aspects.OverwriteReturnValueWithTestDataAspect(42)]
        TestData ReturnCustomStruct()
        {
            return new TestData(0);
        }

        public void ExternTestCustomStruct()
        {
            Result = ExternReturnCustomStruct().ToString();
        }

        [OverwriteReturnValueWithTestDataAspect(42)]
        TestData ExternReturnCustomStruct()
        {
            return new TestData(0);
        }
        
        public void TestMultipleSimultaneousAspects()
        {
            Result = ReturnMultipleAspects(4).ToString();
        }

        [ReturnValueTimesN(2)]  // Has order First, so its OnExit should be run last.
        [ReturnValuePlusN(5)]
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

        [Aspects.OverwriteReturnValueAspect(42)]
        T ReturnGenericInt<T>() => default(T);

        public void ExternTestGenericReturnOverwriteInt()
        {
            Result = ExternReturnGenericInt<int>().ToString();
        }

        [OverwriteReturnValueAspect(42)]
        T ExternReturnGenericInt<T>() => default(T);

        public void TestGenericReturnOverwriteString()
        {
            Result = ReturnGenericString<string>().ToString();
        }

        [Aspects.OverwriteReturnValueAspect("Overwritten")]
        T ReturnGenericString<T>() => default(T);

        public void ExternTestGenericReturnOverwriteString()
        {
            Result = ExternReturnGenericString<string>().ToString();
        }

        [OverwriteReturnValueAspect("Overwritten")]
        T ExternReturnGenericString<T>() => default(T);

        public void TestGenericReturnOverwriteEnum()
        {
            Result = ReturnGenericEnum<LongExampleEnum>().ToString();
        }

        [Aspects.OverwriteReturnValueAspect(LongExampleEnum.LongFifth)]
        T ReturnGenericEnum<T>() => default(T);

        public void ExternTestGenericReturnOverwriteEnum()
        {
            Result = ExternReturnGenericEnum<LongExampleEnum>().ToString();
        }

        [OverwriteReturnValueAspect(LongExampleEnum.LongFifth)]
        T ExternReturnGenericEnum<T>() => default(T);

        public void TestGenericReturnOverwriteStruct()
        {
            Result = ReturnGenericStruct<TestData>().ToString();
        }

        [Aspects.OverwriteReturnValueWithTestDataAspect(42)]
        T ReturnGenericStruct<T>() => default(T);

        public void ExternTestGenericReturnOverwriteStruct()
        {
            Result = ExternReturnGenericStruct<TestData>().ToString();
        }

        [OverwriteReturnValueWithTestDataAspect(42)]
        T ExternReturnGenericStruct<T>() => default(T);
    }
}