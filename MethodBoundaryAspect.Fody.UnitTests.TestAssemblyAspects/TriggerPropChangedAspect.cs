using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Linq;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssemblyAspects
{
    public class TriggerPropChangedAspect : OnMethodBoundaryAspect
    {
        string _param;
        Type[] _types;

        public TriggerPropChangedAspect(string param)
        {
            _param = param;
        }

        public TriggerPropChangedAspect(params Type[] types)
        {
            _types = types;
        }
        
        public override void OnEntry(MethodExecutionArgs arg)
        {
            string value;
            if (_types == null)
                value = _param;
            else
                value = String.Join(";", _types.Select(t => t.FullName));

            arg.Method.DeclaringType.GetMethod("set_Result", BindingFlags.Static | BindingFlags.Public)
                    .Invoke(null, new object[] { value });
        }
    }
}
