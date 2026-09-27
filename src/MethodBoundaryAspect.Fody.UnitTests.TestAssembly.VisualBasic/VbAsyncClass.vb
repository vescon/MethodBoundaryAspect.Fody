Imports System.Threading.Tasks

Public Class VbAsyncClass

    Public Shared Property Result As String

    Public Sub AttemptThrowTask()
        Result = ""
        Try
            ThrowTask().Wait()
        Catch ex As AggregateException
            Result &= "[Caught:" & ex.InnerException.Message & "]"
        End Try
    End Sub

    Public Sub AttemptThrowTaskOfT()
        Result = ""
        Try
            ThrowTaskOfT().Wait()
        Catch ex As AggregateException
            Result &= "[Caught:" & ex.InnerException.Message & "]"
        End Try
    End Sub

    Public Sub AttemptReturnValue()
        Result = ""
        Dim value = ReturnValue().Result
        Result &= "[Value:" & value & "]"
    End Sub

    Public Sub AttemptSwallowException()
        Result = ""
        Dim value = SwallowException().Result
        Result &= "[Value:" & value & "]"
    End Sub

    Public Sub AttemptThrowSync()
        Result = ""
        Try
            ThrowSync()
        Catch ex As InvalidOperationException
            Result &= "[Caught:" & ex.Message & "]"
        End Try
    End Sub

    <VbAsyncAspect>
    Public Async Function ThrowTask() As Task
        Await Task.Delay(1)
        Throw New InvalidOperationException("VB exception")
    End Function

    <VbAsyncAspect>
    Public Async Function ThrowTaskOfT() As Task(Of Integer)
        Await Task.Delay(1)
        Throw New InvalidOperationException("VB exception")
    End Function

    <VbAsyncAspect>
    Public Async Function ReturnValue() As Task(Of Integer)
        Await Task.Delay(1)
        Return 7
    End Function

    <VbAsyncSwallowExceptionAspect>
    Public Async Function SwallowException() As Task(Of Integer)
        Await Task.Delay(1)
        Throw New InvalidOperationException("VB exception")
    End Function

    <VbAsyncAspect>
    Public Sub ThrowSync()
        Throw New InvalidOperationException("VB exception")
    End Sub
End Class
