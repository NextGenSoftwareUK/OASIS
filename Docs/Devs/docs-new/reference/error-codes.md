# Error Codes Reference

## Overview

This document provides a comprehensive reference for all error codes returned by the OASIS API.

**Base URL:** `http://api.oasisweb4.com/api`

---

## Standard Error Response Format

All errors follow this standard format:

```json
{
  "result": null,
  "isError": true,
  "message": "Human-readable error message",
  "errorCode": "ERROR_CODE",
  "errors": [
    {
      "field": "fieldName",
      "message": "Field-specific error message"
    }
  ],
  "metadata": {
    "requestId": "req-123",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

---

## HTTP Status Codes

| Status Code | Meaning | Description |
|-------------|---------|-------------|
| 200 | OK | Request succeeded (may still have `isError: true` in body) |
| 400 | Bad Request | Invalid request data or validation failed |
| 401 | Unauthorized | Missing or invalid authentication |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |

**Note:** OASIS API may return HTTP 200 even for errors, with `isError: true` in the response body. Always check the `isError` field.

---

## Authentication Errors

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| UNAUTHORIZED | 401 | Missing or invalid JWT token |
| INVALID_CREDENTIALS | 401 | Username/password incorrect |
| EMAIL_NOT_VERIFIED | 401 | Email not verified |
| TOKEN_EXPIRED | 401 | JWT token expired |
| TOKEN_INVALID | 401 | JWT token is invalid |
| REFRESH_TOKEN_INVALID | 401 | Refresh token is invalid |

**Example:**
```json
{
  "result": null,
  "isError": true,
  "message": "Unauthorized. Try Logging In First With api/avatar/authenticate REST API Route.",
  "errorCode": "UNAUTHORIZED"
}
```

---

## Validation Errors

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| VALIDATION_ERROR | 400 | Request validation failed |
| INVALID_PARAMETER | 400 | Invalid parameter value |
| MISSING_REQUIRED_FIELD | 400 | Required field is missing |
| INVALID_EMAIL | 400 | Email format is invalid |
| INVALID_GUID | 400 | GUID format is invalid |
| INVALID_WALLET_ADDRESS | 400 | Wallet address format is invalid |

**Example:**
```json
{
  "result": null,
  "isError": true,
  "message": "Validation failed",
  "errorCode": "VALIDATION_ERROR",
  "errors": [
    {
      "field": "email",
      "message": "Email is required"
    },
    {
      "field": "password",
      "message": "Password must be at least 8 characters"
    }
  ]
}
```

---

## Resource Errors

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| NOT_FOUND | 404 | Resource not found |
| AVATAR_NOT_FOUND | 404 | Avatar does not exist |
| NFT_NOT_FOUND | 404 | NFT does not exist |
| WALLET_NOT_FOUND | 404 | Wallet does not exist |
| COLLECTION_NOT_FOUND | 404 | Collection does not exist |

**Example:**
```json
{
  "result": null,
  "isError": true,
  "message": "Avatar not found",
  "errorCode": "AVATAR_NOT_FOUND"
}
```

---

## Permission Errors

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| FORBIDDEN | 403 | Insufficient permissions |
| ACCESS_DENIED | 403 | Access denied to resource |
| WIZARD_ONLY | 403 | Wizard/Admin access required |

**Example:**
```json
{
  "result": null,
  "isError": true,
  "message": "Unauthorized. Only Wizards can access this endpoint.",
  "errorCode": "WIZARD_ONLY"
}
```

---

## NFT-Specific Errors

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| INVALID_NFT_ID | 400 | NFT ID is invalid |
| MINT_FAILED | 500 | NFT minting failed |
| TRANSFER_FAILED | 500 | NFT transfer failed |
| INVALID_PROVIDER | 400 | Provider not supported |
| INVALID_NFT_STANDARD | 400 | NFT standard not supported |

**Example:**
```json
{
  "result": null,
  "isError": true,
  "message": "The OnChainProvider is not a valid OASIS NFT Provider.",
  "errorCode": "INVALID_PROVIDER"
}
```

---

## Wallet Errors

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| WALLET_NOT_FOUND | 404 | Wallet does not exist |
| INSUFFICIENT_BALANCE | 400 | Insufficient balance |
| INVALID_WALLET_ADDRESS | 400 | Wallet address format invalid |
| TRANSACTION_FAILED | 500 | Transaction failed |

---

## Rate Limiting Errors

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| RATE_LIMIT_EXCEEDED | 429 | Too many requests |

**Response Headers:**
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1642248000
```

**Example:**
```json
{
  "result": null,
  "isError": true,
  "message": "Rate limit exceeded. Please try again later.",
  "errorCode": "RATE_LIMIT_EXCEEDED"
}
```

---

## Provider Errors

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| PROVIDER_NOT_AVAILABLE | 500 | Provider is not available |
| PROVIDER_ERROR | 500 | Provider returned an error |
| PROVIDER_TIMEOUT | 500 | Provider request timed out |

---

## Server Errors

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| INTERNAL_ERROR | 500 | Internal server error |
| SERVICE_UNAVAILABLE | 503 | Service temporarily unavailable |

---

## Best Practices

### Error Handling

1. **Always check `isError` flag** - Don't rely solely on HTTP status codes
2. **Check error code** - Use `errorCode` for programmatic handling
3. **Read error message** - `message` provides human-readable details
4. **Handle validation errors** - Check `errors` array for field-specific issues
5. **Implement retry logic** - For rate limits and temporary errors

### Example Error Handling

```typescript
async function handleAPIResponse(response: any) {
  if (response.isError) {
    switch (response.errorCode) {
      case 'UNAUTHORIZED':
        // Redirect to login
        await redirectToLogin();
        break;
      case 'RATE_LIMIT_EXCEEDED':
        // Wait and retry
        await waitAndRetry();
        break;
      case 'VALIDATION_ERROR':
        // Show field errors
        showValidationErrors(response.errors);
        break;
      default:
        // Show generic error
        showError(response.message);
    }
    throw new Error(response.message);
  }
  return response.result;
}
```

---

## Related Documentation

- [Rate Limits](rate-limits.md) - Rate limiting information
- [API Reference](../reference/api-reference/web4-complete-reference.md) - Complete API reference

---

*Last Updated: January 24, 2026*
