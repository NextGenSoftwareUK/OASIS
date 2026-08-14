using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// Request body for POST /v1/holonic-memory/holons/{holonId}/documents.
    /// Ingests a document by auto-chunking it and storing each chunk as a HolonicMemoryItem.
    /// </summary>
    public class DocumentIngestRequest
    {
        /// <summary>The full document text to ingest.</summary>
        public string Content { get; set; }

        /// <summary>
        /// Optional name for this document. Used as a prefix on chunk fieldNames
        /// (e.g. "cbt-protocol-v3" → "doc-cbt-protocol-v3-chunk-001").
        /// Defaults to a random 8-char id if not set.
        /// </summary>
        public string DocumentName { get; set; }

        /// <summary>
        /// Approximate tokens per chunk (4 chars ≈ 1 token).
        /// Default 400 tokens ≈ 1600 chars. Reduce for narrow context windows.
        /// </summary>
        public int ChunkTokens { get; set; } = 400;

        /// <summary>
        /// Overlap in tokens between consecutive chunks to preserve context at boundaries.
        /// Default 50 tokens ≈ 200 chars.
        /// </summary>
        public int OverlapTokens { get; set; } = 50;

        /// <summary>Tags applied to every chunk — use for filtering and retrieval.</summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>Retention policy for every chunk. Default Persistent.</summary>
        public RetentionPolicy RetentionPolicy { get; set; } = RetentionPolicy.Persistent;

        /// <summary>Expiry timestamp for TimeLimited chunks. Ignored for other retention policies.</summary>
        public DateTime? ExpiresUtc { get; set; }
    }
}
