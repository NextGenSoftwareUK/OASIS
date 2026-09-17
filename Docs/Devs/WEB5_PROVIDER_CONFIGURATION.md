# WEB5 provider configuration

WEB5 quest storage must list only providers that the deployed STAR API actually registers.

The development STAR deployment stores quests in `MongoDBOASIS`. Its `OASIS_DNA.json` therefore disables automatic load balancing and uses `MongoDBOASIS` as the sole replication, load-balance, and failover provider. Listing an unavailable provider such as `ArbitrumOASIS` violates the runtime provider contract: a normal quest read can select it and fail before the request reaches MongoDB.

`OASIS_DNA_JSON`, when set in Railway, is the runtime authority. Keep its `StorageProviders` values aligned with `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/OASIS_DNA.json`; the Docker entrypoint deliberately writes that environment value to `/app/OASIS_DNA.json`.

After deploying WEB5, authenticate a development avatar and verify `GET /api/quests/all-for-avatar/game` returns an OASIS success response. This is the required gate before running `Scripts/seed_our_world_tree_quest.ps1`.