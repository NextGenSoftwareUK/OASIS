# WEB6 AI Provider & Capability Reference

WEB6 is OASIS's unified AI gateway — one API surface covering every major AI provider and capability in the world, with karma-gated access tiers, automatic fallback chains, semantic caching, and holonic avatar memory. This document covers every supported provider, required environment variables, default models, and all capability endpoints added across the WEB6 build sessions.

> **Running total: 100+ text providers · 9 image providers · 7 video providers · 2 TTS · 2 STT · 8 embedding providers · 6 reranking providers · 4 moderation providers · 6 web search providers · 4 document parsers · 4 code execution sandboxes · 5 translation providers · 3 classification providers · 2 extraction providers · 2 batch providers · 3 memory providers · 4 guardrail providers · 3 fine-tuning providers · 3 GraphRAG providers · 3 prompt optimisers**

---

## How to configure a provider

Set the environment variable(s) listed in the **Env vars** column, or add them to `OASIS_DNA.json` under `OASIS.Web6.ApiKeys`. Environment variables always override DNA values.

**Route to a specific provider:**
```json
POST /v1/complete
{
  "provider": "Anthropic",
  "model": "claude-sonnet-4-6",
  "messages": [{ "role": "user", "content": "Hello" }]
}
```

**Auto-routing** (`"provider": "auto"` or omit): WEB6 selects the best configured provider by `DefaultRoutingPriority` (`cost` | `quality` | `latency`).

---

## Text completion providers

### Foundation models — direct APIs

| Provider | Enum | Default model | Env vars |
|---|---|---|---|
| OpenAI | `OpenAI` | gpt-4o | `OPENAI_API_KEY` |
| Anthropic | `Anthropic` | claude-sonnet-4-6 | `ANTHROPIC_API_KEY` |
| Google Gemini | `Gemini` | gemini-2.0-flash | `GEMINI_API_KEY` |
| xAI Grok | `XAI` | grok-3 | `XAI_API_KEY` |
| Mistral | `Mistral` | mistral-large-latest | `MISTRAL_API_KEY` |
| Cohere | `Cohere` | command-r-plus | `COHERE_API_KEY` |
| DeepSeek | `DeepSeek` | deepseek-chat | `DEEPSEEK_API_KEY` |
| Meta Llama API | `MetaLlamaAPI` | meta-llama/Llama-3.3-70B-Instruct | `META_API_KEY` |
| AI21 Labs | `AI21Labs` | jamba-1.5-large | `AI21_API_KEY` |
| Writer | `Writer` | palmyra-x-004 | `WRITER_API_KEY` |
| Aleph Alpha | `AlephAlpha` | luminous-supreme-control | `ALEPH_ALPHA_API_KEY` |
| Reka AI | `RekaAI` | reka-core | `REKA_API_KEY` |
| Inflection AI | `InflectionAI` | inflection_3_pi | `INFLECTION_API_KEY` |
| AI71 (Falcon) | `AI71` | tiiuae/falcon-180b-chat | `AI71_API_KEY` |

### Inference providers / aggregators

