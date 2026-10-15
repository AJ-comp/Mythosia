using System.IO.Compression;
using System.Security.Cryptography;
using Xunit;

namespace Mythosia.Test.Integrity;

internal static class Fletcher4PublishedVectors
{
    // Thirty literal Fletcher4 checkpoints from an OpenZFS send fixture.
    // https://github.com/openzfs/zfs/blob/34f96a15c73eab27dd6ad17bb5f1263bf26e37d7/tests/zfs-tests/tests/functional/rsend/dedup.zsend.bz2
    // Original BZip2 SHA256: 9CFB7B1F4A003C88DBF3EF1CAE8E813E8AE5E298749978801E8C66206B98C51F.
    // Decompressed full fixture: 241240 bytes, SHA256:
    // 40805840694CB25FB90B064881659981252F651DC60962935423D9077C883C68.
    // This data holder losslessly GZip-compresses source bytes [1496,129088).
    // Each answer is the four little-endian uint64 words physically stored at
    // recordOffset+280; its input starts at BEGIN offset 1496 and ends just
    // before that checkpoint. Earlier headers, payloads and checkpoint fields
    // remain in the input exactly as published. No expected value is generated.
    // The upstream input mapping is explicit in:
    // https://github.com/openzfs/zfs/blob/34f96a15c73eab27dd6ad17bb5f1263bf26e37d7/module/zfs/dmu_send.c#L257-L284
    // https://github.com/openzfs/zfs/blob/34f96a15c73eab27dd6ad17bb5f1263bf26e37d7/include/sys/zfs_ioctl.h#L374-L387
    // Upstream source license: CDDL-1.0; see TestData/OpenZfs.CDDL.txt.
    private static readonly byte[] Source = LoadFixture();

