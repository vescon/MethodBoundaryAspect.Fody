namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Aspects
{
    public struct Placeholder { public int Value { get; set; } }

    public class AsyncTAspect : AsyncAspect
    {
        public AsyncTAspect(string message) : base(message) { }

        public override bool IsRightClass(object o)
        {
            return o is AsyncClass<Placeholder>;
        }

        public override void Set(int value)
        {
            AsyncClass<Placeholder>.Result = value;
        }

        public override void Set(string value)
        {
            AsyncClass<Placeholder>.Result += value;
        }
    }
}
