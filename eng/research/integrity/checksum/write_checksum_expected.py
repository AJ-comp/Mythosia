"""Reproduce frozen example outputs without running the product or editing sources."""

import hashlib
import json
from pathlib import Path
import re


RESEARCH_ROOT = Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = RESEARCH_ROOT.parents[2]
OUTPUT_ROOT = REPOSITORY_ROOT / "TestResults/Research/integrity/checksum"


def xxhash_lines(byte_order):
    lines = []
    examples = (
        (32, 0x937BAD67, 0x02CC5D05),
        (64, 0x8CB841DB40E6AE83, 0xEF46DB3751D8E999),
    )
    for bits, value, empty in examples:
        word = f"{value:0{bits // 4}X}"
        digest = value.to_bytes(bits // 8, byte_order).hex().upper()
        lines.extend([
            f"XXH{bits}: {word}",
            f"XXH{bits} digest: {digest}",
            f"XXH{bits} frame valid: True",
            f"XXH{bits} decoded: True",
            f"Chunked XXH{bits}: {word}",
            f"Reset XXH{bits}: {empty:0{bits // 4}X}",
            f"Stream XXH{bits}: {word}",
            f"Async XXH{bits}: {word}",
        ])
    return lines


def fletcher4_lines(byte_order):
    sums = [0, 0, 0, 0]
    for word in (1, 2, 3, 4):
        sums[0] += word
        for index in range(1, len(sums)):
            sums[index] += sums[index - 1]
    if sums != [10, 20, 35, 56]:
        raise ValueError("Independent Fletcher-4 sums do not match the historical example.")
    digest = b"".join(value.to_bytes(8, byte_order) for value in sums).hex().upper()
    return [
        "Fletcher4 A/B/C/D: 10/20/35/56",
        f"Fletcher4 digest: {digest}",
        "Fletcher4 frame valid: True",
        "Fletcher4 decoded: True",
        "Chunked Fletcher4 matches: True",
        "Reset Fletcher4 A/B/C/D: 0/0/0/0",
        "Stream Fletcher4 matches: True",
        "Async Fletcher4 matches: True",
        "Big-endian Fletcher4 matches: True",
    ]


def ospf_lines(code):
    match = re.search(r'"([0-9A-F]{96})"', code)
    if match is None:
        raise ValueError("The historical OSPF example must contain its 48-byte public vector.")
    packet = bytes.fromhex(match[1])
    first = second = 0
    for value in packet[2:]:
        first = (first + value) % 255
        second = (second + first) % 255
    if (first, second) != (0, 0) or packet[16:18].hex() != "51cb":
        raise ValueError("Independent OSPF checksum residues do not match the example.")
    return [
        "OSPF LSA length: 48",
        "OSPF LSA checksum: 51CB",
        "OSPF LSA checksum field: 51CB",
        "OSPF LSA valid: True",
        "OSPF LSA valid after age change: True",
        "OSPF LSA valid after body change: False",
    ]


def main():
    document = RESEARCH_ROOT / "notes/checksum-families.md"
    blocks = re.findall(r"^```csharp\n(.*?)^```", document.read_text(encoding="utf-8"), re.M | re.S)
    if len(blocks) != 3:
        raise ValueError("Expected exactly three historical checksum examples.")
    cases = []
    for name, code in zip(("xxhash", "fletcher4", "ospf-lsa-checksum"), blocks):
        case = {"id": name, "codeSha256": hashlib.sha256(code.encode()).hexdigest().upper()}
        for byte_order, key in (("little", "stdoutLittleEndian"), ("big", "stdoutBigEndian")):
            if name == "xxhash":
                lines = xxhash_lines(byte_order)
            elif name == "fletcher4":
                lines = fletcher4_lines(byte_order)
            else:
                lines = ospf_lines(code)
            case[key] = lines
        cases.append(case)

    result = {"schemaVersion": 1, "cases": cases}
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    output = OUTPUT_ROOT / "checksum-families.expected.json"
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8", newline="\n")
    snapshot = Path(__file__).with_name("checksum-families.expected.json")
    if result != json.loads(snapshot.read_text(encoding="utf-8")):
        raise SystemExit(f"Generated outputs differ from the frozen snapshot; review {output}")
    print(f"Confirmed all three historical examples; wrote {output}")


if __name__ == "__main__":
    main()
