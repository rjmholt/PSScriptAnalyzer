
using Microsoft.PowerShell.ScriptAnalyzer.Configuration.Psd;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Xunit;

namespace ScriptAnalyzer2.Test
{
    public class PsdTypedObjectConverterTests
    {
        private readonly PsdTypedObjectConverter _converter;

        public PsdTypedObjectConverterTests()
        {
            _converter = new PsdTypedObjectConverter();
        }

        [Fact]
        public void TestSimpleFieldObject()
        {
            var obj = _converter.ParseAndConvert<SimpleFieldObject>("@{ Field = 'Banana' }");
            Assert.Equal("Banana", obj.Field);
        }

        [Fact]
        public void TestSimplePropertyObject()
        {
            var obj = _converter.ParseAndConvert<SimplePropertyObject>("@{ Property = 'Goose' }");
            Assert.Equal("Goose", obj.Property);
        }

        [Fact]
        public void TestSimpleReadOnlyFieldObject()
        {
            var obj = _converter.ParseAndConvert<SimpleReadOnlyFieldObject>("@{ ReadOnlyField = 'Goose' }");
            Assert.Equal("Goose", obj.ReadOnlyField);
        }

        [Fact]
        public void TestSimpleReadOnlyPropertyObject()
        {
            var obj = _converter.ParseAndConvert<SimpleReadOnlyPropertyObject>("@{ ReadOnlyProperty = 'Goose' }");
            Assert.Equal("Goose", obj.ReadOnlyProperty);
        }

        [Fact]
        public void TestNumerics()
        {
            var obj = _converter.ParseAndConvert<NumericObject>(@"
@{
    SByte   = 3
    Byte    = 4
    Short   = 5
    Int     = 6
    Long    = 7
    UShort  = 8
    UInt    = 9
    ULong   = 10
    Decimal = 11.1
    Float   = 12.2
    Double  = 13.3
}");
            Assert.Equal((sbyte)3, obj.SByte);
            Assert.Equal((byte)4, obj.Byte);
            Assert.Equal((short)5, obj.Short);
            Assert.Equal(6, obj.Int);
            Assert.Equal(7, obj.Long);
            Assert.Equal((ushort)8u, obj.UShort);
            Assert.Equal(9u, obj.UInt);
            Assert.Equal(10u, obj.ULong);
            Assert.Equal(11.1m, obj.Decimal);
            Assert.Equal(12.2f, obj.Float);
            Assert.Equal(13.3, obj.Double);
        }

        [Fact]
        public void DateTimeTest()
        {
            var expected = new DateTime(ticks: 637191090850000000);
            var obj = _converter.ParseAndConvert<DateTimeObject>("@{ DateTime = '6/03/2020 4:31:25 PM' }");
            Assert.Equal(expected, obj.DateTime);
        }

        [Fact]
        public void DataContractTest()
        {
            var obj = _converter.ParseAndConvert<DataContractObject>("@{ FirstName = 'Raymond'; MiddleName = 'Luxury'; FamilyName = 'Yacht' }");
            Assert.Equal("Raymond", obj.FirstName);
            Assert.Equal("Yacht", obj.LastName);
            Assert.Null(obj.MiddleName);
        }
    }

    public class SimpleFieldObject
    {
        public string Field;
    }

    public class SimplePropertyObject
    {
        public string Property { get; set; }
    }

    public class SimpleReadOnlyFieldObject
    {
        [JsonConstructor]
        public SimpleReadOnlyFieldObject(string readOnlyField)
        {
            ReadOnlyField = readOnlyField;
        }

        public readonly string ReadOnlyField;
    }

    public class SimpleReadOnlyPropertyObject
    {
        [JsonConstructor]
        public SimpleReadOnlyPropertyObject(string readOnlyProperty)
        {
            ReadOnlyProperty = readOnlyProperty;
        }

        public string ReadOnlyProperty { get; }
    }

    public class NumericObject
    {
        public sbyte SByte;

        public byte Byte;

        public short Short;

        public int Int;

        public long Long;

        public ushort UShort;

        public uint UInt;

        public ulong ULong;

        public decimal Decimal;

        public float Float;

        public double Double;
    }

    public class DateTimeObject
    {
        public DateTime DateTime;
    }

    [DataContract]
    public class DataContractObject
    {
        [DataMember]
        public string FirstName { get; set; }

        [DataMember(Name = "FamilyName")]
        public string LastName { get; set; }

        public string MiddleName { get; set; }
    }

}
