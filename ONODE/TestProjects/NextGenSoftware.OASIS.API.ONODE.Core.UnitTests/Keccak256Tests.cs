using System;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    /// <summary>
    /// Correctness tests for the real Keccak-256 implementation backing ONET's blockchain registry function
    /// selector computation, checked against well-known, independently-verifiable test vectors rather than
    /// just "does it run" - a hash implementation that runs without throwing but produces wrong output would
    /// be just as broken as a stub, only harder to notice.
    /// </summary>
    public class Keccak256Tests
    {
        [Fact]
        public void ComputeHash_EmptyInput_MatchesKnownKeccak256TestVector()
        {
            // keccak256("") is one of the most widely published Keccak test vectors (distinct from
            // SHA3-256(""), which has a different value due to different padding).
            var hash = Keccak256.ComputeHash(Array.Empty<byte>());
            Convert.ToHexString(hash).ToLowerInvariant().Should().Be("c5d2460186f7233c927e7db2dcc703c0e500b653ca82273b7bfad8045d85a470");
        }

        [Fact]
        public void ComputeFunctionSelectorHex_Erc20Transfer_MatchesWellKnownSelector()
        {
            // 0xa9059cbb is the universally-known ERC20 transfer(address,uint256) selector - used across
            // essentially every Ethereum token contract, making it an excellent independent correctness check
            // for the real-world use case (computing selectors from human-readable signatures).
            Keccak256.ComputeFunctionSelectorHex("transfer(address,uint256)").Should().Be("0xa9059cbb");
        }

        [Fact]
        public void ComputeHash_LongerInput_SpanningMultipleBlocks_DoesNotThrowAndProducesStableOutput()
        {
            // 136-byte rate means this input spans two absorption blocks - exercises the multi-block loop,
            // not just the single-final-block path the other tests take.
            var input = new byte[300];
            for (int i = 0; i < input.Length; i++)
                input[i] = (byte)(i % 256);

            var hash1 = Keccak256.ComputeHash(input);
            var hash2 = Keccak256.ComputeHash(input);

            hash1.Should().HaveCount(32);
            hash1.Should().BeEquivalentTo(hash2, options => options.WithStrictOrdering(), "hashing the same input twice must be deterministic");
        }
    }
}
