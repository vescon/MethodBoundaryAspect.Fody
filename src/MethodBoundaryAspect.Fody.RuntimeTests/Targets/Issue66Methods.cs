using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// async methods without await are part of the reproductions
#pragma warning disable CS1998

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    /// <summary>
    /// Code shapes from https://github.com/vescon/MethodBoundaryAspect.Fody/issues/66.
    /// Task.Yield() forces the state machine to suspend and resume.
    /// </summary>
    [Issue66OnExceptionAspect]
    public class Issue66Methods
    {
        public static async Task DoNothing()
        {
        }

        public static async Task DemoBugAsync(int value)
        {
            if (value > 0)
            {
                await DoNothing();
            }
        }

        public async Task Invoke(object context)
        {
        }

        public async Task TryFinallyAroundAwait(List<string> log)
        {
            try
            {
                await new Issue66Methods().InstanceDoNothing();
            }
            finally
            {
                log.Add("finally");
            }
        }

        public async Task InstanceDoNothing()
        {
        }

        public async Task IfWithYield(bool flag)
        {
            if (flag)
            {
                await Task.Yield();
            }
        }

        public async Task<int> EarlyReturns(int value)
        {
            if (value < 0)
                return -1;

            await Task.Yield();

            if (value == 0)
                return 0;

            await Task.Yield();
            return value * 2;
        }

        public async Task<int> Loop(int count, CancellationToken ct)
        {
            var sum = 0;
            for (var i = 0; i < count; i++)
            {
                if (ct.IsCancellationRequested)
                    break;

                await Task.Yield();
                sum += i;
            }

            return sum;
        }

        public async Task<int> WhileWithContinue(int count)
        {
            var sum = 0;
            var i = 0;
            while (i < count)
            {
                i++;
                if (i % 2 == 0)
                    continue;

                await Task.Yield();
                sum += i;
            }

            return sum;
        }

        public async Task<string> Switch(int value)
        {
            switch (value)
            {
                case 0:
                    await Task.Yield();
                    return "zero";
                case 1:
                    return "one";
                case 2:
                    await Task.Yield();
                    goto case 1;
                default:
                    await Task.Yield();
                    return "many";
            }
        }

        public async Task<string> AwaitInCatchAndFinally(bool shouldThrow, List<string> log)
        {
            try
            {
                await Task.Yield();
                if (shouldThrow)
                    throw new InvalidOperationException("caught");

                return "try";
            }
            catch (InvalidOperationException e)
            {
                await Task.Yield();
                return e.Message;
            }
            finally
            {
                await Task.Yield();
                log.Add("finally");
            }
        }

        public async Task<int> NestedTry(int value)
        {
            try
            {
                try
                {
                    await Task.Yield();
                    if (value == 0)
                        throw new DivideByZeroException("inner");

                    return 100 / value;
                }
                catch (DivideByZeroException) when (value == 0)
                {
                    await Task.Yield();
                    throw new InvalidOperationException("outer");
                }
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
        }

        public async Task<long> UsingDeclaration()
        {
            using var stream = new MemoryStream();
            await stream.WriteAsync(new byte[] { 1, 2, 3 }, 0, 3);
            await Task.Yield();
            return stream.Length;
        }

        public async Task ThrowAfterYield(bool shouldThrow)
        {
            await Task.Yield();
            if (shouldThrow)
                throw new InvalidOperationException("after yield");
        }

        public async Task<int> ThrowInsideIf(int value)
        {
            if (value > 0)
            {
                await Task.Yield();
                throw new InvalidOperationException("inside if");
            }

            return value;
        }

        public async void AsyncVoidWithIf(bool flag, TaskCompletionSource<bool> done)
        {
            if (flag)
            {
                await Task.Yield();
            }

            done.SetResult(flag);
        }

        public async Task<T> Generic<T>(T value, bool flag)
        {
            if (flag)
            {
                await Task.Yield();
            }

            return value;
        }

        public async Task<int> ManyAwaits(int value)
        {
            // a long method body, so that branches in it can't be short branches
            var result = value;
            if (value > 0)
            {
                await Task.Yield(); result += 1;
                await Task.Yield(); result += 2;
                await Task.Yield(); result += 3;
                await Task.Yield(); result += 4;
                await Task.Yield(); result += 5;
                await Task.Yield(); result += 6;
                await Task.Yield(); result += 7;
                await Task.Yield(); result += 8;
                await Task.Yield(); result += 9;
                await Task.Yield(); result += 10;
            }

            return result;
        }
    }

    /// <summary>
    /// Like <see cref="Issue66Methods"/>, but with multiple aspects which use the MethodExecutionTag.
    /// </summary>
    [Issue66TimingAspect]
    [Issue66OnExceptionAspect]
    public class Issue66TimedMethods
    {
        public async Task<int> Loop(int count, CancellationToken ct)
        {
            var sum = 0;
            for (var i = 0; i < count; i++)
            {
                if (ct.IsCancellationRequested)
                    return -1;

                await Task.Delay(1, ct);
                sum += i;
            }

            return sum;
        }

        public async Task IfWithYield(bool flag)
        {
            if (flag)
            {
                await Task.Yield();
            }
        }

        public async Task TryFinally(List<string> log)
        {
            try
            {
                await Task.Yield();
            }
            finally
            {
                log.Add("finally");
            }
        }

        public async Task ThrowAfterYield()
        {
            await Task.Yield();
            throw new InvalidOperationException("after yield");
        }

        public IEnumerable<int> Iterate(int count)
        {
            for (var i = 0; i < count; i++)
                yield return i;
        }
    }
}
