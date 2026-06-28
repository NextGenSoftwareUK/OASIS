namespace NextGenSoftware.OASIS.Web6.Core.Enums
{
    /// <summary>
    /// The AI providers/model runtimes that WEB6 can route a completion request to.
    /// </summary>
    public enum AIProviderType
    {
        Auto,
        OpenAI,
        Anthropic,
        Gemini,
        Llama,
        Mistral,
        Groq,
        Ollama,
        HuggingFace,
        Cohere,
        XAI,
        DeepSeek,
        AWSBedrock,
        AzureOpenAI,
        StabilityAI,

        /// <summary>OpenServ SERV inference gateway - one API key (SERV_API_KEY) reaches every model in
        /// the SERV catalog (OpenAI, Anthropic, Google, xAI, Qwen, DeepSeek) via an OpenAI-compatible
        /// chat/completions endpoint. See https://docs.openserv.ai/serv-reasoning/sdk-integration</summary>
        OpenServ
    }
}
