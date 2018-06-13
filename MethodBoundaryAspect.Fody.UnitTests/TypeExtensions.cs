using System;
using System.Linq;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public static class TypeExtensions
    {
        public static TypeInfo TypeInfo(this Type type)
        {
            return UnitTests.TypeInfo.FromClassName(type.FullName);
        }

        public static TypeInfo TypeInfoWithGenericParameters(this Type type, params Type[] genericTypeParameters)
        {
            var genericTypeParameterNames = genericTypeParameters.Select(x => x.FullName).ToArray();

            return UnitTests.TypeInfo.FromGenericClassName(type.FullName, genericTypeParameterNames);
        }
    }
}