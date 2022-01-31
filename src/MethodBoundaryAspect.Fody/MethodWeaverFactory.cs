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
            MethodInfoCompileTimeWeaver methodInfoCompileTimeWeaver)
        {
            var filteredAspects = from a in aspects
                                  let methods = GetUsedAspectMethods(a.AspectAttribute.AttributeType)
                                  where methods != AspectMethods.None
                                  select new { Aspect = a, Methods = methods };

            var asyncAttribute = method.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName.Equals(typeof(AsyncStateMachineAttribute).FullName));
            if (asyncAttribute == null)
            {
                var aspectList = filteredAspects.Select(a => new AspectData(a.Aspect, a.Methods, method, module)).ToList();
                return new MethodWeaver(module, method, aspectList, methodInfoCompileTimeWeaver);
            }

            var moveNextMethod = ((TypeDefinition)asyncAttribute.ConstructorArguments[0].Value).Methods.First(m => m.Name == "MoveNext");
            var aspectDatas = filteredAspects.Select(a => new AspectDataOnAsyncMethod(moveNextMethod, a.Aspect, a.Methods, method, module)).ToList<AspectData>();
            return new AsyncMethodWeaver(
                module,
                method,
                moveNextMethod,
                aspectDatas,
                methodInfoCompileTimeWeaver);
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
