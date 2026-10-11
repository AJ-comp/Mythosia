using System.Text;

namespace Mythosia.Test.Performance;

internal static class IntegrityBenchmarkValidation
{
    public static void Run()
    {
        VerifyKnownVectors();
        int combinations = 0;
        foreach (IntegrityAlgorithm algorithm in Enum.GetValues<IntegrityAlgorithm>())
        foreach (IntegrityInput input in Enum.GetValues<IntegrityInput>())
        foreach (int length in new IntegrityBenchmarks().PayloadLengths)
        {
            var benchmark = new IntegrityBenchmarks { Algorithm = algorithm, Input = input, Length = length };
            benchmark.Setup();
            // Repeat on the same objects to catch exhausted or mutated sources.
            benchmark.VerifyResults();
            combinations++;
        }
        Console.WriteLine($"Integrity validation passed: {combinations} parameter combinations, {combinations * 3} benchmark cases; each checked twice.");
    }

    private static void VerifyKnownVectors()
    {
        byte[] payload = Encoding.ASCII.GetBytes("123456789");
        foreach (IntegrityAlgorithm algorithm in Enum.GetValues<IntegrityAlgorithm>())
        {
            (uint value, int width) = algorithm switch
            {
                IntegrityAlgorithm.ChecksumXor or IntegrityAlgorithm.ChecksumNmea => (0x31u, 1),
                IntegrityAlgorithm.ChecksumModulo256 => (0xDDu, 1),
                IntegrityAlgorithm.ChecksumTwosComplement => (0x23u, 1),
                IntegrityAlgorithm.Crc8Classic => (0xF4u, 1),
                IntegrityAlgorithm.Crc8Maxim => (0xA1u, 1),
                IntegrityAlgorithm.Crc16Classic => (0xBB3Du, 2),
                IntegrityAlgorithm.Crc16Modbus => (0x4B37u, 2),
                IntegrityAlgorithm.Crc16Xmodem => (0x31C3u, 2),
                // Existing Mythosia DNP representation, after swapping canonical 0xEA82.
                IntegrityAlgorithm.Crc16Dnp => (0x82EAu, 2),
                IntegrityAlgorithm.Crc32Classic => (0xCBF43926u, 4),
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
            };
            byte[] expected = IntegrityBenchmarkData.PackNative(value, width);
            if (!IntegrityBenchmarkData.ComputeExpectedDigest(payload, algorithm).SequenceEqual(expected)
                || !IntegrityBenchmarkData.CreateDetector(algorithm).Compute(payload).SequenceEqual(expected))
                throw new InvalidOperationException($"Known vector mismatch: {algorithm}.");
        }
    }
}
