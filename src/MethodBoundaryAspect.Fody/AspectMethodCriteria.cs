using System.Linq;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody
{
    public static class AspectMethodCriteria
    {
        public static readonly string OnEntryMethodName = "OnEntry";
        public static readonly string OnExitMethodName = "OnExit";
        public static readonly string OnExceptionMethodName = "OnException";

        public static bool MatchesSignature(MethodDefinition method)
        {
            return method.IsVirtual
                   && !method.HasGenericParameters
                   && method.Parameters.Count == 1
                   && method.Parameters.Single().ParameterType.FullName ==
                   "MethodBoundaryAspect.Fody.Attributes.MethodExecutionArgs";
        }

        public static bool IsOnEntryMethod(MethodDefinition method)
        {
            return method.Name == OnEntryMethodName && MatchesSignature(method);
        }

        public static bool IsOnExitMethod(MethodDefinition method)
        {
            return method.Name == OnExitMethodName && MatchesSignature(method);
        }

        public static bool IsOnExceptionMethod(MethodDefinition method)
        {
            return method.Name == OnExceptionMethodName && MatchesSignature(method);
        }
    }
}