using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Xunit;

namespace Mythosia.Test
{
    [DataContract]
    public class SerializeTest
    {
        [DataMember]
        public byte Value { get; set; }


        public int Size() => Marshal.SizeOf(this);


        public IEnumerable<byte> Serialize()
        {
            /*
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                DataContractSerializer serializer = new DataContractSerializer(this.GetType());
                serializer.WriteObject(memoryStream, this);
                memoryStream.Position = 0;
                return reader.ReadToEnd();
            }
            */

            throw new NotImplementedException();
        }


        public class TestClassForSystemText
        {
            [JsonPropertyName("testA")]
            public int TestA { get; set; } = 10;
            [JsonPropertyName("testB")]
            public int TestB { get; set; } = 20;
            public string TestC { get; set; } = "abc";
            public float TestD { get; set; } = 10.5f;

            public override bool Equals(object? obj)
            {
                return obj is TestClassForSystemText text &&
                       TestA == text.TestA &&
                       TestB == text.TestB &&
                       TestC == text.TestC &&
                       TestD == text.TestD;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(TestA, TestB, TestC, TestD);
            }

            public static bool operator ==(TestClassForSystemText? left, TestClassForSystemText? right)
            {
                return EqualityComparer<TestClassForSystemText>.Default.Equals(left, right);
            }

            public static bool operator !=(TestClassForSystemText? left, TestClassForSystemText? right)
            {
                return !(left == right);
            }
        }


        public class TestClassForNewtonsoft
        {
            [JsonProperty("testA")]
            public int TestA { get; set; } = 10;
            [JsonProperty("testB")]
            public int TestB { get; set; } = 20;
            public string TestC { get; set; } = "abc";
            public float TestD { get; set; } = 10.5f;

            public override bool Equals(object? obj)
            {
                return obj is TestClassForNewtonsoft newtonsoft &&
                       TestA == newtonsoft.TestA &&
                       TestB == newtonsoft.TestB &&
                       TestC == newtonsoft.TestC &&
                       TestD == newtonsoft.TestD;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(TestA, TestB, TestC, TestD);
            }

            public static bool operator ==(TestClassForNewtonsoft? left, TestClassForNewtonsoft? right)
            {
                return EqualityComparer<TestClassForNewtonsoft>.Default.Equals(left, right);
            }

            public static bool operator !=(TestClassForNewtonsoft? left, TestClassForNewtonsoft? right)
            {
                return !(left == right);
            }
        }


        [Fact]
        public void JsonSerializeTest()
        {
            TestClassForSystemText testClassS = new TestClassForSystemText();
            var jsonStringS = testClassS.ToJsonStringS();
            var compareClassS = jsonStringS.FromJsonStringS<TestClassForSystemText>();

            Assert.True(testClassS == compareClassS);


            TestClassForNewtonsoft testClassN = new TestClassForNewtonsoft();
            var jsonStringN = testClassN.ToJsonStringN();
            var compareClassN = jsonStringN.FromJsonStringN<TestClassForNewtonsoft>();

            Assert.True(testClassN == compareClassN);
        }
    }
}
