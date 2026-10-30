using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Aspects
{
    /// <summary>
    /// Records its label and the method in OnEntry. It can be applied several times, e.g. to the assembly, the class
    /// and the method. Tests using it belong to the collection <c>[Collection(nameof(AssemblyLevelAttribute))]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class AssemblyLevelAttribute : OnMethodBoundaryAspect
    {
        public static readonly List<string> Calls = new List<string>();

        private readonly string _label;

        public AssemblyLevelAttribute(string label)
        {
            _label = label;
        }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            lock (Calls)
                Calls.Add($"{_label} {arg.Method.Name}");
        }
    }
}
