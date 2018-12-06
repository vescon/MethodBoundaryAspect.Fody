using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssemblyDelegateFwk
{
    public class ClassWithDelegate
    {
        public delegate void ThreadFinishedEventHandler();

        public struct MyStruct
        {
            public string MyString { get; set; }
        }

        public void DoSomething()
        {
            int myval = 5 + 2;
        }
    }
}
