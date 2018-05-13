using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly
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
