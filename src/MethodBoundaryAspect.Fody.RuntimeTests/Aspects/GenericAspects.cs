using MethodBoundaryAspect.Fody.Attributes;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public static class GenericAspectLog
    {
        public static readonly List<string> Calls = new List<string>();

        public static void Add(string call)
        {
            lock (Calls)
                Calls.Add(call);
        }
    }

    /// <summary>
    /// A generic aspect (C# 11 generic attribute), applied e.g. as [GenericRecordingAspect&lt;int&gt;("prefix")].
    /// </summary>
    public class GenericRecordingAspect<T> : OnMethodBoundaryAspect
    {
        private readonly string _prefix;

        public string Label { get; set; }

        public int Number;

        public GenericRecordingAspect(string prefix)
        {
            _prefix = prefix;
        }

        public override void OnEntry(MethodExecutionArgs arg) =>
            GenericAspectLog.Add($"{_prefix} {typeof(T).Name} OnEntry {arg.Method.Name} Label={Label} Number={Number}");

        public override void OnExit(MethodExecutionArgs arg) =>
            GenericAspectLog.Add($"{_prefix} {typeof(T).Name} OnExit {arg.Method.Name} {arg.ReturnValue}");

        public override void OnException(MethodExecutionArgs arg) =>
            GenericAspectLog.Add($"{_prefix} {typeof(T).Name} OnException {arg.Method.Name} {arg.Exception.GetType().Name}");
    }

    public class GenericBaseAspect<T> : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg) =>
            GenericAspectLog.Add($"{typeof(T).Name} OnEntry {arg.Method.Name}");
    }

    /// <summary>
    /// A non generic aspect derived from a generic base aspect.
    /// </summary>
    public class DerivedFromGenericAspect : GenericBaseAspect<string>
    {
    }
}

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// A generic aspect with a property of the type parameter, set where it is applied: [GenericValue&lt;int&gt;(Value = 5)].
    /// </summary>
    public class GenericValueAttribute<T> : OnMethodBoundaryAspect
    {
        public T Value { get; set; }

        public T ValueField;

        public override void OnEntry(MethodExecutionArgs arg) =>
            GenericAspectLog.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Value<{0}> {1} {2}", typeof(T).Name, Value, ValueField));
    }
}

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// A generic aspect with a constructor parameter of the type parameter: [GenericCtorValue&lt;int&gt;(5)].
    /// </summary>
    public class GenericCtorValueAttribute<T> : OnMethodBoundaryAspect
    {
        private readonly string _label;
        private readonly T _value;

        public GenericCtorValueAttribute(T value)
            : this("none", value)
        {
        }

        public GenericCtorValueAttribute(string label, T value)
        {
            _label = label;
            _value = value;
        }

        public override void OnEntry(MethodExecutionArgs arg) =>
            GenericAspectLog.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "CtorValue<{0}> {1} {2}", typeof(T).Name, _label, _value));
    }
}
