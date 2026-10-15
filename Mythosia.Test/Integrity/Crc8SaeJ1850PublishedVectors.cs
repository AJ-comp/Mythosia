using System;
using System.Collections.Generic;

namespace Mythosia.Test.Integrity;

internal static class Crc8SaeJ1850PublishedVectors
{
    // Complete CRC-8/SAE-J1850: width 8, polynomial 1D, init FF,
    // refin/refout false, xorout FF. Every answer below is a frozen
    // input/output fact published upstream, independently checked bit by bit.
    // No lookup-table entry, generated answer, or derived codeword counts here.
    internal static IEnumerable<object[]> Cases()
    {
        // AUTOSAR CP R24-11, sections 7.2.1.1, SWS_Crc_00073/00074,
        // printed page 20: seven explicit vectors and the ASCII 123456789 check.
        // https://www.autosar.org/fileadmin/standards/R24-11/CP/AUTOSAR_CP_SWS_CRCLibrary.pdf
        // PDF SHA256: 496F7E2885C308F382AA2B9F9BF3F7B472546731FCDE384FAE622ACE3CC1B92F
        yield return Case("AUTOSAR R24-11 table 1", "00000000", 0x59);
        yield return Case("AUTOSAR R24-11 table 2", "F20183", 0x37);
        yield return Case("AUTOSAR R24-11 table 3", "0FAA0055", 0x79);
        yield return Case("AUTOSAR R24-11 table 4", "00FF5511", 0xB8);
        yield return Case("AUTOSAR R24-11 table 5", "332255AABBCCDDEEFF", 0xCB);
        yield return Case("AUTOSAR R24-11 table 6", "926B55", 0x8C);
        yield return Case("AUTOSAR R24-11 table 7", "FFFFFFFF", 0x74);
        yield return Case("AUTOSAR R24-11 check", "313233343536373839", 0x4B);

        // Only five additional numeric input/output facts are transcribed;
        // the GPL-3.0 project's implementation, macros, and harness are omitted.
        // Its duplicate check and four-FF rows are already represented above.
        // https://github.com/matgla/EmbeddedUtilityLibraries/blob/40e88332115b372e7fb31fe2f350f30c286d7e4f/test/UT/crc/Crc8Tests.cpp#L27-L50
        yield return Case("EmbeddedUtilityLibraries 02 03 FF", "0203FF", 0xE2);
        yield return Case("EmbeddedUtilityLibraries 02 FF FF", "02FFFF", 0x77);
        yield return Case("EmbeddedUtilityLibraries two zeros", "0000", 0xBE);
        yield return Case("EmbeddedUtilityLibraries one zero", "00", 0x3B);
        yield return Case("EmbeddedUtilityLibraries one FF", "FF", 0xFF);

        // Fixed Python string.digits fixtures: reversed 1-9, all digits,
        // and reversed all digits. Empty and duplicate check rows are omitted.
        // https://github.com/Nicoretti/crc/blob/4e65ac45056afb9b6d60a45df4573ecf5622043b/test/unit/test_crc.py#L207-L221
        yield return Case("Nicoretti reversed 1-9", "393837363534333231", 0xD6);
        yield return Case("Nicoretti all digits", "30313233343536373839", 0x74);
        yield return Case("Nicoretti reversed all digits", "39383736353433323130", 0xC7);

        // Exact ASCII data2 and fixed expected hex string "34".
        // https://github.com/ratriches/crc/blob/7af57cb68d6c9143ee4b0451ba6a8b09666f75e4/test/crcCalc.test.js#L3-L4
        // https://github.com/ratriches/crc/blob/7af57cb68d6c9143ee4b0451ba6a8b09666f75e4/test/crcCalc.test.js#L118-L127
        yield return Case("ratriches mixed 50 bytes", "313233343536373839306162636465666768696A4142434445464748494A3132333435363738393031323334353637383930", 0x34);

        // The upstream fixture freezes complete-message CRCs for these exact
        // byte_lengths prefixes of its random 300-byte message_pool. They are
        // final CRCs, not internal registers or lookup-table coefficients.
        // This selection covers short lengths and byte/word/block boundaries.
        // https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/crc_reference_vectors.hpp#L25-L45
        // https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/crc_reference_vectors.hpp#L366-L377
        // https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/ut_crc_vectors.cpp#L29-L44
        foreach ((int length, byte expected) in new (int, byte)[]
        {
            (1, 0x0D),
            (2, 0x52),
            (3, 0x33),
            (4, 0x61),
            (5, 0x3B),
            (7, 0xA9),
            (8, 0x97),
            (15, 0x21),
            (16, 0xD0),
            (31, 0x4D),
            (32, 0x09),
            (63, 0xF8),
            (64, 0x48),
            (72, 0x7E),
            (96, 0x20),
            (128, 0x4B),
            (160, 0x05),
            (192, 0x98),
            (224, 0x45),
            (255, 0x53),
            (256, 0x7C),
            (257, 0x05),
            (300, 0x80),
        })
        {
            yield return new object[]
            {
                $"checksum published prefix {length} bytes",
                MessagePool.AsSpan(0, length).ToArray(),
                expected,
            };
        }
    }

