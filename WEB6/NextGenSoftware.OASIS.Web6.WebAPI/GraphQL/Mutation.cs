using HotChocolate;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Memory;
using NextGenSoftware.OASIS.Web6.Core.Models;
using NextGenSoftware.OASIS.Web6.WebAPI.GraphQL.Types;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GraphQL
{
    /// <summary>Root Mutation type for Web6 AI/ML/FAHRN/A2A operations.</summary>
    public class Mutation
    {
        private static OASISDNA DNA => OASISBootLoader.OASISBootLoader.OASISDNA;

        /// <summary>
        /// Generate text embeddings for one or more input strings.
        /// Returns a list of float vectors, one per input.
        /// </summary>
        public async Task<EmbeddingsResult> GenerateEmbeddingsAsync(
            [GraphQLDescription("One or more text strings to embed.")] List<string> inputs,
            [GraphQLDescription("AI provider to use for embedding (e.g. OpenAI).")] string provider = "",
            [GraphQLDescription("Specific model to use (leave empty for provider default).")] string model = "",
            [GraphQLDescription("Avatar GUID for karma-gated access (optional).")] string avatarId = "")
        {
            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;
            var request = new EmbeddingRequest { Texts = inputs, Provider = provider, Model = model };
            var manager = new EmbeddingManager(aid, DNA);
            var result = await manager.EmbedAsync(request);
            if (result.IsError)
                return new EmbeddingsResult { IsError = true, Message = result.Message };

            return new EmbeddingsResult
            {
                IsError = false,
                Vectors = result.Result?.Embeddings?
                    .Select(vec => new EmbeddingVector { Values = vec?.Select(f => (double)f).ToList() ?? new List<double>() })
                    .ToList() ?? new List<EmbeddingVector>()
            };
        }

        /// <summary>
        /// Dispatch a problem to the FAHRN agent network.
        /// FAHRN classifies the task, scores available agents, and returns the winning agent's answer
        /// along with a Mermaid reasoning plan.
        /// </summary>
        public async Task<AgentDispatchResult> DispatchAgentAsync(
            [GraphQLDescription("The problem or question to dispatch to the FAHRN network.")] string problem,
            [GraphQLDescription("Optional task type hint (e.g. 'creative', 'analytical').")] string taskType = "",
            [GraphQLDescription("Avatar GUID for context injection and karma gating (optional).")] string avatarId = "")
        {
            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;
            var request = new DispatchRequest { Problem = problem, TaskType = taskType, AvatarId = aid };
            var manager = new FAHRNManager(aid, DNA);
            var result = await manager.DispatchAsync(request);
            if (result.IsError)
                return new AgentDispatchResult { IsError = true, Message = result.Message };

            return new AgentDispatchResult
            {
                IsError          = false,
                Answer           = result.Result?.FinalAnswer ?? "",
                FinalMermaidPlan = result.Result?.FinalMermaidPlan ?? "",
                ModeUsed         = result.Result?.ModeUsed.ToString() ?? "",
                TotalLatencyMs   = result.Result?.TotalLatencyMs ?? 0
            };
        }

        /// <summary>
        /// Store a memory entry via an external memory provider (Mem0, Zep, Letta, etc.).
        /// </summary>
        public async Task<StoreMemoryResult> StoreMemoryAsync(
            [GraphQLDescription("Text content to store in memory.")] string content,
            [GraphQLDescription("External memory provider name (e.g. 'mem0', 'zep', 'letta').")] string provider = "",
            [GraphQLDescription("Avatar GUID for ownership tagging (optional).")] string avatarId = "")
        {
            if (string.IsNullOrWhiteSpace(content))
                return new StoreMemoryResult { IsError = true, Message = "content is required" };

            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;

            IExternalMemoryProvider memProvider = string.IsNullOrWhiteSpace(provider)
                ? MemoryProviderManager.Instance.ProviderNames.Count > 0
                    ? MemoryProviderManager.Instance.Get(MemoryProviderManager.Instance.ProviderNames[0])
                    : null
                : MemoryProviderManager.Instance.Get(provider);

            if (memProvider == null)
                return new StoreMemoryResult { IsError = true, Message = "No external memory provider is registered. Configure MEM0_API_KEY, ZEP_BASE_URL, or LETTA_BASE_URL." };

            await memProvider.AddAsync(aid, content, new Dictionary<string, string>());

            return new StoreMemoryResult
            {
                IsError = false,
                Id      = Guid.NewGuid().ToString(),
                Message = "Memory entry stored successfully."
            };
        }

        /// <summary>
        /// Generate an image from a text prompt using the configured image-generation provider.
        /// Returns the image as a base64-encoded string.
        /// </summary>
        public async Task<ImageGenerationResult> GenerateImageAsync(
            [GraphQLDescription("Text prompt describing the image to generate.")] string prompt,
            [GraphQLDescription("AI provider to use (e.g. StabilityAI, OpenAI).")] string provider = "StabilityAI",
            [GraphQLDescription("Model name override (leave empty for provider default).")] string model = "",
            [GraphQLDescription("Image width in pixels.")] int width = 1024,
            [GraphQLDescription("Image height in pixels.")] int height = 1024,
            [GraphQLDescription("Avatar GUID for karma-gated access (optional).")] string avatarId = "")
        {
            Guid aid = Guid.TryParse(avatarId, out var g) ? g : Guid.Empty;

            if (!Enum.TryParse<AIProviderType>(provider, true, out var providerType))
                providerType = AIProviderType.StabilityAI;

            var request = new ImageGenerationRequest
            {
                Prompt   = prompt,
                Provider = providerType,
                Model    = model,
                Size     = $"{width}x{height}"
            };

            var manager = new AIProviderManager(aid, DNA);
            var result = await manager.GenerateImageAsync(request);
            if (result.IsError)
                return new ImageGenerationResult { IsError = true, Message = result.Message };

            return new ImageGenerationResult
            {
                IsError     = false,
                Base64Image = result.Result?.ImageBase64 ?? "",
                Message     = "Image generated successfully."
            };
        }
    }

    // ──────────────────────────── Result types ────────────────────────────────

    public class EmbeddingsResult
    {
        public bool IsError { get; set; }
        public string Message { get; set; } = "";
        public List<EmbeddingVector> Vectors { get; set; } = new();
    }

    public class EmbeddingVector
    {
        public List<double> Values { get; set; } = new();
    }

    public class AgentDispatchResult
    {
        public bool IsError { get; set; }
        public string Message { get; set; } = "";
        public string Answer { get; set; } = "";
        public string FinalMermaidPlan { get; set; } = "";
        public string ModeUsed { get; set; } = "";
        public double TotalLatencyMs { get; set; }
    }

    public class StoreMemoryResult
    {
        public bool IsError { get; set; }
        public string Message { get; set; } = "";
        public string Id { get; set; } = "";
    }

    public class ImageGenerationResult
    {
        public bool IsError { get; set; }
        public string Message { get; set; } = "";
        public string Base64Image { get; set; } = "";
    }
}
