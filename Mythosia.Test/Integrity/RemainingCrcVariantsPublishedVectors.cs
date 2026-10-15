using Mythosia.Integrity.CRC;

namespace Mythosia.Test.Integrity;

internal static class RemainingCrcVariantsPublishedVectors
{
    // Fixed whole-message answers published by the upstream checksum project.
    // Model parameters match the CRC RevEng catalogue; Sensirion's SHT4x model
    // is CRC-8/NRSC-5 (31/FF/no reflection/xor00). The thirty selected nonempty
    // message_pool prefixes per model span 1..300 bytes and byte/word boundaries.
    // These are final published CRCs, not lookup-table entries, raw registers,
    // locally generated values, or bit-length variants counted as byte inputs.
    // Pool/lengths:
    // https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/crc_reference_vectors.hpp#L25-L45
    // Upstream consumer hashes exactly message_pool.first(byte_lengths[index]):
    // https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/ut_crc_vectors.cpp#L29-L44
    internal static IEnumerable<object[]> Cases()
    {
        // crc8_nrsc_5: https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/crc_reference_vectors.hpp#L336-L341
        foreach ((int length, uint expected) in new (int, uint)[]
        {
            (1, 0x6Bu),
            (2, 0xE8u),
            (3, 0xA2u),
            (4, 0x70u),
            (5, 0x72u),
            (6, 0x1Au),
            (7, 0x1Au),
            (8, 0x9Cu),
            (9, 0xC8u),
            (10, 0xBBu),
            (11, 0x49u),
            (15, 0x78u),
            (16, 0xCFu),
            (17, 0x77u),
            (23, 0x87u),
            (31, 0xFAu),
            (32, 0x8Cu),
            (33, 0xB9u),
            (47, 0xE6u),
            (63, 0xCAu),
            (64, 0x80u),
            (72, 0xC9u),
            (96, 0x3Au),
            (128, 0x78u),
            (160, 0xD1u),
            (192, 0xBFu),
            (224, 0xC8u),
            (255, 0xCEu),
            (256, 0x3Cu),
            (300, 0x9Bu),
        })
        {
            yield return new object[] { 8, (int)CRC8Type.SensirionSht4x, $"crc8_nrsc_5 published prefix {length}", MessagePool.AsSpan(0, length).ToArray(), expected };
        }

        // crc16_mcrf4xx: https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/crc_reference_vectors.hpp#L778-L784
        foreach ((int length, uint expected) in new (int, uint)[]
        {
            (1, 0xD14Au),
            (2, 0x61C7u),
            (3, 0x6A2Fu),
            (4, 0xB1C1u),
            (5, 0xA5BBu),
            (6, 0x133Cu),
            (7, 0x0C73u),
            (8, 0x979Du),
            (9, 0x04B7u),
            (10, 0xE872u),
            (11, 0x21EAu),
            (15, 0x111Bu),
            (16, 0x0A41u),
            (17, 0xCD5Eu),
            (23, 0x107Fu),
            (31, 0x0D0Du),
            (32, 0xC871u),
            (33, 0xA0EAu),
            (47, 0x6255u),
            (63, 0x5BFEu),
            (64, 0x6F3Du),
            (72, 0x3348u),
            (96, 0x0EB0u),
            (128, 0xDCA8u),
            (160, 0x3909u),
            (192, 0xEC8Eu),
            (224, 0xE6A9u),
            (255, 0x04E7u),
            (256, 0x8964u),
            (300, 0x4056u),
        })
        {
            yield return new object[] { 16, (int)CRC16Type.MCRF4XX, $"crc16_mcrf4xx published prefix {length}", MessagePool.AsSpan(0, length).ToArray(), expected };
        }

        // crc16_usb: https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/crc_reference_vectors.hpp#L922-L928
        foreach ((int length, uint expected) in new (int, uint)[]
        {
            (1, 0x4541u),
            (2, 0x49C4u),
            (3, 0xFC89u),
            (4, 0x423Du),
            (5, 0x8602u),
            (6, 0xD446u),
            (7, 0x0915u),
            (8, 0xEC89u),
            (9, 0x83ACu),
            (10, 0x7CC2u),
            (11, 0xD33Cu),
            (15, 0x3DB0u),
            (16, 0x7FBCu),
            (17, 0x1DBEu),
            (23, 0x4508u),
            (31, 0x3C21u),
            (32, 0x7EFDu),
            (33, 0x5DFFu),
            (47, 0x3B9Cu),
            (63, 0x4B96u),
            (64, 0x2C0Au),
            (72, 0xDD22u),
            (96, 0x4391u),
            (128, 0x4B09u),
            (160, 0x610Bu),
            (192, 0xA260u),
            (224, 0xEE79u),
            (255, 0x55BFu),
            (256, 0xBCD5u),
            (300, 0xE27Fu),
        })
        {
            yield return new object[] { 16, (int)CRC16Type.USB, $"crc16_usb published prefix {length}", MessagePool.AsSpan(0, length).ToArray(), expected };
        }

        // crc32_mpeg_2: https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/crc_reference_vectors.hpp#L1320-L1329
        foreach ((int length, uint expected) in new (int, uint)[]
        {
            (1, 0x1097F90Bu),
            (2, 0xFDE03DC8u),
            (3, 0xB0F47E40u),
            (4, 0x73904DF6u),
            (5, 0xD5DE161Eu),
            (6, 0xDB5DE8A4u),
            (7, 0xEE80D981u),
            (8, 0x68654C9Au),
            (9, 0xDCB30AC9u),
            (10, 0x40BAA23Bu),
            (11, 0xFF31DB1Eu),
            (15, 0x876B5625u),
            (16, 0xA5E20522u),
            (17, 0xFF57A423u),
            (23, 0x33632C3Bu),
            (31, 0x6F8666F4u),
            (32, 0x87EC1F13u),
            (33, 0xE15C35D9u),
            (47, 0x9D53813Fu),
            (63, 0xFF1829C6u),
            (64, 0xD8CB16DDu),
            (72, 0xE161714Fu),
            (96, 0xC4D0EDA6u),
            (128, 0x178BBDECu),
            (160, 0x56EAA012u),
            (192, 0x5447EC61u),
            (224, 0x3D2D6171u),
            (255, 0xD652C63Du),
            (256, 0xCB6F60F3u),
            (300, 0x37A809A1u),
        })
        {
            yield return new object[] { 32, (int)CRC32Type.Mpeg2, $"crc32_mpeg_2 published prefix {length}", MessagePool.AsSpan(0, length).ToArray(), expected };
        }

        // crc32_bzip2: https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/crc_reference_vectors.hpp#L1194-L1203
        foreach ((int length, uint expected) in new (int, uint)[]
        {
            (1, 0xEF6806F4u),
            (2, 0x021FC237u),
            (3, 0x4F0B81BFu),
            (4, 0x8C6FB209u),
            (5, 0x2A21E9E1u),
            (6, 0x24A2175Bu),
            (7, 0x117F267Eu),
            (8, 0x979AB365u),
            (9, 0x234CF536u),
            (10, 0xBF455DC4u),
            (11, 0x00CE24E1u),
            (15, 0x7894A9DAu),
            (16, 0x5A1DFADDu),
            (17, 0x00A85BDCu),
            (23, 0xCC9CD3C4u),
            (31, 0x9079990Bu),
            (32, 0x7813E0ECu),
            (33, 0x1EA3CA26u),
            (47, 0x62AC7EC0u),
            (63, 0x00E7D639u),
            (64, 0x2734E922u),
            (72, 0x1E9E8EB0u),
            (96, 0x3B2F1259u),
            (128, 0xE8744213u),
            (160, 0xA9155FEDu),
            (192, 0xABB8139Eu),
            (224, 0xC2D29E8Eu),
            (255, 0x29AD39C2u),
            (256, 0x34909F0Cu),
            (300, 0xC857F65Eu),
        })
        {
            yield return new object[] { 32, (int)CRC32Type.Bzip2, $"crc32_bzip2 published prefix {length}", MessagePool.AsSpan(0, length).ToArray(), expected };
        }

        // crc32_autosar: https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/crc_reference_vectors.hpp#L1158-L1167
        foreach ((int length, uint expected) in new (int, uint)[]
        {
            (1, 0xF21C4D38u),
            (2, 0x1D90BCE6u),
            (3, 0x3ADEE8ABu),
            (4, 0xB1E8D1FBu),
            (5, 0xF7C82094u),
            (6, 0xB7633E9Au),
            (7, 0xEED54DDEu),
            (8, 0xD6703C1Du),
            (9, 0xC6BB09E6u),
            (10, 0x7AE8FDE1u),
            (11, 0x84486D66u),
            (15, 0x82ECFF32u),
            (16, 0xF9C41556u),
            (17, 0x4D85CACBu),
            (23, 0x31F16C17u),
            (31, 0x62AAE048u),
            (32, 0xDE858BF5u),
            (33, 0x8189856Du),
            (47, 0x9BC87A09u),
            (63, 0x413B7232u),
            (64, 0x9ABF1F8Fu),
            (72, 0xFD0D3DD5u),
            (96, 0xFFD4C033u),
            (128, 0x8A1DD7CEu),
            (160, 0xD8B32CEDu),
            (192, 0x3CA99579u),
            (224, 0xFA1DF469u),
            (255, 0x130D5AF1u),
            (256, 0x88AC7B93u),
            (300, 0xC36548E9u),
        })
        {
            yield return new object[] { 32, (int)CRC32Type.AutosarP4, $"crc32_autosar published prefix {length}", MessagePool.AsSpan(0, length).ToArray(), expected };
        }

        // Sensirion SHT4x datasheet v7.3, June 2026, section 4.4, table 7, p.11.
        // https://sensirion.com/resource/datasheet/sht4x
        // Retrieved 2026-10-15, SHA256:
        // 8db4a43f17149b76811cfb504caaeca4ef844ddc710cb9b45905c51c7ddfe3c2
        yield return new object[] { 8, (int)CRC8Type.SensirionSht4x, "Sensirion SHT4x table 7 BEEF", Convert.FromHexString("BEEF"), 0x92u };

        // AUTOSAR CP R24-11, SWS_Crc_00084, printed p.26: seven literal rows.
        // https://www.autosar.org/fileadmin/standards/R24-11/CP/AUTOSAR_CP_SWS_CRCLibrary.pdf
        // PDF SHA256 496f7e2885c308f382aa2b9f9bf3f7b472546731fcde384fae622ace3cc1b92f.
        foreach ((string input, uint expected) in new (string, uint)[]
        {
            ("00000000", 0x6FB32240u),
            ("F20183", 0x4F721A25u),
            ("0FAA0055", 0x20662DF8u),
            ("00FF5511", 0x9BD7996Eu),
            ("332255AABBCCDDEEFF", 0xA65A343Du),
            ("926B55", 0xEE688A78u),
            ("FFFFFFFF", 0xFFFFFFFFu),
        })
            yield return new object[] { 32, (int)CRC32Type.AutosarP4, $"AUTOSAR P4 table {input}", Convert.FromHexString(input), expected };
    }

    private static readonly byte[] MessagePool = Convert.FromHexString(
        "58083EDB6198F0D0D9A852D8DE9C32B13AAF4E7FDB234466C0F4DF5F5B940F238474CD66358EAC021622367AAA6B3443A73D" +
        "863A876A34C9EC07A5E0C6EC9C5288A4F2F5381662DBD8EB3CAA7F1B44D355D5137FC0C3E9927A113A7B24B57DCB4EC0C2F1" +
        "3869B4D60FDCA9741420A07D227737C9BFB55652F2AC720734C943112E4EC59B0423DFD63AE4A3A23A165BAECF594B5234E6" +
        "9EEA7CD19B73AF3F543A7F289DF4342EAC366D0144CAE0C54FA1F0402042F319F9A422D5AB4C676A7C45AF7C80665E996457" +
        "C468F0D9634FAE6642927F799C0316C614412A7C0377CE0B2A4D180763CE7F2A95CBF601C8F396231A9979B6577A9848251F" +
        "261DCE5CC2BA481231B391787868660D79CF51ED7B4567526DD6E6972045CA016348067DD07C8F75F42163AD2DE271175EFB");

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
