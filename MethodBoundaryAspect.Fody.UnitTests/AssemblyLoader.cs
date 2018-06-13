using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class AssemblyLoader : MarshalByRefObject
    {
        private Assembly _assembly;
        private AppDomain _domain;
        private string _assemblyPath;

        public void SetDomain(AppDomain domain)
        {
            _domain = domain;
        }

        public void Load(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
            _assembly = Assembly.UnsafeLoadFrom(assemblyPath);
        }

        public object InvokeMethod(TypeInfo typeInfo, string methodName, params object[] arguments)
        {
            return InvokeMethodInternal(typeInfo, methodName, false, arguments);
        }

        public object InvokeMethodSwallowException(TypeInfo typeInfo, string methodName, params object[] arguments)
        {
            return InvokeMethodInternal(typeInfo, methodName, true, arguments);
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
        public object CreateInstance(string name)
        {
            var type = _assembly.GetTypes().Single(x => x.Name == name);
            var instance = _domain.CreateInstanceFrom(_assemblyPath, type.FullName);
            return instance;
        }
        
        private object InvokeMethodInternal(
            TypeInfo typeInfo,
            string methodName,
            bool swallowException,
            params object[] arguments)
        {
            var type = _assembly.GetType(typeInfo.ClassName, true);
            if (type.IsGenericType)
            {
                var genericTypeArguments = typeInfo.GenericTypeParameterNames.Select(x => _assembly.GetType(x, true)).ToArray();
                type = type.MakeGenericType(genericTypeArguments);
            }

            var methodInfo = type.GetMethod(methodName);
            if (methodInfo == null)
                throw new MissingMethodException(
                    $"Method '{methodName}' in class '{typeInfo.ClassName}' in assembly '{_assembly.FullName}' not found.");

            // unwrap object handles
            arguments = arguments
                .Select(x => x is ObjectHandle handle ? handle.Unwrap() : x)
                .ToArray();

            if (methodInfo.IsGenericMethod)
            {
                var argumentTypes = arguments
                    .Select(x => x.GetType())
                    .ToArray();

                methodInfo = methodInfo.MakeGenericMethod(argumentTypes);
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

            var resultClass = _assembly.GetType(typeInfo.ClassName, true);
            var resultProperty = resultClass.GetProperty("Result");
            if (resultProperty == null)
                return returnValue;

            var resultValue = resultProperty.GetValue(instance, new object[0]);
            return resultValue;
        }
    }
}