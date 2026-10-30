using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class AccessorAspectMethods
    {
        private int _getterOnly;
        private int _setterOnly;
        private int _both;

        public int GetterOnly
        {
            [AccessorAspect] get => _getterOnly;
            set => _getterOnly = value;
        }

        public int SetterOnly
        {
            get => _setterOnly;
            [AccessorAspect] set => _setterOnly = value;
        }

        public int Both
        {
            [AccessorAspect] get => _both;
            [AccessorAspect] set => _both = value;
        }

        [AccessorAspect]
        public int AutoPropertyOfTheProperty { get; set; }

        public int AutoPropertyWithAspectOnAccessors { [AccessorAspect] get; [AccessorAspect] set; }
    }
}
