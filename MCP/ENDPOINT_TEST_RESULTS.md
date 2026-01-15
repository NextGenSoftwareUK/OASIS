# OASIS MCP Endpoint Test Results

**Test Date:** 2026-01-07  
**Tester:** Auto (AI Assistant)  
**Test Environment:** OASIS API ONODE WebAPI

## Executive Summary

Tested **~15 endpoints** across multiple categories. Found **several working endpoints** and **multiple issues** that need to be addressed.

### Overall Status
- ✅ **Working:** 6 endpoints
- ❌ **Issues Found:** 9 endpoints
- ⚠️ **Needs Investigation:** Multiple endpoints

---

## Test Results by Category

### ✅ Health Check
| Endpoint | MCP Tool | Status | Notes |
|----------|----------|--------|-------|
| `GET /api/health` | `oasis_health_check` | ✅ **PASS** | Returns healthy status correctly |

**Result:**
```json
{
  "status": "healthy",
  "data": {
    "status": "healthy",
    "timestamp": "2026-01-07T14:41:18.359866Z",
    "service": "OASIS API ONODE WebAPI"
  }
}
```

---

### ❌ Search Controller - **MAJOR ISSUES**

| Endpoint | MCP Tool | Status | Error | Issue |
|----------|----------|--------|-------|-------|
| `GET /api/search` | `oasis_basic_search` | ❌ **FAIL** | 404 | Route mismatch - controller expects `{searchParams}` route param, client uses query string |
| `GET /api/search/advanced` | `oasis_advanced_search` | ❌ **FAIL** | 400 | Requires request body but client sends query params |
| `POST /api/search/search-avatars` | `oasis_search_avatars` | ❌ **FAIL** | 405 | Endpoint doesn't exist in SearchController |
| `POST /api/search/search-nfts` | `oasis_search_nfts` | ❌ **FAIL** | 405 | Endpoint doesn't exist in SearchController |
| `POST /api/search/search-files` | `oasis_search_files` | ❌ **FAIL** | 405 | Endpoint doesn't exist in SearchController |
| `POST /api/data/search-holons` | `oasis_search_holons` | ❌ **FAIL** | 404 | Endpoint not found |

**Root Cause Analysis:**
1. **SearchController.cs** only has:
   - `[HttpGet("{searchParams}")]` - expects route parameter, not query string
   - No POST endpoints for search-avatars, search-nfts, search-files

2. **Client Implementation Issues:**
   - `basicSearch()` uses `GET /api/search?searchQuery=...` but controller expects `GET /api/search/{searchParams}`
   - `advancedSearch()` uses `GET /api/search/advanced` with query params, but endpoint requires request body
   - Search endpoints for avatars, NFTs, files don't exist in the API

**Recommendation:** 
- Either implement missing endpoints in SearchController, OR
- Update inventory to mark these as ❌ (not implemented)
- Fix route matching for basic search

---

### ✅ Wallet Controller

| Endpoint | MCP Tool | Status | Notes |
|----------|----------|--------|-------|
| `GET /api/wallet/supported-chains` | `oasis_get_supported_chains` | ✅ **PASS** | Returns 10 supported chains correctly |

**Result:**
```json
{
  "result": {
    "$values": [
      {"id": "ethereum", "name": "Ethereum", "symbol": "ETH", ...},
      {"id": "bitcoin", "name": "Bitcoin", "symbol": "BTC", ...},
      {"id": "solana", "name": "Solana", "symbol": "SOL", ...},
      // ... 7 more chains
    ]
  }
}
```

---

### ✅ Avatar Controller

| Endpoint | MCP Tool | Status | Notes |
|----------|----------|--------|-------|
| `GET /api/avatar/get-all-avatar-names/{includeUsernames}/{includeIds}` | `oasis_get_all_avatar_names` | ✅ **PASS** | Returns empty array (no avatars in system) |

**Result:**
```json
{
  "result": {
    "result": {
      "$values": []
    }
  }
}
```

---

### ✅ A2A Controller

