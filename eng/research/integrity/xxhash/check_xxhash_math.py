"""Check the historical nine-byte example using scalar math and no hash library."""


MASK32 = (1 << 32) - 1
MASK64 = (1 << 64) - 1
INPUT = b"123456789"


def rotate_left(value, shift, width):
    mask = (1 << width) - 1
    value &= mask
    return ((value << shift) | (value >> (width - shift))) & mask


def readme_xxh32():
    prime1, prime2 = 2654435761, 2246822519
    prime3, prime4, prime5 = 3266489917, 668265263, 374761393
    value = prime5 + len(INPUT)
    for offset in (0, 4):
        word = int.from_bytes(INPUT[offset:offset + 4], "little")
        value = rotate_left(value + word * prime3, 17, 32) * prime4 & MASK32
    value = rotate_left(value + INPUT[8] * prime5, 11, 32) * prime1 & MASK32
    value ^= value >> 15
    value = value * prime2 & MASK32
    value ^= value >> 13
    value = value * prime3 & MASK32
    return value ^ (value >> 16)


def readme_xxh64():
    prime1, prime2 = 11400714785074694791, 14029467366897019727
    prime3, prime4, prime5 = 1609587929392839161, 9650029242287828579, 2870177450012600261
    lane = rotate_left(int.from_bytes(INPUT[:8], "little") * prime2, 31, 64) * prime1 & MASK64
    value = rotate_left((prime5 + len(INPUT)) ^ lane, 27, 64) * prime1 + prime4
    value &= MASK64
    value = rotate_left(value ^ (INPUT[8] * prime5 & MASK64), 11, 64) * prime1 & MASK64
    value ^= value >> 33
    value = value * prime2 & MASK64
    value ^= value >> 29
    value = value * prime3 & MASK64
    return value ^ (value >> 32)


def main():
    actual = (readme_xxh32(), readme_xxh64())
    if actual != (0x937BAD67, 0x8CB841DB40E6AE83):
        raise SystemExit(f"Historical example hashes do not match: {actual!r}")
    print("Independent scalar math confirmed both historical example hashes.")


if __name__ == "__main__":
    main()
