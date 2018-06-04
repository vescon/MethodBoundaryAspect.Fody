using System;
using System.Linq;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class AssemblyLoader : MarshalByRefObject
    {
        private Assembly _assembly;

        public void Load(string assemblyPath)
        {
            _assembly = Assembly.LoadFrom(assemblyPath);
        }

        public object InvokeMethodWithResultClass(
            string resultClassName,
            string className,
            string methodName,
            params object[] arguments)
        {
            return InvokeMethodWithResultClass(resultClassName, className, methodName, false, arguments);
        }

        public object InvokeMethod(string className, string methodName, params object[] arguments)
        {
            return InvokeMethodWithResultClass(className, className, methodName, arguments);
        }

        public object InvokeMethodSwallowException(string className, string methodName, params object[] arguments)
        {
            return InvokeMethodWithResultClass(className, className, methodName, true, arguments);
        }

        public object GetLastResult(string className)
        {
            var type = _assembly.GetType(className, true);

            var resultProperty = type.GetProperty("Result");
            var resultValue = resultProperty.GetValue(null, new object[0]);
            return resultValue;
        }

        /// <summary>
        /// Gets the requested type from the just created weaved assembly
        /// </summary>
        /// <param name="name">Name of the type to return.</param>
        /// <returns></returns>
        public Type GetTypeFromWeavedAssembly(string name)
        {
            return _assembly.GetTypes().FirstOrDefault(x => x.Name == name);
        }

        private object InvokeMethodWithResultClass(
            string resultClassName,
            string className,
            string methodName,
            bool swallowException,
            params object[] arguments)
        {
            var type = _assembly.GetType(className, true);
            var methodInfo = type.GetMethod(methodName);
            if (methodInfo == null)
                throw new MissingMethodException(
                    $"Method '{methodName}' in class '{className}' in assembly '{_assembly.FullName}' not found.");
            if (methodInfo.IsGenericMethod)
            {
                var types = arguments.Select(x => x.GetType()).ToArray();
                methodInfo = methodInfo.MakeGenericMethod(types);
            }

            var instance = Activator.CreateInstance(type);
            object returnValue = null;
            try
            {
                returnValue = methodInfo.Invoke(instance, arguments);
                //ignore return value of direct call
                //if (returnValue != null)
                //    return returnValue;
            }
            catch (Exception)
            {
                if (!swallowException)
                    throw;
            }

            var resultClass = _assembly.GetType(resultClassName, true);
            var resultProperty = resultClass.GetProperty("Result");
            if (resultProperty == null)
                return returnValue;

            var resultValue = resultProperty.GetValue(instance, new object[0]);
            return resultValue;
        }
    }
}