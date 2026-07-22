using System;
using System.Buffers.Binary;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// Real Keccak-256 (the original Keccak padding/parameters Ethereum uses for function selectors and
    /// hashing - NOT NIST SHA3-256, which uses different domain-separation padding and would produce a
    /// different digest for the same input). Implements the standard Keccak-f[1600] permutation and sponge
    /// construction directly, so ONET's blockchain registry discovery can compute a real 4-byte function
    /// selector from a human-readable signature like "getRegisteredNodes()" instead of requiring the caller
    /// to pre-compute and supply it.
    /// </summary>
    public static class Keccak256
    {
        private const int StateSize = 25; // 5x5 lanes of 64 bits = 1600 bits
        private const int Rounds = 24;
        private const int RateBytes = 136; // 1088 bits rate for Keccak-256 (1600 - 2*256 capacity)
        private const int OutputBytes = 32;

        private static readonly ulong[] RoundConstants =
        {
            0x0000000000000001UL, 0x0000000000008082UL, 0x800000000000808AUL, 0x8000000080008000UL,
            0x000000000000808BUL, 0x0000000080000001UL, 0x8000000080008081UL, 0x8000000000008009UL,
            0x000000000000008AUL, 0x0000000000000088UL, 0x0000000080008009UL, 0x000000008000000AUL,
            0x000000008000808BUL, 0x800000000000008BUL, 0x8000000000008089UL, 0x8000000000008003UL,
            0x8000000000008002UL, 0x8000000000000080UL, 0x000000000000800AUL, 0x800000008000000AUL,
            0x8000000080008081UL, 0x8000000000008080UL, 0x0000000080000001UL, 0x8000000080008008UL
        };

        // r[x][y] rotation offsets, flattened with index = x + 5*y.
        private static readonly int[] RotationOffsets =
        {
            0, 1, 62, 28, 27,
            36, 44, 6, 55, 20,
            3, 10, 43, 25, 39,
            41, 45, 15, 21, 8,
            18, 2, 61, 56, 14
        };

        public static byte[] ComputeHash(byte[] input)
        {
            var state = new ulong[StateSize];
            int offset = 0;

            while (input.Length - offset >= RateBytes)
            {
                AbsorbBlock(state, input, offset, RateBytes);
                KeccakF1600(state);
                offset += RateBytes;
            }

            var finalBlock = new byte[RateBytes];
            int remaining = input.Length - offset;
            Array.Copy(input, offset, finalBlock, 0, remaining);
            finalBlock[remaining] = 0x01; // Keccak (not SHA3) multi-rate padding start
            finalBlock[RateBytes - 1] |= 0x80; // padding end marker

            AbsorbBlock(state, finalBlock, 0, RateBytes);
            KeccakF1600(state);

            var output = new byte[OutputBytes];
            for (int i = 0; i < OutputBytes / 8; i++)
                BinaryPrimitives.WriteUInt64LittleEndian(output.AsSpan(i * 8, 8), state[i]);

            return output;
        }

        /// <summary>Computes the 4-byte Ethereum ABI function selector for a canonical signature like "getRegisteredNodes()" or "transfer(address,uint256)".</summary>
        public static byte[] ComputeFunctionSelector(string functionSignature)
        {
            var hash = ComputeHash(System.Text.Encoding.ASCII.GetBytes(functionSignature));
            return hash[..4];
        }

        public static string ComputeFunctionSelectorHex(string functionSignature)
        {
            return "0x" + Convert.ToHexString(ComputeFunctionSelector(functionSignature)).ToLowerInvariant();
        }

        private static void AbsorbBlock(ulong[] state, byte[] block, int offset, int length)
        {
            for (int i = 0; i < length / 8; i++)
                state[i] ^= BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(offset + i * 8, 8));
        }

        private static void KeccakF1600(ulong[] state)
        {
            var c = new ulong[5];
            var d = new ulong[5];
            var b = new ulong[StateSize];

            for (int round = 0; round < Rounds; round++)
            {
                // Theta
                for (int x = 0; x < 5; x++)
                    c[x] = state[x] ^ state[x + 5] ^ state[x + 10] ^ state[x + 15] ^ state[x + 20];

                for (int x = 0; x < 5; x++)
                    d[x] = c[(x + 4) % 5] ^ RotateLeft(c[(x + 1) % 5], 1);

                for (int x = 0; x < 5; x++)
                    for (int y = 0; y < 5; y++)
                        state[x + 5 * y] ^= d[x];

                // Rho + Pi
                for (int x = 0; x < 5; x++)
                {
                    for (int y = 0; y < 5; y++)
                    {
                        int newX = y;
                        int newY = (2 * x + 3 * y) % 5;
                        b[newX + 5 * newY] = RotateLeft(state[x + 5 * y], RotationOffsets[x + 5 * y]);
                    }
                }

                // Chi
                for (int x = 0; x < 5; x++)
                    for (int y = 0; y < 5; y++)
                        state[x + 5 * y] = b[x + 5 * y] ^ (~b[(x + 1) % 5 + 5 * y] & b[(x + 2) % 5 + 5 * y]);

                // Iota
                state[0] ^= RoundConstants[round];
            }
        }

        private static ulong RotateLeft(ulong value, int shift)
        {
            shift %= 64;
            return shift == 0 ? value : (value << shift) | (value >> (64 - shift));
        }
    }
}
