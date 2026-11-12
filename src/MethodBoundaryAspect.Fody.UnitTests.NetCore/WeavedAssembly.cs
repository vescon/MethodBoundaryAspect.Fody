using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// A woven test assembly loaded into its own, collectible <see cref="AssemblyLoadContext"/>, so the tests can run
    /// the woven code (the assembly that was woven is loaded in the default context for the other tests). The
    /// references, e.g. the runtime library, are loaded by the default context.
    /// </summary>
    internal sealed class WeavedAssembly : IDisposable
    {
        private sealed class Context : AssemblyLoadContext
        {
            public Context() : base(isCollectible: true)
            {
            }
        }

        private readonly Context _context = new Context();

        private WeavedAssembly(string path)
        {
            // from a stream, the file is not locked and can be deleted
            using (var stream = new MemoryStream(File.ReadAllBytes(path)))
                Assembly = _context.LoadFromStream(stream);
        }

        public Assembly Assembly { get; }

        public static WeavedAssembly Load(string weavedAssemblyPath) => new WeavedAssembly(weavedAssemblyPath);

        public Type GetType(Type originalType) =>
            Assembly.GetType(originalType.FullName, throwOnError: true);

        /// <summary>Creates the object and calls the method (static fields of the aspects are read with <see cref="GetStaticField"/>).</summary>
        public object Invoke(Type originalType, string methodName, params object[] arguments)
        {
            var type = GetType(originalType);
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var instance = method.IsStatic ? null : Activator.CreateInstance(type);
            try
            {
                return method.Invoke(instance, arguments);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        public object GetStaticField(Type originalType, string fieldName) =>
            GetType(originalType).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        public void Dispose() => _context.Unload();
    }
}
