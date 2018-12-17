using System;

namespace MethodBoundaryAspect.Fody.Ordering
{
    public class InvalidAspectConfigurationException : Exception
    {
        public InvalidAspectConfigurationException(string msg)
            : base(msg)
        {
        }
    }
}