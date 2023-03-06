namespace MethodBoundaryAspect.Fody
{
    /// <summary>
    /// Flow of weaved method
    /// create special method with AggressiveInlining
    /// clone old method into new method "executor"
    /// modify old method to call new method with required stuff before and after
    /// create methodExecutionArgs
    /// invoke OnEntry/OnExit/OnException
    /// </summary>
    class MethodWeaverLogic
    {
        //public object Executor(object param1, object param2, object param3)
        //{
        //    var args = new[] {param1, param2, param3};

        //    var methodExecutionArgs = new MethodExecutionArgs
        //    {
        //        Instance = this,
        //        Arguments = args,
        //        Method = MethodBase.GetCurrentMethod()
        //    };

        //    var aspect = new MyOnMethodBoundaryAspect();
        //    aspect.OnEntry(methodExecutionArgs);

        //    try
        //    {
        //        var returnValue = OriginalMethod(args[0], args[1], args[2]);
        //        methodExecutionArgs.ReturnValue = returnValue;
        //    }
        //    catch (Exception ex)
        //    {
        //        methodExecutionArgs.Exception = ex;
        //        aspect.OnException(methodExecutionArgs);
        //        throw;
        //    }

        //    aspect.OnExit(methodExecutionArgs);
        //    return methodExecutionArgs.ReturnValue;
        //}

        //public object OriginalMethod(object param1, object param2, object param3)
        //{
        //    return null;
        //}

        //public object ClonedMethod(object param1, object param2, object param3)
        //{
        //    return OriginalMethod(param1, param2, param3);
        //}

        //private class MyOnMethodBoundaryAspect : OnMethodBoundaryAspect
        //{
        //}
    }
}