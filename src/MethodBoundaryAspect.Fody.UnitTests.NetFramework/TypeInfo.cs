using System;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    [Serializable]
    public class TypeInfo
    {
        private TypeInfo(string className, string[] genericTypeParameterNames)
        {
            ClassName = className;
            GenericTypeParameterNames = genericTypeParameterNames;
        }

        public string ClassName { get; }
        public string[] GenericTypeParameterNames { get; }

        public static TypeInfo FromClassName(string className) => new TypeInfo(className, new string [] { });
        public static TypeInfo FromGenericClassName(string className, params string[] genericTypeParameterNames) => new TypeInfo(className, genericTypeParameterNames);
    }
}