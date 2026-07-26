using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// Response from POST /v1/embed.
    /// </summary>
    public class EmbeddingResponse
    {
        public List<float[]> Embeddings { get; set; } = new List<float[]>();
        public string Provider { get; set; }
        public string Model { get; set; }
        public int TotalTokens { get; set; }
    }
}
