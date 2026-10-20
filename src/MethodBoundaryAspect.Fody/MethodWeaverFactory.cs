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
            AspectInstanceCompileTimeWeaver aspectInstances,
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

            // nothing to weave: the method stays as it is, also if it is of a structure the weavers don't know
            if (filteredAspects.Count == 0)
                return new MethodWeaver(module, method, new List<AspectData>(), methodInfoCompileTimeWeaver, executionArgsUsage);

            var asyncAttribute = method.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName.Equals(typeof(AsyncStateMachineAttribute).FullName));
            if (asyncAttribute == null)
            {
                var aspectList = filteredAspects.Select(a => new AspectData(a.Aspect, a.Methods, method, module, aspectInstances.GetGetter(a.Aspect))).ToList();
                return new MethodWeaver(module, method, aspectList, methodInfoCompileTimeWeaver, executionArgsUsage);
            }

            var moveNextMethod = ((TypeDefinition)asyncAttribute.ConstructorArguments[0].Value).Methods.FirstOrDefault(m => m.Name == "MoveNext");
            if (moveNextMethod == null)
            {
                unsupportedReason = "the state machine has no MoveNext method";
                return null;
            }

            var weavesOnException = filteredAspects.Any(a => (a.Methods & AspectMethods.OnException) != 0);
            unsupportedReason = AsyncMethodWeaver.GetUnsupportedReason(method, moveNextMethod, weavesOnException);
            if (unsupportedReason != null)
                return null;

            var aspectDatas = filteredAspects.Select(a => new AspectDataOnAsyncMethod(moveNextMethod, a.Aspect, a.Methods, method, module, aspectInstances.GetGetter(a.Aspect))).ToList<AspectData>();
            return new AsyncMethodWeaver(
                module,
                method,
                moveNextMethod,
                aspectDatas,
                methodInfoCompileTimeWeaver,
                executionArgsUsage);
        }

        // the aspect methods of an aspect type, determined once for all methods it is applied to. Keyed by the type
        // definition, which belongs to the module that was read (another module has other definitions).
        private static readonly ConditionalWeakTable<TypeDefinition, object> AspectMethodsCache =
            new ConditionalWeakTable<TypeDefinition, object>();

        static AspectMethods GetUsedAspectMethods(TypeReference aspectType)
        {
            var key = aspectType.Resolve();
            if (key == null)
                return DetermineUsedAspectMethods(aspectType);

            return (AspectMethods)AspectMethodsCache.GetValue(key, _ => DetermineUsedAspectMethods(aspectType));
        }

        static AspectMethods DetermineUsedAspectMethods(TypeReference aspectTypeDefinition)
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
