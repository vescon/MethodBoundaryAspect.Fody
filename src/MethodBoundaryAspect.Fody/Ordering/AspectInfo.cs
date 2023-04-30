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
            InitTargetMembers();
            InitChangingInputArguments(aspectAttributes);
            InitNamespaceFilter();
            InitTypeNameFilter();
            InitMethodNameFilter();
        }

        public TypeDefinition AspectTypeDefinition { get; }

        public string Name { get; private set; }

        public string Role { get; private set; }

        public bool SkipProperties { get; private set; }

        public CustomAttribute AspectAttribute { get; private set; }

        public List<CustomAttribute> AspectRoleDependencyAttributes { get; private set; }

#pragma warning disable 0618
        public AspectOrder Order { get; private set; }
#pragma warning restore 0618

        public int? OrderIndex { get; private set; }

        public bool AllowChangingInputArguments { get; private set; }

        public string NamespaceFilter { get; set; }
        public string TypeNameFilter { get; set; }
        public string MethodNameFilter { get; set; }

        public IEnumerable<MethodAttributes> AttributeTargetMemberAttributes { get; set; } =
            new List<MethodAttributes>
            {
                MethodAttributes.Private,
                MethodAttributes.FamANDAssem,
                MethodAttributes.Assembly,
                MethodAttributes.Family,
                MethodAttributes.FamORAssem,
                MethodAttributes.Public
            };

        public bool HasTargetMemberAttribute(MethodAttributes visibility) =>
            AttributeTargetMemberAttributes.Contains(visibility);

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
            InternalInitOrderIndex(assemblyAspectAttributes, "assembly");
            InternalInitOrderIndex(classAspectAttributes, "class");
            InternalInitOrderIndex(methodAspectAttributes, "method");
        }

        private void InternalInitOrderIndex(IEnumerable<CustomAttribute> aspectAttributes, string level)
        {
            var orderIndexAttributes = aspectAttributes
                    .Where(c => c.AttributeType.FullName == AttributeFullNames.AspectOrderIndexAttribute &&
                                ((TypeReference)c.ConstructorArguments[0].Value).FullName == AspectTypeDefinition.FullName)
                    .ToList();

            if (orderIndexAttributes.Count > 1)
                throw new InvalidAspectConfigurationException(string.Format(AspectHasMultipleOrderIndicesDefinedOnTheSameLevel, Name, level));

            if (orderIndexAttributes.Count == 1)
                OrderIndex = (int)orderIndexAttributes[0].ConstructorArguments[1].Value;
        }

        private void InitTargetMembers()
        {
            var targetMembersAttribute = AspectAttribute.Properties
                .FirstOrDefault(property => property.Name == AttributeNames.AttributeTargetMemberAttributes);

            if (targetMembersAttribute.Equals(default(CustomAttributeNamedArgument)))
            {
                return;
            }

            var memberAttributes = new List<MethodAttributes>();

            var attributes = (MulticastAttributes) targetMembersAttribute.Argument.Value;
            if (attributes.HasFlag(MulticastAttributes.Private))
            {
                memberAttributes.Add(MethodAttributes.Private);
            }
            if (attributes.HasFlag(MulticastAttributes.Protected))
            {
                memberAttributes.Add(MethodAttributes.Family);
            }
            if (attributes.HasFlag(MulticastAttributes.Internal))
            {
                memberAttributes.Add(MethodAttributes.Assembly);
            }
            if (attributes.HasFlag(MulticastAttributes.InternalAndProtected))
            {
                memberAttributes.Add(MethodAttributes.FamANDAssem);
            }
            if (attributes.HasFlag(MulticastAttributes.InternalOrProtected))
            {
                memberAttributes.Add(MethodAttributes.FamORAssem);
            }
            if (attributes.HasFlag(MulticastAttributes.Public))
            {
                memberAttributes.Add(MethodAttributes.Public);
            }

            AttributeTargetMemberAttributes = memberAttributes;
        }

        private void InitChangingInputArguments(IEnumerable<CustomAttribute> aspectAttributes)
        {
            AllowChangingInputArguments = aspectAttributes
                .Any(c => c.AttributeType.FullName == AttributeFullNames.AllowChangingInputArguments);

        }

        private void InitNamespaceFilter()
        {
            var namespaceFilterArgument = AspectAttribute.Properties
                .FirstOrDefault(property => property.Name == AttributeNames.NamespaceFilter);

            if (namespaceFilterArgument.Equals(default(CustomAttributeNamedArgument)))
                return;

            if (!(namespaceFilterArgument.Argument.Value is string argumentValue))
                return;

            NamespaceFilter = argumentValue;
        }

        private void InitTypeNameFilter()
        {
            var typeNameFilterArgument = AspectAttribute.Properties
                .FirstOrDefault(property => property.Name == AttributeNames.TypeNameFilter);

            if (typeNameFilterArgument.Equals(default(CustomAttributeNamedArgument)))
                return;

            if (!(typeNameFilterArgument.Argument.Value is string argumentValue))
                return;

            TypeNameFilter = argumentValue;
        }

        private void InitMethodNameFilter()
        {
            var methodNameFilterArgument = AspectAttribute.Properties
                .FirstOrDefault(property => property.Name == AttributeNames.MethodNameFilter);

            if (methodNameFilterArgument.Equals(default(CustomAttributeNamedArgument)))
                return;

            if (!(methodNameFilterArgument.Argument.Value is string argumentValue))
                return;

            MethodNameFilter = argumentValue;
        }
    }
}