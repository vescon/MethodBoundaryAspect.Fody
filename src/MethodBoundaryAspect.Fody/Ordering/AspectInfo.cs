using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace MethodBoundaryAspect.Fody.Ordering
{
    public class AspectInfo
    {
        private const string AspectCanTHaveRoleAndBeOrderedBeforeOrAfterThatRole =
            "Aspect '{0}' can't have role '{1}' and be ordered before or after that role";

        private const string AspectHasToProvideANonEmptyMethodboundaryaspectAttributesProvideaspectroleAttribute =
            "Aspect '{0}' has to provide a non-empty MethodBoundaryAspect.Attributes.ProvideAspectRoleAttribute attribute";

        public AspectInfo(CustomAttribute aspectAttribute)
        {
            AspectAttribute = aspectAttribute;
            Name = aspectAttribute.AttributeType.FullName;
            var aspectType = aspectAttribute.AttributeType.Resolve();

            var aspectAttributes = aspectType.CustomAttributes;
            InitRole(aspectAttributes);
            InitOrder(aspectAttributes);
            InitSkipProperties(aspectAttributes);
        }
        
        public string Name { get; private set; }

        public string Role { get; private set; }

        public bool SkipProperties { get; set; }

        public CustomAttribute AspectAttribute { get; private set; }

        public List<CustomAttribute> AspectRoleDependencyAttributes { get; private set; }

        public AspectOrder Order { get; private set; }

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
                    string.Format(AspectHasToProvideANonEmptyMethodboundaryaspectAttributesProvideaspectroleAttribute,
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

            var aspectOrder = new AspectOrder(this);
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
    }
}