using System;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    public class InstanceReuseMethods
    {
        [StatelessReuseAspect]
        public void Stateless()
        {
        }

        [ValuesReuseAspect("name", DayOfWeek.Friday, Level = 2, Text = "text")]
        public void WithValues()
        {
        }

        [DerivedReuseAspect]
        public void Derived()
        {
        }

        [CounterStateAspect]
        public void Counter()
        {
        }

        [MutableFieldAspect]
        public void MutableField()
        {
        }

        [ConstructorWithEffectAspect]
        public void ConstructorWithEffect()
        {
        }

        [SetterCallAspect]
        public void SetterCall()
        {
        }

        [BaseSetterCallAspect]
        public void BaseSetterCall()
        {
        }

        [NonTrivialSetterAspect]
        public void NonTrivialSetter()
        {
        }

        [ThisLeakAspect]
        public void ThisLeak()
        {
        }

        [LambdaThisAspect]
        public void LambdaThis()
        {
        }

        [FinalizerAspect]
        public void Finalizer()
        {
        }

        [ArrayFieldAspect]
        public void ArrayField()
        {
        }

        [StatelessReuseAspect]
        public async Task<int> Async()
        {
            await Task.Yield();
            return 1;
        }

        [OptOutReuseAspect]
        public void OptOut()
        {
        }

        [PrivateNested]
        public void PrivateNestedAspect()
        {
        }

        private class PrivateNestedAttribute : Fody.Attributes.OnMethodBoundaryAspect
        {
            public override void OnEntry(Fody.Attributes.MethodExecutionArgs arg)
            {
            }
        }
    }

    [StatelessReuseAspect]
    public class InstanceReuseClassAspect
    {
        public void First()
        {
        }

        public void Second()
        {
        }
    }
}
