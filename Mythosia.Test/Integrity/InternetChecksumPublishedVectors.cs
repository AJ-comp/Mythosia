namespace Mythosia.Test.Integrity;

// Frozen public packet-checksum answers from google/gopacket v1.1.19, commit
// a9779d139771f6a06fc983b18e0efd23ca30222f. These expected values are the
// checksum fields already present in the published fixtures, not values
// calculated by Mythosia or by our independent reference implementation.
//
// IPv4 cases use exactly IHL * 4 header bytes, with bytes 10-11 replaced by zero.
// ICMP cases use the message with bytes 2-3 replaced by zero. TCP/UDP cases use
// the RFC IPv4 pseudoheader (source, destination, 00, protocol, 16-bit length)
// followed by the segment with its checksum field replaced by zero. No payload
// is omitted from those transport cases. Fixture offsets are zero based.
// Malformed DNS payloads remain valid UDP checksum inputs; no DNS parsing is used.
//
// Copyright and full BSD license for the adapted packet data:
/*
 * Copyright (c) 2012 Google, Inc. All rights reserved.
 * Copyright (c) 2009-2011 Andreas Krennmair. All rights reserved.
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
 *    * Neither the name of Andreas Krennmair, Google, nor the names of its
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
internal static class InternetChecksumPublishedVectors
{
    public static IEnumerable<object[]> Cases()
    {
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L23
        // testSimpleTCPPacket, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "decode_test.go:23:14 IPv4", Convert.FromHexString("450001A439DF400040060000AC115149ADDEFEE1"), (ushort)0x555A };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L533
        // smallPacket, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "decode_test.go:533:14 IPv4", Convert.FromHexString("450000280000400040060000AC1151C5AC115149"), (ushort)0x3F9F };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L1019
        // data, IPv4 starts at fixture byte 18; IPv4.
        yield return new object[] { "decode_test.go:1019:18 IPv4", Convert.FromHexString("4560003C0FA900006E11000047E6EE2EAC165973"), (ushort)0x010A };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L1086
        // testICMP, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "decode_test.go:1086:14 IPv4", Convert.FromHexString("4500003800000000FC010000480EDEE2AC1D140F"), (ushort)0xD7A7 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L1086
        // testICMP, IPv4 starts at fixture byte 42; IPv4.
        yield return new object[] { "decode_test.go:1086:42 IPv4", Convert.FromHexString("4520004D000040003E110000AC1D140F0A4249C9"), (ushort)0x2849 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L1149
        // testMPLS, IPv4 starts at fixture byte 18; IPv4.
        yield return new object[] { "decode_test.go:1149:18 IPv4", Convert.FromHexString("45000064000B0000FF0100000A0102010A220001"), (ushort)0xA569 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L1188
        // testPPPGREIPv4IPv6VLAN, IPv4 starts at fixture byte 58; IPv4.
        yield return new object[] { "decode_test.go:1188:58 IPv4", Convert.FromHexString("4500008B8CAF0000402F0000100000C8C034A69A"), (ushort)0x75FE };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L1188
        // testPPPGREIPv4IPv6VLAN, IPv4 starts at fixture byte 98; IPv4.
        yield return new object[] { "decode_test.go:1188:98 IPv4", Convert.FromHexString("45000063000040003C110000AC102C0308080808"), (ushort)0x5667 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L1261
        // testPFLogUDP, IPv4 starts at fixture byte 64; IPv4.
        yield return new object[] { "decode_test.go:1261:64 IPv4", Convert.FromHexString("4500002C0216000040110000AC17E8CCAC17E8FF"), (ushort)0x4EB0 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L1309
        // testPacketMPLSInMPLS, IPv4 starts at fixture byte 22; IPv4.
        yield return new object[] { "decode_test.go:1309:22 IPv4", Convert.FromHexString("4500006400500000FF0100000A1F00010A220001"), (ushort)0xA706 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L1341
        // testPacketIPv4Fragmented, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "decode_test.go:1341:14 IPv4", Convert.FromHexString("4500003800F22000401100000A010101816F1E1B"), (ushort)0xAF37 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/udp_test.go#L36
        // testUDPPacketDNS, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "udp_test.go:36:14 IPv4", Convert.FromHexString("450000E668CF00003F110000AC10FF01AC1D140F"), (ushort)0xA6F9 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/tcp_test.go#L70
        // testPacketTCPOptionDecode, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "tcp_test.go:70:14 IPv4", Convert.FromHexString("450000340000000080060000C0A80001C0A80002"), (ushort)0xB970 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L26
        // testPacketDNSRegression, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "dns_test.go:26:14 IPv4", Convert.FromHexString("4500004489C40000381100006DC2A0045FD35C0E"), (ushort)0x2F3D };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L50
        // testParseDNSTypeTXT, IPv4 starts at fixture byte 4; IPv4.
        yield return new object[] { "dns_test.go:50:4 IPv4", Convert.FromHexString("450000730000400039110000D043DEDE0ABA2306"), (ushort)0x6498 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L81
        // testParseDNSBadVers, IPv4 starts at fixture byte 4; IPv4.
        yield return new object[] { "dns_test.go:81:4 IPv4", Convert.FromHexString("45000038A5A0400038110000C00505F1AC1E2A43"), (ushort)0x00BD };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L159
        // testParseDNSTypeURI, IPv4 starts at fixture byte 4; IPv4.
        yield return new object[] { "dns_test.go:159:4 IPv4", Convert.FromHexString("4500006F3E650000401100007F0000017F000001"), (ushort)0x3E17 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L595
        // testDNSMalformedPacket, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "dns_test.go:595:14 IPv4", Convert.FromHexString("45000039D4310000F31100000A4D2B830A010011"), (ushort)0xB3A0 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L623
        // testDNSMalformedPacket2, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "dns_test.go:623:14 IPv4", Convert.FromHexString("4500007937674000391100000A4D00F50A01002D"), (ushort)0xF49D };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L681
        // testBlankNameRootQuery, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "dns_test.go:681:14 IPv4", Convert.FromHexString("4500021876B24000721100000A4D001A0A0100E9"), (ushort)0x7AD2 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L821
        // testAnotherMalformedDNS, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "dns_test.go:821:14 IPv4", Convert.FromHexString("450005C12EEA4000361100000A4D001D0A010006"), (ushort)0xFBD1 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L938
        // testMalformedDNSAgain, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "dns_test.go:938:14 IPv4", Convert.FromHexString("450000912DFF0000781100000A4D00040A010029"), (ushort)0xFFE2 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L971
        // testMalformedDNSOhGodMakeItStop, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "dns_test.go:971:14 IPv4", Convert.FromHexString("45000079C51C4000351100000A4D00130A010013"), (ushort)0x6BE4 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L1048
        // testPacketDNSPanic7, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "dns_test.go:1048:14 IPv4", Convert.FromHexString("4500021CB5CA4000FA1100000A4D000B0A010043"), (ushort)0xB46A };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/gre_test.go#L27
        // testPacketGRE, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "gre_test.go:27:14 IPv4", Convert.FromHexString("4500006C843C4000402F0000C0A80101C0A80102"), (ushort)0x32D3 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/gre_test.go#L27
        // testPacketGRE, IPv4 starts at fixture byte 38; IPv4.
        yield return new object[] { "gre_test.go:27:38 IPv4", Convert.FromHexString("450000540488400040010000AC100101AC100201"), (ushort)0xDAFE };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/gre_test.go#L145
        // testPacketEthernetOverGRE, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "gre_test.go:145:14 IPv4", Convert.FromHexString("4500007A0ACD4000402F0000C0A80101C0A80102"), (ushort)0xAC34 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/gre_test.go#L145
        // testPacketEthernetOverGRE, IPv4 starts at fixture byte 52; IPv4.
        yield return new object[] { "gre_test.go:145:52 IPv4", Convert.FromHexString("45000054D970400040010000AC100101AC100102"), (ushort)0x0715 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/gtp_test.go#L27
        // testGTPPacket, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "gtp_test.go:27:14 IPv4", Convert.FromHexString("4500007C0000400040110000C0A828B2C0A828B3"), (ushort)0x67BB };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/gtp_test.go#L27
        // testGTPPacket, IPv4 starts at fixture byte 54; IPv4.
        yield return new object[] { "gtp_test.go:27:54 IPv4", Convert.FromHexString("450000540676000040010000C0A828B2CA0B289E"), (ushort)0x982F };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/igmp_test.go#L21
        // igmpv1MembershipReportPacket, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "igmp_test.go:21:14 IPv4", Convert.FromHexString("4500001C6A7F0000010200000A3C0084E000013C"), (ushort)0x6365 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/igmp_test.go#L53
        // igmpv2MembershipQueryPacket, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "igmp_test.go:53:14 IPv4", Convert.FromHexString("45C0001C01530000010200000A3C00BDE0000001"), (ushort)0xCCD3 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/igmp_test.go#L84
        // igmpv2MembershipReportPacket, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "igmp_test.go:84:14 IPv4", Convert.FromHexString("4600002079F00000010200000A3C0567EFFFFFFD94040000"), (ushort)0xAB47 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/igmp_test.go#L115
        // igmp3v3MembershipQueryPacket, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "igmp_test.go:115:14 IPv4", Convert.FromHexString("46C0002417F1400001020000C0A801FEE000000194040000"), (ushort)0x297B };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/igmp_test.go#L147
        // igmpv3MembershipReport2Records, IPv4 starts at fixture byte 14; IPv4.
        yield return new object[] { "igmp_test.go:147:14 IPv4", Convert.FromHexString("46580030013C000001020000C0A80142E000001694040000"), (ushort)0x8133 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L533
        // smallPacket, IPv4 starts at fixture byte 14; TCP+IPv4 pseudoheader.
        yield return new object[] { "decode_test.go:533:14 TCP+IPv4 pseudoheader", Convert.FromHexString("AC1151C5AC1151490006001400639AEF000000002EC127835014000000000000"), (ushort)0xC308 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/decode_test.go#L1086
        // testICMP, IPv4 starts at fixture byte 14; ICMPv4.
        yield return new object[] { "decode_test.go:1086:14 ICMPv4", Convert.FromHexString("030D0000000000004520004D000040003E112849AC1D140F0A4249C98ECC62E10039769D"), (ushort)0x946E };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L26
        // testPacketDNSRegression, IPv4 starts at fixture byte 14; UDP+IPv4 pseudoheader.
        yield return new object[] { "dns_test.go:26:14 UDP+IPv4 pseudoheader", Convert.FromHexString("6DC2A0045FD35C0E00110030E1A6003500300000F6180010000100000000000108706963736C69666502727500000100010000291000000080000000"), (ushort)0xA597 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L50
        // testParseDNSTypeTXT, IPv4 starts at fixture byte 4; UDP+IPv4 pseudoheader.
        yield return new object[] { "dns_test.go:50:4 UDP+IPv4 pseudoheader", Convert.FromHexString("D043DEDE0ABA23060011005F003581B2005F000098718180000100010000000106676F6F676C6503636F6D0000100001C00C0010000100000E10002423763D7370663120696E636C7564653A5F7370662E676F6F676C652E636F6D207E616C6C0000291000000000000000"), (ushort)0xDCB5 };
        // https://github.com/google/gopacket/blob/a9779d139771f6a06fc983b18e0efd23ca30222f/layers/dns_test.go#L595
        // testDNSMalformedPacket, IPv4 starts at fixture byte 14; UDP+IPv4 pseudoheader.
        yield return new object[] { "dns_test.go:595:14 UDP+IPv4 pseudoheader", Convert.FromHexString("0A4D2B830A01001100110025ED2E003500250000474554202F20485454502F312E310D0A486F73743A207777770D0A0D0A"), (ushort)0x0832 };
    }
}
