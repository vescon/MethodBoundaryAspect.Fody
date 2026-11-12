using AwesomeAssertions;
using MethodBoundaryAspect.Fody.Ordering;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MethodBoundaryAspect.Fody.UnitTests.NetCore
{
    /// <summary>
    /// The aspects of a method are ordered by their [AspectOrderIndex]: if one aspect has an index all need one, and
    /// different aspects need different indices. An aspect applied twice (to the class and to the method) has the
    /// same index twice.
    /// </summary>
    public class AspectOrderIndexTests
    {
        private readonly ModuleDefinition _module = WeaverTestHelper.ReadModule(typeof(OnlyOnEntryAspect));

        [Fact]
        public void AspectsAreOrderedByTheirIndex()
        {
            var entry = CreateAspect(typeof(OnlyOnEntryAspect), 2);
            var exit = CreateAspect(typeof(OnlyOnExitAspect), 1);

            var ordered = AspectOrderer.Order(new List<AspectInfo> { entry, exit });

            ordered.Should().Equal(exit, entry);
        }

        [Fact]
        public void DifferentAspectsWithTheSameIndexAreAnError()
        {
            var entry = CreateAspect(typeof(OnlyOnEntryAspect), 1);
            var exit = CreateAspect(typeof(OnlyOnExitAspect), 1);

            Action order = () => AspectOrderer.Order(new List<AspectInfo> { entry, exit });

            order.Should().Throw<InvalidAspectConfigurationException>().WithMessage("*No unique order index*");
        }

        [Fact]
        public void AnAspectWithoutIndexIsAnErrorIfAnotherHasOne()
        {
            var entry = CreateAspect(typeof(OnlyOnEntryAspect), 1);
            var exit = CreateAspect(typeof(OnlyOnExitAspect), null);

            Action order = () => AspectOrderer.Order(new List<AspectInfo> { entry, exit });

            order.Should().Throw<InvalidAspectConfigurationException>().WithMessage("*No order index provided*OnlyOnExitAspect*");
        }

        [Fact]
        public void TheSameAspectTwiceWithTheSameIndexKeepsItsPlace()
        {
            var onClass = CreateAspect(typeof(OnlyOnEntryAspect), 1);
            var exit = CreateAspect(typeof(OnlyOnExitAspect), 2);
            var onMethod = CreateAspect(typeof(OnlyOnEntryAspect), 1);

            var ordered = AspectOrderer.Order(new List<AspectInfo> { exit, onClass, onMethod });

            ordered.Should().Equal(onClass, onMethod, exit);
        }

        private AspectInfo CreateAspect(Type aspectType, int? orderIndex)
        {
            var aspect = new CustomAttribute(_module.ImportReference(aspectType.GetConstructor(Type.EmptyTypes)));
            var info = new AspectInfo(aspect);

            var classAttributes = new List<CustomAttribute>();
            if (orderIndex != null)
                classAttributes.Add(CreateOrderIndex(aspectType, orderIndex.Value));

            info.InitOrderIndex(
                Enumerable.Empty<CustomAttribute>(),
                Enumerable.Empty<IEnumerable<CustomAttribute>>(),
                classAttributes,
                Enumerable.Empty<CustomAttribute>());
            return info;
        }

        private CustomAttribute CreateOrderIndex(Type aspectType, int orderIndex)
        {
            // the runtime library is no direct reference of this project, get it via the base class of an aspect
            var orderIndexType = aspectType.BaseType.Assembly
                .GetType("MethodBoundaryAspect.Fody.Attributes.AspectOrderIndexAttribute", throwOnError: true);
            var attribute = new CustomAttribute(_module.ImportReference(orderIndexType.GetConstructors().Single()));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(_module.ImportReference(typeof(Type)), _module.ImportReference(aspectType)));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(_module.TypeSystem.Int32, orderIndex));
            return attribute;
        }
    }
}
