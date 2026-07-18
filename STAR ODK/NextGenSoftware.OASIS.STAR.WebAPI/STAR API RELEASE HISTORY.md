
# WEB5 STAR API RELEASE HISTORY

This needs to be updated whenever we do any work that will affect the WEB5 STAR API.
This file is linked to the Swagger documentation for the WEB5 STAR API.

----------------------------------------------------------------------------------------------------------------------------
## 1.0.0 (24/09/25)

Initial release of the WEB5 STAR API — the REST API surface for the STAR ODK (Software Development Kit).

- Added WEB5 STAR API WebAPI project with controllers for Celestial Bodies, Spaces, Missions, Quests, Libraries, Templates and more.
- Built on top of the WEB4 OASIS API foundation (identity, karma, COSMIC ORM, OASIS HyperDrive, 50+ providers).
- JWT authentication middleware matching the WEB4 OASIS API pattern.
- Full Swagger/OpenAPI documentation.
- Unit tests and integration tests added.
- STAR Web UI updated to work with the new API.

----------------------------------------------------------------------------------------------------------------------------
## 1.1.0 (26/10/25)

- STAR CLI improvements: deploy and manage the WEB4 OASIS API and WEB5 STAR API servers from within the CLI.
- Railway cloud deployment support for the STAR API.
- Provider improvements across multiple OASIS Providers.
- NFT API bug fixes and improvements.
- Improved error handling throughout.

----------------------------------------------------------------------------------------------------------------------------
## 1.2.0 (22/12/25)

- Added new OASIS Providers to the STAR API: StarknetOASIS, AztecOASIS, MidenOASIS, ZcashOASIS, RadixOASIS, TelegramOASIS & MonadOASIS.
- Full web3 NFT support added to STAR CLI: edit, search, list; ability to start/stop WEB4 OASIS API and WEB5 STAR API servers from STAR CLI.
- Improved STARNET interop library system with support for more languages.
- Multiple bug fixes for NFTs in STAR CLI.
- Various provider bug fixes.

----------------------------------------------------------------------------------------------------------------------------
## 1.3.0 (04/04/26)

- **OASIS Omniverse integration complete**: ODOOM (Doom engine) and OQUAKE (Quake engine) fully integrated with the OASIS Avatar & inventory system via the WEB5 STAR API.
- **STARAPIClient**: native C shared library (star_api.so/dll) allowing games and apps on any platform/language to call the WEB4/WEB5 APIs without needing .NET. Cross-platform scripts for Linux/macOS/Windows added.
- **Quest System/API**: completely rewritten to support cross-game dynamic objectives, an objective/quest builder system, sub-quests, pre-requisites and progress tracking. ODOOM and OQUAKE both show a quest popup and HUD tracker; active quests and objectives persist correctly across sessions.
- **GeoHotSpot API upgraded**: now supports Text, Image, Video & Weblink hotspot types in addition to GeoNFTs, forming the foundation of the cross-game OGEngine (WEB4 + WEB5 + STARAPIClient).
- Cross-game inventory, weapons, powerups and XP — items picked up in ODOOM/OQUAKE are stored in your OASIS inventory and can be used in other games.
- New XP endpoint on WEB4 OASIS API; ODOOM and OQUAKE display XP in real time.
- Upgraded to .NET 10.
- OASIS HyperDrive v2 settings added to default OASISDNA.
- Comprehensive Quest System documentation added.
- Various performance improvements and bug fixes.

----------------------------------------------------------------------------------------------------------------------------
## 2.0.0 (17/07/26)

- **ONET/ONODE system** (Phases 1–7): P2P ONET bootstrap/registration; ONODEService supervisor; Avalonia tray app with Metrics/Network/Audit tabs; ONODE Manager CLI extensions; Web4 Holon bridge endpoints; SQLite metrics history; WebSocket push; rate limiting; audit log; active-nodes endpoint; provider management; GitHub Actions release workflow; comprehensive unit and integration test coverage.
- **Subscription system**: built from scratch; persists via HolonManager; Stripe keys from env vars with OASISDNA fallback.
- **Wizard account overrides**: Wizards can pass MintedByAvatarId to mint NFTs on behalf of other avatars; VerificationToken returned in register response; SuppressVerificationEmail per-request.
- **DID SSO & 3-layer encryption**: 3-layer password encryption (BCrypt + AES256 + Rijndael); full DID SSO support; DID challenge nonce endpoint; pluggable nonce store (InMemory + Redis).
- **Security fix**: BCrypt password hashing applied in all update-by-id/email/username endpoints.
- **WEB6-WEB10 APIs launched**: 250+ MCP tools across WEB6–WEB10; full WEB6 AI layer (FAHRN, SSE streaming, embeddings, HTTP MCP transport, A2A, DID/VCs); WEB7 Symbiotic layer; WEB8 Inter-Galactic mesh; WEB9 Singularity aggregator; WEB10 Source layer.
- NFT collection support (CollectionPublicKey); updating existing NFTs now supported; CollectNFT & CollectGeoNFT added.
- New OASIS providers: AptosOASIS, ElrondOASIS, NEAROASIS, Leela AI (WEB6).
- Azure Container deployment fixes and Docker build improvements.
- Various performance improvements, bug fixes and misc improvements.

Full Changelog: https://github.com/NextGenSoftwareUK/OASIS/compare/OASIS-Runtime-v4.6.0...OASIS-Runtime-v5.0.0
