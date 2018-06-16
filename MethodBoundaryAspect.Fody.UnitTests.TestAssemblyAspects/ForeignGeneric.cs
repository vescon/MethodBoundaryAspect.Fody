using System;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssemblyAspects
{
    public class ForeignGeneric<T1, T2> where T1 : IList<T2>
    {
        [Serializable]
        public class Inner<T3> { }
    }
}
