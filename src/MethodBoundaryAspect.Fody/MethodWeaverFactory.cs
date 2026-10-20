using MethodBoundaryAspect.Fody.Ordering;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MethodBoundaryAspect.Fody
{
    public static class MethodWeaverFactory
    {
        public static MethodWeaver MakeWeaver(ModuleDefinition module,
            MethodDefinition method,
            IEnumerable<AspectInfo> aspects,
            MethodInfoCompileTimeWeaver methodInfoCompileTimeWeaver,
            ExecutionArgsUsageAnalyzer executionArgsUsageAnalyzer,
            out string unsupportedReason)
        {
            unsupportedReason = null;

            var filteredAspects = (from a in aspects
                                  let methods = GetUsedAspectMethods(a.AspectAttribute.AttributeType)
                                  where methods != AspectMethods.None
                                  select new { Aspect = a, Methods = methods })
                .ToList();

            // the executor method would not get the variable arguments, the method would silently lose them
            if (filteredAspects.Count > 0 && method.CallingConvention == MethodCallingConvention.VarArg)
            {
                unsupportedReason = "methods with a variable argument list (__arglist) are not supported";
                return null;
            }

            // all aspects of a method share one MethodExecutionArgs instance
            var executionArgsUsage = filteredAspects.Aggregate(
                ExecutionArgsUsage.None,
                (usage, a) => usage | executionArgsUsageAnalyzer.GetUsage(a.Aspect.AspectTypeDefinition));

            var asyncAttribute = method.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName.Equals(typeof(AsyncStateMachineAttribute).FullName));
            if (asyncAttribute == null)
            {
                var aspectList = filteredAspects.Select(a => new AspectData(a.Aspect, a.Methods, method, module)).ToList();
                return new MethodWeaver(module, method, aspectList, methodInfoCompileTimeWeaver, executionArgsUsage);
            }

            var moveNextMethod = ((TypeDefinition)asyncAttribute.ConstructorArguments[0].Value).Methods.FirstOrDefault(m => m.Name == "MoveNext");
            if (moveNextMethod == null)
            {
                unsupportedReason = "the state machine has no MoveNext method";
                return null;
            }

            var weavesOnException = filteredAspects.Any(a => (a.Methods & AspectMethods.OnException) != 0);
            // methods without any aspect method to weave are not touched, nothing to check
            if (filteredAspects.Count > 0)
            {
                unsupportedReason = AsyncMethodWeaver.GetUnsupportedReason(method, moveNextMethod, weavesOnException);
                if (unsupportedReason != null)
                    return null;
            }

            var aspectDatas = filteredAspects.Select(a => new AspectDataOnAsyncMethod(moveNextMethod, a.Aspect, a.Methods, method, module)).ToList<AspectData>();
            return new AsyncMethodWeaver(
                module,
                method,
                moveNextMethod,
                aspectDatas,
                methodInfoCompileTimeWeaver,
                executionArgsUsage);
        }

        static AspectMethods GetUsedAspectMethods(TypeReference aspectTypeDefinition)
        {
            var overloadedMethods = new Dictionary<string, MethodDefinition>();

            var currentType = aspectTypeDefinition;
            do
            {
                var typeDefinition = currentType.Resolve();
                var methods = typeDefinition.Methods
                    .Where(AspectMethodCriteria.MatchesSignature)
                    .ToList();
                foreach (var method in methods)
                {
                    if (overloadedMethods.ContainsKey(method.Name))
                        continue;

                    overloadedMethods.Add(method.Name, method);
                }

                currentType = typeDefinition.BaseType;
            } while (currentType.FullName != AttributeFullNames.OnMethodBoundaryAspect);

            var aspectMethods = AspectMethods.None;
            if (overloadedMethods.ContainsKey(AspectMethodCriteria.OnEntryMethodName))
                aspectMethods |= AspectMethods.OnEntry;
            if (overloadedMethods.ContainsKey(AspectMethodCriteria.OnExitMethodName))
                aspectMethods |= AspectMethods.OnExit;
            if (overloadedMethods.ContainsKey(AspectMethodCriteria.OnExceptionMethodName))
                aspectMethods |= AspectMethods.OnException;
            return aspectMethods;
        }
    }
}
