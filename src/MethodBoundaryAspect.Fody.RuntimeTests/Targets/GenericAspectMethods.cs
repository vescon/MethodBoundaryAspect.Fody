using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class GenericAspectMethods
    {
        [GenericRecordingAspect<int>("A", Label = "label", Number = 3)]
        public int Sync(int value) => value;

        [GenericRecordingAspect<List<string>>("B")]
        public int Throwing() => throw new InvalidOperationException();

        [GenericRecordingAspect<Guid>("C")]
        public async Task<int> Async()
        {
            await Task.Yield();
            return 7;
        }

        [GenericRecordingAspect<int>("D")]
        public async Task AsyncThrowing()
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }

        [DerivedFromGenericAspect]
        public int FromGenericBase() => 1;
    }

    [GenericRecordingAspect<byte>("E")]
    public class GenericAspectOnGenericClass<TClass>
    {
        public TClass Method<TMethod>(TClass value, TMethod other) => value;
    }
}

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class GenericValueMethods
    {
        [GenericValue<int>(Value = 5)]
        public void IntProperty()
        {
        }

        [GenericValue<string>(Value = "text")]
        public void StringProperty()
        {
        }

        [GenericValue<double>(Value = 1.5, ValueField = 2.5)]
        public void DoublePropertyAndField()
        {
        }

        [GenericValue<DayOfWeek>(Value = DayOfWeek.Friday)]
        public void EnumProperty()
        {
        }
    }
}

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class GenericCtorValueMethods
    {
        [GenericCtorValue<int>(5)]
        public void Int()
        {
        }

#if NETCOREAPP
        // .NET Framework cannot read the arguments of these attributes with reflection (CustomAttributeFormatException),
        // so xunit fails to discover the methods. The weaver does not read them with reflection.
        [GenericCtorValue<string>("text")]
        public void String()
        {
        }
#endif

        [GenericCtorValue<double>("label", 1.5)]
        public void LabelAndDouble()
        {
        }

        [GenericCtorValue<DayOfWeek>(DayOfWeek.Friday)]
        public void Enum()
        {
        }

#if NETCOREAPP
        [GenericCtorValue<Type>(typeof(int))]
        public void TypeArgument()
        {
        }
#endif
    }
}
