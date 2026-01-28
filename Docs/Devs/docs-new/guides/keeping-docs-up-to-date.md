# Keeping Documentation Up-to-Date

This guide explains how we keep the OASIS API documentation in sync with the live API using **OpenAPI/Swagger** as the single source of truth.

---

## 1. OpenAPI as the source of truth

The OASIS API is built with **ASP.NET Core** and uses **Swashbuckle** to expose an **OpenAPI (Swagger) specification** generated from the code. That means:

- **The spec is always generated from the current code.** When you add or change controllers, the spec updates automatically.
- **The spec is the authoritative list of endpoints**, paths, methods, parameters, and response schemas.

**Relevant standards and references:**

- **[OpenAPI Specification](https://swagger.io/specification/#openapi-document)** – The standard that defines how APIs are described (paths, operations, schemas). Our Swagger JSON conforms to this.
- **[NestJS OpenAPI introduction](https://docs.nestjs.com/openapi/introduction)** – Shows how another framework (NestJS) keeps docs in sync with code via OpenAPI. OASIS uses ASP.NET Core + Swashbuckle instead, but the idea is the same: **code-first → OpenAPI → docs**.

**Where the spec lives:**

- **Live spec (JSON):** `http://api.oasisweb4.com/swagger/v1/swagger.json`
- **Swagger UI (interactive):** [http://api.oasisweb4.com/swagger/index.html](http://api.oasisweb4.com/swagger/index.html)

Treat the **live spec** (or a snapshot you generate from the same codebase) as the source of truth for *what exists*; the markdown docs add narrative, examples, and guides.

---

## 2. Strategy: three ways to stay in sync

### A. Link to the live spec everywhere

- In overview and reference pages, link to **Swagger UI** and **swagger.json**.
- Tell readers: “For the latest list of endpoints and parameters, see [Swagger](http://api.oasisweb4.com/swagger/index.html) or the OpenAPI spec.”

This way, **endpoint lists are always up-to-date** because they come from the running API.

### B. Generate reference content from the spec

- Run a script that **fetches the OpenAPI spec** and **generates** an endpoint inventory (e.g. a markdown table or JSON list).
- Commit that generated file (e.g. `reference/openapi-endpoint-inventory.md`) or generate it in CI and publish it with the docs.
- Update the script if the API base URL or spec path changes.

**Script provided:** `scripts/generate-openapi-inventory.js` (see below).

### C. Validate docs against the spec (CI)

- Keep a list of **paths we document** (or derive it from our doc structure).
- In CI, **fetch the spec** and check that:
  - Every documented path exists in the spec, and/or
  - New paths in the spec are either documented or called out in a “changelog” or backlog.
- **Fail the build** if documented endpoints are missing from the spec (e.g. typo or removed API), so we fix the docs.

**Script provided:** `scripts/validate-docs-against-openapi.js` (see below). Edit `scripts/documented-paths.txt` to list the paths you document; comment out paths not yet in the spec (e.g. `# /api/competition`).

---

## 3. Scripts provided in this repo

| Script | Purpose |
|--------|---------|
| `scripts/generate-openapi-inventory.js` | Fetches the live OpenAPI spec and writes `reference/openapi-endpoint-inventory.md` (paths, methods, tags). Run after API changes to refresh the list. |
| `scripts/validate-docs-against-openapi.js` | Fetches the spec and optionally checks that a set of “documented” paths exist in the spec. Use in CI to ensure docs don’t reference removed or wrong endpoints. |
| `test-api-endpoints.sh` | Hits live endpoints to verify they respond (auth, 200, etc.). Complements the spec: spec says *what*, tests say *that it works*. |

**Run from repo root (or from `Docs/Devs/docs-new`):**

```bash
# Generate the endpoint inventory (requires Node.js)
node Docs/Devs/docs-new/scripts/generate-openapi-inventory.js

# Validate documented paths against the spec (optional, for CI)
node Docs/Devs/docs-new/scripts/validate-docs-against-openapi.js
```

---

## 4. Recommended workflow

1. **Developers:** Add or change API endpoints in the ONODE WebAPI (C# controllers). The OpenAPI spec is regenerated from the code when the API runs.
2. **Docs:**  
   - Update **narrative** docs (overview, guides, examples) by hand when behavior or intent changes.  
   - Regenerate **reference** content from the spec (e.g. run `generate-openapi-inventory.js`) when you want a fresh endpoint list.
3. **CI (optional):**  
   - Run `validate-docs-against-openapi.js` so that if someone documents an endpoint that no longer exists (or mistypes a path), the build fails.  
   - Run `test-api-endpoints.sh` against a deployed or local API to catch runtime regressions.

---

## 5. What to do when the API changes

| Change | Action |
|--------|--------|
| New endpoint | Spec updates automatically. Update the right API doc (e.g. `wallet-api.md`) and run `generate-openapi-inventory.js` if you use it. |
| Removed endpoint | Remove or adjust the doc. CI (validate script) can fail if the doc still references the old path. |
| New controller / area | Add a new doc page and link it from the overview; run the inventory script. |
| Parameter or response schema change | Update the narrative and examples in the doc; the spec already reflects the new schema in Swagger UI. |

---

## 6. Summary

- **OpenAPI/Swagger** is the single source of truth for *what the API exposes*; see [OpenAPI Specification](https://swagger.io/specification/#openapi-document) and [NestJS OpenAPI](https://docs.nestjs.com/openapi/introduction) for the pattern (code → OpenAPI → docs).
- **Link** to the live spec and Swagger UI so readers always see current endpoints.
- **Generate** an endpoint inventory from the spec so reference docs don’t drift.
- **Validate** in CI that documented paths still exist in the spec.
- **Test** live endpoints with `test-api-endpoints.sh` to ensure they respond as expected.

---

*Last Updated: January 24, 2026*
