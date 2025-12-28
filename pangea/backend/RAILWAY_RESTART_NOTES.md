# Railway Container Restarts - Notes

**Date:** December 22, 2025

---

## SIGTERM Errors

If you see logs showing:
```
Stopping Container
npm error signal SIGTERM
npm error command failed
```

This is **normal Railway behavior** when:
- Railway restarts the container (automatic restarts, health checks, etc.)
- Railway redeploys after a code push
- Railway scales resources up/down

**Not an error** - Railway sends SIGTERM to gracefully stop the container.

---

## Fix Applied

Added graceful shutdown handling in `main.ts`:
- Enable NestJS shutdown hooks
- Handle SIGTERM and SIGINT signals
- Close application gracefully before exit
- Prevents npm error messages on shutdown

---

## If Restarts Keep Happening

If the container restarts frequently (every few minutes), check:

1. **Health Check Failures**
   - Check Railway health check configuration
   - Verify `/api/health` endpoint returns 200 OK

2. **Resource Limits**
   - Check Railway resource usage (CPU, memory)
   - Verify you're not hitting limits

3. **Application Errors**
   - Check Railway logs for actual errors (not just SIGTERM)
   - Look for unhandled exceptions or crashes

4. **Database/Redis Connection Issues**
   - Check if database connections are dropping
   - Verify Redis connectivity

---

## Normal Behavior

**It's normal for Railway to:**
- Restart containers periodically
- Restart after code deployments
- Restart if health checks fail
- Restart if resource limits are approached

**As long as:**
- The application starts successfully
- Health checks pass
- No actual errors in logs (besides SIGTERM on shutdown)
- The service is available most of the time

Then everything is working correctly.

---

## Monitoring

To check if restarts are excessive:
1. Go to Railway dashboard → Your service → Metrics
2. Check restart frequency
3. Look for patterns (e.g., restarts every X minutes)

If restarts are too frequent (>1 per hour consistently), investigate the causes above.

---

**Last Updated:** December 22, 2025