    private static object[] Case(string name, string hexData, byte expected)
        => new object[] { name, Convert.FromHexString(hexData), expected };

    private static readonly byte[] MessagePool = Convert.FromHexString(
        "58083EDB6198F0D0D9A852D8DE9C32B13AAF4E7FDB234466C0F4DF5F5B940F238474CD66358EAC021622367AAA6B3443" +
        "A73D863A876A34C9EC07A5E0C6EC9C5288A4F2F5381662DBD8EB3CAA7F1B44D355D5137FC0C3E9927A113A7B24B57DCB" +
        "4EC0C2F13869B4D60FDCA9741420A07D227737C9BFB55652F2AC720734C943112E4EC59B0423DFD63AE4A3A23A165BAE" +
        "CF594B5234E69EEA7CD19B73AF3F543A7F289DF4342EAC366D0144CAE0C54FA1F0402042F319F9A422D5AB4C676A7C45" +
        "AF7C80665E996457C468F0D9634FAE6642927F799C0316C614412A7C0377CE0B2A4D180763CE7F2A95CBF601C8F39623" +
        "1A9979B6577A9848251F261DCE5CC2BA481231B391787868660D79CF51ED7B4567526DD6E6972045CA016348067DD07C" +
        "8F75F42163AD2DE271175EFB");

    // Upstream fixture license notices follow.
    /*
     * Copyright (c) 2016, Nicola Coretti
     * All rights reserved.
     *
     * Redistribution and use in source and binary forms, with or without
     * modification, are permitted provided that the following conditions are met:
     *
     *  1. Redistributions of source code must retain the above copyright notice, this
     *     list of conditions and the following disclaimer.
     *  2. Redistributions in binary form must reproduce the above copyright notice,
     *     this list of conditions and the following disclaimer in the documentation
     *     and/or other materials provided with the distribution.
     *
     * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
     * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
     * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
     * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
     * ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
     * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
     * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
     * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
     * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
     * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
     */

    /*
     * (The MIT License)
     *
     * Copyright (c) 2022 Ricardo A.T. <ratriches@yahoo.com.br>, Bastian Molkenthin <http://www.sunshine2k.de/>
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to deal
     * in the Software without restriction, including without limitation the rights
     * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
     * copies of the Software, and to permit persons to whom the Software is
     * furnished to do so, subject to the following conditions:
     *
     * The above copyright notice and this permission notice shall be included in
     * all copies or substantial portions of the Software.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
     * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
     * THE SOFTWARE.
     */

    /*
     * MIT License
     *
     * Copyright (c) 2026 Jan Macheta
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to deal
     * in the Software without restriction, including without limitation the rights
     * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
     * copies of the Software, and to permit persons to whom the Software is
     * furnished to do so, subject to the following conditions:
     *
     * The above copyright notice and this permission notice shall be included in all
     * copies or substantial portions of the Software.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
     * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
     * SOFTWARE.
     */
}
