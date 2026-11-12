using FluentAssertions;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using MethodBoundaryAspect.Fody.RuntimeTests.Targets;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        public void UsesOfAnAttributeWithTheSameValuesShouldShareOneInstance()
        {
            // "method", "async"+Level 2, "throwing", "throwing"+Level 1, "first" (Two, TwoAsync), "second" (Two, TwoAsync)
            SharedAspectFields<SharedAspectAttribute>().Should().HaveCount(6);
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
        public async Task OverlappingCallsShouldNotMixUpTheTagsOfTheCalls()
        {
            const int calls = 8;
            using (InstanceReuseAspects.OverlapBarrier = new Barrier(calls))
            {
                // the barrier in OnEntry lets a call return only when all calls are between OnEntry and OnExit
                var tasks = Enumerable.Range(0, calls)
                    .Select(i => Task.Factory.StartNew(() => _target.Overlapping(i), TaskCreationOptions.LongRunning))
                    .ToArray();

                (await Task.WhenAll(tasks)).Should().BeEquivalentTo(Enumerable.Range(0, calls));
            }

            InstanceReuseAspects.MixedUpTags.Should().Be(0);
        }

        [Fact]
        public void SeveralSharedAspectsOnOneMethodShouldHaveTheirOwnInstanceAndTag()
        {
            _target.Two(7);
            _target.Two(8);

            InstanceReuseAspects.Calls.Where(c => c.StartsWith("first")).Should()
                .Equal("first0:entry:Two", "first0:exit:Two", "first0:entry:Two", "first0:exit:Two");
            InstanceReuseAspects.Calls.Where(c => c.StartsWith("second")).Should()
                .Equal("second0:entry:Two", "second0:exit:Two", "second0:entry:Two", "second0:exit:Two");
            // an aspect that wrote a foreign tag would have been counted
            InstanceReuseAspects.MixedUpTags.Should().Be(0);
        }

        [Fact]
        public async Task SeveralSharedAspectsOnOneAsyncMethodShouldHaveTheirOwnInstanceAndTag()
        {
            await Task.WhenAll(Enumerable.Range(0, 100).Select(i => _target.TwoAsync(i)));

            InstanceReuseAspects.MixedUpTags.Should().Be(0);
            InstanceReuseAspects.Calls.Count(c => c.StartsWith("first0:exit")).Should().Be(100);
            InstanceReuseAspects.Calls.Count(c => c.StartsWith("second0:exit")).Should().Be(100);
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

        [Fact]
        public void AspectWithASynchronizedMethodShouldNotBeShared()
        {
            _target.SynchronizedAspect();

            SharedAspectFields<SynchronizedAspectAttribute>().Should().BeEmpty();
        }

        // the fields of the generated class holding the shared aspects
        private static FieldInfo[] SharedAspectFields<TAspect>()
        {
            var aspectsClass = typeof(InstanceReuseMethods).Assembly.GetType("OnMethodBoundaryAspectCompile.Aspects");
            // without the class every "is not shared" assertion would pass
            aspectsClass.Should().NotBeNull("the assembly has shared aspects");
            return aspectsClass.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(TAspect))
                .ToArray();
        }
    }
}
