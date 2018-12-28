using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetFramework;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework
{
    public class GenericTestBase : MethodBoundaryAspectNetFrameworkTestBase
    {
        protected static IEnumerable<object[]> GetMethodNames(Type typeToSearch)
        {
            return typeToSearch.GetMethods()
                    .Where(m => m.GetCustomAttributes<AddLogs>().Any())
                    .Select(m => new object[] { m.Name });
        }

        protected string GetExpectedString(Type type)
        {
            if (type == null || type.FullName == typeof(void).FullName)
                return String.Empty;
            if (type.IsByRef)
                return GetExpectedString(type.GetElementType());
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
                return new Regex(Regex.Escape("IList")).Replace(type.ToString(), "List", 1);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                return new Regex(Regex.Escape("IDictionary")).Replace(type.ToString(), "Dictionary", 1);
            return type.ToString().Trim('&');
        }

        protected Type OpenClassType;
        protected Type ClosedClassType;
        protected MethodInfo ClosedMethod;

        protected void SetClosedMethod(string methodName, params Type[] tArgs)
        {
            ClosedMethod = ClosedClassType.GetMethod(methodName);
            ClosedMethod = ClosedMethod.GetGenericMethodDefinition().MakeGenericMethod(tArgs);
        }

        protected void SetClosedMethod(string methodName)
        {
            ClosedMethod = ClosedClassType.GetMethod(methodName);
            if (ClosedMethod.IsGenericMethod)
            {
                var genericParameters = ClosedMethod.GetGenericArguments();
                if (genericParameters.Length == 1)
                    ClosedMethod = ClosedMethod.GetGenericMethodDefinition().MakeGenericMethod(typeof(MethodDisposable));
                else
                    ClosedMethod = ClosedMethod.GetGenericMethodDefinition().MakeGenericMethod(typeof(MethodDisposable), typeof(List<MethodDisposable[]>));
            }
        }

        protected string ExpectedReturnTypeString() => GetExpectedString(ClosedMethod.ReturnType);

        protected string ExpectedArgTypeString() => GetExpectedString(ClosedMethod.GetParameters().FirstOrDefault()?.ParameterType);

        protected object[] Args
        {
            get
            {
                var parameters = ClosedMethod.GetParameters();
                object[] args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; ++i)
                {
                    var pType = parameters[i].ParameterType;
                    if (pType.IsByRef)
                        pType = pType.GetElementType();
                    args[i] = Generic<Disposable>.NewT(pType);
                }
                return args;
            }
        }

        protected void Weave(string methodName)
        {
            WeaveAssemblyMethodAndLoad(OpenClassType, methodName);
        }

        protected object WeaveAndRun(string methodName)
        {
            Weave(methodName);
            return Run(methodName);
        }

        protected object Run(string methodName)
        {
            object result;
            var method = ClosedClassType.GetMethod(methodName);
            if (method.IsGenericMethod)
            {
                var generics = method.GetGenericArguments();
                if (generics.Length == 1)
                    result = AssemblyLoader.InvokeGenericMethod(ClosedClassType.TypeInfo(), methodName, new Type[] { typeof(MethodDisposable) }, Args);
                else
                    result = AssemblyLoader.InvokeGenericMethod(ClosedClassType.TypeInfo(), methodName, new Type[] { typeof(MethodDisposable), typeof(List<MethodDisposable[]>) }, Args);
            }
            else
                result = AssemblyLoader.InvokeMethod(ClosedClassType.TypeInfo(), methodName, Args);

            return result;
        }

        protected string ExpectedEntryString(string methodName) => $"Entry: {ClosedClassType} {methodName} {ExpectedArgTypeString()}".Trim();

        protected string ExpectedExitString(string methodName) => $"Exit: {ClosedClassType} {methodName} {ExpectedReturnTypeString()}".Trim();

        protected void RunTest(string methodName)
        {
            // Arrange
            SetClosedMethod(methodName);

            // Act
            object result = WeaveAndRun(methodName);

            // Assert
            var results = result as List<string>;
            results[0].Trim().Should().Be(ExpectedEntryString(methodName));
            results[1].Trim().Should().Be(ExpectedExitString(methodName));
            results.Count.Should().Be(2);
        }
    }
}