| Provider | Enum | Default model | Env vars | Notes |
|---|---|---|---|---|
| OpenRouter | `OpenRouter` | openai/gpt-4o | `OPENROUTER_API_KEY` | 200+ models via one key |
| SambaNova | `SambaNova` | Meta-Llama-3.1-405B-Instruct | `SAMBANOVA_API_KEY` | |
| Nvidia NIM | `NvidiaNIM` | meta/llama-3.1-405b-instruct | `NVIDIA_API_KEY` | |
| Together AI | `TogetherAI` | meta-llama/Llama-3-70b-chat | `TOGETHER_API_KEY` | |
| Groq | `Groq` | llama-3.3-70b-versatile | `GROQ_API_KEY` | Fastest inference |
| Fireworks AI | `FireworksAI` | accounts/fireworks/models/llama-v3-70b-instruct | `FIREWORKS_API_KEY` | |
| Lepton AI | `LeptonAI` | llama3-1-405b | `LEPTON_API_KEY` | |
| Hyperbolic | `Hyperbolic` | meta-llama/Meta-Llama-3.1-405B-Instruct | `HYPERBOLIC_API_KEY` | |
| DeepInfra | `DeepInfra` | meta-llama/Meta-Llama-3.1-70B-Instruct | `DEEPINFRA_API_KEY` | |
| FriendliAI | `FriendliAI` | meta-llama-3.1-70b-instruct | `FRIENDLIAI_API_KEY` | |
| Lambda Labs | `LambdaLabs` | llama3.1-405b-instruct-fp8 | `LAMBDA_API_KEY` | |
| Modal | `Modal` | (custom endpoint) | `MODAL_API_KEY` + `MODAL_EXEC_ENDPOINT` | Serverless |
| OctoAI | `OctoAI` | meta-llama-3.1-405b-instruct | `OCTOAI_API_KEY` | |
| Novita AI | `NovitaAI` | meta-llama/llama-3.1-70b-instruct | `NOVITA_API_KEY` | |
| TensorArt | `TensorArt` | llama-3.1-70b | `TENSORART_API_KEY` | Also image gen |
| Featherless | `Featherless` | meta-llama/Meta-Llama-3.1-70B-Instruct | `FEATHERLESS_API_KEY` | |
| InferenceNet | `InferenceNet` | meta-llama/llama-3.1-70b-instruct | `INFERENCENET_API_KEY` | |
| Venice AI | `VeniceAI` | llama-3.3-70b | `VENICE_API_KEY` | Privacy-first |
| KlusterAI | `KlusterAI` | klusterai/Meta-Llama-3.1-405B-Instruct-Turbo | `KLUSTER_API_KEY` | |
| Chutes AI | `ChutesAI` | deepseek-ai/DeepSeek-V3-0324 | `CHUTES_API_KEY` | |
| Mancer AI | `MancerAI` | mancer/weaver | `MANCER_API_KEY` | Uncensored |
| AI Horde | `AIHorde` | (community cluster) | `AIHORDE_API_KEY` | Free tier |
| RunPod | `RunPod` | (serverless endpoint) | `RUNPOD_API_KEY` + `RUNPOD_ENDPOINT_ID` | |
| Baseten | `Baseten` | (truss endpoint) | `BASETEN_API_KEY` + `BASETEN_MODEL_ID` | |
| NLP Cloud | `NLPCloud` | finetuned-llama-3-70b | `NLPCLOUD_API_KEY` | |
| Predibase | `Predibase` | (lora endpoint) | `PREDIBASE_API_KEY` + `PREDIBASE_TENANT_ID` | Fine-tune serving |
| OpenPipe | `OpenPipe` | openpipe:your-model | `OPENPIPE_API_KEY` | Fine-tune + logging |

### Regional / sovereign AI

| Provider | Enum | Default model | Env vars | Notes |
|---|---|---|---|---|
| Alibaba Qwen | `AlibabaQwen` | qwen-max | `QWEN_API_KEY` | DashScope |
| Baidu ERNIE | `BaiduERNIE` | ernie-4.0-8k | `BAIDU_API_KEY` + `BAIDU_SECRET_KEY` | OAuth token exchange |
| Tencent Hunyuan | `TencentHunyuan` | hunyuan-pro | `HUNYUAN_SECRET_ID` + `HUNYUAN_SECRET_KEY` | Base64 HMAC auth |
| Zhipu AI | `ZhipuAI` | glm-4-plus | `ZHIPUAI_API_KEY` | |
| Doubao (ByteDance) | `Doubao` | doubao-pro-32k | `DOUBAO_API_KEY` | |
| Yi AI | `YiAI` | yi-large | `YI_API_KEY` | |
| MiniMax | `MiniMax` | abab6.5s-chat | `MINIMAX_API_KEY` + `MINIMAX_GROUP_ID` | |
| Stepfun | `Stepfun` | step-2-16k | `STEPFUN_API_KEY` | |
| iFlytek Spark | `SparkAI` | spark-max | `SPARK_API_KEY` + `SPARK_APP_ID` | |
| HyperCLOVA X | `HyperCLOVAX` | HCX-003 | `HYPERCLOVA_STUDIO_KEY` + `HYPERCLOVA_APIGW_KEY` | Naver/CLOVA |
| EXAONE | `EXAONE` | exaone-3.5-32b-instruct | `EXAONE_API_KEY` | LG AI Research |
| JAIS | `JAIS` | inceptionai/jais-adapted-70b-chat | `JAIS_BASE_URL` (optional) | Inception AI |