| Endpoint | MCP Tool | Status | Notes |
|----------|----------|--------|-------|
| `GET /api/a2a/agents` | `oasis_get_all_agents` | ✅ **PASS** | Returns empty array (no agents registered) |
| `GET /api/a2a/agents/discover-serv` | `oasis_discover_agents_via_serv` | ✅ **PASS** | Returns empty array (no agents discovered) |
| `GET /api/a2a/agents/by-service/{service}` | `oasis_get_agents_by_service` | ✅ **PASS** | Returns empty array (no agents for service) |
| `GET /api/a2a/agent-card/{agentId}` | `oasis_get_agent_card` | ⚠️ **PARTIAL** | Returns 404 for invalid ID (expected behavior) |

**Note:** All A2A endpoints work correctly but return empty results (no agents registered in system).

---

### ❌ Karma Controller

| Endpoint | MCP Tool | Status | Error | Issue |
|----------|----------|--------|-------|-------|
| `GET /api/karma/get-positive-karma-weighting/{karmaType}` | `oasis_get_positive_karma_weighting` | ❌ **FAIL** | 400 | Invalid karma type "HelpOthers" - need to check valid karma types |

**Error:**
```json
{
  "errors": {
    "karmaType": [
      "The value 'HelpOthers' is not valid."
    ]
  }
}
```

**Recommendation:** Need to document valid karma types or update tool to accept enum values.

---

## Summary of Issues

### Critical Issues (Blocking)
1. **Search endpoints don't match implementation** - 6 endpoints failing
2. **Route parameter mismatch** - basic search uses wrong route format
3. **Missing endpoints** - search-avatars, search-nfts, search-files don't exist

### Minor Issues
1. **Karma type validation** - need to document valid enum values
2. **Advanced search** - requires request body format clarification

---

## Recommendations

### Immediate Actions
1. ✅ **Update ENDPOINT_INVENTORY.md** to mark non-existent endpoints as ❌
2. 🔧 **Fix basic search route** - either update client or controller
3. 📝 **Document valid karma types** for karma weighting endpoints
4. 🔍 **Verify advanced search** endpoint requirements

### Future Improvements
1. Implement missing search endpoints (search-avatars, search-nfts, search-files) if needed
2. Add integration tests for all MCP tools
3. Create endpoint validation script

---

## Endpoints Not Yet Tested

### Avatar Controller
- `oasis_get_avatar` (by ID, username, email)
- `oasis_get_avatar_detail`
- `oasis_get_avatar_portrait`
- `oasis_update_avatar`
- `oasis_register_avatar`
- `oasis_authenticate_avatar`

### NFT Controller
- `oasis_get_nft`
- `oasis_get_nfts`
- `oasis_mint_nft`
- `oasis_send_nft`

### Wallet Controller
- `oasis_get_wallet`
- `oasis_get_provider_wallets`
- `oasis_create_wallet`
- `oasis_send_transaction`

### Data/Holon Controller
- `oasis_get_holon`
- `oasis_save_holon`
- `oasis_update_holon`
- `oasis_delete_holon`

### Karma Controller
- `oasis_get_karma`
- `oasis_add_karma`
- `oasis_remove_karma`

### A2A Controller (Write Operations)
- `oasis_register_agent_capabilities`
- `oasis_register_agent_as_serv_service`
- `oasis_send_a2a_jsonrpc_request`

---

## Next Steps

1. **Fix Search Endpoints** - Priority 1
2. **Test Authentication Flow** - Priority 2
3. **Test Write Operations** - Priority 3
4. **Complete Endpoint Coverage** - Priority 4

---

## Test Commands Used

```bash
# Health check
mcp_oasis-unified_oasis_health_check

# Search (failing)
mcp_oasis-unified_oasis_basic_search searchQuery="test"
mcp_oasis-unified_oasis_search_avatars searchQuery="test"
mcp_oasis-unified_oasis_search_holons searchQuery="test"

# Working endpoints
mcp_oasis-unified_oasis_get_supported_chains
mcp_oasis-unified_oasis_get_all_avatar_names
mcp_oasis-unified_oasis_get_all_agents
mcp_oasis-unified_oasis_discover_agents_via_serv
```

---

**End of Test Report**









