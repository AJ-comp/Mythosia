using System.Text;
using Mythosia.Integrity.CRC;

namespace Mythosia.Test.Integrity;

internal static class Crc64VariantPublishedVectors
{
    // Go names this golden column outECMA, but its reflected register and
    // all-one initial/final XOR identify CRC-64/XZ, not CRC-64/ECMA-182.
    // Only the exact nonempty input/answer facts are imported; marshal states
    // and the ISO column are omitted. No expected value is calculated here.
    // https://github.com/golang/go/blob/6e676ab2b809d46623acb5988248d95d1eb7939c/src/hash/crc64/crc64_test.go#L28-L61
    /*
     * Copyright 2009 The Go Authors.
     *
     * Redistribution and use in source and binary forms, with or without
     * modification, are permitted provided that the following conditions are
     * met:
     *
     *    * Redistributions of source code must retain the above copyright
     * notice, this list of conditions and the following disclaimer.
     *    * Redistributions in binary form must reproduce the above
     * copyright notice, this list of conditions and the following disclaimer
     * in the documentation and/or other materials provided with the
     * distribution.
     *    * Neither the name of Google LLC nor the names of its
     * contributors may be used to endorse or promote products derived from
     * this software without specific prior written permission.
     *
     * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
     * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
     * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
     * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
     * OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
     * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
     * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
     * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
     * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
     * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
     * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
     */
    internal static IEnumerable<object[]> Xz()
    {
        yield return new object[] { CRC64Type.XZ, "Go golden 01", Encoding.ASCII.GetBytes("a"), 0x330284772E652B05UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 02", Encoding.ASCII.GetBytes("ab"), 0xBC6573200E84B046UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 03", Encoding.ASCII.GetBytes("abc"), 0x2CD8094A1A277627UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 04", Encoding.ASCII.GetBytes("abcd"), 0x3C9D28596E5960BAUL };
        yield return new object[] { CRC64Type.XZ, "Go golden 05", Encoding.ASCII.GetBytes("abcde"), 0x040BDF58FB0895F2UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 06", Encoding.ASCII.GetBytes("abcdef"), 0xD08E9F8545A700F4UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 07", Encoding.ASCII.GetBytes("abcdefg"), 0xEC20A3A8CC710E66UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 08", Encoding.ASCII.GetBytes("abcdefgh"), 0x67B4F30A647A0C59UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 09", Encoding.ASCII.GetBytes("abcdefghi"), 0x9966F6C89D56EF8EUL };
        yield return new object[] { CRC64Type.XZ, "Go golden 10", Encoding.ASCII.GetBytes("abcdefghij"), 0x32093A2ECD5773F4UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 11", Encoding.ASCII.GetBytes("Discard medicine more than two years old."), 0x8A0825223EA6D221UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 12", Encoding.ASCII.GetBytes("He who has a shady past knows that nice guys finish last."), 0x8562C0AC2AB9A00DUL };
        yield return new object[] { CRC64Type.XZ, "Go golden 13", Encoding.ASCII.GetBytes("I wouldn't marry him with a ten foot pole."), 0x3EE2A39C083F38B4UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 14", Encoding.ASCII.GetBytes("Free! Free!/A trip/to Mars/for 900/empty jars/Burma Shave"), 0x1F603830353E518AUL };
        yield return new object[] { CRC64Type.XZ, "Go golden 15", Encoding.ASCII.GetBytes("The days of the digital watch are numbered.  -Tom Stoppard"), 0x02FD681D7B2421FDUL };
        yield return new object[] { CRC64Type.XZ, "Go golden 16", Encoding.ASCII.GetBytes("Nepal premier won't resign."), 0x790EF2B16A745A41UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 17", Encoding.ASCII.GetBytes("For every action there is an equal and opposite government program."), 0x3EF8F06DACCDCDDFUL };
        yield return new object[] { CRC64Type.XZ, "Go golden 18", Encoding.ASCII.GetBytes("His money is twice tainted: 'taint yours and 'taint mine."), 0x049E41B2660B106DUL };
        yield return new object[] { CRC64Type.XZ, "Go golden 19", Encoding.ASCII.GetBytes("There is no reason for any individual to have a computer in their home. -Ken Olsen, 1977"), 0x561CC0CFA235AC68UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 20", Encoding.ASCII.GetBytes("It's a tiny change to the code and not completely disgusting. - Bob Manchek"), 0xD4FE9EF082E69F59UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 21", Encoding.ASCII.GetBytes("size:  a.out:  bad magic"), 0xE3B5E46CD8D63A4DUL };
        yield return new object[] { CRC64Type.XZ, "Go golden 22", Encoding.ASCII.GetBytes("The major problem is with sendmail.  -Mark Horton"), 0x865AAF6B94F2A051UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 23", Encoding.ASCII.GetBytes("Give me a rock, paper and scissors and I will move the world.  CCFestoon"), 0x7ECA10D2F8136EB4UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 24", Encoding.ASCII.GetBytes("If the enemy is within range, then so are you."), 0xD7DD118C98E98727UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 25", Encoding.ASCII.GetBytes("It's well we cannot hear the screams/That we create in others' dreams."), 0x70FB33C119C29318UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 26", Encoding.ASCII.GetBytes("You remind me of a TV show, but that's all right: I watch it anyway."), 0x57C891E39A97D9B7UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 27", Encoding.ASCII.GetBytes("C is as portable as Stonehedge!!"), 0xA1F46BA20AD06EB7UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 28", Encoding.ASCII.GetBytes("Even if I could be Shakespeare, I think I should still choose to be Faraday. - A. Huxley"), 0x7AD25FAFA1710407UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 29", Encoding.ASCII.GetBytes("The fugacity of a constituent in a mixture of gases at a given temperature is proportional to its mole fraction.  Lewis-Randall Rule"), 0x73CEF1666185C13FUL };
        yield return new object[] { CRC64Type.XZ, "Go golden 30", Encoding.ASCII.GetBytes("How can you write a big system without C++?  -Paul Glick"), 0xB41858F73C389602UL };
        yield return new object[] { CRC64Type.XZ, "Go golden 31", Encoding.ASCII.GetBytes("This is a test of the emergency broadcast system."), 0x27DB187FC15BBC72UL };
    }

    // Exact frozen CRC-64/ECMA-182 answers from .NET 8.0.0, where the
    // non-reflected polynomial and zero initial/final XOR are explicit.
    // The upstream output hex is a numeric value, independently of the
    // native-endian digest framing used by this library.
    // https://github.com/dotnet/runtime/blob/3c6d09ef060f1523859e28d729b8f500815095a5/src/libraries/System.IO.Hashing/tests/Crc64Tests.cs#L33-L97
    /*
     * The MIT License (MIT)
     *
     * Copyright (c) .NET Foundation and Contributors
     *
     * All rights reserved.
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
    internal static IEnumerable<object[]> Ecma182()
    {
        yield return new object[] { CRC64Type.ECMA182, ".NET One", Convert.FromHexString("01"), 0x42F0E1EBA9EA3693UL };
        yield return new object[] { CRC64Type.ECMA182, ".NET { 0x00 }", Convert.FromHexString("00"), 0x0000000000000000UL };
        yield return new object[] { CRC64Type.ECMA182, ".NET { 0x01, 0x00 }", Convert.FromHexString("0100"), 0xAF052A6B538EDF09UL };
        yield return new object[] { CRC64Type.ECMA182, ".NET Zero-Residue", Convert.FromHexString("0000000000000000"), 0x0000000000000000UL };
        yield return new object[] { CRC64Type.ECMA182, ".NET Self-test 123456789", Encoding.UTF8.GetBytes("123456789"), 0x6C40DF5F0B497347UL };
        yield return new object[] { CRC64Type.ECMA182, ".NET Self-test residue", Convert.FromHexString("3132333435363738396C40DF5F0B497347"), 0x0000000000000000UL };
        yield return new object[] { CRC64Type.ECMA182, ".NET The quick brown fox jumps over the lazy dog", Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog"), 0x41E05242FFA9883BUL };
        yield return new object[] { CRC64Type.ECMA182, ".NET Lorem ipsum 256", Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent non ipsum quis mauris euismod hendrerit id sed lacus. Duis quam neque, porta et volutpat nec, tempor eget nisl. Nunc quis leo quis nisi mattis molestie. Donec a diam velit. Sed a tempus nec."), 0xDA70046E6B79DD83UL };
        yield return new object[] { CRC64Type.ECMA182, ".NET Lorem ipsum 272", Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent non ipsum quis mauris euismod hendrerit id sed lacus. Duis quam neque, porta et volutpat nec, tempor eget nisl. Nunc quis leo quis nisi mattis molestie. Donec a diam velit. Sed a tempus nec1234567890abcdef."), 0xA94F5E9C5557F65AUL };
        yield return new object[] { CRC64Type.ECMA182, ".NET Lorem ipsum 384", Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam lobortis non felis et pretium. Suspendisse commodo dignissim sagittis. Etiam vestibulum luctus mollis. Ut finibus, nisl in sodales sagittis, leo mauris sollicitudin odio, id sodales nisl ante vitae quam. Nunc ut mi at lacus ultricies efficitur vitae eu ligula. Donec tincidunt, nisi suscipit facilisis auctor, metus non."), 0x5768E3F2E9A63829UL };
        yield return new object[] { CRC64Type.ECMA182, ".NET Lorem ipsum 1001", Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer ac urna vitae nibh sagittis porttitor et vel ante. Ut molestie sit amet velit ac mattis. Sed ullamcorper nunc non neque imperdiet, vehicula bibendum sapien efficitur. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Suspendisse potenti. Duis sem dui, malesuada non pharetra at, feugiat id mi. Nulla facilisi. Fusce a scelerisque magna. Ut leo justo, auctor quis nisi et, sollicitudin pretium odio. Sed eu nibh mollis, pretium lectus nec, posuere nulla. Morbi ac euismod purus. Morbi rhoncus leo est, at volutpat nunc pretium in. Aliquam erat volutpat. Curabitur eu lacus mollis, varius lectus ut, tincidunt eros. Nullam a velit hendrerit, euismod magna id, fringilla sem. Phasellus scelerisque hendrerit est, vel imperdiet enim auctor a. Aenean vel ultricies nunc. Suspendisse ac tincidunt urna. Nulla tempor dolor ut ligula accumsan, tempus auctor massa gravida. Aenean non odio et augue pellena."), 0x3ECF3A363FC5BD59UL };

        // XCKS/ZXCKS 1.0.0 specification, test-vector definitions and the
        // CRC-64 answer column. These are numeric input/output facts only;
        // no algorithm implementation, document prose or harness is copied.
        // Snapshot retrieved 2026-10-15; SHA256 of the complete HTML:
        // edd5bf91c46101006a2e75863002db01edf88df813157992c38376283f3a0f76
        // https://xcks.sourceforge.net/xcks-specs/xcks-specs.html#xcks-specs-checksum-algorithms-test-vectors
        // UTF-8 octets are copied from the published hexadecimal definitions,
        // including TV10's U+00A0 and U+2019. Empty TV01 and duplicate TV08
        // are omitted; TV08 is already the .NET quick-brown-fox input above.
        yield return new object[] { CRC64Type.ECMA182, "XCKS TV02", Convert.FromHexString("616263"), 0x66501A349A0E0855UL };
        yield return new object[] { CRC64Type.ECMA182, "XCKS TV03", Convert.FromHexString("D0B0D0B1D0B2"), 0xBD610400506D2A6AUL };
        yield return new object[] { CRC64Type.ECMA182, "XCKS TV04", Convert.FromHexString("CEB1CEB2CEB3"), 0xEFF0002CA8161896UL };
        yield return new object[] { CRC64Type.ECMA182, "XCKS TV05", Convert.FromHexString("E2A081E2A083E2A089"), 0x567C6CDAE10624FFUL };
        yield return new object[] { CRC64Type.ECMA182, "XCKS TV06", Convert.FromHexString("3133333720357C2A33347C3C"), 0xCCC01732E4416DF7UL };
        yield return new object[] { CRC64Type.ECMA182, "XCKS TV07", Convert.FromHexString("30313233343536373839"), 0x2A71AB4164C3BBE8UL };
        yield return new object[] { CRC64Type.ECMA182, "XCKS TV09", Convert.FromHexString("4D6F6E7369657572204A61636B2C20766F75732064616374796C6F677261706869657A206269656E206D696575782071756520576F6C66"), 0x60D88D674DEC0717UL };
        yield return new object[] { CRC64Type.ECMA182, "XCKS TV10", Convert.FromHexString("44616E676C617273207375697669742045646D6F6E64206574204D657263C3A964C3A873206465732079657578206A75737175E28099C3A020636520717565206C6573206465757820616D616E74732065757373656E74206469737061727520C3A0206CE28099756E2064657320616E676C657320647520666F7274205361696E742D4E69636F6C6173C2A03B20707569732C207365207265746F75726E616E7420616C6F72732C20696C2061706572C3A77574204665726E616E642C2071756920C3A974616974207265746F6D62C3A92070C3A26C65206574206672C3A96D697373616E7420737572207361206368616973652C2074616E646973207175652043616465726F757373652062616C62757469616974206C6573207061726F6C65732064E28099756E65206368616E736F6E20C3A020626F6972652E"), 0xC203567B583ED9C8UL };

        // Classic hash-battery numeric facts (six distinct nonempty inputs),
        // originally published by Classless.net/Jason Simeone, copyright 2009,
        // MPL 1.1. No source implementation or test harness is copied.
        // Inputs:
        // https://github.com/divyang4481/classless-hasher/blob/77d4f6b73e93a52ff3a0b758207021fd910d4f1c/HasherTests/TestVectors.cs#L36-L45
        // Answers:
        // https://github.com/divyang4481/classless-hasher/blob/77d4f6b73e93a52ff3a0b758207021fd910d4f1c/HasherTests/TestVectorsHash.cs#L383-L392
        yield return new object[] { CRC64Type.ECMA182, "Hash battery a", Encoding.ASCII.GetBytes("a"), 0x548F120162451C62UL };
        yield return new object[] { CRC64Type.ECMA182, "Hash battery alphabet", Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz"), 0x97A2566B552FCC4EUL };
        yield return new object[] { CRC64Type.ECMA182, "Hash battery alphanumeric", Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"), 0x5CA18585B92C58B9UL };
        yield return new object[] { CRC64Type.ECMA182, "Hash battery numeric", Encoding.ASCII.GetBytes("1234567890"), 0x27B28F2E2139D6A1UL };
        yield return new object[] { CRC64Type.ECMA182, "Hash battery numeric80", Encoding.ASCII.GetBytes("12345678901234567890123456789012345678901234567890123456789012345678901234567890"), 0xBE4C46263774953CUL };
        yield return new object[] { CRC64Type.ECMA182, "Hash battery message", Encoding.ASCII.GetBytes("message digest"), 0xC04D61278997BA5EUL };

        // Five binary input/output facts from EmbeddedUtilityLibraries,
        // Mateusz Stadnik, copyright 2021, GPL 3.0-or-later. No implementation,
        // test macro or harness source is copied. Overlapping inputs omitted.
        // https://github.com/matgla/EmbeddedUtilityLibraries/blob/40e88332115b372e7fb31fe2f350f30c286d7e4f/test/UT/crc/Crc64Tests.cpp#L27-L40
        yield return new object[] { CRC64Type.ECMA182, "Binary FF", Convert.FromHexString("FF"), 0x9AFCE626CE85B507UL };
        yield return new object[] { CRC64Type.ECMA182, "Binary 10", Convert.FromHexString("10"), 0x663D78FF90E185EFUL };
        yield return new object[] { CRC64Type.ECMA182, "Binary 0203ABCDEF", Convert.FromHexString("0203ABCDEF"), 0xADBCE7F22506ACB7UL };
        yield return new object[] { CRC64Type.ECMA182, "Binary 0000", Convert.FromHexString("0000"), 0x0000000000000000UL };
        yield return new object[] { CRC64Type.ECMA182, "Binary FFFFFFFF", Convert.FromHexString("FFFFFFFF"), 0xD2F9D878539E5AD0UL };

        // Two independent numeric facts from magiclen/crc-any's CRC64
        // (ECMA-182) example and test. Published big-endian bytes represent
        // these uint64 values; no external implementation is copied.
        // https://github.com/magiclen/crc-any/blob/83b653820662f3aef5edeb575c47b56e95920f11/README.md#L116-L121
        yield return new object[] { CRC64Type.ECMA182, "crc-any hello", Encoding.ASCII.GetBytes("hello"), 0x40544A306137B6ECUL };
        // https://github.com/magiclen/crc-any/blob/ad896713056e2f0959a91c14ded8b15814f9cf3d/tests/alloc.rs#L65-L70
        yield return new object[] { CRC64Type.ECMA182, "crc-any URL", Encoding.ASCII.GetBytes("https://magiclen.org"), 0x2EDB6855240A60F8UL };
    }
}
