using System;
using System.Collections.Generic;
using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.NetCore.Aspects
{
    // Aspects for the analysis if one instance can be shared by all calls, see AspectInstanceReuseAnalyzer

    public class StatelessReuseAspect : OnMethodBoundaryAspect
    {
        public static int Calls;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Calls++;
        }
    }

    public class ValuesReuseAspect : OnMethodBoundaryAspect
    {
        private readonly string _name;
        private readonly string _initialized = "text";
        private readonly DayOfWeek _day;

        public ValuesReuseAspect(string name, DayOfWeek day = DayOfWeek.Monday)
        {
            _name = name;
            _day = day;
        }

        public int Level { get; set; }

        public string Text;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Console.WriteLine(_name + _initialized + _day + Level + Text + GetType().Name + Helper());
        }

        private string Helper() => _name;
    }

    public class BaseReuseAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            Log(arg.Method.Name);
        }

        protected virtual void Log(string text)
        {
        }
    }

    public class DerivedReuseAspect : BaseReuseAspect
    {
        public override void OnExit(MethodExecutionArgs arg)
        {
            base.OnEntry(arg);
        }

        protected override void Log(string text)
        {
            Console.WriteLine(text);
        }
    }

    public class CounterStateAspect : OnMethodBoundaryAspect
    {
        private int _calls;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            _calls++;
            arg.MethodExecutionTag = _calls;
        }
    }

    public class MutableFieldAspect : OnMethodBoundaryAspect
    {
        private readonly List<string> _log = new List<string>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            _log.Add(arg.Method.Name);
        }
    }

    public class ConstructorWithEffectAspect : OnMethodBoundaryAspect
    {
        public static int Created;

        public ConstructorWithEffectAspect()
        {
            Created++;
        }

        public override void OnEntry(MethodExecutionArgs arg)
        {
        }
    }

    public class SetterCallAspect : OnMethodBoundaryAspect
    {
        public int Value { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Value = 5;
        }
    }

    public class BaseSetterCallAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            NamespaceFilter = "changed";
        }
    }

    public class NonTrivialSetterAspect : OnMethodBoundaryAspect
    {
        private int _value;

        public int Value
        {
            get => _value;
            set
            {
                Console.WriteLine(value);
                _value = value;
            }
        }

        public override void OnEntry(MethodExecutionArgs arg)
        {
        }
    }

    public class ThisLeakAspect : OnMethodBoundaryAspect
    {
        public static readonly List<object> Instances = new List<object>();

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Instances.Add(this);
        }
    }

    public class LambdaThisAspect : OnMethodBoundaryAspect
    {
        public string Name { get; set; }

        public override void OnEntry(MethodExecutionArgs arg)
        {
            Func<string> func = () => Name;
            Console.WriteLine(func());
        }
    }

    public class FinalizerAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }

        ~FinalizerAspect()
        {
        }
    }

    public class ArrayFieldAspect : OnMethodBoundaryAspect
    {
        private readonly string[] _names = { "a" };

        public override void OnEntry(MethodExecutionArgs arg)
        {
            _names[0] = arg.Method.Name;
        }
    }

    /// <summary>
    /// The aspect has to be created for every call (opt-out in the configuration).
    /// </summary>
    public class OptOutReuseAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
        }
    }
}