### Cloud/enterprise AI

| Provider | Enum | Default model | Env vars |
|---|---|---|---|
| Azure OpenAI | `AzureOpenAI` | gpt-4o | `AZURE_OPENAI_API_KEY` + `AZURE_OPENAI_ENDPOINT` + `AZURE_OPENAI_DEPLOYMENT` |
| AWS Bedrock | `AWSBedrock` | anthropic.claude-3-5-sonnet | `AWS_ACCESS_KEY_ID` + `AWS_SECRET_ACCESS_KEY` + `AWS_DEFAULT_REGION` |
| Google Vertex AI | `GoogleVertexAI` | gemini-1.5-pro-002 | `VERTEX_PROJECT_ID` + `VERTEX_LOCATION` (GCP ADC token) |
| IBM WatsonX | `IBMWatsonX` | ibm/granite-34b-code-instruct | `WATSONX_API_KEY` + `WATSONX_PROJECT_ID` |
| Snowflake Cortex | `SnowflakeCortex` | llama3.3-70b | `SNOWFLAKE_ACCOUNT` + `SNOWFLAKE_USER` + `SNOWFLAKE_PASSWORD` |
| Databricks Serving | `DatabricksServing` | databricks-meta-llama-3-1-70b-instruct | `DATABRICKS_TOKEN` + `DATABRICKS_HOST` |
| Cloudflare Workers AI | `CloudflareWorkersAI` | @cf/meta/llama-3.1-70b-instruct | `CLOUDFLARE_API_TOKEN` + `CLOUDFLARE_ACCOUNT_ID` |
| OpenServ | `OpenServ` | (SERV catalog) | `SERV_API_KEY` | OASIS-native gateway |

### Self-hosted / local (no API key required)

| Provider | Enum | Default base URL | Notes |
|---|---|---|---|
| Ollama | `Ollama` | `OLLAMA_BASE_URL` (`:11434`) | All local Ollama models |
| LM Studio | `LMStudio` | `LMSTUDIO_BASE_URL` (`:1234`) | OpenAI-compat local server |
| VLLM | `VLLM` | `VLLM_BASE_URL` (`:8000`) | High-throughput inference |
| Text Generation Inference | `TGI` | `TGI_BASE_URL` (`:8080`) | Hugging Face TGI |
| Jan | `Jan` | `JAN_BASE_URL` (`:1337`) | Local GUI + server |
| Llamafile | `Llamafile` | `LLAMAFILE_BASE_URL` (`:8080`) | Single-file model server |
| GPT4All | `GPT4All` | `GPT4ALL_BASE_URL` (`:4891`) | Local cross-platform |
| GaiaNet | `GaiaNet` | `GAIANET_BASE_URL` | Decentralised network |
| Custom | `Custom` | `CUSTOM_AI_BASE_URL` | Bring your own |

---

## Image generation providers

| Provider | Enum | Default model | Env vars | Notes |
|---|---|---|---|---|
| Stability AI | `StabilityAI` | stable-image-ultra | `STABILITY_API_KEY` | Default |
| OpenAI | `OpenAI` | gpt-image-1 | `OPENAI_API_KEY` | |
| Black Forest Labs | `BlackForestLabs` | flux-pro-1.1 | `BFL_API_KEY` | Async poll |
| Ideogram | `Ideogram` | V_2_TURBO | `IDEOGRAM_API_KEY` | |
| Leonardo AI | `LeonardoAI` | (any) | `LEONARDOAI_API_KEY` | Async poll |
| Novita AI | `NovitaAI` | (any) | `NOVITA_API_KEY` | Async poll |
| TensorArt | `TensorArt` | (any) | `TENSORART_API_KEY` | Async poll |
| xAI Aurora | `XAIAurora` | (aurora) | `XAI_API_KEY` | |
| ComfyUI | `ComfyUI` | (workflow) | `COMFYUI_BASE_URL` | Self-hosted, workflow JSON |

**Endpoint:** `POST /v1/images/generate`

---

## Video generation providers

