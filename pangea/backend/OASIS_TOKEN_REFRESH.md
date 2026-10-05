# OASIS API Token Auto-Refresh

The backend now automatically manages OASIS API token lifecycle, refreshing tokens before expiration to ensure continuous operation.

## How It Works

1. **Token Manager Service** (`OasisTokenManagerService`):
   - Authenticates with OASIS using admin credentials on startup
   - Stores token in Redis cache with expiration time
   - Automatically refreshes token when it's about to expire (5 minutes before expiry)
   - Provides token to OASIS services on demand

2. **Scheduled Refresh Job** (`OasisTokenRefreshJob`):
   - Runs every 10 minutes via cron job
   - Checks if token needs refreshing
   - Automatically authenticates and updates token if needed

3. **Service Integration**:
   - `OasisWalletService` uses the token manager to get fresh tokens for each API request
   - Token is injected dynamically into request headers
   - No manual intervention required

## Configuration

### Required Environment Variables

```env
# OASIS API URL (HTTP, not HTTPS)
OASIS_API_URL=http://api.oasisweb4.com

# Admin credentials for authentication
OASIS_ADMIN_USERNAME=OASIS_ADMIN
OASIS_ADMIN_PASSWORD=Uppermall1!
```

### Optional

```env
# Initial token (will be replaced by auto-refresh)
OASIS_API_KEY=<optional-initial-token>
```

## Token Lifecycle

1. **On Startup**: Service authenticates with OASIS and caches token
2. **Every 10 Minutes**: Scheduled job checks token expiration
3. **5 Minutes Before Expiry**: Token is automatically refreshed
4. **On Each API Request**: Fresh token is retrieved from cache or refreshed if needed

## Benefits

- ✅ **No Manual Intervention**: Tokens refresh automatically
- ✅ **No Service Interruption**: Token refreshed before expiration
- ✅ **Resilient**: Handles token expiration gracefully
- ✅ **Efficient**: Tokens cached in Redis to reduce API calls

## Monitoring

Check logs for token refresh activity:

```
[OasisTokenManagerService] ✅ OASIS API token refreshed successfully. Expires in 15 minutes
[OasisTokenRefreshJob] Running OASIS token refresh check...
```

## Troubleshooting

### Token Refresh Fails

If token refresh fails, check:
1. `OASIS_API_URL` is correct (should be `http://api.oasisweb4.com`)
2. `OASIS_ADMIN_USERNAME` and `OASIS_ADMIN_PASSWORD` are correct
3. Redis is running and accessible
4. OASIS API is reachable from the backend

### Service Can't Authenticate

If authentication fails:
1. Verify admin credentials are correct
2. Check OASIS API is accessible
3. Review logs for specific error messages

## Manual Token Refresh

You can force a token refresh programmatically:

```typescript
// In any service that injects OasisTokenManagerService
await this.tokenManager.forceRefresh();
```




