using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody.Ordering
{
    public class AspectInfo
    {
        private const string AspectCanTHaveRoleAndBeOrderedBeforeOrAfterThatRole =
            "Aspect '{0}' can't have role '{1}' and be ordered before or after that role";

        private const string AspectHasToProvideANonEmptyMethodBoundaryAspectAttributesProvideAspectRoleAttribute =
            "Aspect '{0}' has to provide a non-empty MethodBoundaryAspect.Attributes.ProvideAspectRoleAttribute attribute";

        private const string AspectHasMultipleOrderIndicesDefinedOnTheSameLevel =
            "Aspect '{0}' has multiple order indices defined on {1} level";

        public AspectInfo(CustomAttribute aspectAttribute)
        {
            AspectAttribute = aspectAttribute;
            Name = aspectAttribute.AttributeType.FullName;
            AspectTypeDefinition = aspectAttribute.AttributeType.Resolve();

            var aspectAttributes = AspectTypeDefinition.CustomAttributes;
            InitRole(aspectAttributes);
            InitOrder(aspectAttributes);
            InitSkipProperties(aspectAttributes);
        }
        
        public TypeDefinition AspectTypeDefinition { get; }

        public string Name { get; private set; }

        public string Role { get; private set; }

        public bool SkipProperties { get; set; }

        public CustomAttribute AspectAttribute { get; private set; }

        public List<CustomAttribute> AspectRoleDependencyAttributes { get; private set; }

#pragma warning disable 0618
        public AspectOrder Order { get; private set; }
#pragma warning restore 0618

        public int? OrderIndex { get; set; }

        private void InitRole(IEnumerable<CustomAttribute> aspectAttributes)
        {
            Role = "<Default>";

            var roleAttribute = aspectAttributes
                .SingleOrDefault(c => c.AttributeType.FullName == AttributeFullNames.ProvideAspectRoleAttribute);

            if (roleAttribute == null)
                return;

            var role = (string) roleAttribute.ConstructorArguments[0].Value;
            if (string.IsNullOrEmpty(role))
            {
                var msg =
                    string.Format(AspectHasToProvideANonEmptyMethodBoundaryAspectAttributesProvideAspectRoleAttribute,
                        Name);
                throw new InvalidAspectConfigurationException(msg);
            }

            Role = role;
        }

        private void InitOrder(IEnumerable<CustomAttribute> aspectAttributes)
        {
            AspectRoleDependencyAttributes =
                aspectAttributes.Where(
                    c => c.AttributeType.FullName == AttributeFullNames.AspectRoleDependencyAttribute).ToList();

            if (AspectRoleDependencyAttributes.Count == 0)
                return;

#pragma warning disable 0618
            var aspectOrder = new AspectOrder(this);
#pragma warning restore 0618

            foreach (var roleDependencyAttribute in AspectRoleDependencyAttributes)
            {
                var role = (string) roleDependencyAttribute.ConstructorArguments[2].Value;
                if (role == Role)
                {
                    var msg = string.Format(AspectCanTHaveRoleAndBeOrderedBeforeOrAfterThatRole, Name, role);
                    throw new InvalidAspectConfigurationException(msg);
                }

                var position = (int) roleDependencyAttribute.ConstructorArguments[1].Value;

                aspectOrder.AddRole(role, position);
            }

            Order = aspectOrder;
        }

        private void InitSkipProperties(IEnumerable<CustomAttribute> aspectAttributes)
        {
            var skipPropertiesAttribute = aspectAttributes
                .SingleOrDefault(c => c.AttributeType.FullName == AttributeFullNames.AspectSkipPropertiesAttribute);

            if (skipPropertiesAttribute == null)
                return;

            var skipProperties = (bool)skipPropertiesAttribute.ConstructorArguments[0].Value;

            SkipProperties = skipProperties;
        }

        public void InitOrderIndex(
            IEnumerable<CustomAttribute> assemblyAspectAttributes,
            IEnumerable<CustomAttribute> classAspectAttributes,
            IEnumerable<CustomAttribute> methodAspectAttributes)
        {
            var aspectAttributes = new[] {assemblyAspectAttributes, classAspectAttributes, methodAspectAttributes};

            for (int i = 0; i < aspectAttributes.Length; i++)
            {
                var orderIndexAttributes = aspectAttributes[i]
                    .Where(c => c.AttributeType.FullName == AttributeFullNames.AspectOrderIndexAttribute &&
                                ((TypeReference)c.ConstructorArguments[0].Value).FullName == AspectTypeDefinition.FullName)
                    .ToList();

                if (orderIndexAttributes.Count > 1)
                    throw new InvalidAspectConfigurationException(
                        string.Format(AspectHasMultipleOrderIndicesDefinedOnTheSameLevel,
                            Name,
                            i == 0 ? "assembly" : i == 1 ? "class" : "method"));

                if (orderIndexAttributes.Count == 1)
                    OrderIndex = (int)orderIndexAttributes[0].ConstructorArguments[1].Value;
            }
        }
    }
}