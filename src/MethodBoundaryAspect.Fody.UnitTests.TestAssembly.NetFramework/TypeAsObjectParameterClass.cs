using System;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework
{
    public class TypeAsObjectParameterClass
    {
        public static string[] Result { get; set; }

        public string GetTypeName(string arg)
        {
            return GetTypeName2(Type.GetType(arg));
        }

        [TypeArrayTakingAspect(typeof(TypeAsObjectParameterClass))]
        public string GetTypeName2(Type type)
        {
            return type.ToString();
        }

        [TypeArrayTakingAspect("ExpectedResult")]
        public void GetTypeName3()
        {
        }
    }
}