| Provider | Enum | Default model | Env vars | Notes |
|---|---|---|---|---|
| RunwayML | `RunwayML` | gen3a_turbo | `RUNWAY_API_KEY` | Text-to-video + image-to-video |
| Luma AI | `LumaAI` | dream-machine | `LUMA_API_KEY` | |
| Pika Labs | `PikaLabs` | pika-1.5 | `PIKA_API_KEY` | |
| Kling AI | `KlingAI` | kling-v1 | `KLING_ACCESS_KEY` + `KLING_SECRET_KEY` | JWT auth |
| HailuoAI (MiniMax) | `HailuoAI` | video-01 | `MINIMAX_API_KEY` + `MINIMAX_GROUP_ID` | |
| Vidu | `Vidu` | vidu-2.0 | `VIDU_API_KEY` | |
| Wan Video | `WanVideo` | wan2.1-t2v-turbo | `DASHSCOPE_API_KEY` | Alibaba DashScope async |

**Endpoint:** `POST /v1/video/generate`  
All providers use async job submission + polling (up to 5 min).

---

## Text-to-speech providers

| Provider | Enum | Env vars | Notes |
|---|---|---|---|
| ElevenLabs | `ElevenLabs` | `ELEVENLABS_API_KEY` | Default voice: Rachel |
| PlayHT | `PlayHT` | `PLAYHT_API_KEY` + `PLAYHT_USER_ID` | |

**Endpoint:** `POST /v1/audio/speech`  
Returns `audio/mpeg` bytes.

---

## Speech-to-text providers

| Provider | Enum | Env vars | Notes |
|---|---|---|---|
| AssemblyAI | `AssemblyAI` | `ASSEMBLYAI_API_KEY` | Upload + async poll |
| Deepgram | `Deepgram` | `DEEPGRAM_API_KEY` | Real-time REST |

**Endpoint:** `POST /v1/audio/transcriptions`  
Accepts `AudioBytes` (base64) or `AudioUrl`.

---

## Embedding providers

| Provider | Default model | Env vars |
|---|---|---|
| OpenAI | text-embedding-3-large | `OPENAI_API_KEY` |
| Cohere | embed-english-v3.0 | `COHERE_API_KEY` |
| Voyage AI | voyage-3-large | `VOYAGE_API_KEY` |
| Jina AI | jina-embeddings-v3 | `JINA_API_KEY` |
| Mistral | mistral-embed | `MISTRAL_API_KEY` |
| Nomic | nomic-embed-text-v1.5 | `NOMIC_API_KEY` |
| HuggingFace | BAAI/bge-large-en-v1.5 | `HUGGINGFACE_API_KEY` |
| Ollama | nomic-embed-text | `OLLAMA_BASE_URL` |
| Nvidia NIM | nv-embedqa-e5-v5 | `NVIDIA_API_KEY` |
| Together AI | m2-bert-80M-8k-retrieval | `TOGETHER_API_KEY` |
| DeepInfra | BAAI/bge-m3 | `DEEPINFRA_API_KEY` |

**Endpoint:** `POST /v1/embed`  
Auto-selects first configured provider.

---

## Reranking providers

| Provider | Default model | Env vars |
|---|---|---|
| Cohere | rerank-v3.5 | `COHERE_API_KEY` |
| Jina AI | jina-reranker-v2-base-multilingual | `JINA_API_KEY` |
| Voyage AI | rerank-2 | `VOYAGE_API_KEY` |
| Mixedbread | mxbai-rerank-large-v2 | `MIXEDBREAD_API_KEY` |
| Nvidia NIM | nv-rerankqa-mistral-4b-v3 | `NVIDIA_API_KEY` |
| FlashRank | ms-marco-MiniLM-L-12-v2 | `FLASHRANK_BASE_URL` (`:8090`) |

**Endpoint:** `POST /v1/rerank`

---

## Moderation providers

| Provider | Notes | Env vars |
|---|---|---|
| OpenAI | omni-moderation-latest; free | `OPENAI_API_KEY` |
| Azure Content Safety | 4 categories, severity 0-6 | `AZURE_CONTENT_SAFETY_KEY` + `AZURE_CONTENT_SAFETY_ENDPOINT` |
| Llama Guard 4 | Groq (fastest) → Together → local Ollama | `GROQ_API_KEY` or `TOGETHER_API_KEY` |
| Google Perspective | 7 toxicity attributes | `PERSPECTIVE_API_KEY` |

**Endpoint:** `POST /v1/moderation`

---

## Web search providers

