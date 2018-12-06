using System;
using System.IO;
using FluentAssertions;
using Xunit;


namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class DelegateTests : MethodBoundaryAspectTestBase
    {
        [Fact]
        public void WeaveWithoutErrorEvenIfDelegateExistsCore()
        {
            WeaveAssembly(typeof(TestAssemblyDelegateCore.ClassWithDelegate));
        }

        [Fact]
        public void WeaveWithoutErrorEvenIfDelegateExistsFramework()
        {
            WeaveAssembly(typeof(TestAssemblyDelegateFwk.ClassWithDelegate));
        }

        [Fact]
        public void WeaveWithoutErrorEvenIfDelegateExistsFramework2()
        {
            // This fails without code which skips weaving methods that don't have a body.
            WeaveAssembly(typeof(TestAssemblyDelegateFwk2.ClassWithDelegate));
        }

    }
}