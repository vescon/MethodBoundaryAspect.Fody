using System;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody
{
    public static class ReferenceExtensions
    {
        public static TypeReference MakeGenericType (this TypeReference self, params TypeReference [] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException ();
           
            var instance = new GenericInstanceType (self);

            foreach (var argument in arguments)
                instance.GenericArguments.Add (argument);

            return instance;
        }

        // instantiated with its own generic parameters, e.g. to call the executor of a generic method
        public static MethodReference MakeGeneric (this MethodReference self)
        {
            var baseReference = self.DeclaringType.Module.ImportReference(self);
            var reference = new GenericInstanceMethod(baseReference);
            
            foreach (var genericParameter in baseReference.GenericParameters)
                reference.GenericArguments.Add(genericParameter);

            return reference;
        }
    }
}