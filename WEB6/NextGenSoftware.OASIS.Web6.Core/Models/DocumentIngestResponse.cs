using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>Response from POST /v1/holonic-memory/holons/{holonId}/documents.</summary>
    public class DocumentIngestResponse
    {
        public string DocumentName { get; set; }
        public int TotalChunks { get; set; }
        public int StoredChunks { get; set; }
        /// <summary>Chunks skipped because a near-identical item (cosine similarity ≥ 0.98) was already in the holon.</summary>
        public int DeduplicatedChunks { get; set; }
        /// <summary>The fieldName of each stored chunk — use these to retrieve or delete individual chunks.</summary>
        public List<string> ChunkFieldNames { get; set; }
        /// <summary>Per-chunk errors, if any chunks failed to store. Null when all chunks succeeded.</summary>
        public List<string> Errors { get; set; }
    }
}
