# Manual Tweet Refresh Strategy (Free Tier)

Since Free tier only allows 1 search per 24 hours, here's how to make the most of it:

## Daily Workflow

### Once Per Day (Best Time: Evening)

1. **Stop the backend** (if running):
   ```bash
   lsof -ti:3002 | xargs kill -9
   ```

2. **Restart the backend** to clear the 24-hour timer:
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN/BillionsHealed/backend
   npm start &
   ```

3. **Manually trigger a tweet fetch**:
   ```bash
   curl -X POST http://localhost:3002/api/twitter/initialize \
     -H "Content-Type: application/json" \
     -d '{"bearerToken": "AAAAAAAAAAAAAAAAAAAAALpN4wEAAAAAkpF8OpifhMmGmkFyip4whzmM5Qk%3DEDPTLhrrIEwfDVnCRv4UPOsULGJOWC8NBrvxWMulDXPvKEzvFc", "hashtag": "#billionshealed"}'
   ```

4. **Refresh the frontend** to see the latest tweets

## Benefits of Manual Approach

✅ **Free** - No monthly API costs  
✅ **Controlled** - You choose when to update  
✅ **Strategic** - Update when you know new tweets are posted  
✅ **Reliable** - Works within Free tier limits  

## When to Upgrade

Consider upgrading to **Basic tier ($200/month)** when:
- Getting 20+ #billionshealed tweets per day
- Need real-time monitoring
- Want automatic updates
- Have budget for it

## Alternative: Semi-Manual Updates

Create a simple admin button on the frontend that triggers a refresh once per day.


