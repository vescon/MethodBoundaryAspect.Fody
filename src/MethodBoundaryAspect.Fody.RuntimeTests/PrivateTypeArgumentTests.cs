using AwesomeAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An aspect without state is shared and created in a generated class, which cannot access a private type used
    /// as type argument of the aspect: the call failed with a MethodAccessException.
    /// </summary>
    [Collection(nameof(ReadArgumentsAttribute))]
    public class PrivateTypeArgumentTests
    {
        [Fact]
        public void AspectWithAPrivateTypeArgumentShouldBeCreated()
        {
            new PrivateTypeArgumentMethods().PrivateTypeArgument();

            ReadArgumentsAttribute.LastType.Should().Be("PrivateKey");
        }
    }
}