    internal static TheoryData<string, byte[], ulong, ulong, ulong, ulong> Cases
    {
        get
        {
            var cases = new TheoryData<string, byte[], ulong, ulong, ulong, ulong>();
            cases.Add("OpenZFS dedup checkpoint offset 1808, prefix 592", Source.AsSpan(0, 592).ToArray(), 0x000000049CED5516UL, 0x0000022AEA29CA65UL, 0x00008DCBC6851667UL, 0x001947BDC98275B1UL);
            cases.Add("OpenZFS dedup checkpoint offset 2120, prefix 904", Source.AsSpan(0, 904).ToArray(), 0x0000000AC5D03812UL, 0x0000053A8E194732UL, 0x0001A8A835AC5B11UL, 0x0069D070EED7E77DUL);
            cases.Add("OpenZFS dedup checkpoint offset 2432, prefix 1216", Source.AsSpan(0, 1216).ToArray(), 0x00000011F78F240EUL, 0x00000A3AECD898EFUL, 0x0003F91FABB96506UL, 0x013C6A2C7AAFB5D2UL);
            cases.Add("OpenZFS dedup checkpoint offset 3256, prefix 2040", Source.AsSpan(0, 2040).ToArray(), 0x0000001CFCDC9355UL, 0x00001F549A4172BCUL, 0x00144039A7EF7C20UL, 0x09DC1F059877E2EBUL);
            cases.Add("OpenZFS dedup checkpoint offset 3568, prefix 2352", Source.AsSpan(0, 2352).ToArray(), 0x00000020EFFDD7C2UL, 0x00002941F0629C9CUL, 0x001F50B32FC8033CUL, 0x11A8F538C571AE0CUL);
            cases.Add("OpenZFS dedup checkpoint offset 4056, prefix 2840", Source.AsSpan(0, 2840).ToArray(), 0x0000002932C7CD01UL, 0x00003BD8BF8BEBBAUL, 0x00374C43B0CA7B39UL, 0x25FD99D5656E2C40UL);
            cases.Add("OpenZFS dedup checkpoint offset 4368, prefix 3152", Source.AsSpan(0, 3152).ToArray(), 0x000000305AF118D3UL, 0x00004A16D8B2319EUL, 0x004BAD60F8FB5727UL, 0x39DF4D1808C71EF3UL);
            cases.Add("OpenZFS dedup checkpoint offset 5192, prefix 3976", Source.AsSpan(0, 3976).ToArray(), 0x00000034628C129CUL, 0x0000739959E94174UL, 0x0097FC73864A0190UL, 0x9361B979BEF9D07FUL);
            cases.Add("OpenZFS dedup checkpoint offset 5504, prefix 4288", Source.AsSpan(0, 4288).ToArray(), 0x0000003809EAC71AUL, 0x0000848CF13AF82BUL, 0x00BDCED6B6341B03UL, 0xC764E6A3FF4EEC34UL);
            cases.Add("OpenZFS dedup checkpoint offset 5992, prefix 4776", Source.AsSpan(0, 4776).ToArray(), 0x00000040B2029AC0UL, 0x0000A253CE32F2D6UL, 0x0103FB4894CA420DUL, 0x32241844A0222632UL);
            cases.Add("OpenZFS dedup checkpoint offset 6304, prefix 5088", Source.AsSpan(0, 5088).ToArray(), 0x000000477EAFCCA7UL, 0x0000B7B75619C80EUL, 0x0138B2B5AD70B5C3UL, 0x895256A7D8BE73C3UL);
            cases.Add("OpenZFS dedup checkpoint offset 8152, prefix 6936", Source.AsSpan(0, 6936).ToArray(), 0x000000BF5E4ACCDFUL, 0x0001C2FDEBDF9F3CUL, 0x03516141D8573368UL, 0x589DEE0C3A7CB9BEUL);
            cases.Add("OpenZFS dedup checkpoint offset 8640, prefix 7424", Source.AsSpan(0, 7424).ToArray(), 0x000000C7D0D17D6FUL, 0x0002211E73E23A83UL, 0x043EC391FC564AA8UL, 0x24A4953E1A5A0D93UL);
            cases.Add("OpenZFS dedup checkpoint offset 8952, prefix 7736", Source.AsSpan(0, 7736).ToArray(), 0x000000D059FA2AD5UL, 0x00025FFDD7E13480UL, 0x04EE7DFAF3EDE11FUL, 0x8A64954C072AAC65UL);
            cases.Add("OpenZFS dedup checkpoint offset 25648, prefix 24432", Source.AsSpan(0, 24432).ToArray(), 0x000006A44656264FUL, 0x003FF606A5F798DDUL, 0x9EE7BE4D7FA4FE12UL, 0x3F513FCA89ABAE64UL);
            cases.Add("OpenZFS dedup checkpoint offset 42344, prefix 41128", Source.AsSpan(0, 41128).ToArray(), 0x00000C682DA09A1AUL, 0x00DBCDCBF017F637UL, 0x273D652CB84917CBUL, 0xF7C027209EA29ED9UL);
            cases.Add("OpenZFS dedup checkpoint offset 59040, prefix 57824", Source.AsSpan(0, 57824).ToArray(), 0x00001229597E065EUL, 0x01D591EC420DE8B8UL, 0x9A9F29D50FA4F139UL, 0x69B3C3CE736ACC50UL);
            cases.Add("OpenZFS dedup checkpoint offset 75736, prefix 74520", Source.AsSpan(0, 74520).ToArray(), 0x000017EB00560364UL, 0x032D4A6FB95CFDCCUL, 0xF47430025D51450FUL, 0xFF807B53E20BE60FUL);
            cases.Add("OpenZFS dedup checkpoint offset 92432, prefix 91216", Source.AsSpan(0, 91216).ToArray(), 0x00001DB1317F8438UL, 0x04E30987483D9704UL, 0x31060E906DFF6A5CUL, 0x1103C4BDDFE183CDUL);
            cases.Add("OpenZFS dedup checkpoint offset 109128, prefix 107912", Source.AsSpan(0, 107912).ToArray(), 0x00002371CFB8BFF7UL, 0x06F6A4F597D4E992UL, 0x4C436421069713DCUL, 0x095DFEF5949F38BCUL);
            cases.Add("OpenZFS dedup checkpoint offset 125824, prefix 124608", Source.AsSpan(0, 124608).ToArray(), 0x0000247223324AF0UL, 0x09476B4517328C8CUL, 0xAD4ADABEF5507589UL, 0x60D5925B5B67265FUL);
            cases.Add("OpenZFS dedup checkpoint offset 126312, prefix 125096", Source.AsSpan(0, 125096).ToArray(), 0x0000247A2D86F0B5UL, 0x0958CC6D48E8760CUL, 0x1D80AA6EDE350EC5UL, 0x973B7C004403D7A6UL);
            cases.Add("OpenZFS dedup checkpoint offset 126624, prefix 125408", Source.AsSpan(0, 125408).ToArray(), 0x000024839ACDD563UL, 0x0963EBC0BF4C8AFAUL, 0xF846353E4E1C1D8CUL, 0x3FE3D4CFC7058BDFUL);
            cases.Add("OpenZFS dedup checkpoint offset 126936, prefix 125720", Source.AsSpan(0, 125720).ToArray(), 0x0000248DCDC1D20EUL, 0x096F0E404DD6A780UL, 0xD66FCA01E984D0C7UL, 0x16A5B736CD931785UL);
            cases.Add("OpenZFS dedup checkpoint offset 127248, prefix 126032", Source.AsSpan(0, 126032).ToArray(), 0x0000249703A0ECABUL, 0x097A33AB8E732B12UL, 0xB7FE57ED8ACFFA20UL, 0x2421A39503EDE5CBUL);
            cases.Add("OpenZFS dedup checkpoint offset 127560, prefix 126344", Source.AsSpan(0, 126344).ToArray(), 0x000024A00EE1A42AUL, 0x09855BB30E8CD0E8UL, 0x9CF2B02FC94EBC61UL, 0x713C516469FA6353UL);
            cases.Add("OpenZFS dedup checkpoint offset 127872, prefix 126656", Source.AsSpan(0, 126656).ToArray(), 0x000024A93412D390UL, 0x09908686586DABEBUL, 0x854DA9548B3012E1UL, 0x071A2B27CDD4D006UL);
            cases.Add("OpenZFS dedup checkpoint offset 128184, prefix 126968", Source.AsSpan(0, 126968).ToArray(), 0x000024B1E3EF9EF2UL, 0x099BB41164924138UL, 0x71101BED981D222CUL, 0xEF21D0A1A68E382DUL);
            cases.Add("OpenZFS dedup checkpoint offset 128496, prefix 127280", Source.AsSpan(0, 127280).ToArray(), 0x000024BA9DDF3047UL, 0x09A6E46960F82842UL, 0x603AE0B93DDB3C87UL, 0x32FB8A5BDA1A619DUL);
            cases.Add("OpenZFS dedup checkpoint offset 128808, prefix 127592", Source.AsSpan(0, 127592).ToArray(), 0x000024BE633539D4UL, 0x09B2164D4578E4D9UL, 0x52CEA7B888B56EB7UL, 0xDC8E35EF2E277CFAUL);
            return cases;
        }
    }

