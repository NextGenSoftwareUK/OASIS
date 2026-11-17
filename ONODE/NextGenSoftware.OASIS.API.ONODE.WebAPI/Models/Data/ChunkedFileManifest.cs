using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    /// <summary>
    /// Describes a large file stored as ordinary file holons (one per chunk). The manifest itself is saved as a
    /// file holon whose bytes start with <see cref="Magic"/>; its id is the file's id, and read access is checked on it.
    /// </summary>
    public class ChunkedFileManifest
    {
        public const int MaxChunkBytes = 8 * 1024 * 1024;
        public const int MaxChunks = 4096;

        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("OASIS-CHUNKED-FILE/1\n");
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string MimeType { get; set; }
        public long TotalSize { get; set; }
        public string Sha256 { get; set; }
        public List<Guid> ChunkIds { get; set; } = new();

        public byte[] ToBytes()
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(this, JsonOptions);
            var bytes = new byte[Magic.Length + json.Length];
            Magic.CopyTo(bytes, 0);
            json.CopyTo(bytes, Magic.Length);
            return bytes;
        }

        public static bool IsManifest(byte[] data) =>
            data != null && data.Length > Magic.Length && data.AsSpan(0, Magic.Length).SequenceEqual(Magic);

        public static ChunkedFileManifest TryParse(byte[] data)
        {
            if (!IsManifest(data)) return null;
            try
            {
                var manifest = JsonSerializer.Deserialize<ChunkedFileManifest>(data.AsSpan(Magic.Length), JsonOptions);
                return manifest?.Validate() == null ? manifest : null;
            }
            catch (JsonException) { return null; }
        }

        /// <summary>Returns an error message, or null when the manifest is usable.</summary>
        public string Validate()
        {
            if (ChunkIds == null || ChunkIds.Count == 0) return "At least one chunk is required.";
            if (ChunkIds.Count > MaxChunks) return $"A file may have at most {MaxChunks} chunks.";
            if (ChunkIds.Any(id => id == Guid.Empty)) return "Chunk ids must not be empty.";
            if (ChunkIds.Distinct().Count() != ChunkIds.Count) return "Chunk ids must be unique.";
            if (TotalSize <= 0 || TotalSize > (long)MaxChunks * MaxChunkBytes) return "TotalSize is out of range.";
            if (string.IsNullOrWhiteSpace(FileName)) return "FileName is required.";
            return null;
        }
    }

    public class SaveFileChunkRequest : OASISRequest
    {
        public byte[] Data { get; set; }
    }

    public class CompleteChunkedFileRequest : OASISRequest
    {
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string MimeType { get; set; }
        public long TotalSize { get; set; }
        public string Sha256 { get; set; }
        public List<Guid> ChunkIds { get; set; } = new();
    }

    public class LoadFileChunkRequest : OASISRequest
    {
        public Guid Id { get; set; }
        public int Index { get; set; }
    }

    /// <summary>
    /// Result of load-file-info. For a chunked file it describes the chunks to fetch with load-file-chunk;
    /// for an ordinary file it carries the bytes inline so the client never downloads twice.
    /// </summary>
    public class FileInfoResponse
    {
        public bool IsChunked { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string MimeType { get; set; }
        public long TotalSize { get; set; }
        public string Sha256 { get; set; }
        public int ChunkCount { get; set; }
        public byte[] Data { get; set; }
    }
}
