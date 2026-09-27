Imports MethodBoundaryAspect.Fody.Attributes

Public Class VbAsyncAspect
    Inherits OnMethodBoundaryAspect

    Public Overrides Sub OnEntry(arg As MethodExecutionArgs)
        VbAsyncClass.Result &= "[OnEntry]"
    End Sub

    Public Overrides Sub OnExit(arg As MethodExecutionArgs)
        VbAsyncClass.Result &= "[OnExit]"
    End Sub

    Public Overrides Sub OnException(arg As MethodExecutionArgs)
        VbAsyncClass.Result &= "[OnException:" & arg.Exception.Message & "]"
    End Sub
End Class

''' <summary>
''' Swallows the exception and replaces the result, exercising the FlowBehavior.Return path in MoveNext.
''' </summary>
Public Class VbAsyncSwallowExceptionAspect
    Inherits OnMethodBoundaryAspect

    Public Overrides Sub OnException(arg As MethodExecutionArgs)
        VbAsyncClass.Result &= "[OnException:" & arg.Exception.Message & "]"
        arg.FlowBehavior = FlowBehavior.Return
        arg.ReturnValue = 42
    End Sub
End Class
