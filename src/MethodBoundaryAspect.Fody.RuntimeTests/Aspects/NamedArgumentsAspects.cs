using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    public abstract class NamedArgumentsBaseAspect : OnMethodBoundaryAspect
    {
        public static string LastResult;

        public string BaseField;

        public int BaseProperty { get; set; }
    }

    /// <summary>
    /// Field and property set as named arguments are declared in the base class.
    /// </summary>
    public class InheritedNamedArgumentsAspect : NamedArgumentsBaseAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            LastResult = $"BaseField: {BaseField}, BaseProperty: {BaseProperty}";
        }
    }

    public enum ArrayArgumentKind
    {
        First,
        Second
    }

    public class ArrayArgumentsAspect : NamedArgumentsBaseAspect
    {
        private readonly string[] _ctorValues;
        private readonly Type _ctorType;

        public ArrayArgumentsAspect()
        {
        }

        public ArrayArgumentsAspect(string[] ctorValues, Type ctorType)
        {
            _ctorValues = ctorValues;
            _ctorType = ctorType;
        }

        public string[] Tags { get; set; }

        public int[] Numbers { get; set; }

        public ArrayArgumentKind[] Kinds { get; set; }

        public Type[] Types { get; set; }

        public string[] FieldTags;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            LastResult = string.Join("; ",
                $"Ctor: {Format(_ctorValues)} {_ctorType?.Name ?? "null"}",
                $"Tags: {Format(Tags)}",
                $"Numbers: {Format(Numbers)}",
                $"Kinds: {Format(Kinds)}",
                $"Types: {Format(Types?.Select(t => t.Name))}",
                $"FieldTags: {Format(FieldTags)}");
        }

        private static string Format<T>(IEnumerable<T> values) =>
            values == null ? "null" : "[" + string.Join(", ", values) + "]";
    }

    public class NamespaceFilterRecordingAspect : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add(arg.Method.Name);
        }
    }
}
