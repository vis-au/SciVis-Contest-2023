# pymorton (https://github.com/trevorprater/pymorton)
# Author: trevor.prater@gmail.com
# License: MIT


def partition_by_2(x):
    x &= 0x3ffffffffff
    x = (x | x << 64) & 0x3ff0000000000000000ffffffff
    x = (x | x << 32) & 0x3ff00000000ffff00000000ffff
    x = (x | x << 16) & 0x30000ff0000ff0000ff0000ff0000ff
    x = (x | x << 8) & 0x300f00f00f00f00f00f00f00f00f00f
    x = (x | x << 4) & 0x30c30c30c30c30c30c30c30c30c30c3
    x = (x | x << 2) & 0x9249249249249249249249249249249
    return x




def interleave3(x, y, z):
    for arg in [x, y, z]:
        if not isinstance(arg, int):
            print('Usage: interleave3(x, y, z)')
            raise ValueError("Supplied arguments contain a non-integer!")

    return partition_by_2(x) | (partition_by_2(y) << 1) | (
        partition_by_2(z) << 2)