    private static byte[] LoadFixture()
    {
        const string encoded =
            "H4sIAAAAAAAC/+1ceZwU1bWuYXBkBgTEPEHDUoiBgM0oiC8IKjOsDhkWAZmgBqzuvt1VTHVVU8s0jQlOVDQIRo3G7akgIGgQBY1PMQRJiKIIZJ5h" +
            "UdQQMYkaIGgiwbDIO+fequplFkbfH74J5/7k11PddZc69yzfOfezJEm0p9546VAr+OzmXa+++97p+InftYN/V/T710Dr4jV/8H6WHGY7F8bsMttQ" +
            "ktIp2AryPv/D/6FV7h35cvt3b2dffeDh1vDJNvf9az+QRfzsea++cYckrXFvev1XY86RWufJzZfXCa+davLyW6fBda+UwOfAMef8ZMhpknTmtU9d" +
            "8sQTBdIPP9z1t2SdJhU2pm95cvTl18a7cfAvDl98w/lld1972lnxe4ffuaNDj+XmVdfNSLUb/WavA+r8h3JUtgW29uffefhM+Dz4wFv7h4AAi9ja" +
            "lT3+VSi9+fwzc0IzLyvw5FY7Ysp7Q/7vsxmmlVB0bY7iaKbxFfq7Tmywaejprzp/RLGZzQxbc7QazWnOMKflXE0dNWlyxYTxX2JGOedqcvmM8ilT" +
            "Jk1ufv+eOVcjR1WOmjJqxlVXj7p6VPP6n5dzNWnChClfTmKN+Rv/h1PV31x9z7vHusLnOqv8oSk9YJ9/cHDFpWXfkPbtTT1wWo93iwvyxPUN+Bfy" +
            "vlh9Cse33+w6fhBt4uGHw5+U95Wkywpfu/C5iT2kdk/PemXwoSfOvGbihVKr1l2kA+W58mt/Ev0eCeJsc4skfXC7wF0L+z/RGT+/7HW+Ly/M/Nnx" +
            "5YLu8FEmPyl1wusy/GydD1soHgvt3rppIGyv9NK+BS+/NVSSLr1h8+oRld+RykIG2/Hg8W8VNmAfzYnH59wydvmJQcNjn7Zrvb33iClndpv88p+q" +
            "E9+xv/F64Y0f73ive0uX2++7fHrNRfD56IBn3zp7rCT1qTr6+fWrviv9o/umNl3G7bnUj8ev3DejSKJ2yrVWeX7J/zxV/YzfHu60MDwI8+ryj6c9" +
            "aEvSXQVjb7WP3S/dWPev9em1yj1+PPaN5iw/Hhed2vH43E1/LR4Mnxd8PuTThRA/C7856L93bvuVNGj/+BNL/xLd5MfjZT/ygqAHiGecJB5jXMR4" +
            "XHGHiK+xLn/m8fVHC8X10H172jTnuqCJeLzIi8fzvXg8PyseB86xiOIxtg0PtXq2DD53/n3gtslLJOmM4ZvvvfJoYcHA3uctHtnl/IGFDdhHTitq" +
            "OB6fccPCu5dvmXFBhze/ffmqmwuu6bN5gLOtYOXAjZ1uWPrZsjGDWkmtWnR+vGLLM3PHIP587ZypL7wgSRufT656/tnBBRvt9W+tmDpp/mRH03W5" +
            "Qp6k2axk+DR5XPm0crl8/JhRlROuLimZZrpyQknLKUtzmJxgctRMGbJmyKpmO6aVLqnSHFVOm64lhzXHYVZIdlLwE4vKusbsUDCAY5lR7A9dHZXJ" +
            "NcxKy1HNckqGu45s4xpC0KMaJnBtJyRX9IFFWbCk0pKSkSaz5URathXb1gxm27KbtJmDsw4rqVLTsmIxvJDDDL9O4YriumkmhpXsrX1whOLaDJ4v" +
            "pejVYoaKPjVMjpuObGq6nGK6bpdMdBNJzYjj6mAiHXJruLBgCJh+LCxIdEyYpmHLihEVl7ZrwANyAeAjRZjlKJrhpGUzJjtaFJ8+01c1k/AUdtKC" +
            "kXFwVYuroRJP+NkPq0X5s6QUw5EdU7YZF3vYMquZMaxkuJkC0apMiYp1wKUFX7A0s4eVTFZNV48yy5Zjiq7jLHy3+PwOU6yoZSZxxUyBsaBXOM3F" +
            "Cp1iri5HLA0HCcStKm5cdYTAzViMGVEh8ZGm0YcLX3ZgHFlzZCWF/VWYIFvgOvbPk3jc1EELcEx40DgIAu73hI4rDSuR6jQMU5pRPFs1oR9IIBUo" +
            "Wsq0olmaFXHzfkdhZH6uRgnn/K4qDoMF44OF6qmfooEKV8CyGtA/Nrue+vHfQAQRE6ZQQDlAK6wkdiyZAvOAGKKKEWF5YohqSsI0onZJucNVJ8GY" +
            "g9sFigPzOKgbuBMTYG2oS3CH6jq4C77ZwfpAmVQlwUoq+DJLrk7KMctMwAKSio2jKg6/CZQYjRFknATl9O+u6IN3hnWQt2xGmGLA0zOFmwCqVQq0" +
            "F/VEKBF+Y6e8iwqwMsXyzRjVHCRUyRRuMWGmanCzAesXywWHYJkWHyEG3YLpDdBsBUSTDlugjJnVpkAqlunaOmyrntVhuG82OGlci8Ho2AelheJF" +
            "mdhyXKmBVVfISoLfFoWhE3xqLkCwP1+Ytg53lgaLyf4oLTlVzzFOz8eVZac2rtyzZez0l+HzskV79h3/TYGkXlz1VrlyVeH6tT8Y0u5vj3wvH1d2" +
            "XSDktLiZuHKnhytnjxAl1te9687lIwc15/qr4srTA0BVQLgSmvnD/6nbBJ83D9lrd+/ZSnpi7NRjd2+8ovU9Z1xz7hU/W3Z+YQP2kdPKGsaVO5Pb" +
            "z7r0sQXzquwN6v7CO96ee93sHUXHHt+26MVVn53x8TNze0htpQ4tWG47+h2ZVoeFnEHv7zo+o5XU4/0D/zjyw7+1Zk/1O73yZ9HbK+QrwcvK5fJI" +
            "9MMlI61SeZxiQZiTK11wwpb8XfDnIXmsBfGD+2xVSSbTCHlmmnCTH67hCwgTGG9S6PBTGM3jZj4MxdDLQwPMhQfTcpRBgLUdi5/TyDGIQTGLsSgE" +
            "SC9y+R0hJCAmMPiNsJTRGizajpgIKSEAQUSPmyGIVXxkuTzBLC2C4ZIvyASgY6cTYVPXIhiOYVUAKAFP8KiDCw/JthZHrIVzjkpAsNKSYk0TLTOi" +
            "KwkxrzwF1gP4MsEgMro2LD8Cy5Ujio8qxPQQeiMmAjqIrxwNYFADiSVAKhpiU/huPItbpghxNi4R5ApIjzED8ItiCSiAi4nB5Iz3QFEzS8DfmQBY" +
            "tQgrlSucrNlnmmm+qiBemzLz4qpuQj/DXxB8o1nQMSmOp0CeiK9MAwEMRHaYXQhVV0SqAAN46+VAGGRgADzCrSqVJzTVS9diQTgXI0BfMYitRBE+" +
            "WFoyqQuQyyGWYigAKfgD29CBxcU2+PggogI84r9GNRs6A0j1tmbCyVcPCQOODKOhPBhMr8EGwMgwXNKEXMdJ+2JPaFGbi0qRaxCocfTFgR8Oqim6" +
            "nLRMOwl/gviaM3fw3DBh3NVs1ctj+EOZloHpAAzvay7A/YjGnLTAZIDWbDCFhM10WJEhA8LVmWdXHJHjU5TKk01UaxXtmQNdUBfmGSaoQtTiajyH" +
            "IfpFTMqzCUBymmdTFSgYPLVkucNA34ztcfwHmqM5IAP4BU46VegWUVmkulSuUpl4JMWKqJCKRgS+xO4WS7phNMCUBVDX2+y4ocVAjyGH4tmCryoj" +
            "0Clojpuz9SMZGKLnKeC+CiPKkqDeDIAlFzRkv/i8aMgcDuMOJTTbRvdh8ClBPKoWUWXG09pA1Cn0SiZPxmQ0DM/MeZ8UNywxEhNgVuGJCvgWzF88" +
            "cA7XaIKIvvmnKtJwuCmFuR4Ythx3YfGGwzwn4xpwPs0MJQwbaQVAHO0FM5ww6mIoePSka9muJnwJuF+e3uCGOahWZrhGQ7P3NlrN+D+4GX1UTHF1" +
            "TC5M3BrokC8XMCgzBskC3IwOP6Khkggji5g65gUWagLIGdQU1KwCNgczW+7Z4KRdAH5UcCWCFmDCNguzDeWsJA7mZ2SZRJKZYPqY4MBgXIFCviZ5" +
            "G4W9uAqGuZAVqxqG31v7MKzYjYHiaKg5MTA8u3Rv7SMyOjHQXAs02+bbHWYg4xpv33DisGJU47o9/4niw68sN+mA8jbV1WJcDPVn9o1YuP4alLXQ" +
            "+WTStBzX0LxKA1cpz1flG6pvR/wmIYC9tfcGwnAyIRXDnoupLWwmBE9fQUDGqnCafvz0f7FZxLW8NQRRo8qbXNFtM1gBn1wF7ea1Cztp8sKGxRKY" +
            "K/r76BloTGMWiM+14mB+fGzDTHl2w00HUk4xKjPiSjyoJ+nubFdE84hp6iKXBlFZfH4sU/BsFexklqsBfYPXRiyXZ9xxS4m6YDN2olQeDwFcs73U" +
            "VsyTwN6wBbowGGGwIlAAxohYSiTdYD/MJkVKzpNRUDEuOigLmejDYUN1naXzwxEXFy8t6WCWABVUvAMmwcgQiLmB6cDAnSxvmpkZHhh22lYM4QW9" +
            "oYJIH8wIUotCtSDC1RjKTaiaUHuJNi4UfwSFiwdVAXEWejEYYYwZFR5d1XQIX4bwKoHTiino5vF+HNYIHh6DJZTuqoUry2gB9+ocHnnKgAUJR6AW" +
            "202AEuWgAT61zuLgcTCu8phugo80PHVHlAF1EtB3A2KnZ4Ya1vZAMjVa3MRwgL7eddyEka/+bJbLnxdEAx7T4dhKm+2k+zsqojYPxWA4NaLgyMF5" +
            "gCuCxRgYP3D9iBs5NOP4zfchwnkZMA+6cvAUYDBcjdErJrzCDF98CkWY80DC6mTLjcJHCut7HAbEssVrMce1DB6Swq5XSQU/6NrwNLgqlAEfDAY3" +
            "GAeF0AcwioHb5NuOgDK+2Qbyy6CROI9GUQ4hfJevakkvGPGJ0AtbekrzdZLVmLr3IPhUmuFyLQMw4VluzASPyJ/CzkXsWQsI8wkQpGY7YgYLjTPb" +
            "Q6LBRtugS44qAgwv0yWwUmsLQANVJS+E4CYJNG8KP5NSLDQrEIuKpUwRTaBSxd21Z0pJBUI3y/GLFYbvPSKinArFKo1vEUcwuHAETUneETw4Xw0q" +
            "EY/vmi7cLAAcI473RUFHQJKVUIV0hbJBmZjDchukYsfSfFh4PguGyU5+AAxHwWaqcebAQ0TcJLd5Xr8XWgEPDGKBmAtyq/KWA+MgvuHIzo0IZwMZ" +
            "lhuPg6BMP6uCki88hcEEko7zKMX9HliglgTPLKJE8IA8LPCxIhjnMAwlEcfZPEhEGXgAZikcTMB1Uk3boHa6DMhER4BWKpejJPkUCv/Llx73v96O" +
            "JJSZDHcCUZiPiPyaazAkPF/EKxNjSVxcC3UFgFDDdMRCBkthtgUgFR1TBkxANIIqugfBhClA+EuIKB08LCqKzBEVVj81lJ6XDIDmC2wnFC/Edw2i" +
            "cJCgil89vwzwEOes0ThI9XMcyLySFrIDI9kIPZQLB7irnpPxOtApirIx0tzHazAaqAOXAk7r/cbFkD7pSEGOjdbAZjvoIsKQDYXRfH24793E1QBQ" +
            "MsqFn80omDqBvpUj3OVfhYLNTPiOAFO6uDcnZtoIK6BnSknbuEuwGwoejmQPjk6P4zzfxyHccgIfjFeKXe0Xl6Pgi8CjC5AKSazu+a0QwkOehXA3" +
            "JY6bPHMDoQ1DmCjmBB1BM8n+FXeLJ8pemSLwlfwoTItAlPLDF5wpJSHF5vBdNbF6bossUkdvFrZcxws9jU4Wyp6NH8yZUTyKw1OjmrTYWu5ToUPc" +
            "FVm0BRsLJyCexLgd6WY0npVKJkAqepBHoZ1zmWfK69k/o9sHn5u1CSeVBo/YYQVMEaYKe3HG9s807ASiJUuOq8xBm0b7gZwtjmHf8ygnlzx3Mh4c" +
            "4dsO9gdFAhbNVC4wD0bQw5/LMsPh7B99dwbWhrkg5t0CJYBmjAZzrUITtSFn1+F85pHmPL3ia4Ihj4PECf+DJMzvhYooHFvmtvHgf6aZgCW9NAIz" +
            "K+6AoAMPZ+g3hFsCIeEIiN9C8DM3JnxqHi6ylAVhBcvAopy1iujqx1LLhLPSrDPFFNYihA5wI2HgIXn8ECdp4CjhyzSKGfCLX+bDOVwDsT9GMm7I" +
            "GIxRzGhUeUUGUxyBiRzIwfqIp3OWFnZ1gQgw7WlogBhGaaFBhgKWlJJnKvA0ETzxbbwP6hvMhrk2E+fKFmBolhdIdRYTZ6BhBaNmps6EVcWEMFmQ" +
            "DeRIQakBtCUez741wD/1jXuavyheuBMH5jCN4iXPfqTElJFj4FKsbAS4KYUKkmXn4i8F5Q41JKxGBh3RxmBJLIEVO/S+Y0yRE8MwWSoZwpqr/325" +
            "roSh4pPz3WTYKFUeoViYfOX+NIaZVlzL/a7ShEqVpuTdKc7m3ITYYmHrGbwHSS/siOE5l5BcDTg4AHCoQirssCgXQIVHoEN0CgFuDvMqnxHHUkMW" +
            "dEoJCOITE4K0DEIfHJmK8rQHDIN6dAgxYgwOzGEHocRrYuUH+5t45J1CmUdEvIpqmNRD6q6JkCJqKaI8nED3noLTZq+A52F4fm7JC7CYkHjHmID4" +
            "khBIM6e5OHhQaIoGBpY9hpAMFl9F/SaoEwhxcIAEsV7wGWqErYk02cUak2J4R9IaACYsR0eH8BiIzl7nXt/m9zqqSCkY9579BSpxhuTUtESlhxfn" +
            "vfQJ6ypNL9iDlBZPJXQRXDxd8jJWYQ1glpj+eRXvoDzRwK9Y0USH5WsDD7IImiHV5l7NMeOMpz1eRubwO/Ly4aZXzfxSFAYH3jfHjBTv+6z0NTBU" +
            "FX0cytvPzUNN3gZ1IHBwNq+G+U+EWZrNHzkqADPWliGo1qvg+JlJww+Dyo02B87I0VkmaPJp/CflOoPQO9AqATp5wTQrmsx0AbEFTk8U/jIBF4AX" +
            "z5CDn0VSmzlLAFqJEgEZNLBUtKWmtyMUHBQFTovLEVUacBcvb2JFxEaAF/wSxyoEJr2qoDWg5eha0sbcKZnM2YygugzSZlbStLXA3xsuHMqA8XPh" +
            "DA3EJrJULxHNWpcna1H2DZsethKXcciW66stPydTeWGHr8cbwcsU/BHEpRhBQd9oB0Hbzya+kmi9YrfnLwUQh8Wx2QrWhEPez2i5fK4EpABIj8rc" +
            "mVCieHyUEocZFnedPAHOPCm/Bb7TjEzJOgI+EKu1Dd2KB34o3szdwAOzggJSJWxWZn7MZhXdh0D4ZUxHwCBuQN6V5gQeAWTh1yA5hQgKN5mqZF6U" +
            "rciPaSI44h4BKBS3IbTlPVKs3r6qLOXjHoG8PcllYhJ3I6ZIssVSmjFs4Bv4qDMhEAqKGJbGPCXOqqsI74EnjoBgEKnBOWcSqvLpeuXBZkzNAYkv" +
            "yhB+k7R4RM36Jigl5Hwb52Cf47a8u3HTIHplg7LMHTnYIGtBeKevwP6WBr5TFXqdwiSvkSpmA6HD9waKqA2IsMmzxXFwCol7Z6Xx8gFH8/Mj4dgB" +
            "gfhnhMG5jPc7QgKO6iqFBaOvQJ+scHOFLUOt9TvztFjT43BmydcK7LMoFhAQyQoj9DXI5j/ozAkkxrEjj8TlMJgWHBNyreYx3T969ku0/JwaU/Iw" +
            "E4UAAAv83CF/2Ex9Ceo5kDpxX4vuwAESojioTgFFIJGE4qDFBCJruL+XRgRP4XfGPKipfqLYIyqh5eClAFAaHgybyAzDTus1sFlKT+SvNTaGDYoE" +
            "h5KYJE6C2rjXfQQGMTjtb7IrHI/UgIvCx7Z1zv7EngDwQWXFtFiRxCCJ/0MoV9WhjUhxMjf2cVmOwANCjS6gEpwk+pDsPlPgoQEwMNZor3o+W2f8" +
            "IhfFlMqjm61bQrFS4vSWHwOBLA0MtryUwrz6X8aCebfMzzig5tRbItRldDz94dV4sWglAffmqL1AWplbIHFIhxpyT1BxEWUv7i5O5gNyjmaDAMvP" +
            "ZMeylIisYxgmzpilTBTFTCiJi19GgA1jumeHmornPF/Mqr7kHGIj+vZYE0nNwlxH5/5mNDo3Bb2K7fQUri64gieDA0p4FLAEYU9BPSDnRu4LSi7/" +
            "Cg3BAjF+iPFDjB9i/BDjhxg/xPghxg8xfojxQ4wfYvwQ44cYP8T4IcYPMX6I8UOMH2L8EOOHGD/E+CHGDzF+iPFDjJ9/T8ZPo+9NKGv6vQl17kfl" +
            "iaPfOt5tnvTL8bUrun6Y6lt79Klt7z1XcuCilc/3X9vS35swoffU0cvgXWTvPXD48aJ/DpM6fbHsxnHrP3w0+vTK+ZuHXTWMmFLElCKmFDGliClF" +
            "TCliSv3/ZkoRT4p4UsSTIp4U8aSIJ0U8KeJJEU+KeFLEkyKeFPGkiCdFPCniSRFPinhSxJMinhTxpIgnRTwp4kkRT4p4UsSTIp4UvRmJ+D7E9yG+" +
            "D/F9iO9DfB/i+xDfh/g+xPchvg/xfYjvQ3wf4vsQ34f4PsT3Ib4P8X2I70N8H+L7EN+H+D7E9zk1+D6Nvjehtun3JvTesm7o7p27e63eNa/29e6r" +
            "2r7b9nitds6kP/ZfsWBY3yVL17X09yac+9Di/mo7SfrOPzt/8sbWd6Q3Ole8GGKX93n70SWPyn02HCaeFPGkiCdFPCniSRFPinhSxJMinhTxpIgn" +
            "RTwp4kkRT4p4UsSTIp4U8aSIJ0U8KeJJEU+KeFLEkyKeFPGkiCdFPCniSRFPinhSxJMinhTxpIgnRe9FIr4P8X2I70N8H+L7EN+H+D7E9yG+D/F9" +
            "iO9DfB/i+xDfh/g+xPchvg/xfYjvQ3wf4vsQ34f4PsT3Ib4P8X2I7/P1830afW/Chqbfm1Bx9pr1e965/tVldYmth+dMT/R7rbsb6t1/Utniafd1" +
            "6HVwY0t/b8L0ornT+naSpBc/OmP4/rt3FFz66bIOO/ouemjilpn2to3PacSTIp4U8aSIJ0U8KeJJEU+KeFLEkyKeFPGkiCdFPCniSRFPinhSxJMi" +
            "nhTxpIgnRTwp4kkRT4p4UsSTIp4U8aSIJ0U8KeJJEU+KeFLEkyKeFPGk6L1IxPchvg/xfYjvQ3wf4vsQ34f4PsT3Ib4P8X2I70N8H+L7EN+H+D7E" +
            "9yG+D/F9iO9DfB/i+xDfh/g+xPchvg/xfb5+vk+j702QCpp8b8KSmypvO1EdaqOXTz8ye9b43R2PDH06cuuA1IPnxXrN3aXtb+nvTYgWTpX2dZak" +
            "LcevW2uO7V/YYdRV3291kfNZh7+03Tv5htoTxJMinhTxpIgnRTwp4kkRT4p4UsSTIp4U8aSIJ0U8KeJJEU+KeFLEkyKeFPGkiCdFPCniSRFPinhS" +
            "xJMinhTxpIgnRTwp4kkRT4p4UsSTIp4U8aTovUjE9yG+D/F9iO9DfB/i+xDfh/g+xPchvg/xfYjvQ3wf4vsQ34f4PsT3Ib4P8X2I70N8H+L7EN+H" +
            "+D7E9yG+D/F9vn6+T6PvTShr+r0J9/287Fik0mg3tEvHqYfWPPbj38pProveuemKzUu7vnpXVXu1pb83YfAtNw5Y002SWt9/+ZW3FX/Q+rqZJxJ3" +
            "tS8asPXm9/f86reFZxJPinhSxJMinhTxpIgnRTwp4kkRT4p4UsSTIp4U8aSIJ0U8KeJJEU+KeFLEkyKeFPGkiCdFPCniSRFPinhSxJMinhTxpIgn" +
            "RTwp4kkRT4p4UsSTovciEd+H+D7E9yG+D/F9iO9DfB/i+xDfh/g+xPchvg/xfYjvQ3wf4vsQ34f4PsT3Ib4P8X2I70N8H+L7EN+H+D7E9/n6+T6N" +
            "vjehtun3JixfsWTzF6PebrX/ptGhume3X/L5BX+peuSbcwd8Hv/g5r/++iV4b0Jhi35vwuGXX/zdrF6S9NOPt99/aNk/i9496/6intERlesGL7r3" +
            "0BffLyaeFPGkiCdFPCniSRFPinhSxJMinhTxpIgnJXhSEjVq1KhRo0aNGjVq1KhRo0aNGjVq1KhRo0aNGjVq1KhRo0aNGjVq1KhRo0aNGjVq1KhR" +
            "o0aNGjVq1Kh9jc17DYLURsq8NyEkifclrM66w39vwqkil0/GDuxlnS9JCxcO7DyqekzxfHfiofW7x66a0Tt+7bU/3XH9NRMvlFq17iIt+5G4v+sC" +
            "IafFjYx3nvfZGmV9iyS9cce90/G6cO6vz8XPL3tdkDd+YebPjosKuuMGyvOlTnwn8bN13j5LG8QIJ7x2qu2v357/5Nb+c2Cf29V8dGViy/eKX2l/" +
            "yR+Mn9d2W76rcKT0g6H3F+XLzW9lUo5d+J+n593mv2dkZ3L7WZc+tmBelb1B3V94x9tzr5u9o+jY49sWvbjqszM+fmZuD6lti3rPSGTH1oduBrkd" +
            "ub3y5Q37IsULu3Udf8Uloz/fs+C0Tb/b/sGwenIra6bcynLlVud+VJ44+q3j3eZJvxxfu6Lrh6m+tUef2vbecyUHLlr5fP+1LU1u7d/89dY7QG61" +
            "K3aOK2tvFm+qu+Xjgs3mznmd79n6ny88fnY9udU2U261uXLrvWXd0N07d/davWte7evdV7V9t+3xWu2cSX/sv2LBsL5Llq5raXJbuX9x4f0gt04X" +
            "2D9ZefGcYvnI724/UPXFC2/8+UDhz5b2PL+e3DY0U24bcuVWcfaa9Xveuf7VZXWJrYfnTE/0e627G+rdf1LZ4mn3deh1cGNLk1u/Ze+3Xwxy+6hu" +
            "Yfvnrp1XrKwb//qFq//+8OTIES161WWz6vu3gmb6t4IcuS25qfK2E9WhNnr59COzZ43f3fHI0Kcjtw5IPXherNfcXdr+lia3u37fadCTILd9KxPf" +
            "u/XWu4rf73TRgilPjptXVLd9a58Lzj29vn9rptzKcuV238/LjkUqjXZDu3ScemjNYz/+rfzkuuidm67YvLTrq3dVtVdbmtz+/ujBD9aA3AaX/zR6" +
            "5i/+qzh0XrcHDnyz46z+g3+y/LG6ngfr+7dmyq02V27/bu/tGnPRnkdeArkN//bn12t/Wl5822XvXL72j0Ouf0Q5d/e1tx8d6OO9YsLpOe1/AW5G" +
            "ojBo8gEA";
        using var compressed = new MemoryStream(Convert.FromBase64String(encoded));
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        byte[] bytes = output.ToArray();
        if (bytes.Length != 127592 || Convert.ToHexString(SHA256.HashData(bytes)) != "61C64C9B11CB70D73342A7DAB9FE80127607EA6380CE878220274113A38A8630")
            throw new InvalidDataException("The frozen OpenZFS fixture differs from its published byte slice.");
        return bytes;
    }
}
