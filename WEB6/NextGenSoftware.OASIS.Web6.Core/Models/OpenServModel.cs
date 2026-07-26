using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>One model entry in the OpenServ SERV inference catalog.</summary>
    public class OpenServModel
    {
        public string Id { get; set; }
        public string Label { get; set; }

        public OpenServModel(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }

    /// <summary>
    /// The SERV catalog reachable through OpenServ's OpenAI-compatible chat/completions endpoint with a
    /// single SERV_API_KEY - spans every underlying provider (OpenAI, Anthropic, Google, xAI, Qwen, DeepSeek).
    /// Kept in sync with the OASIS IDE's OPENSERV_MODELS list (src/main/services/OpenServAgentService.ts).
    /// See https://docs.openserv.ai/serv-reasoning/sdk-integration and https://docs.openserv.ai/serv-reasoning/models
    /// </summary>
    public static class OpenServCatalog
    {
        public const string DefaultModel = "gpt-5.4";

        public static readonly List<OpenServModel> Models = new List<OpenServModel>
        {
            new OpenServModel("gpt-5.5", "GPT-5.5 (OpenAI)"),
            new OpenServModel("gpt-5.4", "GPT-5.4 (OpenAI)"),
            new OpenServModel("gpt-5.4-mini", "GPT-5.4 Mini (OpenAI)"),
            new OpenServModel("gpt-5.4-nano", "GPT-5.4 Nano (OpenAI)"),
            new OpenServModel("o3", "o3 (OpenAI)"),
            new OpenServModel("o3-mini", "o3-mini (OpenAI)"),
            new OpenServModel("o3-pro", "o3-pro (OpenAI)"),
            new OpenServModel("o4-mini", "o4-mini (OpenAI)"),
            new OpenServModel("claude-opus-4.6", "Claude Opus 4.6 (Anthropic)"),
            new OpenServModel("claude-sonnet-4.6", "Claude Sonnet 4.6 (Anthropic)"),
            new OpenServModel("claude-haiku-4.5", "Claude Haiku 4.5 (Anthropic)"),
            new OpenServModel("gemini-flash-latest", "Gemini Flash (Google)"),
            new OpenServModel("gemini-pro-latest", "Gemini Pro (Google)"),
            new OpenServModel("gemma-4-26b-a4b-it", "Gemma 4 26B (Google)"),
            new OpenServModel("gemma-4-31b-it", "Gemma 4 31B (Google)"),
            new OpenServModel("grok-4.3", "Grok 4.3 (xAI)"),
            new OpenServModel("grok-4.20", "Grok 4.20 (xAI)"),
            new OpenServModel("qwen3.6-flash", "Qwen3.6 Flash"),
            new OpenServModel("qwen3.6-max-preview", "Qwen3.6 Max Preview"),
            new OpenServModel("deepseek-v4-pro", "DeepSeek v4 Pro"),
            new OpenServModel("deepseek-v4-flash", "DeepSeek v4 Flash"),
        };
    }
}
