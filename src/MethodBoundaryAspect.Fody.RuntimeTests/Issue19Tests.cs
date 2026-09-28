using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// https://github.com/vescon/MethodBoundaryAspect.Fody/issues/19
    /// An aspect with AttributeTargetMemberAttributes = MulticastAttributes.Public is only applied to public methods,
    /// the other methods are not rewritten at all.
    /// </summary>
    [Collection(nameof(VisibilityRecordingAspect))]
    public class Issue19Tests
    {
        private const string ExecutorPrefix = "$_executor_";

        public Issue19Tests()
        {
            VisibilityRecordingAspect.Calls.Clear();
        }

        [Fact]
        public void PublicShouldOnlyWeavePublicMethods()
        {
            new PublicOnlyMethods().CallAll();
            PublicOnlyMethods.PublicStaticMethod();

            VisibilityRecordingAspect.Calls.Should().Equal("CallAll", "PublicStaticMethod");
        }

        [Fact]
        public void PublicShouldOnlyWeavePublicPropertyAccessors()
        {
            var target = new PublicOnlyMethods();
            target.PublicProperty = target.PublicProperty;
            target.CallAll(); // accesses the private property

            VisibilityRecordingAspect.Calls.Should().Equal("get_PublicProperty", "set_PublicProperty", "CallAll");
        }

        [Fact]
        public async Task PublicShouldOnlyWeavePublicAsyncMethods()
        {
            var target = new PublicOnlyMethods();
            await target.PublicAsyncMethod();
            await target.CallPrivateAsyncMethod();

            VisibilityRecordingAspect.Calls.Should().Equal("PublicAsyncMethod", "CallPrivateAsyncMethod");
        }

        [Fact]
        public void PublicShouldNotRewriteNonPublicMethods()
        {
            var executorNames = typeof(PublicOnlyMethods)
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(m => m.Name)
                .Where(name => name.StartsWith(ExecutorPrefix))
                .Select(name => name.Substring(ExecutorPrefix.Length));

            executorNames.Should().BeEquivalentTo(
                "CallAll",
                "PublicStaticMethod",
                "CallPrivateAsyncMethod",
                "get_PublicProperty",
                "set_PublicProperty");
        }

        [Fact]
        public void PublicShouldAlsoFilterMethodLevelAspects()
        {
            new PublicOnlyMethodAspects().PublicMethod();

            VisibilityRecordingAspect.Calls.Should().Equal("PublicMethod");
        }
    }
}
