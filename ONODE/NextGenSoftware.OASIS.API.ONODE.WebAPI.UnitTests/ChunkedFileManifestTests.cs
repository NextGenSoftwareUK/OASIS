using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests
{
    public class ChunkedFileManifestTests
    {
        private static ChunkedFileManifest Valid() => new()
        {
            FileName = "video.mp4",
            FileExtension = "mp4",
            MimeType = "video/mp4",
            TotalSize = 20_000_000,
            Sha256 = "ABCDEF",
            ChunkIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
        };

        [Fact]
        public void ToBytes_RoundTripsThroughTryParse()
        {
            var manifest = Valid();

            var parsed = ChunkedFileManifest.TryParse(manifest.ToBytes());

            parsed.Should().NotBeNull();
            parsed.Should().BeEquivalentTo(manifest);
        }

        [Fact]
        public void TryParse_ReturnsNull_ForOrdinaryFileBytes()
        {
            ChunkedFileManifest.TryParse(Encoding.UTF8.GetBytes("hello world")).Should().BeNull();
            ChunkedFileManifest.TryParse(Array.Empty<byte>()).Should().BeNull();
            ChunkedFileManifest.TryParse(null).Should().BeNull();
        }

        [Fact]
        public void TryParse_ReturnsNull_WhenMagicIsFollowedByGarbageOrAnInvalidManifest()
        {
            var magic = Encoding.ASCII.GetBytes("OASIS-CHUNKED-FILE/1\n");

            ChunkedFileManifest.TryParse(magic.Concat(Encoding.UTF8.GetBytes("{not json")).ToArray()).Should().BeNull();

            var noChunks = Valid();
            noChunks.ChunkIds.Clear();
            ChunkedFileManifest.TryParse(noChunks.ToBytes()).Should().BeNull();
        }

        [Fact]
        public void Validate_AcceptsAValidManifest() => Valid().Validate().Should().BeNull();

        [Fact]
        public void Validate_RejectsBadChunkLists()
        {
            var empty = Valid(); empty.ChunkIds.Clear();
            empty.Validate().Should().NotBeNull();

            var emptyId = Valid(); emptyId.ChunkIds[1] = Guid.Empty;
            emptyId.Validate().Should().NotBeNull();

            var duplicate = Valid(); duplicate.ChunkIds[2] = duplicate.ChunkIds[0];
            duplicate.Validate().Should().NotBeNull();

            var tooMany = Valid();
            tooMany.ChunkIds = Enumerable.Range(0, ChunkedFileManifest.MaxChunks + 1).Select(_ => Guid.NewGuid()).ToList();
            tooMany.Validate().Should().NotBeNull();
        }

        [Fact]
        public void Validate_RejectsOutOfRangeSizeAndMissingName()
        {
            var zero = Valid(); zero.TotalSize = 0;
            zero.Validate().Should().NotBeNull();

            var huge = Valid(); huge.TotalSize = (long)ChunkedFileManifest.MaxChunks * ChunkedFileManifest.MaxChunkBytes + 1;
            huge.Validate().Should().NotBeNull();

            var noName = Valid(); noName.FileName = " ";
            noName.Validate().Should().NotBeNull();
        }
    }
}
