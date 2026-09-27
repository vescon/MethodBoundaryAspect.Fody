using System;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class ChangeInputArgumentAspectMethods
    {
        public static object Result { get; set; }

        [ChangeInputArgumentAspect(Index = 0, Value = 42)]
        public static void StaticMethodCallFirstArgument(object arg1)
        {
            Result = arg1;
        }

        [ChangeInputArgumentAspect(Index = 1, Value = "42")]
        public void InstanceMethodCallSecondArgument(object arg1, object arg2)
        {
            Result = arg2;
        }
        
        [ChangeFirstInputArgumentToIntArrayAspect]
        public void InstanceMethodCallFirstArgumentArray(ref object arg1)
        {
            Result = arg1;
        }

        [ChangeInputArgumentsNotAllowedAspect]
        public void InstanceMethodCallNotAllowedChangingInputArguments(object arg1)
        {
            Result = arg1;
        }

        [AllowChangingInputArgumentsOnlyOnEntryAspect]
        public bool InstanceMethodWithOutGuidArguments(out Guid guid1, out Guid guid2)
        {
            guid1 = Guid.NewGuid();
            guid2 = Guid.NewGuid();
            Result = new[] { guid1, guid2 };
            return true;
        }

        [AllowChangingInputArgumentsOnlyOnEntryAspect]
        public void InstanceMethodWithOutIntArgument(out int value)
        {
            value = 42;
            Result = value;
        }

        [CaptureArgumentsOnExitAspect]
        public void InstanceMethodWithRefAndOutArgumentsCapturedOnExit(string input, ref Guid guid, out int value, out string text)
        {
            guid = new Guid("5b9a1f3e-4c2d-4e8f-9a7b-1c2d3e4f5a6b");
            value = 42;
            text = input + "42";
        }

        [SetFirstOutArgumentAndReturnAspect(Value = 42)]
        public bool InstanceMethodWithOutIntArgumentAndEarlyReturn(out int value)
        {
            value = 0;
            Result = "method body should not be executed";
            return false;
        }
    }
}