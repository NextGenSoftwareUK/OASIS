using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>A unified image-generation request - the image-modality counterpart to CompletionRequest.</summary>
    public class ImageGenerationRequest
    {
        public string Prompt { get; set; }

        /// <summary>Which image-generation provider to use. Defaults to StabilityAI.</summary>
        public AIProviderType Provider { get; set; } = AIProviderType.StabilityAI;

        /// <summary>"auto" or a specific model id.</summary>
        public string Model { get; set; } = "auto";

        /// <summary>OpenAI-style size, e.g. "1024x1024".</summary>
        public string Size { get; set; }

        /// <summary>Stability AI-style aspect ratio, e.g. "16:9".</summary>
        public string AspectRatio { get; set; }

        public string OutputFormat { get; set; } = "png";
    }

    /// <summary>A unified image-generation response.</summary>
    public class ImageGenerationResponse
    {
        public string Provider { get; set; }

        public string Model { get; set; }

        public string ImageBase64 { get; set; }

        public string OutputFormat { get; set; }
    }
}
