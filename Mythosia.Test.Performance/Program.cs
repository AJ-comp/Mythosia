using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Mythosia.Test.Performance
{
    public class EnumerableExtensionsBenchmark
    {
        private byte[] _byteArray = Array.Empty<byte>();
        private List<byte> _byteList = new();

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
            if (args.Length == 1 && args[0] == "--validate-integrity")
            {
                IntegrityBenchmarkValidation.Run();
                return;
            }

            if (args.Length == 0)
            {
                BenchmarkRunner.Run<EnumerableExtensionsBenchmark>();
                return;
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