| Provider | Notes | Env vars |
|---|---|---|
| Tavily | AI-native; returns clean text + synthesised answer | `TAVILY_API_KEY` |
| Exa | Neural semantic search; full page content | `EXA_API_KEY` |
| Brave Search | Privacy-first index | `BRAVE_API_KEY` |
| SerpAPI | Aggregates Google/Bing/DDG/Baidu/Yandex | `SERPAPI_API_KEY` |
| Bing Web Search | Microsoft index | `BING_API_KEY` |
| Google Custom Search | Requires search engine ID | `GOOGLE_SEARCH_API_KEY` + `GOOGLE_SEARCH_CX` |

**Endpoint:** `POST /v1/search`

---

## Document processing providers

| Provider | Best for | Env vars |
|---|---|---|
| LlamaParse | RAG-optimised PDF/DOCX → clean markdown | `LLAMA_CLOUD_API_KEY` |
| Unstructured.io | Any file type; structured element extraction | `UNSTRUCTURED_API_KEY` |
| Reducto | High-accuracy PDF, table/figure extraction | `REDUCTO_API_KEY` |
| Azure Document Intelligence | Enterprise documents, forms, receipts | `AZURE_DOCINTEL_KEY` + `AZURE_DOCINTEL_ENDPOINT` |

**Endpoint:** `POST /v1/documents/parse`  
Accepts `FileBytes` + `FileName`, or a public `Url`.

---

## Code execution sandboxes

| Provider | Notes | Env vars |
|---|---|---|
| E2B | Firecracker microVMs; <200ms cold start; Python/JS/Go/Java/R | `E2B_API_KEY` |
| Modal Labs | Serverless; GPU workloads; custom endpoint | `MODAL_API_KEY` + `MODAL_EXEC_ENDPOINT` |
| Daytona | Open-source dev container orchestrator | `DAYTONA_API_KEY` |
| Judge0 | 60+ languages; self-hostable; RapidAPI hosted | `JUDGE0_BASE_URL` (optional `JUDGE0_RAPIDAPI_KEY`) |

**Endpoint:** `POST /v1/code/execute`  
Returns `stdout`, `stderr`, `exitCode`, `success`.  
⚠️ Always run code through `/v1/guardrails/check` + `/v1/moderation` before executing untrusted AI output.

---

## Translation providers

| Provider | Languages | Env vars |
|---|---|---|
| DeepL | 30+ | `DEEPL_API_KEY` (suffix `:fx` for free tier) |
| Google Translate | 100+ | `GOOGLE_TRANSLATE_KEY` |
| Azure Translator | 100+ | `AZURE_TRANSLATOR_KEY` + `AZURE_TRANSLATOR_REGION` |
| LibreTranslate | 30+ (self-hosted) | `LIBRETRANSLATE_URL` |
| ModernMT | 200+ | `MODERNMT_API_KEY` |

**Endpoint:** `POST /v1/translate`

---

## Text classification providers

| Provider | Mode | Env vars |
|---|---|---|
| Cohere Classify | Few-shot with examples | `COHERE_API_KEY` |
| OpenAI | Zero-shot via system prompt | `OPENAI_API_KEY` |
| HuggingFace | Zero-shot via BART-MNLI (or custom) | `HUGGINGFACE_API_KEY` or `HF_INFERENCE_URL` |

**Endpoint:** `POST /v1/classify`

---

## Structured extraction providers

| Provider | Method | Env vars |
|---|---|---|
| OpenAI | JSON mode + schema in system prompt | `OPENAI_API_KEY` |
| Anthropic | Tool use (schema as tool input_schema) | `ANTHROPIC_API_KEY` |

**Endpoint:** `POST /v1/extract`  
Supply a JSON Schema in `schemaJson`; receive extracted data as a JSON string.

---

## Batch completion providers

| Provider | Limit | Discount | Env vars |
|---|---|---|---|
| OpenAI Batch API | 50,000 requests | 50% | `OPENAI_API_KEY` |
| Anthropic Message Batches | 10,000 requests | 50% | `ANTHROPIC_API_KEY` |

**Endpoints:** `POST /v1/batch/submit` · `GET /v1/batch/{id}/status`

---

## Agent memory providers

