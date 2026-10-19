using MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore
{
    public class LastMethodNotWovenMethods
    {
        [OnlyOnEntryAspect]
        public void Woven()
        {
        }

        [NoAspectMethodsAspect]
        public void NotWoven()
        {
        }
    }

    public class GenericParameterFlagsMethods
    {
        [OnlyOnEntryAspect]
        public T Generic<T>(T value) => value;
    }

    public enum Color
    {
        Red,
        Green
    }

    public class RefArrayMethods
    {
        [ChangingInputArgumentsAspect]
        public void SetColors(ref Color[] colors) => colors = new[] { Color.Green };
    }

    public class PropertyFilterMethods
    {
        [OnlyOnEntryAspect]
        public int Value { get; set; }
    }

    public class DebugInformationMethods
    {
        [OnlyOnEntryAspect]
        public int Sync(int a, int b)
        {
            var sum = a + b;
            if (sum > 10)
            {
                sum -= 10;
            }

            return sum;
        }

        [OnlyOnEntryAspect]
        public async System.Threading.Tasks.Task<int> Async(int a)
        {
            await System.Threading.Tasks.Task.Yield();
            var doubled = a * 2;
            return doubled;
        }

        [OnlyOnExceptionAspect]
        public async System.Threading.Tasks.Task AsyncWithOnException(int a)
        {
            await System.Threading.Tasks.Task.Yield();
            if (a < 0)
                throw new System.ArgumentException("negative");
        }
    }

    public class ObsoleteRoleMethods
    {
        [RoleOnlyAspect]
        public void First()
        {
        }

        [RoleOnlyAspect]
        public void Second()
        {
        }

        [RoleDependencyOnlyAspect]
        public void Dependency()
        {
        }

        [OnlyOnEntryAspect]
        public void WithoutRoles()
        {
        }
    }

    public class IgnoredValueMethods
    {
        [ConstructorFilterAspect]
        public void Constructor()
        {
        }

        // the same aspect again: one warning per aspect
        [ConstructorFilterAspect]
        public void ConstructorAgain()
        {
        }

        [DerivedConstructorFilterAspect]
        public void InheritedConstructor()
        {
        }

        [OverridingFilterAspect]
        public void Overriding()
        {
        }

        // set where the aspect is applied: the way it is meant to be
        [OnlyOnEntryAspect(NamespaceFilter = "^MethodBoundaryAspect", MethodNameFilter = "^UsageSite")]
        public void UsageSite()
        {
        }
    }
}
