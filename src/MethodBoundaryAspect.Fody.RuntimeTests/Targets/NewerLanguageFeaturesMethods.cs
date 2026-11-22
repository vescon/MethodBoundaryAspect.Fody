using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    /// <summary>
    /// An extension block (C# 14) is compiled to static methods of the class and to a nested type "&lt;G&gt;$..." with
    /// a copy of each member (with its attributes) whose body only throws. The properties exist only there.
    /// </summary>
    public static class FeatureExtensions
    {
        extension(string text)
        {
            [FeatureRecording]
            public int LengthPlus(int value) => text.Length + value;

            [FeatureRecording]
            public int DoubleLength => text.Length * 2;

            [FeatureRecording]
            public static string Twice(string value) => value + value;

            [DisableWeaving]
            [FeatureRecording]
            public int Unwoven => text.Length;
        }

        extension<T>(List<T> list)
        {
            [FeatureRecording]
            public int Capacity2
            {
                get => list.Capacity;
                set => list.Capacity = value;
            }
        }

        // the same property name for another receiver
        extension(int number)
        {
            public int DoubleLength => number * 2;
        }
    }

    /// <summary>A primary constructor (C# 12) and the "field" keyword (C# 14).</summary>
    public class FeatureCalculator(int factor)
    {
        [FeatureRecording]
        public int Multiply(int value) => value * factor;

        [FeatureRecording]
        public int NotNegative
        {
            get => field;
            set => field = value < 0 ? 0 : value;
        }

        [FeatureRecording]
        public static int Sum(params ReadOnlySpan<int> values)
        {
            var sum = 0;
            foreach (var value in values)
                sum += value;
            return sum;
        }

        [FeatureRecording]
        public static int ReadOnlyReference(ref readonly int value) => value + 1;

        // the name of a file-local type starts with "<", but it is user code
        public static int CallFileLocalType(int value) => new FeatureFileLocal().AddOne(value);
    }

    file class FeatureFileLocal
    {
        [FeatureRecording]
        public int AddOne(int value) => value + 1;
    }

    public readonly struct FeatureMoney
    {
        public FeatureMoney(int amount) => Amount = amount;

        public int Amount { get; }

        [FeatureRecording]
        public static FeatureMoney operator +(FeatureMoney left, FeatureMoney right) => new FeatureMoney(left.Amount + right.Amount);

        [FeatureRecording]
        public static FeatureMoney operator checked +(FeatureMoney left, FeatureMoney right) => new FeatureMoney(checked(left.Amount + right.Amount));

        public override string ToString() => Amount.ToString();
    }

#if NETCOREAPP
    public interface IFeatureStatic
    {
        [FeatureRecording]
        static virtual int Default(int value) => value + 1;

        static abstract int Abstract(int value);
    }

    public class FeatureStatic : IFeatureStatic
    {
        [FeatureRecording]
        public static int Abstract(int value) => value + 2;

        public static int CallDefault<T>(int value) where T : IFeatureStatic => T.Default(value);

        public static int CallAbstract<T>(int value) where T : IFeatureStatic => T.Abstract(value);
    }

    [InlineArray(3)]
    public struct FeatureBuffer
    {
        private int _first;
    }

    public static class FeatureRuntimeMethods
    {
        [FeatureRecording]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public static async ValueTask<int> Pooled(bool fail)
        {
            await Task.Yield();
            if (fail)
                throw new InvalidOperationException("failed");
            return 5;
        }

        [FeatureSwallow]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public static async ValueTask<int> PooledSwallowed()
        {
            await Task.Yield();
            throw new InvalidOperationException("failed");
        }

        [FeatureRecording]
        public static int SumBuffer(FeatureBuffer buffer)
        {
            var sum = 0;
            foreach (var value in buffer)
                sum += value;
            return sum;
        }

        [FeatureRecording]
        [UnmanagedCallersOnly]
        public static int NativeCallback(int value) => value + 1;

        public static unsafe int CallNativeCallback(int value)
        {
            delegate* unmanaged<int, int> callback = &NativeCallback;
            return callback(value);
        }
    }

#endif

#if NET9_0_OR_GREATER
    public static class FeatureAllowsRefStruct
    {
        [FeatureRecording]
        public static int Count<T>(T value, int other) where T : allows ref struct => other;
    }
#endif
}
