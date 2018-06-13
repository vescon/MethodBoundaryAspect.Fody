using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

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

        public static MethodReference MakeGeneric (this MethodReference self, params TypeReference [] arguments)
        {
            MethodReference baseReference = self.DeclaringType.Module.ImportReference(self);

            var reference = new GenericInstanceMethod(baseReference);
            
            foreach (var genericParameter in baseReference.GenericParameters)
                reference.GenericArguments.Add(genericParameter);

            return reference;
        }
    }
}