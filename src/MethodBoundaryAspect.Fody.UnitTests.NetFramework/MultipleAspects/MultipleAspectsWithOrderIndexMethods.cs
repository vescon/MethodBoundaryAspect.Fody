using MethodBoundaryAspect.Fody.Attributes;

namespace MethodBoundaryAspect.Fody.UnitTests.NetFramework.MultipleAspects
{
    [AspectOrderIndex(typeof(Aspect1), 3)]
    [AspectOrderIndex(typeof(Aspect2), 2)]
    [AspectOrderIndex(typeof(Aspect3), 1)]
    public class MultipleAspectsWithOrderIndexMethods
    {
        public static string Result { get; set; }
        
        [Aspect1]
        [Aspect2]
        [Aspect3]
        public void MethodWithOrderIndexSpecifiedOnClassLevel()
        {
        }

        [Aspect1]
        [Aspect2]
        [Aspect3]
        [AspectOrderIndex(typeof(Aspect1), 0)]
        [AspectOrderIndex(typeof(Aspect2), -1)]
        [AspectOrderIndex(typeof(Aspect3), 1)]
        public void MethodWithOrderIndexSpecified()
        {
        }
    }
}