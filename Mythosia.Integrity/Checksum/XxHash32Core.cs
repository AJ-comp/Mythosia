using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{
    internal static class XxHash32Core
    {
        internal static uint Compute(IEnumerable<byte> source, uint seed)
        {
            var accumulator = new XxHash32Accumulator(seed);
            if (source is byte[] bytes) accumulator.Append(bytes);
            else foreach (byte value in source) accumulator.AppendByte(value);
            return accumulator.GetCurrentValue();
        }
    }
}
