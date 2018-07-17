using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;
using System;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
{
    [AsyncAspect("second")]
    public class AsyncClass
    {
        public static object Result { [DisableWeaving]get; [DisableWeaving]set; }

        [DisableWeaving]
        static void TaskShouldHaveThrown(Task task)
        {
            var cw = task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    if (t.Exception == null)
                        Result = "Task had no exception";
                    else if (t.Exception.InnerExceptions == null)
                        Result = "Task exception had no inner exceptions";
                    else if (t.Exception.InnerExceptions.Count != 1)
                        Result = $"Task exception had {t.Exception.InnerExceptions.Count} inner exceptions.";
                }
                else
                    Result = "Task not faulted";
            });
            cw.Wait();

        }

        [DisableWeaving]
        public static void AttemptThrow()
        {
            var task = ThrowTask();
            TaskShouldHaveThrown(task);
        }
        
        [AsyncAspect("first")]
        public static async Task ThrowTask()
        {
            await Task.Delay(10);
            throw new Exception("An exception");
        }

        [DisableWeaving]
        public static void AttemptValue()
        {
            var t = Return4();
        }

        [AsyncAspect("first")]
        public static async Task<int> Return4()
        {
            await Task.Delay(10);
            return 4;
        }

        [DisableWeaving]
        public void AttemptInstance()
        {
            TaskShouldHaveThrown(ThrowInstance());
        }

        [AsyncAspect("first")]
        public async Task ThrowInstance()
        {
            await Task.Delay(10);
            throw new Exception("An exception");
        }

        [DisableWeaving]
        public void AttemptInstanceReturn()
        {
            var t = Return42();
        }

        [AsyncAspect("first")]
        public async Task<int> Return42()
        {
            await Task.Delay(10);
            return 42;
        }

        [DisableWeaving]
        public void AttemptThrowT()
        {
            TaskShouldHaveThrown(Throw<string>());
        }

        [AsyncAspect("first")]
        public async Task<T> Throw<T>()
        {
            await Task.Delay(10);
            throw new Exception("An exception");
        }

        [DisableWeaving]
        public void AttemptIntReturn()
        {
            var t = Return10<string>();
        }

        [AsyncAspect("first")]
        public async Task<int> Return10<T>()
        {
            await Task.Delay(10);
            return 10;
        }
    }

    public class AsyncClass<TClass> where TClass : struct
    {
        public static object Result { [DisableWeaving]get; [DisableWeaving]set; }

        [DisableWeaving]
        static void TaskShouldHaveThrown(Task task)
        {
            var cw = task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    if (t.Exception == null)
                        Result = "Task had no exception";
                    else if (t.Exception.InnerExceptions == null)
                        Result = "Task exception had no inner exceptions";
                    else if (t.Exception.InnerExceptions.Count != 1)
                        Result = $"Task exception had {t.Exception.InnerExceptions.Count} inner exceptions.";
                }
                else
                    Result = "Task not faulted";
            });
            cw.Wait();
        }

        [DisableWeaving]
        public void AttemptTClass() => TaskShouldHaveThrown(ThrowTClass());

        [AsyncTAspect("first")]
        public async Task<TClass> ThrowTClass()
        {
            await Task.Delay(10);
            throw new Exception("An exception");
        }

        [DisableWeaving]
        public void ReturnTClass()
        {
            var t = ReturnClass();
        }

        [AsyncTAspect("first")]
        public async Task<TClass> ReturnClass()
        {
            await Task.Delay(10);
            return (TClass)(object)(new Placeholder() { Value = 40 });
        }
        
        [DisableWeaving]
        public void TestReturnInt()
        {
            var t = ReturnInt();
        }

        [AsyncTAspect("first")]
        public async Task<int> ReturnInt()
        {
            await Task.Delay(10);
            return 50;
        }
    }
}
