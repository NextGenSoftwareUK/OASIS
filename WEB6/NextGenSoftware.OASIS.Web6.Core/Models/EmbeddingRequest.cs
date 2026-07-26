using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// Request for POST /v1/embed — generates float embeddings for one or more texts.
    /// </summary>
    public class EmbeddingRequest
    {
        public List<string> Texts { get; set; } = new List<string>();

        /// <summary>"openai", "cohere", "huggingface", or "auto".</summary>
        public string Provider { get; set; } = "auto";

        /// <summary>Model id; "auto" picks the default for the provider.</summary>
        public string Model { get; set; } = "auto";
    }
}
