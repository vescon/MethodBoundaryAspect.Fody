using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MethodBoundaryAspect.Fody.RuntimeTests
{
    /// <summary>
    /// An aspect without state is created once for all calls, an aspect with state for every call, see
    /// the section "Aspect instances" of the README.
    /// </summary>
    [Collection(nameof(InstanceReuseAspects))]
    public class InstanceReuseTests
    {
        private readonly InstanceReuseMethods _target = new InstanceReuseMethods();

        public InstanceReuseTests()
        {
            InstanceReuseAspects.Calls.Clear();
            InstanceReuseAspects.MixedUpTags = 0;
        }

        [Fact]
        public void AllMethodsOfAClassShouldShareOneInstanceOfTheAspectOfTheClass()
        {
            var classMethods = new InstanceReuseClassMethods();

            classMethods.First(1);
            var instanceAfterFirstCall = SharedAspectFields<SharedClassAspectAttribute>().Single().GetValue(null);
            classMethods.Second(2);
            classMethods.First(3);

            instanceAfterFirstCall.Should().NotBeNull();
            SharedAspectFields<SharedClassAspectAttribute>().Single().GetValue(null).Should().BeSameAs(instanceAfterFirstCall);
            InstanceReuseAspects.Calls.Should().Equal("class:entry:First", "class:entry:Second", "class:entry:First");
        }

        [Fact]
        public void EveryUseOfAnAttributeShouldHaveItsOwnInstance()
        {
            // the values of the attribute differ: Sync, Async, Throwing and ThrowingAsync
            SharedAspectFields<SharedAspectAttribute>().Should().HaveCount(4);
        }

        [Fact]
        public void SharedAspectShouldUseTheValuesOfTheAttribute()
        {
            _target.Sync(1);
            _target.Sync(2);

            InstanceReuseAspects.Calls.Should().Equal("method0:entry:Sync", "method0:exit:Sync", "method0:entry:Sync", "method0:exit:Sync");
        }

        [Fact]
        public async Task SharedAspectShouldWorkForAnAsyncMethod()
        {
            (await _target.Async(5)).Should().Be(5);

            InstanceReuseAspects.Calls.Should().Equal("async2:entry:Async", "async2:exit:Async");
        }

        [Fact]
        public void SharedAspectShouldHandleExceptions()
        {
            Action throwing = () => _target.Throwing();

            throwing.Should().Throw<InvalidOperationException>();
            InstanceReuseAspects.Calls.Should().Equal("throwing0:entry:Throwing", "throwing0:exception:failed");
        }

        [Fact]
        public async Task SharedAspectShouldHandleExceptionsOfAnAsyncMethod()
        {
            Func<Task> throwing = () => _target.ThrowingAsync();

            await throwing.Should().ThrowAsync<InvalidOperationException>();
            // OnExit is called when the method returns its task, OnException on the thread pool after the
            // await: their order depends on timing (not related to sharing the aspect)
            InstanceReuseAspects.Calls.Should().HaveCount(3);
            InstanceReuseAspects.Calls[0].Should().Be("throwing1:entry:ThrowingAsync");
            InstanceReuseAspects.Calls.Skip(1).Should().BeEquivalentTo("throwing1:exit:ThrowingAsync", "throwing1:exception:failed async");
        }

        [Fact]
        public async Task ConcurrentCallsShouldNotMixUpTheTagsOfTheCalls()
        {
            await Task.WhenAll(Enumerable.Range(0, 200).Select(async i =>
            {
                await Task.Yield();
                (await _target.Async(i)).Should().Be(i);
                _target.Sync(i);
            }));

            InstanceReuseAspects.MixedUpTags.Should().Be(0);
            InstanceReuseAspects.Calls.Count(c => c.StartsWith("async2:exit")).Should().Be(200);
        }

        [Fact]
        public void AspectWithStateShouldBeCreatedForEveryCall()
        {
            _target.PerCall();
            _target.PerCall();
            _target.PerCall();

            InstanceReuseAspects.Calls.Should().Equal("calls:1", "calls:1", "calls:1");
            SharedAspectFields<PerCallStateAttribute>().Should().BeEmpty();
        }

        [Fact]
        public async Task AspectWithStateShouldBeCreatedForEveryCallOfAnAsyncMethod()
        {
            await _target.PerCallAsync();
            await _target.PerCallAsync();

            InstanceReuseAspects.Calls.Should().Equal("calls:1", "calls:1");
        }

        // the fields of the generated class holding the shared aspects
        private static FieldInfo[] SharedAspectFields<TAspect>()
        {
            var aspectsClass = typeof(InstanceReuseMethods).Assembly.GetType("OnMethodBoundaryAspectCompile.Aspects");
            return aspectsClass == null
                ? new FieldInfo[0]
                : aspectsClass.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(TAspect))
                    .ToArray();
        }
    }
}
