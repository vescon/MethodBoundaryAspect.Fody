using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MethodBoundaryAspect.Fody.UnitTests.TestAssemblyAspects
{
    public enum ByteExampleEnum : byte
    {
        ByteFirst = 1,
        ByteSecond = byte.MaxValue
    }

    public enum SByteExampleEnum : sbyte
    {
        SByteFirst = 1,
        SByteSecond = sbyte.MaxValue,
        SByteThird = sbyte.MinValue
    }

    public enum ShortExampleEnum : short
    {
        ShortFirst = 1,
        ShortSecond = short.MaxValue,
        ShortThird = short.MinValue
    }

    public enum UShortExampleEnum : ushort
    {
        UShortFirst,
        UShortSecond = ushort.MaxValue
    }

    public enum IntExampleEnum : int
    {
        IntFirst = 1,
        IntSecond = int.MaxValue,
        IntThird = int.MinValue
    }

    public enum UIntExampleEnum : uint
    {
        UIntFirst = 1,
        UIntSecond = uint.MaxValue
    }
    
    public enum ULongExampleEnum: ulong
    {
        ULongFirst = 1,
        ULongSecond = (ulong)int.MaxValue + 1,
        ULongThird = (ulong)uint.MaxValue + 1,
        ULongFourth = ulong.MaxValue
    }

    public enum LongExampleEnum: long
    {
        LongFirst = 1,
        LongSecond = (long)int.MaxValue + 1,
        LongThird = (long)int.MinValue - 1,
        LongFourth = (long)uint.MaxValue + 1,
        LongFifth = long.MaxValue
    }

    public class ComplexAspect : OnMethodBoundaryAspect
    {
        public ComplexAspect(ByteExampleEnum[] e) : this((object)e) { }
        public ComplexAspect(SByteExampleEnum[] e): this((object)e) { }
        public ComplexAspect(ShortExampleEnum[] e): this((object)e) { }
        public ComplexAspect(UShortExampleEnum[] e): this((object)e) { }
        public ComplexAspect(IntExampleEnum[] e) : this((object)e) { }
        public ComplexAspect(UIntExampleEnum[] e): this((object)e) { }
        public ComplexAspect(ULongExampleEnum[] e): this((object)e) { }
        public ComplexAspect(LongExampleEnum[] e): this((object)e) { }

        public ComplexAspect(ByteExampleEnum e) : this((object)e) { }
        public ComplexAspect(SByteExampleEnum e) : this((object)e) { }
        public ComplexAspect(ShortExampleEnum e) : this((object)e) { }
        public ComplexAspect(UShortExampleEnum e) : this((object)e) { }
        public ComplexAspect(IntExampleEnum e) : this((object)e) { }
        public ComplexAspect(UIntExampleEnum e) : this((object)e) { }
        public ComplexAspect(ULongExampleEnum e) : this((object)e) { }
        public ComplexAspect(LongExampleEnum e) : this((object)e) { }

        public ComplexAspect(Type[] t): this((object)t) { }
        public ComplexAspect(string[] o): this((object)o) { }
        public ComplexAspect(int[] o): this((object)o) { }
        public ComplexAspect(object[] o): this((object)o) { }

        public ComplexAspect(object o)
        {
            _o = o;
        }

        object _o;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            arg.Method.DeclaringType.GetMethod("set_Result", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { GetResult() });
        }

        public string GetResult()
        {
            return GetResult(_o);
        }

        string GetResult(object o)
        {
            if (o == null)
                return "null";
            Type t = o.GetType();
            if (t.IsArray)
            {
                string typeName = t.GetElementType().Name;
                List<string> names = new List<string>();
                foreach (object obj in o as IEnumerable)
                {
                    names.Add(GetResult(obj));
                }
                
                return $"new {typeName}[] {{ {String.Join(", ", names)} }}";
            }

            if (t == typeof(String))
            {
                return $"\"{o.ToString()}\"";
            }

            if (o is Type type)
            {
                return $"typeof({type.Name})";
            }
            return o.ToString();
        }
    }
}
