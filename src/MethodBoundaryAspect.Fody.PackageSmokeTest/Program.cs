using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.Attributes;

public class RecordAttribute : OnMethodBoundaryAspect
{
    public static readonly List<string> Calls = new List<string>();

    public override void OnEntry(MethodExecutionArgs arg) => Calls.Add("entry " + arg.Method.Name + " " + string.Join(",", arg.Arguments));

    public override void OnExit(MethodExecutionArgs arg) => Calls.Add("exit " + arg.Method.Name + " " + arg.ReturnValue);
}

public static class Program
{
    [Record]
    private static int Add(int a, int b) => a + b;

    [Record]
    private static async Task<int> AddAsync(int a, int b)
    {
        await Task.Yield();
        return a + b;
    }

    public static int Main()
    {
        var sum = Add(1, 2);
        var asyncSum = AddAsync(3, 4).GetAwaiter().GetResult();

        var expected = new[] { "entry Add 1,2", "exit Add 3", "entry AddAsync 3,4", "exit AddAsync System.Threading.Tasks.Task`1[System.Int32]" };
        var ok = sum == 3 && asyncSum == 7 && string.Join("|", RecordAttribute.Calls).StartsWith(string.Join("|", expected[0], expected[1], expected[2]));
        Console.WriteLine(ok ? "OK" : "FAILED: " + string.Join("|", RecordAttribute.Calls));
        return ok ? 0 : 1;
    }
}
