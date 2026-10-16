"""Extract published LSA checksum facts from an explicitly supplied capture folder."""

import argparse
import hashlib
import json
from pathlib import Path
import struct


REPOSITORY_ROOT = Path(__file__).resolve().parents[4]
OUTPUT_ROOT = REPOSITORY_ROOT / "TestResults/Research/integrity/checksum"


def packets(path):
    data = path.read_bytes()
    if data[:4] == b"\x0a\x0d\x0d\x0a":
        yield from pcapng_packets(data)
        return
    if len(data) < 24:
        raise ValueError(f"Truncated PCAP header: {path}")
    byte_order = "<" if data[:4] in (b"\xd4\xc3\xb2\xa1", b"\x4d\x3c\xb2\xa1") else ">"
    link = struct.unpack_from(byte_order + "I", data, 20)[0]
    offset = 24
    number = 0
    while offset < len(data):
        if offset + 16 > len(data):
            raise ValueError(f"Truncated PCAP packet header: {path}")
        captured = struct.unpack_from(byte_order + "I", data, offset + 8)[0]
        end = offset + 16 + captured
        if end > len(data):
            raise ValueError(f"Truncated PCAP packet: {path}")
        number += 1
        yield number, link, data[offset + 16:end]
        offset = end


def pcapng_packets(data):
    byte_order = "<"
    offset = 0
    interfaces = []
    number = 0
    while offset < len(data):
        if offset + 12 > len(data):
            raise ValueError("Truncated PCAPNG block header.")
        if data[offset:offset + 4] == b"\x0a\x0d\x0d\x0a":
            byte_order = "<" if data[offset + 8:offset + 12] == b"\x4d\x3c\x2b\x1a" else ">"
            interfaces = []
        block_type, length = struct.unpack_from(byte_order + "II", data, offset)
        if length < 12 or offset + length > len(data):
            raise ValueError("Invalid PCAPNG block length.")
        if block_type == 1:
            interfaces.append(struct.unpack_from(byte_order + "H", data, offset + 8)[0])
        if block_type == 6:
            if length < 32:
                raise ValueError("Truncated PCAPNG enhanced packet block.")
            interface, _, _, captured, _ = struct.unpack_from(byte_order + "IIIII", data, offset + 8)
            if interface >= len(interfaces) or captured > length - 32:
                raise ValueError("Invalid PCAPNG packet interface or length.")
            number += 1
            yield number, interfaces[interface], data[offset + 28:offset + 28 + captured]
        offset += length


def extract(packet, link):
    if link == 1 and len(packet) >= 14:
        offset = 14
        packet_type = int.from_bytes(packet[12:14], "big")
        while packet_type in (0x8100, 0x88A8):
            if offset + 4 > len(packet):
                return
            packet_type = int.from_bytes(packet[offset + 2:offset + 4], "big")
            offset += 4
    elif link == 113 and len(packet) >= 16:
        offset = 16
        packet_type = int.from_bytes(packet[14:16], "big")
    else:
        return

    if packet_type == 0x0800 and len(packet) >= offset + 20:
        if packet[offset + 9] != 89:
            return
        offset += (packet[offset] & 15) * 4
    elif packet_type == 0x86DD and len(packet) >= offset + 40:
        if packet[offset + 6] != 89:
            return
        offset += 40
    else:
        return

    if offset + 2 > len(packet) or packet[offset + 1] != 4:
        return
    version = packet[offset]
    if version not in (2, 3):
        return
    position = offset + (24 if version == 2 else 16)
    if position + 4 > len(packet):
        return
    count = int.from_bytes(packet[position:position + 4], "big")
    position += 4
    for _ in range(count):
        if position + 20 > len(packet):
            return
        length = int.from_bytes(packet[position + 18:position + 20], "big")
        if length < 20 or position + length > len(packet):
            return
        yield position, packet[position:position + length]
        position += length


def valid(lsa):
    first = second = 0
    for value in lsa[2:]:
        first = (first + value) % 255
        second = (second + first) % 255
    return first == second == 0 and lsa[16:18] != b"\0\0"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input_directory", type=Path, help="Directory containing public PCAP/PCAPNG inputs.")
    source = parser.parse_args().input_directory
    if not source.is_absolute():
        source = REPOSITORY_ROOT / source
    if not source.is_dir():
        raise SystemExit(f"Capture directory does not exist: {source}")
    captures = sorted(source.glob("*.pcap")) + sorted(source.glob("*.pcapng"))
    if not captures:
        raise SystemExit(f"No PCAP or PCAPNG inputs were found in {source}")

    vectors = []
    seen = set()
    for capture in captures:
        digest = hashlib.sha256(capture.read_bytes()).hexdigest()
        count = 0
        for number, link, packet in packets(capture):
            for offset, lsa in extract(packet, link):
                if not valid(lsa):
                    continue
                key = lsa[2:16] + lsa[18:]
                if key in seen:
                    continue
                seen.add(key)
                count += 1
                vectors.append({
                    "name": f"{capture.name} frame {number} offset {offset}",
                    "hex": lsa.hex().upper(),
                    "checksum": int.from_bytes(lsa[16:18], "big"),
                    "file": capture.name,
                    "sha256": digest,
                })
        print(f"{capture.name}: {count} distinct valid LSA facts")
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    output = OUTPUT_ROOT / "ospf-vectors.json"
    output.write_text(json.dumps(vectors, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"Wrote {len(vectors)} facts to {output}")


if __name__ == "__main__":
    main()
