using System;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

/// <summary>
/// A finalized commit references an unavailable chunk. Public transaction
/// references support read-only diagnosis; no ciphertext, keys or plaintext are
/// included. This is neither record absence nor permission to replay the write.
/// </summary>
public sealed class SolanaMemoChunkUnavailableException : InvalidOperationException
{
    public const string Code = "SOLANA_MEMO_CHUNK_UNAVAILABLE";
    public string RootSignature { get; }
    public string MissingSignature { get; }
    public int ChunkIndex { get; }
    public int ChunkCount { get; }
    public Guid WriteId { get; }

    internal SolanaMemoChunkUnavailableException(string rootSignature, string missingSignature,
        int chunkIndex, int chunkCount, Guid writeId)
        : base("Finalized Solana memo chunk is unavailable; no partial record returned.")
    {
        RootSignature = rootSignature;
        MissingSignature = missingSignature;
        ChunkIndex = chunkIndex;
        ChunkCount = chunkCount;
        WriteId = writeId;
    }
}
