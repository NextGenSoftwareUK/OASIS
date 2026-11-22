# MongoDB Atlas upgrades and primary changes

## What happened (18 Nov 2026)

After the shared Atlas cluster was upgraded from the free tier to Flex, the dev WEB4 API could not select a primary.
Every request that touched MongoDB waited its full deadline. The dev log showed
`A timeout occurred after 30000ms selecting a server ... ReadPreference Mode: Primary`, with two nodes connected as
secondaries and the third `Disconnected / ReportedPrimaryIsStale`. Atlas itself looked idle because no reads reached it.
`/api/health` stayed fast because it does not touch MongoDB. A restart of the service fixed it.

## Runbook

1. After an Atlas tier change, maintenance, failover or any primary change, restart (not rebuild) every OASIS service
   that uses the cluster: WEB4 to WEB10 in each environment (Railway: restart the active deployment).
2. Verify with one made-up login: `POST /api/avatar/authenticate` should answer in well under a second. A response
   that takes exactly the login deadline (`StorageProviders.AvatarLoginTimeoutSeconds`, default 10 s) means MongoDB
   calls are timing out.
3. Look in the service log for `selecting a server` and the `RequestTimingMiddleware` stage
   (`authStage=LoadingUsername ... provider=MongoDBOASIS`).
4. In Atlas check which node is PRIMARY, any in-progress maintenance, and Network Access (the allowlist or peering must
   still allow the Railway egress).

## Code hardening

`MongoDbContext` now sets `ServerSelectionTimeout` and `ConnectTimeout` to 10 s and recycles pooled connections
(`MaxConnectionIdleTime` 5 min, `MaxConnectionLifeTime` 30 min). The driver re-discovers topology by heartbeat on its
own, so this is defence in depth and faster failure, not a proven fix for the stale view seen on 18 Nov: whether the
driver could have recovered without a restart was not established. Other MongoClient instances (the subscription usage
repository and the native import client) still use driver defaults.

