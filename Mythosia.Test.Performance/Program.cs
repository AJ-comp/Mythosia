using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Mythosia.Test.Performance
{
    public class EnumerableExtensionsBenchmark
    {
        private byte[] _byteArray;
        private List<byte> _byteList;

        [GlobalSetup]
        public void Setup()
        {
            _byteArray = new byte[1000];
            _byteList = _byteArray.ToList();
        }

        [Benchmark]
        public byte[] TestAsOrToByteArrayWithArray() => _byteArray.AsOrToArray();

        [Benchmark]
        public byte[] TestAsOrToByteArrayWithList() => _byteList.AsOrToArray();
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<EnumerableExtensionsBenchmark>();
        }
    }
}