| Provider | Notes | Env vars |
|---|---|---|
| Mem0 | Hosted; automatic memory extraction | `MEM0_API_KEY` |
| Zep | Self-hosted or cloud; session-based | `ZEP_API_KEY` + `ZEP_BASE_URL` |
| Letta (MemGPT) | Stateful agents with archival memory | `LETTA_API_KEY` + `LETTA_BASE_URL` + `LETTA_AGENT_ID` |

**Endpoints:** `POST /v1/memory/store` · `POST /v1/memory/query`

---

## Guardrails providers

| Provider | Detects | Env vars |
|---|---|---|
| Guardrails AI | Policy violations (custom guards) | `GUARDRAILS_BASE_URL` |
| Lakera Guard | Prompt injection + harmful content | `LAKERA_API_KEY` |
| Rebuff | Prompt injection (heuristic + vector + model) | `REBUFF_API_KEY` |
| AWS Comprehend | PII (names, SSN, credit card, email, …) | `AWS_ACCESS_KEY_ID` + `AWS_SECRET_ACCESS_KEY` |

**Endpoint:** `POST /v1/guardrails/check`

---

## Fine-tuning providers

| Provider | Base models | Env vars |
|---|---|---|
| OpenAI | gpt-4o-mini, gpt-3.5-turbo | `OPENAI_API_KEY` |
| Together AI | Llama 3.1 8B/70B, Mistral, CodeLlama | `TOGETHER_API_KEY` |
| Mistral | open-mistral-7b, mistral-small | `MISTRAL_API_KEY` |

**Endpoints:** `POST /v1/fine-tuning/jobs` · `GET /v1/fine-tuning/jobs/{jobId}`

---

## GraphRAG providers

| Provider | Mode | Env vars |
|---|---|---|
| Microsoft GraphRAG | global / local | `GRAPHRAG_BASE_URL` |
| LightRAG | naive / local / global / hybrid | `LIGHTRAG_BASE_URL` |
| Neo4j | full-text + vector index | `NEO4J_URI` + `NEO4J_USERNAME` + `NEO4J_PASSWORD` |

**Endpoint:** `POST /v1/graphrag/query`

---

## Prompt optimisation providers

| Provider | Method | Env vars |
|---|---|---|
| PromptPerfect | Hosted; model-specific optimisation | `PROMPTPERFECT_API_KEY` |
| Self-critique loop | DSPy-style iterative rewrite via any LLM | `OPENAI_API_KEY` or `ANTHROPIC_API_KEY` |
| Constitutional AI | Anthropic critique-then-revise (CAI) | `ANTHROPIC_API_KEY` |

**Endpoint:** `POST /v1/prompts/optimise`

---

## Complete endpoint map

| Method | Path | Description |
|---|---|---|
| POST | `/v1/complete` | Unified text completion |
| POST | `/v1/complete/tool-result` | Tool-call loop continuation |
| POST | `/v1/complete/stream` | SSE streaming completion |
| POST | `/v1/embed` | Text embeddings |
| POST | `/v1/rerank` | Document reranking |
| POST | `/v1/moderation` | Content moderation |
| POST | `/v1/search` | Live web search |
| POST | `/v1/documents/parse` | Document parsing |
| POST | `/v1/code/execute` | Sandboxed code execution |
| POST | `/v1/images/generate` | Image generation |
| POST | `/v1/video/generate` | Video generation |
| POST | `/v1/audio/speech` | Text-to-speech |
| POST | `/v1/audio/transcriptions` | Speech-to-text |
| POST | `/v1/translate` | Machine translation |
| POST | `/v1/classify` | Text classification |
| POST | `/v1/extract` | Structured extraction |
| POST | `/v1/batch/submit` | Batch completion submit |
| GET | `/v1/batch/{id}/status` | Batch status poll |
| POST | `/v1/memory/store` | Agent memory store |
| POST | `/v1/memory/query` | Agent memory query |
| POST | `/v1/guardrails/check` | Safety guardrails |
| POST | `/v1/fine-tuning/jobs` | Fine-tune job create |
| GET | `/v1/fine-tuning/jobs/{id}` | Fine-tune job status |
| POST | `/v1/graphrag/query` | Graph-augmented RAG |
| POST | `/v1/prompts/optimise` | Prompt optimisation |
| GET | `/v1/openserv/models` | List SERV model catalog |
| GET | `/v1/providers` | List all configured providers |
| GET | `/v1/health` | Provider health status |
| GET | `/v1/usage` | Avatar usage/quota summary |
| WS | `/v1/ws` | WebSocket session |

