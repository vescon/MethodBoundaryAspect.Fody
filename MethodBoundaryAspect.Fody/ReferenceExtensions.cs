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

        public static MethodReference MakeGeneric (this MethodReference self, params TypeReference [] arguments)
        {
            var reference = new MethodReference(self.Name,self.ReturnType) {
                DeclaringType = self.DeclaringType.MakeGenericType (arguments),
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention,
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add (new ParameterDefinition (parameter.ParameterType));

            foreach (var genericParameter in self.GenericParameters)
                reference.GenericParameters.Add (new GenericParameter (genericParameter.Name, reference));

            return reference;
        }
    }
}