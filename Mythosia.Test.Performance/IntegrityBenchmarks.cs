using BenchmarkDotNet.Attributes;
using Mythosia.Integrity;

namespace Mythosia.Test.Performance;

public enum IntegrityAlgorithm
{
    ChecksumXor,
    ChecksumNmea,
    ChecksumModulo256,
    ChecksumTwosComplement,
    Crc8Classic,
    Crc8Maxim,
    Crc16Classic,
    Crc16Modbus,
    Crc16Xmodem,
    Crc16Dnp,
    Crc32Classic
}

public enum IntegrityInput
{
    Array,
    List,
    Yield
}

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class IntegrityBenchmarks
{
    private ErrorDetection _detector = null!;
    private IEnumerable<byte> _source = Array.Empty<byte>();
    private IEnumerable<byte> _validFrame = Array.Empty<byte>();
    private IEnumerable<byte> _corruptedFrame = Array.Empty<byte>();
    private byte[] _expectedDigest = Array.Empty<byte>();

    [ParamsAllValues]
    public IntegrityAlgorithm Algorithm { get; set; }

    [ParamsAllValues]
    public IntegrityInput Input { get; set; }

    [ParamsSource(nameof(PayloadLengths))]
    public int Length { get; set; }

    public IEnumerable<int> PayloadLengths => [1, 64, 4096, 65536];

    [GlobalSetup]
    public void Setup()
    {
        byte[] payload = IntegrityBenchmarkData.CreatePayload(Length);
        _detector = IntegrityBenchmarkData.CreateDetector(Algorithm);
        _expectedDigest = IntegrityBenchmarkData.ComputeExpectedDigest(payload, Algorithm);

        byte[] validFrame = new byte[payload.Length + _expectedDigest.Length];
        payload.CopyTo(validFrame, 0);
        _expectedDigest.CopyTo(validFrame, payload.Length);
        byte[] corruptedFrame = (byte[])validFrame.Clone();
        corruptedFrame[^1] ^= 1;

        _source = IntegrityBenchmarkData.CreateSource(payload, Input);
        _validFrame = IntegrityBenchmarkData.CreateSource(validFrame, Input);
        _corruptedFrame = IntegrityBenchmarkData.CreateSource(corruptedFrame, Input);

        // Validate with an independent oracle and warm lookup tables outside timing.
        VerifyResults();
    }

    // Compute is eager. Return the same object without copying or enumerating it;
    // BenchmarkDotNet otherwise rejects IEnumerable return types as deferred.
    [Benchmark]
    [BenchmarkCategory("Integrity", "Compute")]
    public object Compute() => _detector.Compute(_source);

    [Benchmark]
    [BenchmarkCategory("Integrity", "Validation")]
    public bool IsErrorValid() => _detector.IsError(_validFrame);

    [Benchmark]
    [BenchmarkCategory("Integrity", "Validation")]
    public bool IsErrorCorruptedTrailer() => _detector.IsError(_corruptedFrame);

    internal void VerifyResults()
    {
        if (!((IEnumerable<byte>)Compute()).SequenceEqual(_expectedDigest))
            throw new InvalidOperationException($"Incorrect digest: {Algorithm}/{Input}/{Length}.");
        if (IsErrorValid())
            throw new InvalidOperationException($"Valid frame rejected: {Algorithm}/{Input}/{Length}.");
        if (!IsErrorCorruptedTrailer())
            throw new InvalidOperationException($"Corrupted trailer accepted: {Algorithm}/{Input}/{Length}.");
    }
}
