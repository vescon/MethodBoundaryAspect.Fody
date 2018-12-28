using System;
using System.Linq;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects
{
    public class IntArrayTakingAspect : OnMethodBoundaryAspect
    {
        int[] _values;
        
        public IntArrayTakingAspect(params int[] values)
        {
            _values = values;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            ArrayTakingAspectMethod.Result = _values;
        }
    }

    public class TypeArrayTakingAspect : OnMethodBoundaryAspect
    {
        Type[] _types;

        public TypeArrayTakingAspect(params Type[] types)
        {
            _types = types;
        }

        string _singleValue;

        public TypeArrayTakingAspect(string singleValue)
        {
            _singleValue = singleValue;
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            if (_types != null)
                TypeAsObjectParameterClass.Result = _types.Select(t => t.ToString()).ToArray();
            else
                TypeAsObjectParameterClass.Result = new[] { _singleValue };
        }
    }
}