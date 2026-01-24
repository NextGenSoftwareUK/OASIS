# Rate Limits

## Overview

OASIS API implements rate limiting to ensure fair usage and system stability. Rate limits are applied per API key or authenticated user.

**Base URL:** `http://api.oasisweb4.com/api`

---

## Rate Limit Tiers

| Tier | Requests per Minute | Burst Limit | Monthly Requests |
|------|---------------------|-------------|------------------|
| Free | 100 | 200 | 100,000 |
| Pro | 1,000 | 2,000 | 1,000,000 |
| Enterprise | Custom | Custom | Custom |

---

## Rate Limit Headers

All API responses include rate limit headers:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1642248000
```

**Header Descriptions:**

| Header | Description |
|--------|-------------|
| `X-RateLimit-Limit` | Maximum requests allowed per window |
| `X-RateLimit-Remaining` | Remaining requests in current window |
| `X-RateLimit-Reset` | Unix timestamp when rate limit resets |

---

## Rate Limit Response

When rate limit is exceeded:

**HTTP Status:** `429 Too Many Requests`

**Response Body:**
```json
{
  "result": null,
  "isError": true,
  "message": "Rate limit exceeded. Please try again later.",
  "errorCode": "RATE_LIMIT_EXCEEDED",
  "metadata": {
    "rateLimitReset": 1642248000,
    "retryAfter": 60
  }
}
```

---

## Best Practices

### 1. Monitor Rate Limit Headers

Always check rate limit headers to avoid hitting limits:

```typescript
const response = await fetch('/api/endpoint', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const limit = response.headers.get('X-RateLimit-Limit');
const remaining = response.headers.get('X-RateLimit-Remaining');
const reset = response.headers.get('X-RateLimit-Reset');

if (parseInt(remaining) < 10) {
  // Slow down requests
  await delay(1000);
}
```

### 2. Implement Exponential Backoff

When you hit rate limits, implement exponential backoff:

```typescript
async function requestWithRetry(url, options, retries = 3) {
  for (let i = 0; i < retries; i++) {
    const response = await fetch(url, options);
    
    if (response.status === 429) {
      const retryAfter = response.headers.get('Retry-After') || Math.pow(2, i);
      await new Promise(resolve => setTimeout(resolve, retryAfter * 1000));
      continue;
    }
    
    return response;
  }
  throw new Error('Max retries exceeded');
}
```

### 3. Use Batch Operations

When possible, use batch operations to reduce request count:

```typescript
// Instead of multiple requests
for (const id of ids) {
  await fetch(`/api/nft/load-nft-by-id/${id}`);
}

// Use batch endpoint (when available)
await fetch('/api/nft/metadata/batch', {
  method: 'POST',
  body: JSON.stringify({ nftIds: ids })
});
```

### 4. Cache Responses

Cache responses when appropriate to reduce API calls:

```typescript
const cache = new Map();

async function getCachedNFT(id) {
  if (cache.has(id)) {
    return cache.get(id);
  }
  
  const response = await fetch(`/api/nft/load-nft-by-id/${id}`);
  const data = await response.json();
  
  cache.set(id, data);
  return data;
}
```

---

## Rate Limit by Endpoint

Some endpoints may have different rate limits:

| Endpoint Category | Rate Limit |
|-------------------|------------|
| Authentication | 10 requests/minute |
| NFT Operations | 50 requests/minute |
| Wallet Operations | 100 requests/minute |
| Data Operations | 200 requests/minute |
| Read Operations | 100 requests/minute |

---

## Upgrading Your Tier

To upgrade your rate limits:

1. Visit [OASIS Platform](https://oasisplatform.world)
2. Navigate to Subscription settings
3. Upgrade to Pro or Enterprise tier

---

## Related Documentation

- [Error Codes](error-codes.md) - Error handling guide
- [Authentication Guide](../getting-started/authentication.md) - Auth documentation

---

*Last Updated: January 24, 2026*
