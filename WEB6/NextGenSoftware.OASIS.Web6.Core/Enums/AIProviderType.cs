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
        OpenServ,

        /// <summary>Cerebras — ~3000 tok/s, fastest inference available. Models: llama-3.3-70b.</summary>
        Cerebras,

        /// <summary>Together AI — 100+ open models via OpenAI-compatible API. Models: meta-llama/Llama-3.3-70B-Instruct-Turbo.</summary>
        TogetherAI,

        /// <summary>Fireworks AI — fast open model inference. Models: accounts/fireworks/models/llama-v3p3-70b-instruct.</summary>
        FireworksAI,

        /// <summary>Moonshot AI (Kimi) — 128k context, strong for long documents. Models: moonshot-v1-128k.</summary>
        MoonshotAI,

        /// <summary>Perplexity — web-grounded answers with citations. Models: sonar-pro.</summary>
        Perplexity,

        /// <summary>LM Studio — local inference, complements Ollama. Env: LM_STUDIO_BASE_URL.</summary>
        LMStudio,

        // ── Decentralised / community providers (Priority 14) ────────────────────────────────────────

        /// <summary>Bittensor — decentralised, censorship-resistant inference via Corcel API or direct subnet.
        /// Env: BITTENSOR_API_KEY, BITTENSOR_SUBNET_UID. IsDecentralised = true.</summary>
        Bittensor,

        /// <summary>GaiaNet — community-run decentralised model nodes. OpenAI-compatible.
        /// Env: GAIANET_NODE_URL.</summary>
        GaiaNet,

        /// <summary>Custom operator-configured OpenAI-compatible endpoint.
        /// Set OASIS_DNA.json → OASIS.Web6.CustomProviders[].BaseUrl.</summary>
        Custom,

        /// <summary>Leela AI — spiritual intelligence / karmic-pattern reasoning via AWS Lambda endpoint.
        /// Env: LEELA_API_KEY, LEELA_BASE_URL.</summary>
        LeelaAI
    }
}
