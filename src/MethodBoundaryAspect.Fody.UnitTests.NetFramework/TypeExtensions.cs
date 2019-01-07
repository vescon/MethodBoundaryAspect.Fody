using System;
using System.Linq;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public static class TypeExtensions
    {
        public static TypeInfo TypeInfo(this Type type)
        {
            return NetFramework.TypeInfo.FromClassName(type.FullName);
        }

        public static TypeInfo TypeInfoWithGenericParameters(this Type type, params Type[] genericTypeParameters)
        {
            var genericTypeParameterNames = genericTypeParameters.Select(x => x.FullName).ToArray();

            return NetFramework.TypeInfo.FromGenericClassName(type.FullName, genericTypeParameterNames);
        }
    }
}