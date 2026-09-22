# WEB6 Documentation

## Subscription protocol update (2026-09-22)

WEB5–WEB10 now share WEB4's operation-ID reserve → execute → settle protocol. Billable calls require a validated bearer and stable `Idempotency-Key`; consuming services use distinct service credentials and durable settlement outboxes. The old `authorize-request` counter is retired (410). See the [sequence, accounting and recovery contract](../../WEB4_SUBSCRIPTION_USAGE_LEDGER.md) and [configuration, historical migration, live tests and operational runbook](../../WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md). Provider measurements and reviewed price catalogues must be configured before enabling paid execution.


WEB6 is the AI Abstraction & Orchestration Layer of the OASIS Omniverse stack — a unified API surface for AI completions, multi-agent orchestration, semantic memory, identity, and IDE tooling across 26+ AI providers.

## Documents in this folder

| Document | Description |
|---|---|
| [WEB6 Getting Started Guide](WEB6-Getting-Started-Guide.md) | **Start here** — quickstart, all 26+ providers, FAHRN/BRAID/Memory, OpenAI-compat drop-in, migration phases, cost savings, auth, ONODE self-hosting |
| [WEB6 REST API Reference](WEB6_REST_API_Reference.md) | Full reference for all REST endpoints — request/response shapes, auth, error codes, gRPC and GraphQL |
| [WEB6 MCP Tool Reference](WEB6_MCP_Tool_Reference.md) | Full reference for all 259 MCP tools — parameters, return values, examples |
| [WEB6 User Guide](WEB6_User_Guide.md) | Common workflows, environment setup, recipes |
| [WEB6 Leela AI Integration Guide](WEB6-Leela-AI-Integration-Guide.md) | Cost reduction playbook — Bedrock swap, BRAID/caching, holonic document storage, Prometheus PromQL examples |

## Quick links

- [oasisomniverse.one](https://oasisomniverse.one)
- [WEB6 GitHub README](../../../../WEB6/README.md)
- [MCP Server README](../../../../WEB6/NextGenSoftware.OASIS.MCP.Server/README.md)
- [WEB4 API Docs](../WEB4%20OASIS%20API/README.md)
- [WEB5 API Docs](../WEB5%20STAR%20API/)
