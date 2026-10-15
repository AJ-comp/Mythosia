using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{
    internal static class XxHash64Core
    {
        internal static ulong Compute(IEnumerable<byte> source, ulong seed)
        {
            var accumulator = new XxHash64Accumulator(seed);
            if (source is byte[] bytes) accumulator.Append(bytes);
            else foreach (byte value in source) accumulator.AppendByte(value);
            return accumulator.GetCurrentValue();
        }
    }
}
