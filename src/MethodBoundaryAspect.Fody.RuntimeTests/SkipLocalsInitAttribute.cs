#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Used by the compiler to emit methods without initializing their locals, only part of .NET 5 and later.
    /// </summary>
    [AttributeUsage(AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Interface, Inherited = false)]
    internal sealed class SkipLocalsInitAttribute : Attribute
    {
    }
}
#endif
