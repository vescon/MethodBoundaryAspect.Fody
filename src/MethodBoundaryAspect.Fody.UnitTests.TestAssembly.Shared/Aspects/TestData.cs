namespace MethodBoundaryAspect.Fody.UnitTests.TestAssembly.Shared.Aspects
{
    public struct TestData
    {
        public int Key;

        public override bool Equals(object obj)
        {
            return obj is TestData data && data.Key == Key;
        }

        public TestData(int key)
        {
            Key = key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public override string ToString()
        {
            return Key.ToString();
        }

        public static bool operator ==(TestData lhs, TestData rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TestData lhs, TestData rhs)
        {
            return !(lhs == rhs);
        }
    }
}