---

## Karma access tiers

| Tier | Karma required | Endpoints available |
|---|---|---|
| Free | 0 | `/v1/complete` (rate-limited), `/v1/embed`, `/v1/moderation` |
| Bronze | 100 | + `/v1/search`, `/v1/translate`, `/v1/classify` |
| Silver | 500 | + `/v1/rerank`, `/v1/documents/parse`, `/v1/extract`, `/v1/prompts/optimise` |
| Gold | 2,000 | + `/v1/images/generate`, `/v1/audio/*`, `/v1/memory/*`, `/v1/guardrails/*` |
| Platinum | 10,000 | + `/v1/video/generate`, `/v1/code/execute`, `/v1/batch/*`, `/v1/graphrag/*` |
| Enterprise | Custom | + `/v1/fine-tuning/*`, custom rate limits, priority routing |

---

## Auto-routing priority modes

Set `OASIS_DNA.Web6.DefaultRoutingPriority`:

- **`quality`** — OpenAI GPT-4o → Anthropic Claude → Google Gemini → ...
- **`cost`** — Groq → Together AI → DeepInfra → Fireworks → OpenRouter → ...
- **`latency`** — Groq → SambaNova → Fireworks → Lepton → NvidiaNIM → ...

Local providers (Ollama, GPT4All, VLLM, TGI, Jan, Llamafile) are always preferred when their base URLs are configured and `PreferLocal = true`.

---

## Session changelog

### Session 1 — Core gateway
- 25 text completion providers wired (OpenAI, Anthropic, Gemini, Groq, Mistral, Cohere, xAI, DeepSeek, HuggingFace, Azure, Bedrock, OpenServ, Ollama, LMStudio, GaiaNet, Custom, StabilityAI, Fireworks, Together, SambaNova, Nvidia, Lepton, Hyperbolic, DeepInfra, FriendliAI)
- `CompletionController` with FAHRN + Holonic BRAID injection
- `EmbeddingController` with OpenAI, Cohere, HuggingFace, Nomic, Ollama

### Session 2 — Provider expansion (100+ providers)
- Added 40+ text providers: OpenRouter, SambaNova, Lambda, Modal, OctoAI, Novita, TensorArt, Featherless, InferenceNet, Venice, KlusterAI, AI21, Writer, Cloudflare Workers AI, Google Vertex AI, Alibaba Qwen, Doubao, MiniMax, ZhipuAI, Stepfun, BaiduERNIE, YiAI, TencentHunyuan, SparkAI, MetaLlamaAPI, AI71, AlephAlpha, ArceeAI, HyperCLOVAX, EXAONE, JAIS, xAIAurora, ComfyUI, HailuoAI, Vidu, WanVideo, GPT4All, RunPod, Baseten, NLPCloud, Predibase, OpenPipe, ChutesAI, MancerAI, AIHorde, VLLM, TGI, Jan, Llamafile, IBMWatsonX, SnowflakeCortex, DatabricksServing, RekaAI, InflectionAI
- `EmbeddingManager` extended: Voyage, Jina, Mistral, Nomic, Ollama, Nvidia, Together, DeepInfra
- **New managers:** `RerankingManager`, `ModerationManager`, `WebSearchManager`, `DocumentProcessingManager`, `CodeExecutionManager`
- **New controllers (Tier 1+2):** `RerankController`, `ModerationController`, `SearchController`, `DocumentsController`, `CodeController`, `VideoController`, `SpeechController`
- **New managers (Tier 3):** `TranslationManager`, `ClassificationManager`, `StructuredExtractionManager`, `BatchManager`, `AgentMemoryManager`, `GuardrailsManager`, `FinetuningManager`, `PromptOptimiserManager`, `GraphRAGManager`
- **New controllers (Tier 3):** `TranslationController`, `ClassificationController`, `ExtractionController`, `BatchController`, `MemoryController`, `GuardrailsController`, `FinetuningController`, `GraphRAGController`, `PromptController`
- Models added: `VideoGenerationRequest/Response`, `SpeechRequest`, `TranscriptionRequest/Response`
