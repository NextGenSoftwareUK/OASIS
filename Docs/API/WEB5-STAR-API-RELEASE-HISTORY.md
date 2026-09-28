# WEB5 STAR API RELEASE HISTORY

This document records every public WEB5 STAR API version in chronological order, oldest to newest.
It is the source history synchronized to the public OASIS repository and linked by WEB5 Swagger.

----------------------------------------------------------------------------------------------------------------------------
## 1.0.0 (24/09/25)

Initial release of the WEB5 STAR API — the REST API surface for the STAR ODK (Software Development Kit).

- Added WEB5 STAR API WebAPI project with controllers for Celestial Bodies, Spaces, Missions, Quests, Libraries, Templates and more.
- Built on top of the WEB4 OASIS API foundation (identity, karma, COSMIC ORM, OASIS HyperDrive, 100+ providers).
- JWT authentication middleware matching the WEB4 OASIS API pattern.
- Full Swagger/OpenAPI documentation.
- Unit tests and integration tests added.
- STAR Web UI updated to work with the new API.

----------------------------------------------------------------------------------------------------------------------------
## 1.0.1 (18/09/26)

- Added the quest `objectiveCompletionOrder` contract (`AnyOrder` default and `InOrder`) to create/load/game DTOs and authoritative WEB5 progress handling.
- Any-order completion no longer advances or replays another objective's activation events. In-order completion activates the next objective normally.
- Updated the Postman generator with a contract-aware any-order quest-create example.

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
## 1.4.0 (17/07/26)

- **ONET/ONODE system** (Phases 1–7): P2P ONET bootstrap/registration; ONODEService supervisor; Avalonia tray app with Metrics/Network/Audit tabs; ONODE Manager CLI extensions; Web4 Holon bridge endpoints; SQLite metrics history; WebSocket push; rate limiting; audit log; active-nodes endpoint; provider management; GitHub Actions release workflow; comprehensive unit and integration test coverage.
- **Subscription system**: built from scratch; persists via HolonManager; Stripe keys from env vars with OASISDNA fallback.
- **Wizard account overrides**: Wizards can pass MintedByAvatarId to mint NFTs on behalf of other avatars; VerificationToken returned in register response; SuppressVerificationEmail per-request.
- **DID SSO & 3-layer encryption**: 3-layer password encryption (BCrypt + AES256 + Rijndael); full DID SSO support; DID challenge nonce endpoint; pluggable nonce store (InMemory + Redis).
- **Security fix**: BCrypt password hashing applied in all update-by-id/email/username endpoints.
- **WEB6-WEB10 APIs launched**: 516 MCP tools across WEB6–WEB10; full WEB6 AI layer (FAHRN, SSE streaming, embeddings, HTTP MCP transport, A2A, DID/VCs); WEB7 Symbiotic layer; WEB8 Inter-Galactic mesh; WEB9 Singularity aggregator; WEB10 Source layer.
- NFT collection support (CollectionPublicKey); updating existing NFTs now supported; CollectNFT & CollectGeoNFT added.
- New OASIS providers: AptosOASIS, ElrondOASIS, NEAROASIS, Leela AI (WEB6).
- Azure Container deployment fixes and Docker build improvements.
- Various performance improvements, bug fixes and misc improvements.

Full Changelog: https://github.com/NextGenSoftwareUK/OASIS/compare/OASIS-Runtime-v4.6.0...OASIS-Runtime-v5.0.0

----------------------------------------------------------------------------------------------------------------------------
## 2.0.0 (20/07/26)

Major WEB5 release aligning STAR with the expanded production OASIS platform.

### What's new in 2.0.0

- Expanded the provider graph with Arweave permanent storage, Lens and Urbit social integrations, Stellar, Azure Storage, SQL Server, Oracle and the accompanying API registration, capabilities, Swagger and documentation updates.
- Replaced remaining provider placeholders with real network, storage, database and Web3 implementations and repaired provider contracts, activation and runtime integration.
- Hardened avatar registration and account management: Wizard assignment protection, non-fatal verification-email delivery, wallet and verification handling, authenticated routes, password hashing and clearer boot/provider diagnostics.
- Corrected NFT and GeoNFT mutability, metadata-freezing, collection, inventory and controller behavior across WEB4 and STAR consumers.
- Improved subscription persistence and API behavior and fixed production routing, controller discovery, Swagger generation and deployment configuration.
- Split STAR ODK, WEB6, API.Core, ONODE.Core, OGEngineClient and ONODE services into independently versioned private repositories, with exact parent gitlinks and deployment dependency pins.
- Added and repaired tests, build checks, Docker paths and CI validation for the expanded source and deployment graph.

### Full changelog

This inventory is generated from every distinct non-merge commit touching STAR ODK or its WEB4, provider, ONET and Edge dependencies between the 2.0.0 and 3.0.0 version points.

<details>
<summary>Complete commit inventory (244 distinct changes)</summary>

- [a5657b3](https://github.com/NextGenSoftwareUK/OASIS/commit/a5657b3bd8eab2f307b69152c016c2b286ec4c08) bump STAR API to v3.0.0
- [4bdabd4](https://github.com/NextGenSoftwareUK/OASIS/commit/4bdabd43b12b822e99fee055d69aa5e3899bef86) chore: update STAR ODK and WEB6 submodule pointers — source now pushed to private repos
- [9c5de57](https://github.com/NextGenSoftwareUK/OASIS/commit/9c5de579889b018d4397bfb0e4d8fd933a8265e6) refactor: replace OGEngineClient and ONODE Manager/Client/Service with private submodules
- [41d9745](https://github.com/NextGenSoftwareUK/OASIS/commit/41d9745c8a135dc8a3aec3d81c0dee00a49e17b7) feat: add StellarOASIS, AzureStorageOASIS, SQLServerDBOASIS, OracleDBOASIS providers
- [c257957](https://github.com/NextGenSoftwareUK/OASIS/commit/c25795700ac7ab4cc790ff6b36a6c2cc7ae0216b) feat: add UrbitOASIS provider; update LensOASIS/UrbitOASIS in solution, swagger, and docs
- [87760f8](https://github.com/NextGenSoftwareUK/OASIS/commit/87760f8f08797dfd2530ff06cf244cd16faf0495) feat: add LensOASIS provider — Lens Protocol v2 decentralised social graph
- [b7e99dc](https://github.com/NextGenSoftwareUK/OASIS/commit/b7e99dc81c9ad222f22d6f6d0c505c2b1527f46f) feat: API.Core and ONODE.Core moved to private submodules; conditional ProjectReference/PackageReference applied to 100 provider projects
- [54d145d](https://github.com/NextGenSoftwareUK/OASIS/commit/54d145d92c9cd7a0a8dcf1d9fa5e0a677277e475) chore: remove private projects (API.Core, ONODE.Core) — moved to private repos
- [64614ae](https://github.com/NextGenSoftwareUK/OASIS/commit/64614ae2a733b29be55938d3be5513c8b6b1e5f9) docs: update all provider lists to match full ProviderType enum — add Arweave, fix TONSOASIS typo, add 25 missing providers to swagger and web4 site
- [a027f14](https://github.com/NextGenSoftwareUK/OASIS/commit/a027f149960b3cf3e9386a15f9b6a2bfbbb07e5e) feat: add public solution and conditional NuGet fallbacks for private submodules
- [0958b80](https://github.com/NextGenSoftwareUK/OASIS/commit/0958b800e3967f55ed8a5e85b33506d90b2dd42e) chore: add STAR ODK and WEB6 as private git submodules
- [e4d313d](https://github.com/NextGenSoftwareUK/OASIS/commit/e4d313d1995e0352aeaccfea9eb3651ad82c8b49) chore: remove STAR ODK and WEB6 — moving to private repos as submodules
- [8828883](https://github.com/NextGenSoftwareUK/OASIS/commit/8828883363a27952f26c7cc55d4c247a9d5f3205) feat: add ArweaveOASIS to WebAPI provider switch, swagger docs, and MCP tool description
- [d40f5c2](https://github.com/NextGenSoftwareUK/OASIS/commit/d40f5c2d9b36b0aac947bbada2d62c4b10b50995) feat: add ArweaveOASIS permanent storage provider
- [0354fe7](https://github.com/NextGenSoftwareUK/OASIS/commit/0354fe7e6959a543bbb6ffb6751ab76844347b04) make isMutable honour FreezeMetadata flag instead of hardcoding false
- [49569f9](https://github.com/NextGenSoftwareUK/OASIS/commit/49569f90bbcbc5e9ed36903cfd722ff34bf6b349) fix: try/finally for IsAutoFailOverEnabled restore + boot/DNA/provider diagnostics in register response
- [16133c4](https://github.com/NextGenSoftwareUK/OASIS/commit/16133c4644923a1c16fa40b1dad84166271c1e54) security: prevent unauthenticated callers from self-assigning Wizard avatar type on registration
- [4e2cd0e](https://github.com/NextGenSoftwareUK/OASIS/commit/4e2cd0e01a75f7176286c0a63ebe6591b4ea29b5) Update AvatarManager-Reset.cs
- [6d60008](https://github.com/NextGenSoftwareUK/OASIS/commit/6d60008a516c0c6180a599c7fdbfcba89d6ffd4e) fix: make verification email failure non-fatal during avatar registration
- [62e2db5](https://github.com/NextGenSoftwareUK/OASIS/commit/62e2db5ff4d0845d7c845611cee86e963fdbe235) FIX
- [955888b](https://github.com/NextGenSoftwareUK/OASIS/commit/955888b2208e4eae93c7dbfd2044166c97f6df14) fix: add missing NextGenSoftware.Logging using in AvatarManager-Private.cs
- [f4ade68](https://github.com/NextGenSoftwareUK/OASIS/commit/f4ade68eec95b94ed107b392447a9ccbf2fc43f4) fix: make wallet creation errors non-fatal in PrepareToRegisterAvatarAsync
- [0c7efca](https://github.com/NextGenSoftwareUK/OASIS/commit/0c7efca1503392bd2661df47057ffac5144ef97c) fix: wrap refresh token pruning in try/catch so null OASISDNA can never crash SaveAvatarAsync
- [cd9180a](https://github.com/NextGenSoftwareUK/OASIS/commit/cd9180aa91988dbec45f8787263738b302de03b7) fix: null-safe OASISDNA access in SaveAvatarAsync/SaveAvatar to prevent NullReferenceException on line 78/176
- [b9c513b](https://github.com/NextGenSoftwareUK/OASIS/commit/b9c513bb73f8fcb2e9bed22a8a731ad79e42fd12) fix: require auth on checkout endpoint; always persist free plan subscription
- [a613e52](https://github.com/NextGenSoftwareUK/OASIS/commit/a613e52db3ec5993790b0272a286ece435e9b245) fix: sort Swagger UI tags alphabetically so AvatarProfile and Keys appear in correct position
- [137ca43](https://github.com/NextGenSoftwareUK/OASIS/commit/137ca43bedf7e6c5ff42d62dfcc5ae7420b743b4) fix: expose Price and Amount on PlanDto so portal SDK can read plan prices
- [abb4bbb](https://github.com/NextGenSoftwareUK/OASIS/commit/abb4bbbb6a01ba24d767e478a98bc782c8fe1fb2) fix: guard IOASISLocalStorageProvider casts in WalletManager — non-local providers (e.g. MongoDBOASIS) skip wallet persistence gracefully
- [83ec843](https://github.com/NextGenSoftwareUK/OASIS/commit/83ec843038567138c9dce8ac7e5931f80ec6215a) fix: DateTime cannot use ?? operator — use conditional instead
- [c56e835](https://github.com/NextGenSoftwareUK/OASIS/commit/c56e835a067e60040e8fa25999f826555fc74330) fix: merge AvatarProfileController and KeysController partial classes into single files — partial classes were preventing endpoints from appearing in Swagger on Railway
- [830d4f1](https://github.com/NextGenSoftwareUK/OASIS/commit/830d4f1b346be41dc1b3dc7567dd4bf319945be4) fix: add Produces attribute to AvatarProfileController; remove dangling doc comment
- [61c3c1b](https://github.com/NextGenSoftwareUK/OASIS/commit/61c3c1b787c0cf6ed9ab3643e3d4af78ebc52559) feat: add AvatarKeyController with provider key management endpoints
- [41b7277](https://github.com/NextGenSoftwareUK/OASIS/commit/41b7277b67461e92d22bc8e304afeab0546fa76b) fixes
- [db7eaa7](https://github.com/NextGenSoftwareUK/OASIS/commit/db7eaa7c4e5d181c758dc42303207886a9f50459) refactor: move inventory endpoints from AvatarAdminController to AvatarProfileController
- [133d9ad](https://github.com/NextGenSoftwareUK/OASIS/commit/133d9ad1b5df010e0cdee9b51df987ccc493aa43) fix: add [Route] and [ApiController] to AvatarProfileController - was missing, causing Swagger 500
- [394a004](https://github.com/NextGenSoftwareUK/OASIS/commit/394a004bf5cb42a54f0b0e0086f78b7cbcd8c2da) fix: resolve Swagger 500 - add ResolveConflictingActions, use fully-qualified CustomSchemaIds
- [0197885](https://github.com/NextGenSoftwareUK/OASIS/commit/0197885305075837c49daba306696f533b368077) feat: add 5 new providers — Arweave, Abstract, Berachain, Farcaster, Nostr
- [35e88e6](https://github.com/NextGenSoftwareUK/OASIS/commit/35e88e6945571a593944b4de135c80491b558f16) fix: correct Stacks transaction signing in BlockStackOASIS
- [aa101c6](https://github.com/NextGenSoftwareUK/OASIS/commit/aa101c645a0915e522659feef360cbc141febb29) feat: implement real storage and API calls in 4 remaining stub providers
- [d9bf036](https://github.com/NextGenSoftwareUK/OASIS/commit/d9bf036a49d994b85ac63f253d2472c4e6348a3e) fix: replace placeholder/hardcoded stubs with real implementations across 6 providers
- [0c30894](https://github.com/NextGenSoftwareUK/OASIS/commit/0c308942b427030e193436814fc38f6c6e865a20) fix: replace placeholder methods with real implementations in PolkadotOASIS, SuiOASIS, and MoralisOASIS
- [b307e1e](https://github.com/NextGenSoftwareUK/OASIS/commit/b307e1ecda0c8bd441e16a231b6e8795616ff7e2) fix: use sync KeyManager.GetProviderPublicKeysForAvatarById in OptimismOASIS UnlockTokenAsync
- [5f31bb3](https://github.com/NextGenSoftwareUK/OASIS/commit/5f31bb30fdd1cc2b8fc47acc471bacfb53098c34) fix: AND-mode search, TelosOASIS error handling, and misleading TODO comments
- [405e52c](https://github.com/NextGenSoftwareUK/OASIS/commit/405e52c63e391c6215020045e12485a0a889485a) fix: resolve provider stubs and logic gaps found in audit
- [3f2427e](https://github.com/NextGenSoftwareUK/OASIS/commit/3f2427ef87ba1b56e66288cb74152d877e7a8860) fix: replace remaining null-returning stubs with real implementations
- [47cff2f](https://github.com/NextGenSoftwareUK/OASIS/commit/47cff2fe9d74fce76d3cfd0b553ac8bfa6541266) fix: replace stubs/placeholders with real SDK implementations across providers
- [b884682](https://github.com/NextGenSoftwareUK/OASIS/commit/b8846824fab045b6e19c67f4df6c6ca30ed50fdf) Revert "feat: enable SubscriptionMiddleware in WEB4 API (removes TODO comment, goes live)"
- [84815dc](https://github.com/NextGenSoftwareUK/OASIS/commit/84815dc62130812f379b175aca83b0daf14ef064) feat: enable SubscriptionMiddleware in WEB4 API (removes TODO comment, goes live)
- [85ef0b8](https://github.com/NextGenSoftwareUK/OASIS/commit/85ef0b873ec4095c39b3fc47d36082c0c3f0aab1) refactor: split TelegramBotService (2010L) into 6 partial class files
- [7a3a528](https://github.com/NextGenSoftwareUK/OASIS/commit/7a3a528d5e4dffce911a1a063206084670217bd9) fix: correct NextGenSoftware-Libraries relative path in ScuttlebuttOASIS and SOLIDOASIS csproj
- [8845a00](https://github.com/NextGenSoftwareUK/OASIS/commit/8845a00073939bb616fb94ac9089ee8d3ab414a8) fixed build
- [4194ff1](https://github.com/NextGenSoftwareUK/OASIS/commit/4194ff139c58ad50f539b4f1ac43a7c6fd0c4ac1) fix: remove duplicate methods after master/Development merge
- [250edf4](https://github.com/NextGenSoftwareUK/OASIS/commit/250edf4b25eae8ca5f9ee308e5886517b6df5e5d) removed redudant files
- [30a60f3](https://github.com/NextGenSoftwareUK/OASIS/commit/30a60f3e4be84a637d9eda9d5d432d16dfc5cbc5) refactor: split large files into partial classes; fix duplicate methods from file splitting
- [a38629e](https://github.com/NextGenSoftwareUK/OASIS/commit/a38629ed937f3eb592da6a1f82299c593ecb526e) fix: point Web4OasisApiBaseUrl to prod API instead of localhost:7777
- [702b4ad](https://github.com/NextGenSoftwareUK/OASIS/commit/702b4adebba31a922ece070767a26cfcba3e6f76) fix: point Web4OasisApiBaseUrl to dev API instead of localhost:7777
- [7b07d42](https://github.com/NextGenSoftwareUK/OASIS/commit/7b07d42e0e35c1eb358a5495b227441a63040b26) feat: implement ScuttlebuttOASIS, SOLIDOASIS, TelegramOASIS providers and TimoRides services
- [0e32949](https://github.com/NextGenSoftwareUK/OASIS/commit/0e32949bd38d5ed37a9df3cd6f1fffd694c8a9e1) security: remove hardcoded OpenAI API key from TelegramBotService.cs comments
- [3f4da6e](https://github.com/NextGenSoftwareUK/OASIS/commit/3f4da6e452e226d8e8d4f9f0def298a7725a73de) refactor: extract light/wiz/game cases from ReadyPlayerOne into partial class handlers (1070L -> 684L)
- [d86795d](https://github.com/NextGenSoftwareUK/OASIS/commit/d86795dea2bcc4011e57c28298745d5f536400df) fix: restore missing CLI partial class methods lost in prior splits
- [b995b48](https://github.com/NextGenSoftwareUK/OASIS/commit/b995b48301d044ddc7f130a76635872135dd951b) refactor: split ArbitrumOASIS.HolonSave.cs (1116L), Mutation.cs (1088L), Quests.cs (1003L) into partial class files
- [14e9ac6](https://github.com/NextGenSoftwareUK/OASIS/commit/14e9ac667724caca00c1f829917925e0fdab510c) refactor: split Star.Extract.cs (1011L) into 3 partial class files
- [823a74f](https://github.com/NextGenSoftwareUK/OASIS/commit/823a74f8f68745a7d9e8fd6b4ed889b9bf8855e9) refactor: split ArbitrumOASIS.AvatarLoad.cs (1053L) and ArbitrumOASIS.Token.cs (1039L) into partial class files
- [9336de7](https://github.com/NextGenSoftwareUK/OASIS/commit/9336de70f24ce0d85a18ab21167fd73648bad7b4) refactor: split GreatGrandSuperStarCore.cs (1074L) and COSMICManager.GetRelated2.cs (1065L) into partial class files
- [054c46d](https://github.com/NextGenSoftwareUK/OASIS/commit/054c46d56806579b0c80c92c94a6c76d296cf4c4) refactor: split STARNETUIBase.ListMore.cs (1088L), STARNETUIBase.List.cs (1046L), STARNETUIBase.Install.cs (1018L) into partial class files
- [ee17e91](https://github.com/NextGenSoftwareUK/OASIS/commit/ee17e9176d5d0fc01ad434a0f89b310279bd631a) refactor: split Holon.cs (1102L) into 3 partial class files
- [cf72a74](https://github.com/NextGenSoftwareUK/OASIS/commit/cf72a742036842500640d2cb296e50dc13abaca7) refactor: split CosmicController.GetMore2.cs (1108L) and CosmicController.Get.cs (1101L) into partial class files
- [a74fdc9](https://github.com/NextGenSoftwareUK/OASIS/commit/a74fdc9b526f306d5ea41af27b2b66135c79e3bf) refactor: split CelestialBodyCore.cs (1109L) into 3 partial class files
- [c2c8c31](https://github.com/NextGenSoftwareUK/OASIS/commit/c2c8c3128dc3402dea114c444b63112c6e78403e) refactor: split COSMICManager.GetRelated.cs (1125L) into 3 partial class files
- [610b8e5](https://github.com/NextGenSoftwareUK/OASIS/commit/610b8e52bf176bbc0bf7b459a44ce06096958864) refactor: split ONETSecurity.cs (1114L) into 2 partial class files
- [54d7c41](https://github.com/NextGenSoftwareUK/OASIS/commit/54d7c414f95c67c744a8e48e9257ea82626fce73) refactor: split Avatars.cs (1133L) into 3 partial class files
- [d90f71e](https://github.com/NextGenSoftwareUK/OASIS/commit/d90f71ec77f364e15c24c3cdf0ede39a6d6a53c8) refactor: split HolonRepository.cs (1134L) into 3 partial class files
- [647fb61](https://github.com/NextGenSoftwareUK/OASIS/commit/647fb610bbf045f9b72d2c066f4e841128d57aea) refactor: split Web3CoreOASISBaseProviderHelper.cs (1141L) into 2 partial class files
- [bb0b008](https://github.com/NextGenSoftwareUK/OASIS/commit/bb0b00822a1485f3e42b48ddb85fa740723765f3) refactor: split COSMICManager.Get.cs (1149L) into 3 partial class files
- [30f9460](https://github.com/NextGenSoftwareUK/OASIS/commit/30f94603241104de4c4836007ae25d555bc8063b) refactor: split MongoDBOASIS.cs (1202L) into 3 partial class files
- [29628d0](https://github.com/NextGenSoftwareUK/OASIS/commit/29628d0b711592cb4b33048e0d320502f4063db3) refactor: split CosmicController.GetMore.cs (1212L) into 3 partial class files
- [ac2a457](https://github.com/NextGenSoftwareUK/OASIS/commit/ac2a457dcd0dde7974647358a42bc2df1ccd0c98) refactor: split COSMICManager.Update.cs (1214 lines) into 4 partial class files
- [98e76e6](https://github.com/NextGenSoftwareUK/OASIS/commit/98e76e619bf2f5fbed6e16e95a07e43183a281b5) refactor: split STARNETManagerBase.Uninstall.cs (1226 lines) into 4 partial class files
- [4dfa3c3](https://github.com/NextGenSoftwareUK/OASIS/commit/4dfa3c39ca7ecabeee6c45b88cc2b93c02a25143) refactor: split STARNETManagerBase.Activate.cs (1243 lines) into 4 partial class files
- [de012f7](https://github.com/NextGenSoftwareUK/OASIS/commit/de012f77bc37933a8eb61642663e5ebb1dbc476b) refactor: split KeysController.cs (1251 lines) into 4 partial class files
- [42c893a](https://github.com/NextGenSoftwareUK/OASIS/commit/42c893a217439c02f1db40651e9525c2ee544888) refactor: split STARNETManagerBase.Publish.cs (1253 lines) into 4 partial class files
- [1771c7a](https://github.com/NextGenSoftwareUK/OASIS/commit/1771c7a85e5bda018576295a33d402f95024f208) refactor: split NFTs.cs (1285 lines) into 4 partial class files
- [d079089](https://github.com/NextGenSoftwareUK/OASIS/commit/d07908974d8b7709e885bbed7457cc68f34aa356) refactor: split SolanaService.cs (1300 lines) into 4 partial class files
- [d3cbcd7](https://github.com/NextGenSoftwareUK/OASIS/commit/d3cbcd742f592c8e4a7d2225c8123e3b9cb164ea) refactor: split HolonManager-Private.cs (1314 lines) into 4 partial class files
- [e1db5e4](https://github.com/NextGenSoftwareUK/OASIS/commit/e1db5e4e4f50e50fe4b8b8393726148f9f7df0c2) refactor: split OASISDNA.cs (1330 lines) into 3 files by class group
- [95155f8](https://github.com/NextGenSoftwareUK/OASIS/commit/95155f8590d0efa09f2e9cb568d14d6eae4d39fd) refactor: split GameManager.cs (1343 lines) into 4 partial class files
- [1994393](https://github.com/NextGenSoftwareUK/OASIS/commit/19943937ac4fe357c10c1f074610ac99426a020a) refactor: split AvtarRepository.cs (1388 lines) into 4 partial class files
- [b081db1](https://github.com/NextGenSoftwareUK/OASIS/commit/b081db14e86b152d873bb1362737c90ba41cae7f) refactor: split ONETProtocol.cs (1411 lines) into 4 partial class files
- [51cb318](https://github.com/NextGenSoftwareUK/OASIS/commit/51cb31837173021da770e3c5aedd4fb79408995d) refactor: split SQLLiteDBOASIS.cs (1416 lines) into 4 partial class files
- [7fee3b7](https://github.com/NextGenSoftwareUK/OASIS/commit/7fee3b7480180f3512075a46493f937f547a2b55) refactor: split ONETAPIGateway.cs (1432 lines) - extract helper classes into own files
- [723754a](https://github.com/NextGenSoftwareUK/OASIS/commit/723754a9ebc5675807ad3950bb8746f55a493e26) refactor: split IPFSOASIS.cs (1539 lines) into 4 partial class files
- [10e1dab](https://github.com/NextGenSoftwareUK/OASIS/commit/10e1dab6976e62f84651f6e790b74323aafe0914) refactor: split GamesController.cs (1513 lines) into 4 partial class files
- [ed50ac4](https://github.com/NextGenSoftwareUK/OASIS/commit/ed50ac44e631b694e5c2a54e1aeda3fc2bea1b90) refactor: split AvatarService.cs (1542 lines) into 4 partial class files
- [827aed2](https://github.com/NextGenSoftwareUK/OASIS/commit/827aed20b8f7bca7274067c17fba4110e72790ed) refactor: split STARNETUIBase.Create.cs (1175 lines) into 4 partial class files
- [77ca549](https://github.com/NextGenSoftwareUK/OASIS/commit/77ca54919fa618bb197647c4b810c3c533c6fc08) refactor: split Star.Validate.cs (1180 lines) into 4 partial class files
- [7c92e09](https://github.com/NextGenSoftwareUK/OASIS/commit/7c92e094c70fb36f42ebb8fc2d8c6a25d92ecdba) refactor: add HolonManager-Private-Load partial files (force-add, ignored by gitignore)
- [34a2527](https://github.com/NextGenSoftwareUK/OASIS/commit/34a252714d29ef01fde64c5278a3157cb2c0a6af) refactor: split HolonManager-Private-Load.cs (1280 lines) into 3 partial class files
- [7e7a4cc](https://github.com/NextGenSoftwareUK/OASIS/commit/7e7a4ccdc130f911e7c8e9aeb659648cb2071cd3) refactor: split AvatarManager-Load.cs (1224 lines) into 4 partial class files
- [4b6fccc](https://github.com/NextGenSoftwareUK/OASIS/commit/4b6fccc59710b61c28776ef45f6e40116fa88207) refactor: split Program.Init.cs (1330 lines) into 3 partial class files
- [971312e](https://github.com/NextGenSoftwareUK/OASIS/commit/971312eb29f6cb1bfcf4f93aec0caa7b5acae08f) refactor: split GeoNFTs.cs (1350 lines) into 4 partial class files
- [3802af7](https://github.com/NextGenSoftwareUK/OASIS/commit/3802af7f7ba983236790b12a522247b6f8e914cc) refactor: split Program.AvatarCommands.cs (1350 lines) into 4 partial class files
- [a419f65](https://github.com/NextGenSoftwareUK/OASIS/commit/a419f6595cc826130c575690e09f2270a6af4659) refactor: split STARNETManagerBase.Create.cs (1314 lines) into 4 partial class files
- [ea52250](https://github.com/NextGenSoftwareUK/OASIS/commit/ea52250d059ca06134b2df8bd7526055d2bd8bdd) refactor: split STARNETManagerBase.WriteDNA.cs (1363 lines) into 4 partial class files
- [ca6aca0](https://github.com/NextGenSoftwareUK/OASIS/commit/ca6aca043cc464326b1465042619fef7d04fbfa5) refactor: split Program.ConfigCommands.cs (1607 lines) into 4 partial class files
- [b90cd3d](https://github.com/NextGenSoftwareUK/OASIS/commit/b90cd3d73ee271e9ffcc148931fa33543ec463d9) refactor: split STARNETManagerBase.Dependencies.cs (1709 lines) into 4 partial class files
- [42bcc85](https://github.com/NextGenSoftwareUK/OASIS/commit/42bcc8570cab6403b8fed100e9e086ec741a3118) refactor: split CelestialBody.cs (1511 lines) into 6 partial class files
- [252154a](https://github.com/NextGenSoftwareUK/OASIS/commit/252154a51504cc71182cb23670f1a52aff6146d1) refactor: split NFTCommon.cs (1434 lines) into 4 partial class files
- [d3dae55](https://github.com/NextGenSoftwareUK/OASIS/commit/d3dae553d3e248f0e37c7715c7ad46a8e8a365ca) refactor: split AvatarProfileController.cs (1457 lines) into 4 partial class files
- [cc13d09](https://github.com/NextGenSoftwareUK/OASIS/commit/cc13d09778f1fa4c9f8f8502a2160513ec6b2f5a) refactor: split OASISBootLoader.cs (1588 lines) into 5 partial class files
- [cddb63f](https://github.com/NextGenSoftwareUK/OASIS/commit/cddb63f2cbd9b1f9cfbe9aa5e24ef16a02061d01) refactor: split COSMIC.cs (1863 lines) into 5 partial class files
- [f2b2ceb](https://github.com/NextGenSoftwareUK/OASIS/commit/f2b2cebf2117fcef015f5a1287ac9392594073b6) refactor: split ProviderManager.cs (1800 lines) into 4 partial class files
- [0526253](https://github.com/NextGenSoftwareUK/OASIS/commit/05262533f4ef218829d4ea47592ae65f30a37146) refactor: split ONETDiscovery.cs (2035 lines) into 4 partial class files
- [423d72e](https://github.com/NextGenSoftwareUK/OASIS/commit/423d72eac5ed337a802ea628710dd0b6db989dbf) refactor: split HyperDriveController.cs (2077 lines) into 6 partial class files
- [2987e92](https://github.com/NextGenSoftwareUK/OASIS/commit/2987e92b174ec92cc367e03a2d10a42651738749) refactor: split DataController.cs (1454 lines) into 5 partial class files
- [461b651](https://github.com/NextGenSoftwareUK/OASIS/commit/461b651f5c8aabe581656da400b0bec396783acb) refactor: split HolonManager-Load.cs (1664 lines) into 3 partial class files
- [53c6973](https://github.com/NextGenSoftwareUK/OASIS/commit/53c6973116f437eb960c5512e4913ac37c434363) refactor: split SEEDSOASIS.cs (1765 lines) into 4 partial class files
- [579acd0](https://github.com/NextGenSoftwareUK/OASIS/commit/579acd0cdb1d69e9ba4b921c3c33cf018481adea) refactor: split AvatarManager-Private.cs (1991 lines) into 3 partial class files
- [6d724a8](https://github.com/NextGenSoftwareUK/OASIS/commit/6d724a88b1fdd1d1be434042e953ebf7342618ec) refactor: split AvatarManager.cs (1857 lines) into 5 partial class files
- [2dfbd17](https://github.com/NextGenSoftwareUK/OASIS/commit/2dfbd17d14c89de006b464ac71a3a03116242fc2) refactor: split AzureCosmosDBOASIS.cs (1930 lines) into 4 partial class files
- [1f790bf](https://github.com/NextGenSoftwareUK/OASIS/commit/1f790bf31d1555d9b1a3c391fe4ab7d252510e5c) refactor: split ActivityPubOASIS.cs (2014 lines) into 4 partial class files
- [fe08758](https://github.com/NextGenSoftwareUK/OASIS/commit/fe087582d6899b291b10173df68789a1a2770a67) refactor: split OAPPs.cs (2033 lines) into 5 partial class files
- [970009e](https://github.com/NextGenSoftwareUK/OASIS/commit/970009e407f1b3cc6d19cad9cf66af520d2864d0) refactor: split QuestManager.cs (2040 lines) into 5 partial class files
- [283c65a](https://github.com/NextGenSoftwareUK/OASIS/commit/283c65aba6335aa2c4d77f927cd50cae944187aa) refactor: split CelestialSpace.cs (2083 lines) into 5 partial class files
- [c97e483](https://github.com/NextGenSoftwareUK/OASIS/commit/c97e483baa07d9f0e47106508999b7ba120e87a6) refactor: split QuestsController.cs (2104 lines) into 4 partial class files
- [978d374](https://github.com/NextGenSoftwareUK/OASIS/commit/978d3742ae8e5e2430f1122b656b05961042b271) refactor: split Star.cs (3664 lines) into 5 partial class files
- [84db180](https://github.com/NextGenSoftwareUK/OASIS/commit/84db1801dd97b80c5ec19d08b087c7badacd387f) refactor: split CosmicController.cs (3515 lines) into 4 partial class files
- [ee1c7bc](https://github.com/NextGenSoftwareUK/OASIS/commit/ee1c7bccd562fe2f162b91fb0dc75bab8f10e823) refactor: split STARNETUIBase.cs (4240 lines) into 5 partial class files
- [72ad6f7](https://github.com/NextGenSoftwareUK/OASIS/commit/72ad6f7becbac610749b814863550701b408fbe6) refactor: split COSMICManager.cs (4549 lines) into 5 partial class files
- [20026ed](https://github.com/NextGenSoftwareUK/OASIS/commit/20026ed73559a7d88a2f37ddfd075ebf3ec78f04) refactor: split Program.cs (5778 lines) into 5 partial class files
- [23032bb](https://github.com/NextGenSoftwareUK/OASIS/commit/23032bb7a583ab47bcb9de575e0626643347ddcb) refactor: split STARNETManagerBase.cs (7993 lines) into 7 partial class files
- [159c161](https://github.com/NextGenSoftwareUK/OASIS/commit/159c161d786aef7fd81454bf2166a43de67558e8) refactor: split AWSOASIS.cs (2146 lines) into 4 partial class files
- [de16199](https://github.com/NextGenSoftwareUK/OASIS/commit/de161995bd13a89a381fd97329187078f1bafcd2) refactor: split PinataOASIS.cs (2231 lines) into 4 partial class files
- [392e756](https://github.com/NextGenSoftwareUK/OASIS/commit/392e756df49f136d90b43346c23df78ef81a421f) refactor: split PLANOASIS.cs (2360 lines) into 4 partial class files
- [36c1850](https://github.com/NextGenSoftwareUK/OASIS/commit/36c1850b413b2d5c52309bbf1fb8f9180e1d8328) refactor: split ChainLinkOASIS.cs (2479 lines) into 6 partial class files
- [9a9033b](https://github.com/NextGenSoftwareUK/OASIS/commit/9a9033b913b1dcd54746f397d2c9501e17bfb657) refactor: split KeyManager.cs (2648 lines) into 5 partial class files
- [59aeb0d](https://github.com/NextGenSoftwareUK/OASIS/commit/59aeb0d2dd3f445f6ea3ac71768806bddb9c03cb) refactor: split Neo4jOASIS.cs (2455 lines) into 5 partial class files
- [1d73470](https://github.com/NextGenSoftwareUK/OASIS/commit/1d73470ae9597a4ff799f7edb25b063c3bd9bd70) refactor: split ONETAPIGateway.cs (2503 lines) into 4 partial class files
- [6a587f9](https://github.com/NextGenSoftwareUK/OASIS/commit/6a587f96599c5b9cd69aa8c24432cc3447476136) refactor: split AztecOASIS.cs (2556 lines) into 6 partial class files
- [06c0b21](https://github.com/NextGenSoftwareUK/OASIS/commit/06c0b21c1e9ad01443391216481cf750994e562f) refactor: split MidenOASIS.cs (2527 lines) into 6 partial class files
- [b995906](https://github.com/NextGenSoftwareUK/OASIS/commit/b995906e875ccfbcc395af6e590b98ce46267465) refactor: split ONETRouting.cs (2311 lines) into 6 partial class files
- [95d00ce](https://github.com/NextGenSoftwareUK/OASIS/commit/95d00ceb54b6f8954832fecf1f1dfa63bc58c6d6) refactor: split NFTManager.cs (5188 lines) into 11 partial class files
- [322691d](https://github.com/NextGenSoftwareUK/OASIS/commit/322691dc68ff8642975284e1a0b3c2e492899c6e) Update NFTManager.cs
- [0da491c](https://github.com/NextGenSoftwareUK/OASIS/commit/0da491cb6db6adfeb9129ab2995e068d2cfa51fe) refactor: split ZcashOASIS.cs (2644 lines) into 6 partial class files
- [b3d72d1](https://github.com/NextGenSoftwareUK/OASIS/commit/b3d72d10bce61bebd1ba987586c7f42f9f97259d) refactor: split MoralisOASIS.cs (2884 lines) into 7 partial class files
- [1331f54](https://github.com/NextGenSoftwareUK/OASIS/commit/1331f54b1b96d6de3985a169f0a9a8b54c9dacf3) refactor: split Neo4jOASIS.Aura (3347 lines) into 8 partial class files
- [5c223ff](https://github.com/NextGenSoftwareUK/OASIS/commit/5c223ff665a9e60c7548d641ea5c34c383aadc4d) refactor: split GoogleCloud.cs (3429 lines) into 9 partial class files
- [4b65383](https://github.com/NextGenSoftwareUK/OASIS/commit/4b65383d3703878b6fa77294b7fb9fe2c554a8e3) refactor: split RadixOASIS.cs (3458 lines) into 8 partial class files
- [89c1dfe](https://github.com/NextGenSoftwareUK/OASIS/commit/89c1dfe6fd2a8a5747dd7f7edace69ba0bdde9fc) refactor: split ThreeFoldOASIS.cs (3510 lines) into 8 partial class files
- [6935a67](https://github.com/NextGenSoftwareUK/OASIS/commit/6935a67e4ad8114fc5873a79a794f421dadeecf2) refactor: split FantomOASIS.cs (3628 lines) into 8 partial class files
- [9220fb9](https://github.com/NextGenSoftwareUK/OASIS/commit/9220fb91d2ae4d2eb95f01b790987a6f8bd373a1) refactor: split HoloOASIS.cs (3632 lines) into 8 partial class files
- [31d094d](https://github.com/NextGenSoftwareUK/OASIS/commit/31d094d9daa87779895c0a1a4fd4d7bd8ae33794) refactor: split OptimismOASIS.cs (3842 lines) into 8 partial class files
- [e1b820f](https://github.com/NextGenSoftwareUK/OASIS/commit/e1b820f7f1e8fce841a79622feffe753f2f585f9) refactor: split ElrondOASIS.cs (3871 lines) into 9 partial class files
- [fa70843](https://github.com/NextGenSoftwareUK/OASIS/commit/fa70843fb5a1832476444a90674090001b5751cd) refactor: split SuiOASIS.cs (3871 lines) into 8 partial class files
- [0ee8c54](https://github.com/NextGenSoftwareUK/OASIS/commit/0ee8c542950748cd140091f4719ff786013c68aa) refactor: split BaseOASIS.cs (4031 lines) into 8 partial class files; extract file-scoped helpers to BaseContractHelper.cs
- [83a9f49](https://github.com/NextGenSoftwareUK/OASIS/commit/83a9f497028a21ba7262c2771fa1902eee854e57) refactor: split LocalFileOASIS.cs (4501 lines) into 6 partial class files
- [4a5f541](https://github.com/NextGenSoftwareUK/OASIS/commit/4a5f54193a95d122c14ec64b531e901e38db8ec3) refactor: split EOSIOOASIS.cs (4616 lines) into 9 partial class files
- [4934c35](https://github.com/NextGenSoftwareUK/OASIS/commit/4934c3564e507cec15c3c12b918630327d7c3c7c) refactor: split BitcoinOASIS.cs (4644 lines) into 9 partial class files
- [7096e9b](https://github.com/NextGenSoftwareUK/OASIS/commit/7096e9b29ebd1285f7a9a03bb39f2b5e7c150388) refactor: split HashgraphOASIS.cs (4657 lines) into 8 partial class files
- [7d973e1](https://github.com/NextGenSoftwareUK/OASIS/commit/7d973e137895fdd9d5f46adf1657f8fe51e72d53) refactor: split CosmosBlockChainOASIS.cs (4749 lines) into 8 partial class files
- [f3202d1](https://github.com/NextGenSoftwareUK/OASIS/commit/f3202d1c28252f60f2ce40610ed2321000048254) refactor: split CardanoOASIS (4864 lines) into 9 partial class files
- [cd5e37b](https://github.com/NextGenSoftwareUK/OASIS/commit/cd5e37b818392a710cf57efa49e7e6a54c6ed421) refactor: split EthereumOASIS (4913 lines) into 9 partial class files
- [642795c](https://github.com/NextGenSoftwareUK/OASIS/commit/642795c7c61ed94b4d5cf6dc6126cf5c02525174) refactor: split BlockStackOASIS (4991 lines) into 9 partial class files
- [a171f5e](https://github.com/NextGenSoftwareUK/OASIS/commit/a171f5e835aa52b25a7a78b141f070c9d27c63dc) refactor: split NEAROASIS (5142 lines) into 10 partial class files
- [61c9dce](https://github.com/NextGenSoftwareUK/OASIS/commit/61c9dcee35a01e1439d1c8074806a1e237ba420c) refactor: split AptosOASIS (5156 lines) into 10 partial class files
- [2810162](https://github.com/NextGenSoftwareUK/OASIS/commit/28101624392b4468cdfae1ad4e0c4f52e59cdd7f) refactor: split TRONOASIS (5162 lines) into 10 partial class files
- [bbb56c1](https://github.com/NextGenSoftwareUK/OASIS/commit/bbb56c1109b67be1494a7ce269fde0904bcd0370) refactor: split WalletManager (5357 lines) into 11 partial class files
- [0a4d438](https://github.com/NextGenSoftwareUK/OASIS/commit/0a4d438fd9ecda678bdd370101bd1054fa962eb9) refactor: split StarknetOASIS (3956 lines) into 9 partial class files
- [05082d8](https://github.com/NextGenSoftwareUK/OASIS/commit/05082d891c4f7d003bab4ecf7e7a818c8ba4dbf9) refactor: split PolkadotOASIS (4086 lines) into 9 partial class files
- [7146d2c](https://github.com/NextGenSoftwareUK/OASIS/commit/7146d2cffe92fc32a6d623ae229af167b43c109c) fix: remove spurious payer account from SetCollectionSize; only 3 accounts needed (metadata, authority, mint)
- [8409726](https://github.com/NextGenSoftwareUK/OASIS/commit/840972614a7f3107069cab00829f723a0554f371) fix: surface SetCollectionSize program error via simulate step; fix last-error swallowed before timeout
- [81d8a32](https://github.com/NextGenSoftwareUK/OASIS/commit/81d8a32c8e71d5ec99973829a1860b7cfd3250c2) fix: suppress send NFT retry noise when send ultimately succeeds
- [1968db6](https://github.com/NextGenSoftwareUK/OASIS/commit/1968db6b31c02d15c8bfbd181922c90588aab744) fix: revert to correct discriminators, add payer account, suppress retry noise
- [c753f27](https://github.com/NextGenSoftwareUK/OASIS/commit/c753f27f5283f4538c4c0119442cf86b3914d1f6) fix: guard null blockhash RPC response in SolanaService
- [95d75c4](https://github.com/NextGenSoftwareUK/OASIS/commit/95d75c4c2a57576f0bec810546f8e9d13ace3a58) refactor: split TelosOASIS (4066 lines) into 9 partial class files
- [cabe2d5](https://github.com/NextGenSoftwareUK/OASIS/commit/cabe2d5c20394ffdbe1c383b1fd8bdd2c15aa9fc) refactor: split AvalancheOASIS (4170 lines) into 11 partial class files
- [3556646](https://github.com/NextGenSoftwareUK/OASIS/commit/35566461ca7ff7866b4ae639cc80711113579c12) refactor: split SolanaOasis (4484 lines) into 10 partial class files
- [702a8af](https://github.com/NextGenSoftwareUK/OASIS/commit/702a8af43d1d282e7a5ba6db4a6641d443c73635) refactor: split BNBChainOASIS (4477 lines) into 10 partial class files
- [db06097](https://github.com/NextGenSoftwareUK/OASIS/commit/db0609747442bfa8855fe7fded7007ca5df4abfe) refactor: split Web3CoreOASISBaseProvider (4484 lines) into 8 partial class files
- [918d0dd](https://github.com/NextGenSoftwareUK/OASIS/commit/918d0dd53009a91dcedf3300ba31f212fe35e048) refactor: split ArbitrumOASIS (4524 lines) into 8 partial class files
- [0f8f9e6](https://github.com/NextGenSoftwareUK/OASIS/commit/0f8f9e66e1c4a8a9a00b00b4c4c7e1c58784cf59) feat: OASIS architecture review — bug fixes, controller split, sandbox playground
- [cb8cd07](https://github.com/NextGenSoftwareUK/OASIS/commit/cb8cd07be6a7a6b448702179ccfd85ce3cd673aa) refactor: rename CreateCollectionNFT -> MintOnChainCollectionNFT across NFT stack
- [48d1702](https://github.com/NextGenSoftwareUK/OASIS/commit/48d17020e378d8c21c30360e9e4a3a9b0436f5bb) feat: rework CreateCollectionNFT to wrap Web4 MintNft flow with retry logic for SetCollectionSize
- [4051fc8](https://github.com/NextGenSoftwareUK/OASIS/commit/4051fc818f9fd093a03c8a3993aa215e6e0f7aa4) fix: reduce MongoDB pool size to 30 to stay under Atlas free tier 500 connection limit across 14 instances
- [2b0ec3b](https://github.com/NextGenSoftwareUK/OASIS/commit/2b0ec3b8ff9490d74e5cddc9e39b73c32a8d4526) fix: make MongoClient a singleton per connection string and cap pool size to 50 to prevent Atlas connection exhaustion
- [3acb5a0](https://github.com/NextGenSoftwareUK/OASIS/commit/3acb5a0bb3b82c36aa956bba2d631a9b8fed8b59) Update HolonManager-Private.cs
- [532b21a](https://github.com/NextGenSoftwareUK/OASIS/commit/532b21ae7f5571b31d9c0629fa24f5558acf048b) fix: guarantee IsActive=true for new holons at MetaData write point in PrepareHolonForSaving
- [a491c7b](https://github.com/NextGenSoftwareUK/OASIS/commit/a491c7b15a281e6bd33759e5faf128858b37f28d) fix: default IsActive to true on all Holon/HolonBase entities
- [8dcb116](https://github.com/NextGenSoftwareUK/OASIS/commit/8dcb1167fbdea895c7515bb390eb128e96e62642) Update MapEntitiesController.cs
- [ac55bb4](https://github.com/NextGenSoftwareUK/OASIS/commit/ac55bb4945ee873776a6e8b51a0f2f6176f27530) perf: replace in-memory MetaData scan with server-side dollar-expr+dollar-getField filter
- [70c29b9](https://github.com/NextGenSoftwareUK/OASIS/commit/70c29b92f9b9557ca8fee27e30868d413399e4cf) fix: add missing MetaData exempt keys for encryption compatibility
- [c30ed80](https://github.com/NextGenSoftwareUK/OASIS/commit/c30ed809c44a93bff44b90340b386e116b4198ad) fix: restore NFT MetaData query and local dev port/HTTPS config
- [2827f61](https://github.com/NextGenSoftwareUK/OASIS/commit/2827f61d77ce1f93688f1dbb890f44c7b452f716) feat: implement UnlockPortal across all 10 games + PortalsController STAR API
- [6d6db3a](https://github.com/NextGenSoftwareUK/OASIS/commit/6d6db3a422dd2d4a50f7bfcbc9722e874f01d2d0) feat(omniverse): implement player warp + entity spawn across all 10 OGames
- [3079336](https://github.com/NextGenSoftwareUK/OASIS/commit/3079336b8930887a0afbb491b2591e44147ecc26) refactor: cross-game story arcs are Chapter→Mission→Quest→Objective
- [a0456aa](https://github.com/NextGenSoftwareUK/OASIS/commit/a0456aa9c56e52287d13c3faa2c21c2cad31febb) refactor: rewrite StoriesController + MapEntitiesController to use HolonManager
- [6226646](https://github.com/NextGenSoftwareUK/OASIS/commit/6226646d46c8f7925002737fbacdf752ee20be64) feat(star-api): add Teleport, SpawnEvents, Stories, and MapEntities controllers
- [a67f1b2](https://github.com/NextGenSoftwareUK/OASIS/commit/a67f1b236b97c9d9d4a2fa07a0561e6183bca4c9) feat: add OQuake2/OQuake2-RTX/OQuake3 integrations; complete 10-game Omniverse
- [4cdc14d](https://github.com/NextGenSoftwareUK/OASIS/commit/4cdc14da8fc9317555395f7ab67f20e168af9a68) Expand holon MetaData encryption exempt keys and add AdditionalQueryableKeys
- [248b257](https://github.com/NextGenSoftwareUK/OASIS/commit/248b25748dd7613d1f2769a78257df05099a52fe) Extend 3-layer encryption to wallet private keys and holon data
- [d5ee1d4](https://github.com/NextGenSoftwareUK/OASIS/commit/d5ee1d46a76453c266e5f154c40d5f4457d44228) Fix REST route hyphens and misleading NFT mint success message
- [b460d12](https://github.com/NextGenSoftwareUK/OASIS/commit/b460d12232412da311ab89a480f5d31cea29a544) Implement efficient token lookups across all remaining providers
- [e608567](https://github.com/NextGenSoftwareUK/OASIS/commit/e608567e38218b88f028071dc687a0104812b127) Implement efficient token lookups in SQLite provider
- [c0f6509](https://github.com/NextGenSoftwareUK/OASIS/commit/c0f650912c32341dbf1e331c93bd6ca069826dd0) Implement efficient token lookups in MongoDB provider
- [f31d9d5](https://github.com/NextGenSoftwareUK/OASIS/commit/f31d9d5846ca77462bec1a2af472a07cef29ed11) Eliminate LoadAllAvatars in security-sensitive lookup paths
- [8381a6c](https://github.com/NextGenSoftwareUK/OASIS/commit/8381a6cc22e1f29bbd3d84f29c5cb50b73088034) Add rate limiting and API key middleware to Web5-Web10 WebAPIs
- [80f9b2b](https://github.com/NextGenSoftwareUK/OASIS/commit/80f9b2b00c36f3de6139f68c89814d3b93051b25) Add rate limiting and API key middleware, config via OASISDNA SecuritySettings
- [6425dc3](https://github.com/NextGenSoftwareUK/OASIS/commit/6425dc3f2352b49ba7e9c472039cbcbd8fef3f83) Fix unbounded memory usage causing nightly Railway memory spikes
- [944667a](https://github.com/NextGenSoftwareUK/OASIS/commit/944667a729ae3079b4bbb77493bc1256754a5dff) Revert "Enforce IsNewHolon=true contract: fix remaining callers, add XML doc + audit doc"
- [dddab91](https://github.com/NextGenSoftwareUK/OASIS/commit/dddab914ba77d2e0ebf5ee68a9e7c314a3eca852) Revert "Revert Phase 4: restore IsNewHolon=true in all callers"
- [80797b2](https://github.com/NextGenSoftwareUK/OASIS/commit/80797b29c93065ea34cc94ceddb43907f70fdf66) Revert "Phase 3+4: AvatarRepository upsert fallback; comment out redundant IsNewHolon/CreatedDate"
- [d99b002](https://github.com/NextGenSoftwareUK/OASIS/commit/d99b002f1c63aeace9d66009011245511159d95c) Revert "Phase 1+2: BsonIgnore IsNewHolon, fix PrepareHolonForSaving, add audit doc"
- [221fbc5](https://github.com/NextGenSoftwareUK/OASIS/commit/221fbc5126cca5ab3e3ae6fc627494e0a07960f4) updated oportal uris etc
- [d3fe9d1](https://github.com/NextGenSoftwareUK/OASIS/commit/d3fe9d1a55f78870c79cea41953234602694d726) Make remaining avatar email links white
- [6f25637](https://github.com/NextGenSoftwareUK/OASIS/commit/6f256371251892c9ac2ec7fd131f6b7b508bb24f) Improve email text readability in avatar emails
- [3c9d113](https://github.com/NextGenSoftwareUK/OASIS/commit/3c9d113c789cc5e6fa4395642e70d17e27e55564) Enforce IsNewHolon=true contract: fix remaining callers, add XML doc + audit doc
- [b8d3848](https://github.com/NextGenSoftwareUK/OASIS/commit/b8d384814dce4c83c7b5a4395147b2ba76dba2fa) Revert Phase 4: restore IsNewHolon=true in all callers
- [c28f787](https://github.com/NextGenSoftwareUK/OASIS/commit/c28f787804228cdf60f4a5116a8547e740478a7e) Fix Web5 MetaData gRPC/GraphQL: correct UpdateAsync, DeleteAsync, CloneAsync, PublishAsync, LoadVersionsAsync, SearchAsync signatures
- [0017b9a](https://github.com/NextGenSoftwareUK/OASIS/commit/0017b9a0dabfcd54b1eff1ce65f276009589c6c3) Set isMutable: false on NFT and collection mints to improve RugCheck score and Phantom visibility
- [49e5b34](https://github.com/NextGenSoftwareUK/OASIS/commit/49e5b347dffdeedc44d490e931e7e19b3314a4b6) Phase 3+4: AvatarRepository upsert fallback; comment out redundant IsNewHolon/CreatedDate
- [74705e6](https://github.com/NextGenSoftwareUK/OASIS/commit/74705e6a10325a3176713c47d29aa9f39f3fd511) Phase 1+2: BsonIgnore IsNewHolon, fix PrepareHolonForSaving, add audit doc
- [ec94a6a](https://github.com/NextGenSoftwareUK/OASIS/commit/ec94a6ac81b3d38f130703e14707f702c843b547) Add upsert fallback to HolonRepository Update/UpdateAsync
- [891b9e3](https://github.com/NextGenSoftwareUK/OASIS/commit/891b9e355ee8737b9c5fb00739d724db1caaa992) Fix MongoDBOASIS SaveHolonAsync/SaveHolon always inserting instead of updating
- [7a9cb4c](https://github.com/NextGenSoftwareUK/OASIS/commit/7a9cb4c3e37d0e2dfef7787c90a208d25b8b853d) Reapply "Gate mint-nft and send-nft endpoints to Wizard avatars only"
- [2ab604f](https://github.com/NextGenSoftwareUK/OASIS/commit/2ab604f224b098dc587a1e0ced7b9860898d4e38) Gate mint-nft and send-nft endpoints to Wizard avatars only
- [bcac4ab](https://github.com/NextGenSoftwareUK/OASIS/commit/bcac4ab94c261f92055021262a0120e94a0262a8) Revert "Gate mint-nft and send-nft endpoints to Wizard avatars only"
- [e7ef487](https://github.com/NextGenSoftwareUK/OASIS/commit/e7ef487f02163fd152c75ba025c41541ea50148d) new script and missing grpc endpoints
- [30124aa](https://github.com/NextGenSoftwareUK/OASIS/commit/30124aa40db5fadb872001f0ab351c6e8c59292e) Comment out RevokeTokenAuthorities everywhere in the NFT mint stack
- [17f4fe7](https://github.com/NextGenSoftwareUK/OASIS/commit/17f4fe7d5c09c7ecc6219dd668eb8c3f6fb10b57) Disable RevokeTokenAuthorities: Metaplex transfers authority to Master Edition PDA before we can revoke
- [584d764](https://github.com/NextGenSoftwareUK/OASIS/commit/584d764004a2c382f23f362705621cdbfa61f924) Fix build: add RevokeTokenAuthorities and FreezeMetadata to MintAndPlaceWeb4GeoSpatialNFTRequest
- [58dbc32](https://github.com/NextGenSoftwareUK/OASIS/commit/58dbc32dcc2978fa58fe5483a801813c8ccd0e06) Wire RevokeTokenAuthorities and FreezeMetadata through full mint stack (interfaces, models, controller, NFTManager)
- [b0697ab](https://github.com/NextGenSoftwareUK/OASIS/commit/b0697ab0f8e814538bfc9c153f5c3cb46af69e76) Add FreezeMetadata option to MintWeb3NFTRequest and SolanaService
- [4c33c2e](https://github.com/NextGenSoftwareUK/OASIS/commit/4c33c2ed78f983d8f820a57895968a1b66a83cc6) Add RevokeTokenAuthorities option to MintWeb3NFTRequest and SolanaService
- [a626f03](https://github.com/NextGenSoftwareUK/OASIS/commit/a626f03fd1a31d41c50cbc9ff9164b0a9cabd017) Embed instruction bytes in error message for diagnostics
- [11e37ae](https://github.com/NextGenSoftwareUK/OASIS/commit/11e37ae3b8b130b666264a7210c79f0343d0ab50) Add debug logging: print instruction bytes before simulate
- [a376bd0](https://github.com/NextGenSoftwareUK/OASIS/commit/a376bd052cb3e4fc8057fa6df7fe489790afce8b) Fix SetAndVerifyCollection: use instruction 32 for sized collections
- [68819ee](https://github.com/NextGenSoftwareUK/OASIS/commit/68819ee5d7a6f3677f482b4043b5f70b4f5bce7c) Fix SetAndVerifyCollection for sized collections
- [438c53c](https://github.com/NextGenSoftwareUK/OASIS/commit/438c53c95aa7f4522a9ae44fa214f0e3f0600476) Add CreateCollectionNFT and SetCollectionSize to NFT API stack
- [4405677](https://github.com/NextGenSoftwareUK/OASIS/commit/4405677ef9985d634bae16551023494da1ab8302) Add CreateCollectionNftAsync and SetCollectionSizeAsync to SolanaService
- [2e4ed95](https://github.com/NextGenSoftwareUK/OASIS/commit/2e4ed95e8184a8be15ae761330c80d06076e0670) Add 100% gRPC and GraphQL coverage for EOSIO, Holochain, Map, OLand, Share, Solana, NFT, Subscription
- [0505868](https://github.com/NextGenSoftwareUK/OASIS/commit/050586878a95303b7c4012c2924f425396441aa3) Fix universal 401s: move JwtMiddleware before auth and register OASIS auth scheme
- [03ce629](https://github.com/NextGenSoftwareUK/OASIS/commit/03ce629fd0a07b442523256f6d658ae565a68a74) Karma: add Benevolent platform action enums, fill all GetKarmaForType cases; fix STAR 401
- [f4a28e0](https://github.com/NextGenSoftwareUK/OASIS/commit/f4a28e05cbeed07572d2e12f9482cb1e0e6d9f99) Add 100% gRPC coverage matching GraphQL endpoints
- [71779e8](https://github.com/NextGenSoftwareUK/OASIS/commit/71779e8263db57ea178ee40be8885c073003bc3a) Update Program.cs
- [154df0a](https://github.com/NextGenSoftwareUK/OASIS/commit/154df0a58e2945eb04c8eacdedde4bb39b2af5ac) gRPC support

</details>

[Compare the complete OASIS source range](https://github.com/NextGenSoftwareUK/OASIS/compare/fb084c0daca859d855788929d7a9f2e0b57e97e6...a5657b3bd8eab2f307b69152c016c2b286ec4c08)
----------------------------------------------------------------------------------------------------------------------------
## 3.0.0 (21/08/26)

Major WEB5 release establishing the independently versioned STAR API and its production dependency graph.

### What's new in 3.0.0

- Completed the STAR ODK/WEB5 move to its dedicated repository while keeping exact OASIS gitlinks and Railway deployment pins synchronized.
- Expanded and hardened production providers across AI, blockchain, network, identity, maps, permanent storage, social graph and enterprise database categories, with real API implementations and contract tests.
- Added major WEB5 quest, GeoNFT, shared inventory and game-integration work used by Our World, ODOOM, OQUAKE and OGEngineClient.
- Integrated HyperDrive v2, ONET persistence and consensus improvements, offline session grants and synchronization foundations while preserving the selected-provider architecture.
- Strengthened avatar identity, authentication, JSON contracts, subscription and Stripe behavior, provider management and persistence lifecycle invariants.
- Updated the .NET/package graph, security dependencies, CI runtimes, Docker/Railway deployment graph and coordinated release tooling.
- Added broader unit, integration and live coverage for providers, ONET, subscriptions, quest progress, inventory and cross-game synchronization.

### Full changelog

The OASIS parent inventory covers every distinct non-merge change touching STAR or its WEB4/provider/ONET/Edge dependencies. The STAR inventory covers every distinct non-merge commit in the dedicated STAR repository through the 3.2.0 source point.

<details>
<summary>Complete commit inventory (288 distinct changes)</summary>

- [8b3e843](https://github.com/NextGenSoftwareUK/OASIS/commit/8b3e8431d88e833ab3fb79d59602e7004a2ad040) Automate coordinated OASIS platform releases
- [1edd8a0](https://github.com/NextGenSoftwareUK/OASIS/commit/1edd8a0c17e36c076a0205c1286b7eb18641e3f4) test(ONET): serialize singleton integration coverage
- [89061f7](https://github.com/NextGenSoftwareUK/OASIS/commit/89061f76efb2c2671194e7072194f7ec651af98d) fix(ci): repair consensus and release workflows
- [f2a9008](https://github.com/NextGenSoftwareUK/OASIS/commit/f2a9008213f11efce92a97eea3a3ec93bff6fdcf) Publish verified MCP documentation across releases
- [edf15ec](https://github.com/NextGenSoftwareUK/OASIS/commit/edf15ecd6b9158cc950e2ee0016448f173a5556d) feat: fully implement AI and Blockchain providers with real APIs, fix build errors
- [1e5657d](https://github.com/NextGenSoftwareUK/OASIS/commit/1e5657d1823c0aa55b442f12ee78a0e88a6125d9) feat: fully implement all 12 Network providers with real APIs, fix interfaces
- [dc61c33](https://github.com/NextGenSoftwareUK/OASIS/commit/dc61c33b833fb01779b4bbe91f9ee7eabb9bfc9a) Repair provider contracts and deployed dependency pins
- [b39add3](https://github.com/NextGenSoftwareUK/OASIS/commit/b39add3918d3c2c0905899bb7703d4cf9a39c25f) Stabilize Edge capability renewal test
- [eb6af1f](https://github.com/NextGenSoftwareUK/OASIS/commit/eb6af1fff4d8f359d96fb483b51d3be290de956c) feat: fully implement Identity and Maps providers with real APIs
- [907cfd4](https://github.com/NextGenSoftwareUK/OASIS/commit/907cfd414a71c7664f72ea1c864370a1d3017122) chore: bump ONODE Core submodule to d6faf00 (delete LOST_METHODS_REVIEW.cs)
- [9a688f8](https://github.com/NextGenSoftwareUK/OASIS/commit/9a688f8fc4d82e5820f89306ee6187817a9fb7a4) feat(ONET): consensus event enforcement + richer stats + 3 new integration tests
- [07e868f](https://github.com/NextGenSoftwareUK/OASIS/commit/07e868ffabb3499450975907b5639fddcda29ef9) Preserve nullable OASIS error semantics
- [6448ab0](https://github.com/NextGenSoftwareUK/OASIS/commit/6448ab0a78bab3b8853e124ecef9cb64158b50fd) Modernize GitHub Actions runtimes
- [fbd7ee5](https://github.com/NextGenSoftwareUK/OASIS/commit/fbd7ee5a419f42558b0dcf10fa8f62e854bb07a1) Align BootLoader contract tests with public API
- [ff46276](https://github.com/NextGenSoftwareUK/OASIS/commit/ff46276b15e3918900b5db11f55993edbd6d8609) Restore Google Cloud provider unit coverage
- [8a5d542](https://github.com/NextGenSoftwareUK/OASIS/commit/8a5d542166d93f72a529fc3f1dc097bc513a4bae) Resolve obsolete package integration constraints
- [831174b](https://github.com/NextGenSoftwareUK/OASIS/commit/831174bf7ef1bc6dc3f710d51d551100d5db29dc) Isolate subscription endpoint integration tests
- [b16db31](https://github.com/NextGenSoftwareUK/OASIS/commit/b16db31f51df5f785ac33d8b6285d19ab69c45d6) Fix full-solution package restore graph
- [259f81a](https://github.com/NextGenSoftwareUK/OASIS/commit/259f81a82f297af906c88b2cd9fd790aaad912ae) Use explicit QuestDB timestamp conversion
- [a230abe](https://github.com/NextGenSoftwareUK/OASIS/commit/a230abe210b0f723f861441ecb85de980081afd5) Use QuestDB native timestamp for deduplication
- [eb19e5f](https://github.com/NextGenSoftwareUK/OASIS/commit/eb19e5f6625e7a8003c5a7a6e9badc193897b4d3) Make live provider checks required
- [c42360c](https://github.com/NextGenSoftwareUK/OASIS/commit/c42360cc33a2d3855fc1b72136febda6147294e6) Match QuestDB timestamp parameter semantics
- [44d5bf2](https://github.com/NextGenSoftwareUK/OASIS/commit/44d5bf292128e2b78553b8cfa30846e427e57269) Fix remaining integration and coverage failures
- [f925ce7](https://github.com/NextGenSoftwareUK/OASIS/commit/f925ce7b006808584ca7aa11ee667119a3157557) Eliminate remaining NuGet vulnerabilities
- [4428d88](https://github.com/NextGenSoftwareUK/OASIS/commit/4428d8834903971ccf31b4422d4ceb026b9c7d32) Fix provider CI and patch database dependencies
- [7669d98](https://github.com/NextGenSoftwareUK/OASIS/commit/7669d98538b8ba1a481fd524badb03d0e161ffa1) Restore clean provider builds and integration startup
- [28f0219](https://github.com/NextGenSoftwareUK/OASIS/commit/28f0219083d612e2bdac7a190a17a49e17cc1742) Repair remaining project reference graph
- [824af81](https://github.com/NextGenSoftwareUK/OASIS/commit/824af81ed6a18c84e9cd7531c5a56b3903a665b5) Update vector and graph provider API contracts
- [c588228](https://github.com/NextGenSoftwareUK/OASIS/commit/c58822840916ac6bfa3f334c3cf16f8f90f615ef) Configure Garnet integration activation
- [d4da5d2](https://github.com/NextGenSoftwareUK/OASIS/commit/d4da5d2c8db2b68ba4d92eda42c149c07f73ef17) Initialize vector providers and type QuestDB parameters
- [98ffc7a](https://github.com/NextGenSoftwareUK/OASIS/commit/98ffc7add61d7e2b7a84cef1eb8bde9a49a19a75) Fix STAR and provider CI configuration
- [902adbd](https://github.com/NextGenSoftwareUK/OASIS/commit/902adbd692216ce8a006f775c96d0e81f5ad9fa5) Repair CI dependency and provider project references
- [22705b9](https://github.com/NextGenSoftwareUK/OASIS/commit/22705b9f4b338d7e509aab564d42c9664c435a06) Finalize cross-game release artifacts and dependency pins
- [72f594b](https://github.com/NextGenSoftwareUK/OASIS/commit/72f594b593b3f5f28193ffe82ab45bf02db39b92) Use hosted Mongo replica-set CI variable
- [08d212f](https://github.com/NextGenSoftwareUK/OASIS/commit/08d212fadae9163db996e7a8274bf2d93fa4a166) Pin OASIS API Core provider routing release
- [b9bd219](https://github.com/NextGenSoftwareUK/OASIS/commit/b9bd219f34e250c91e7f4eed110feca543d875f9) Honor authoritative providers in HyperDrive v2
- [958a366](https://github.com/NextGenSoftwareUK/OASIS/commit/958a36682eeef6f20d9c4d3ec9025f397e6e06b1) Scope Mongo Holon identity index to canonical records
- [29fa9ef](https://github.com/NextGenSoftwareUK/OASIS/commit/29fa9ef8b1a6136fc39a3cb7df34726a832d07e3) Poll accepted commands until outcomes arrive
- [f40672f](https://github.com/NextGenSoftwareUK/OASIS/commit/f40672f4247142a4b08f2e759a9c25ea42979f36) Retry concurrent hosted sync transactions
- [700d40d](https://github.com/NextGenSoftwareUK/OASIS/commit/700d40da25b67e1bc8b15aa08df7605fa850bbab) Coordinate Mongo primary handoff
- [8bf61b3](https://github.com/NextGenSoftwareUK/OASIS/commit/8bf61b32180ea9f7562c335a4d6eafddbc110f74) Force Mongo failover beyond election timeout
- [191ba5e](https://github.com/NextGenSoftwareUK/OASIS/commit/191ba5e2e6c573f3cdcc867e268b96b7a376f7c6) Require a completed Mongo primary handoff
- [85a95f9](https://github.com/NextGenSoftwareUK/OASIS/commit/85a95f9868fb019a28cdc98dee5d5c37b864c822) Serialize ONET controller lifecycle tests
- [b496df5](https://github.com/NextGenSoftwareUK/OASIS/commit/b496df5d1724e7e6f5f56dcea314ff491936ca98) Pin released ONODE lifecycle contract
- [3b529f7](https://github.com/NextGenSoftwareUK/OASIS/commit/3b529f7b79a83d90b02ceca11bef5d5b87f6ce12) Repair ONET lifecycle test contracts
- [cabf235](https://github.com/NextGenSoftwareUK/OASIS/commit/cabf2351ccc20e810ea2e117f2b26ea08292be3f) Serialize disruptive Mongo integration tests
- [4370446](https://github.com/NextGenSoftwareUK/OASIS/commit/43704462c47e07fa02a0a4258112ab62132bee7e) Fix clean-clone Edge and integration CI
- [493a88f](https://github.com/NextGenSoftwareUK/OASIS/commit/493a88fa665521bfc9ef8dcd33e027427c52b37e) Serialize Mongo class map registration
- [e750eec](https://github.com/NextGenSoftwareUK/OASIS/commit/e750eecf65e44e31a260da854b1a2d8697983530) Make Mongo serializer registration idempotent
- [29cdc1e](https://github.com/NextGenSoftwareUK/OASIS/commit/29cdc1eda2aaceee3599a1509bde19d86a796d72) Use shared subscription contracts in STAR
- [8becd55](https://github.com/NextGenSoftwareUK/OASIS/commit/8becd55167aa6c6e464ff851461e1f82596659b4) Prepare production three-game Edge release
- [b12d746](https://github.com/NextGenSoftwareUK/OASIS/commit/b12d746e2cac3063c1374dbc84208630dd4dccf1) Publish typed quest progress projections
- [f88b6dc](https://github.com/NextGenSoftwareUK/OASIS/commit/f88b6dc96f9fb9db7fdf3f30e1e0e25028cfdfd2) Migrate legacy HyperDrive inventory payloads
- [8d3ee50](https://github.com/NextGenSoftwareUK/OASIS/commit/8d3ee5049b7e6d17e87212a6545b37c555b244c8) Reproject existing HyperDrive inventory payloads
- [3d8b0b2](https://github.com/NextGenSoftwareUK/OASIS/commit/3d8b0b2e91a7fc332b1871af2aa43283736eb17a) Fix HyperDrive inventory result projection
- [db67776](https://github.com/NextGenSoftwareUK/OASIS/commit/db67776bf207e080b95172df74312f4cde1fe686) Publish typed HyperDrive command projections
- [d71b144](https://github.com/NextGenSoftwareUK/OASIS/commit/d71b1440c5a40038d65adda315042c63291dc0b1) Fix WEB4 STAR DNA loading
- [7a2854e](https://github.com/NextGenSoftwareUK/OASIS/commit/7a2854ebf1da8dbb07cd8ad6d8129ecfc4f5e885) Unify HyperDrive v2 with durable sync
- [2e894aa](https://github.com/NextGenSoftwareUK/OASIS/commit/2e894aa80b0f0aeaa9cac2111c101c7175d724f1) Select hosted fan-out targets by capability
- [ac2df30](https://github.com/NextGenSoftwareUK/OASIS/commit/ac2df30e95e3491caa7fa402b0769e2a542c9c37) Honor hosted fan-out configuration
- [7f693ee](https://github.com/NextGenSoftwareUK/OASIS/commit/7f693eefa6fbdc8e6650782e4a359d7c59ed030e) Serialize OASIS startup for HyperDrive workers
- [ff3effe](https://github.com/NextGenSoftwareUK/OASIS/commit/ff3effee75a3a20118f2e077f0fe658c4399fc2c) Initialize hosted domain capture automatically
- [d7156f9](https://github.com/NextGenSoftwareUK/OASIS/commit/d7156f998bd7791cf99fe34b8e7c497e7cb90521) Simplify hosted sync provider integration
- [711d47a](https://github.com/NextGenSoftwareUK/OASIS/commit/711d47a6c7fb9049e1baa88e58b0fee37d187246) Start hosted HyperDrive processing workers
- [59f1927](https://github.com/NextGenSoftwareUK/OASIS/commit/59f1927ca83ea322dab6c33a8cc42bb006fe1070) Declare Mongo hosted synchronization contracts
- [c5e6e03](https://github.com/NextGenSoftwareUK/OASIS/commit/c5e6e03de94a5fe2bccb60daf1eb4b81a2b94d1d) Load OASIS DNA before WebAPI service registration
- [8a204b3](https://github.com/NextGenSoftwareUK/OASIS/commit/8a204b3904c20177fa0ca8d6b0e74d949121372f) Fix Railway restore target framework mismatch
- [6653e8c](https://github.com/NextGenSoftwareUK/OASIS/commit/6653e8c700ead9175b56920842a39139bcf2ce3d) Add end-to-end Edge offline synchronization
- [6a62b40](https://github.com/NextGenSoftwareUK/OASIS/commit/6a62b409eafe4ca7de34a4e66d001f91fb9d6c1b) Gate WEB4 subscription telemetry during rollout
- [9fcc8e0](https://github.com/NextGenSoftwareUK/OASIS/commit/9fcc8e0ff874fcfca62f50dbb694263f134837c1) Gate WEB4 ledger workers during rollout
- [b8a314a](https://github.com/NextGenSoftwareUK/OASIS/commit/b8a314ae829bad1bd94dd9191e854206b2839aa3) Authorize established Wizard admin sessions
- [1243a93](https://github.com/NextGenSoftwareUK/OASIS/commit/1243a93589939bd522f76ffed99409b9dd54d43e) Pin merged subscription admin dependencies
- [1f1c9f4](https://github.com/NextGenSoftwareUK/OASIS/commit/1f1c9f425bd886faf750822aa17d972ba3cf3506) Add safe subscription administration rollout
- [fab351f](https://github.com/NextGenSoftwareUK/OASIS/commit/fab351f46cab872343a9f2ef037b88a5a43ce47b) Update submodule refs: Web5 STAR ODK and Web6 access control fixes
- [378af78](https://github.com/NextGenSoftwareUK/OASIS/commit/378af78d3db7b54a964ac12762120d40584e3bc8) Enforce auth/ownership across ONET, ONODE, Bridge, Map, EOSIO, Holochain, Clan, Seeds
- [f895c03](https://github.com/NextGenSoftwareUK/OASIS/commit/f895c030a4c3e93adaec1e2e81a97270144d84b2) Include deployment configuration in reconciliation tests
- [b3e449f](https://github.com/NextGenSoftwareUK/OASIS/commit/b3e449fdc0577c2d3633cbb34a035d269f78d9cc) Integrate provider capabilities and reuse deployed service configuration
- [ed0075a](https://github.com/NextGenSoftwareUK/OASIS/commit/ed0075aa9a43445c0f56505193e0ea54d643fe08) Refine HyperDriveController auth: read endpoints for all users, writes Wizard-only
- [4da558c](https://github.com/NextGenSoftwareUK/OASIS/commit/4da558cc9ae1cd5e1f6d86cb86ee60b5cadc4121) Enforce access control on Session, Search, Profile, Share, HyperDrive controllers
- [1bd49a7](https://github.com/NextGenSoftwareUK/OASIS/commit/1bd49a7de8fedf3c3b2ad48a716e64f8e48b9052) Add ownership checks to KeysController and [Authorize] to SubscriptionController
- [08351e6](https://github.com/NextGenSoftwareUK/OASIS/commit/08351e61c427e73148a79cc9406d988027413fa9) Implement atomic WEB4 usage ledger and durable cross-service settlement
- [4bfa75d](https://github.com/NextGenSoftwareUK/OASIS/commit/4bfa75d11fff05df82e4ff4b22a36b191fb8c374) Add ownership checks to Avatar, NFT, OLand, Stats, Karma, WalletToken controllers
- [cdf7b31](https://github.com/NextGenSoftwareUK/OASIS/commit/cdf7b31334fdbddb91cd2631252f261b581b4a41) Fix WEB4 subscription usage Mongo mapping
- [f87c13c](https://github.com/NextGenSoftwareUK/OASIS/commit/f87c13caa330afbc23611cfacc0f361edc301c19) Add ownership checks to all KeysController endpoints
- [f9c48a7](https://github.com/NextGenSoftwareUK/OASIS/commit/f9c48a753364d5bac9949c4844e375cadf73967c) Enforce ownership on AvatarDetail, Karma, and all Wallet endpoints
- [c57674e](https://github.com/NextGenSoftwareUK/OASIS/commit/c57674e90a7bdf5aec2a33771738bf57a46be1a9) Fill missing ProviderCapabilities across Blockchain/AI providers; update docs
- [cc58235](https://github.com/NextGenSoftwareUK/OASIS/commit/cc582356605272dd6b9d1b1a52ef628cb5ba6d67) Fix redundant/stray ProviderCapabilities in AzureStorageOASIS and Neo4jOASIS2
- [65ea975](https://github.com/NextGenSoftwareUK/OASIS/commit/65ea97519598237b1cf05dcda8b42d9e070e694d) Add ProviderCapabilities to Cloud, Network, Social, Identity, Maps, Storage providers
- [654b000](https://github.com/NextGenSoftwareUK/OASIS/commit/654b00069195aca3b3d94efb619cac30d2e99fcd) Update OASIS Architecture submodule pointer (ProviderCapabilities rename)
- [dcae513](https://github.com/NextGenSoftwareUK/OASIS/commit/dcae51352d4af6369e6bf904658d8a6e3d323ecd) Rename ProviderCategories → ProviderCapabilities in all 227 providers; remove duplicate Add() entries
- [fe15aee](https://github.com/NextGenSoftwareUK/OASIS/commit/fe15aee7cccf46b764ce5feaf1ae16ad3bf64561) Update OASIS Architecture submodule pointer
- [bed7c68](https://github.com/NextGenSoftwareUK/OASIS/commit/bed7c68a50efd930a1d473604dc6f185e2e32ce3) Add Get/IsProvider helpers for Cloud/Social/Identity/AI/Map/Spatial categories; fix Other folder categories
- [394e67f](https://github.com/NextGenSoftwareUK/OASIS/commit/394e67fd8fefc8e7ce343e60a7b81247c1b14f79) Assign EVMBlockchain category to EVM-compatible providers in Blockchain folder
- [2b56b80](https://github.com/NextGenSoftwareUK/OASIS/commit/2b56b8007dd893e4a9d40489c11f3d6906023eb9) Fix GaladrielOASIS ProviderCategory to EVMBlockchain (it is an EVM chain, not a generic AI provider)
- [df71ec1](https://github.com/NextGenSoftwareUK/OASIS/commit/df71ec1ba9c591603c357dcda279e612c4e23604) Fix ProviderCategory assignments across all provider categories
- [f984c1f](https://github.com/NextGenSoftwareUK/OASIS/commit/f984c1f5648af1cd2f464f99730009f3235b13b0) Add native MongoDB visibility filtering for holon load operations
- [a0e922a](https://github.com/NextGenSoftwareUK/OASIS/commit/a0e922aabd490d1e4000473c5e0a753b7857c4da) Move holon visibility filtering to manager/provider level; pass avatarId from controllers
- [bb9ca9f](https://github.com/NextGenSoftwareUK/OASIS/commit/bb9ca9fb71d2c8f6c55cd4393268252fa7a329ef) Add SearchOnlyForCurrentAvatar + IncludePublic to load-holons-for-parent routes
- [7094517](https://github.com/NextGenSoftwareUK/OASIS/commit/70945175f0e7f05abf545eb502cc17247b626d46) Add SearchOnlyForCurrentAvatar + IncludePublic to load-all-holons
- [be767fb](https://github.com/NextGenSoftwareUK/OASIS/commit/be767fb2ae4f043d526b95b9534326b8d3259fb7) load-all-holons filters by avatar for non-Wizards; add LoadFile ownership check
- [912cf11](https://github.com/NextGenSoftwareUK/OASIS/commit/912cf11a9ac2fe978158b808d24b39c0d867d1b0) Enforce holon ownership on delete, save, and provider-key routes
- [95c773b](https://github.com/NextGenSoftwareUK/OASIS/commit/95c773befb320cb9f971bea8706f11e2910ab293) Restrict LoadHolon to creator, public holons, or Wizard
- [f9b0dc4](https://github.com/NextGenSoftwareUK/OASIS/commit/f9b0dc43da0a0d3f821a6a83db7e43549129adc8) Restrict LoadAllHolons to Wizard; filter LoadHolonsForParent by avatar/public
- [1c4ae74](https://github.com/NextGenSoftwareUK/OASIS/commit/1c4ae747c78917f3454f5f4426ac2bff0802765f) Restrict all-holons path to Wizard role in LoadHolonsByMetaData and SearchHolons
- [c6f5ae4](https://github.com/NextGenSoftwareUK/OASIS/commit/c6f5ae45237ab09e3165ceb9db0032d02942004c) Update submodule pointers: IsPublic + metadata/search avatar filtering
- [c971ea3](https://github.com/NextGenSoftwareUK/OASIS/commit/c971ea3e5d72539fd7157502bcb124644d3182e1) Add SearchOnlyForCurrentAvatar + IncludePublic to metadata/search endpoints
- [dbfc6ce](https://github.com/NextGenSoftwareUK/OASIS/commit/dbfc6cea6c028a17b17047ffaff89d527170a83c) Add IsPublic to Moralis HolonBase entity
- [cd0d697](https://github.com/NextGenSoftwareUK/OASIS/commit/cd0d6979cc03685403a85ca890fea706f3ffc0d3) Add IsPublic to Mongo HolonBase entity
- [69b727e](https://github.com/NextGenSoftwareUK/OASIS/commit/69b727e8992cfb792a3cebf91a5e7fa86c067434) fix: resolve all build errors for new providers (stubs, usings, enum values)
- [1f72733](https://github.com/NextGenSoftwareUK/OASIS/commit/1f72733ea754667949a17ac82c4119cbac55041e) feat: implement real API/SDK for all new providers (21 providers across 6 categories)
- [b55b0a7](https://github.com/NextGenSoftwareUK/OASIS/commit/b55b0a78dba6d8b7e3dbd964f3e2e0c127b49d6b) docs: update WEB6 provider count 99→100, orchestrators 17→22 across all docs
- [133f590](https://github.com/NextGenSoftwareUK/OASIS/commit/133f5907d587d493bbf479ce2da611c4cd934943) Add missing WebAPI manager endpoints: ProviderKey data ops, avatar sessions, wallet tokens, level, achievement stats, search POST
- [ef40dc5](https://github.com/NextGenSoftwareUK/OASIS/commit/ef40dc51bd7117bd37ae64e825726f61f010ae79) Centralize WEB5-WEB10 subscriptions in WEB4
- [4779ac0](https://github.com/NextGenSoftwareUK/OASIS/commit/4779ac09e3adfe2e6027a06f44968918e3340842) Add missing DataController endpoints: LoadHolonByMetaData, LoadHolonsByMetaData, SearchHolons, SaveHolons (bulk)
- [58abdca](https://github.com/NextGenSoftwareUK/OASIS/commit/58abdcaf6b451ac9dda65907a6dbcf9aeb7fdd20) Remove direct enterprise entitlement bypass
- [6c95bb6](https://github.com/NextGenSoftwareUK/OASIS/commit/6c95bb616a40d5dffef2035184b8b1e6f5d66058) Grant test avatar enterprise API access
- [7ed6fd6](https://github.com/NextGenSoftwareUK/OASIS/commit/7ed6fd6da33cd681d0f4b5b024cd416bf335c92b) Add playable GeoHotSpot quest matrix
- [d955d44](https://github.com/NextGenSoftwareUK/OASIS/commit/d955d44c438dbf0abc51505b5be094d1eb4be251) docs: add README.md for 57 providers + bump submodule pointers
- [f0619e4](https://github.com/NextGenSoftwareUK/OASIS/commit/f0619e4d063d6ef7e0e18cf8e638ab1577c4b156) feat: add 14 new providers + AI category — 220 total
- [46f0fd9](https://github.com/NextGenSoftwareUK/OASIS/commit/46f0fd97480805c3ca726f59789388bd751b3357) chore: bump provider count 206→220 across docs, frontend, and Core submodule
- [9065d02](https://github.com/NextGenSoftwareUK/OASIS/commit/9065d02a9d4e92afe418bddca04c9c40c78d3288) feat: fully implement NATSJetStream, Temporal, Dapr, IntelOpenVINO providers
- [3753e34](https://github.com/NextGenSoftwareUK/OASIS/commit/3753e3420fdc9ba4712162c48ee5aa543c0625d0) feat: add 4 new providers + orchestrators — 206 total (NATS, Temporal, Dapr, OpenVINO)
- [92011ff](https://github.com/NextGenSoftwareUK/OASIS/commit/92011ffe55eaa6a7f19fc10a7e98e06f5e6d8358) feat: add 9 new providers + sync 4 pre-existing — 206 total
- [afaf548](https://github.com/NextGenSoftwareUK/OASIS/commit/afaf5481e304909ffeaa3e30e5ffab82ba9ced5a) chore: bump submodules + docs — provider total 202, ONET P2P docs, portal coords
- [27aa12e](https://github.com/NextGenSoftwareUK/OASIS/commit/27aa12ebd0b768e74d823b5eb3cad5942f707c30) feat: add 9 new providers — 202 total (+restaking, +sequencing, +edge storage, +identity)
- [62952b6](https://github.com/NextGenSoftwareUK/OASIS/commit/62952b6a85cec39943c65a1370c42dee83906f64) chore: propagate OASIS_DNA security config to all test harnesses
- [fc93f51](https://github.com/NextGenSoftwareUK/OASIS/commit/fc93f51bf12fcfd816b70b0ec719b6b77b200650) feat: add 9 new providers — 193 total (+cross-chain, +ZK identity, +Web3 infra)
- [7717b59](https://github.com/NextGenSoftwareUK/OASIS/commit/7717b594b86ba2309b2c7919f32fb80c85820595) feat: add 10 new providers — 184 total (+cross-chain, +ZK identity, +edge cloud)
- [6fbcd4a](https://github.com/NextGenSoftwareUK/OASIS/commit/6fbcd4ac40486ac0c40225fbef1ee1097004acf4) Verify MongoDB and Neo4j persistence locally
- [f9c9b62](https://github.com/NextGenSoftwareUK/OASIS/commit/f9c9b62a924d0e5d9f2dc90cd96a49573666e46b) Preserve SQLite data across provider reactivation
- [9505b01](https://github.com/NextGenSoftwareUK/OASIS/commit/9505b01deb99f0f7044b46ab8cc403ccdf5ec600) Verify GeoHotSpot persistence on disposable SQLite
- [01d3902](https://github.com/NextGenSoftwareUK/OASIS/commit/01d3902790cd61d285ce85f779ab6bab5faa904a) Repair storage provider verification harnesses
- [23bb409](https://github.com/NextGenSoftwareUK/OASIS/commit/23bb40950766b24e507cceba72d4fac2d6443ddf) Advance ONODE replayable quest effects
- [57d82a7](https://github.com/NextGenSoftwareUK/OASIS/commit/57d82a74f53458385591e2296155bc04f680a28a) Add durable GeoHotSpot trigger transactions
- [635c832](https://github.com/NextGenSoftwareUK/OASIS/commit/635c83219efee66653865f3e00d9154a0b52db58) Advance validated GeoHotSpot creation API
- [266c14b](https://github.com/NextGenSoftwareUK/OASIS/commit/266c14ba4a49e7d2de1a4b7aeb58004c9a3915ea) Complete GeoHotSpot integration docs and dependency pin
- [7021983](https://github.com/NextGenSoftwareUK/OASIS/commit/702198308ed9d05135ca9ec7724b7558efc8dad3) Advance STAR GeoHotSpot validation
- [c9ef0c2](https://github.com/NextGenSoftwareUK/OASIS/commit/c9ef0c2837cfaa151cb5d91d4e445724fea5d79b) Complete GeoHotSpot quest trigger integration
- [ada3e61](https://github.com/NextGenSoftwareUK/OASIS/commit/ada3e61bf09c679fabcc179e05f29a090a5f4c7f) feat: document GeoHotSpot trigger integration
- [05f62a5](https://github.com/NextGenSoftwareUK/OASIS/commit/05f62a5c23de99e2788de9d5a12fe631158a1c98) feat: expose typed WEB4 GeoNFT lookup
- [16f9295](https://github.com/NextGenSoftwareUK/OASIS/commit/16f929553abdd61ff480fcb283f4dbc2e703cf7c) docs: define WEB4 and WEB5 GeoNFT architecture
- [a1cd909](https://github.com/NextGenSoftwareUK/OASIS/commit/a1cd909d23385e02405110da3c40345ca0c43d38) docs: preserve distinct GeoNFT and GeoHotSpot APIs
- [dc9fc1b](https://github.com/NextGenSoftwareUK/OASIS/commit/dc9fc1be34bb48c62d54ae339f5821ff65ba37e5) build: advance STAR ODK GeoHotSpot contract
- [63f00ef](https://github.com/NextGenSoftwareUK/OASIS/commit/63f00efb1ed1d5657a9d03a9227373ed6999b749) feat: unify GeoHotSpot spawn policy
- [0a0de9f](https://github.com/NextGenSoftwareUK/OASIS/commit/0a0de9fe8b2be592ae5b5e43447f91a645cc5c3f) Expand Our World quest fixtures and persist collection dates
- [e42c73b](https://github.com/NextGenSoftwareUK/OASIS/commit/e42c73bc3125192f47ca958fde5eb802611b75bd) fix: persist quest authorship in test fixtures
- [6b20f24](https://github.com/NextGenSoftwareUK/OASIS/commit/6b20f24a1461747b0e84269f37b661d0b6b07f8b) feat: migrate Anorak to five canonical trees
- [09473f0](https://github.com/NextGenSoftwareUK/OASIS/commit/09473f07e74dc5a29137033d3eecc02eeefae3c0) fix: persist NFT ownership for GeoNFT authoring
- [2641199](https://github.com/NextGenSoftwareUK/OASIS/commit/2641199fb19620fd8d2463b6509fd9de49ee38d9) feat: add Our World quest and spawn test fixtures
- [bc5c29d](https://github.com/NextGenSoftwareUK/OASIS/commit/bc5c29dd745f2e62e2361106db785a9c362d38fd) fix: advance quest completion event invariant
- [a074148](https://github.com/NextGenSoftwareUK/OASIS/commit/a074148f19f6635a3f0282eef48ca3b41c5cedad) docs: define quest objective ordering contract
- [2956cac](https://github.com/NextGenSoftwareUK/OASIS/commit/2956cac942191b4e9a13bcd8dc8dc0ee28dc72a4) Advance ordered quest enforcement
- [44de364](https://github.com/NextGenSoftwareUK/OASIS/commit/44de36428e18783e11ad8bebe59a0d64ae2ea960) Deploy quest objective order contract
- [a377c5b](https://github.com/NextGenSoftwareUK/OASIS/commit/a377c5bda44d1dafd760c564d1f48a67aed61f01) Advance ONODE Core quest progression fix
- [80f2d2b](https://github.com/NextGenSoftwareUK/OASIS/commit/80f2d2b769ac37eb2f7b9b5b012c1554887ead64) Deploy GeoNFT eligibility routes and add targeted Anorak reset workflow
- [38d7e59](https://github.com/NextGenSoftwareUK/OASIS/commit/38d7e599359ad361376fe5c112512f5375048f5f) update sub module
- [ca43f88](https://github.com/NextGenSoftwareUK/OASIS/commit/ca43f88546e5b82d8c1a492614eb1dd09750da13) REMOVED OLD FILES
- [05f014d](https://github.com/NextGenSoftwareUK/OASIS/commit/05f014dad890866250e58484e4ec1b95aee79ff8) Deliver canonical quest events to Our World
- [e0a21be](https://github.com/NextGenSoftwareUK/OASIS/commit/e0a21bea633da69f1cf358a26c5bd34b112b64a4) Preserve failover diagnostics in WEB5 quest responses
- [efbcb26](https://github.com/NextGenSoftwareUK/OASIS/commit/efbcb26ba44f78969269710eae996f55635289cc) Revert "Restrict default WEB5 provider routing to Mongo"
- [02b8baf](https://github.com/NextGenSoftwareUK/OASIS/commit/02b8bafffe521b4fcb0cc7929578c983915ce39c) Restrict default WEB5 provider routing to Mongo
- [13e75e7](https://github.com/NextGenSoftwareUK/OASIS/commit/13e75e790660faa580f966cd83ff7dd9e1cb7d77) Audit holon persistence providers
- [b1b8740](https://github.com/NextGenSoftwareUK/OASIS/commit/b1b874087bb5f9d094a33b17ddacb719a23a415f) Preserve creation audit data in Neo4j holon saves
- [d14426a](https://github.com/NextGenSoftwareUK/OASIS/commit/d14426add2e86983453e4d562e2a093a31a12254) Use public holon IDs for SQLite saves
- [fe530e1](https://github.com/NextGenSoftwareUK/OASIS/commit/fe530e1577e764f5a299891a927e3c6d36e3672e) Pin verified STAR API references and Postman collection
- [2ae5b03](https://github.com/NextGenSoftwareUK/OASIS/commit/2ae5b03ac2beb3c1aaa28c2996a649aac61972ef) Extend holon lifecycle validation to game and mission APIs
- [45d2b47](https://github.com/NextGenSoftwareUK/OASIS/commit/45d2b479719cd842c50c2ab88fc2f5e6b063b0d8) Enforce public GUID holon persistence across APIs
- [b753d3f](https://github.com/NextGenSoftwareUK/OASIS/commit/b753d3f54e7e71b5885f3d3c8e2b8ff164427358) Preserve holon creation audit data across GUID saves
- [2dfc1a9](https://github.com/NextGenSoftwareUK/OASIS/commit/2dfc1a95bd5b4722a13c1e37dfbed3c3816abd2c) Resolve Mongo holon persistence by public identity
- [885c0a2](https://github.com/NextGenSoftwareUK/OASIS/commit/885c0a273f7d09b04123eef274d071b3eb9b6ead) Make holon persistence lifecycle explicit
- [7ce2118](https://github.com/NextGenSoftwareUK/OASIS/commit/7ce21183090f208cac33ea90681923cfd89c550d) Use create lifecycle across STAR API entry points
- [ea47901](https://github.com/NextGenSoftwareUK/OASIS/commit/ea479016efb7d21e01f6b43b79c647cfc378e48b) Create API quests as persisted STAR holons
- [f33f0b0](https://github.com/NextGenSoftwareUK/OASIS/commit/f33f0b03672623c2d389e657eb9310cddfbbde43) Keep encrypted STARNET quest identities queryable
- [e128f09](https://github.com/NextGenSoftwareUK/OASIS/commit/e128f0963224f12af8fbdd967ebe45895787da24) Persist WEB5 quest seeds reliably
- [c43d6dd](https://github.com/NextGenSoftwareUK/OASIS/commit/c43d6ddcb2b038c73173d1a550738aef6fe378ea) Deploy shared JWT runtime invariant
- [7f0b15a](https://github.com/NextGenSoftwareUK/OASIS/commit/7f0b15ae7e6179db1ed4daea75e403645aeac1c8) Deploy aligned WEB5 JWT runtime
- [558fda2](https://github.com/NextGenSoftwareUK/OASIS/commit/558fda25aeb626b00e3337c37b91d4ddb7231027) Align production dependency graph after main promotions
- [905fdac](https://github.com/NextGenSoftwareUK/OASIS/commit/905fdac918ac7ba837cec930d2b160cda3c2dfbf) Align master submodules and manifest to main
- [4741329](https://github.com/NextGenSoftwareUK/OASIS/commit/474132908f498e740c9897399fe8353636f61ed0) Pin coherent Railway dependency graph for WEB4-WEB10
- [63c69f8](https://github.com/NextGenSoftwareUK/OASIS/commit/63c69f8c1575a2cfe6e623e42586cdd06d029426) Share quest completion transitions across games
- [fca4ce1](https://github.com/NextGenSoftwareUK/OASIS/commit/fca4ce1f2cbd4affc833bbbe3464cf032ba0cd95) Add API-backed Anorak GeoNFT quest flow
- [e2eb3a0](https://github.com/NextGenSoftwareUK/OASIS/commit/e2eb3a0c01fd46e71ba84a99ec208a7bb475ac44) Update quest authoring API and media event contracts
- [c1c1650](https://github.com/NextGenSoftwareUK/OASIS/commit/c1c165016148f40372dabcf3879f50a0daeee7f7) Validate creator ownership and preserve GeoNFT placement coordinates
- [27a3fc0](https://github.com/NextGenSoftwareUK/OASIS/commit/27a3fc0fa0ac49fa4b10ada9ee4ccdce8fc4770e) Update API core nature karma rewards
- [cd568a0](https://github.com/NextGenSoftwareUK/OASIS/commit/cd568a041fe4c72882807093ebd2c5b85646b50a) Update API core for nature karma type
- [76cc176](https://github.com/NextGenSoftwareUK/OASIS/commit/76cc176a5299b397e1f79b111dccc5c82a32d0bf) Default game clients to hosted development APIs
- [0dd1ec5](https://github.com/NextGenSoftwareUK/OASIS/commit/0dd1ec5084ab93ef886a47a601293b4b0911d9de) Complete shared inventory runtime integration
- [50986a9](https://github.com/NextGenSoftwareUK/OASIS/commit/50986a9cc5093a4f41bfea3878c328345aa2b8b0) Model GeoNFT identity independently from inventory category
- [6850b8e](https://github.com/NextGenSoftwareUK/OASIS/commit/6850b8e6e3b107c7bd9656f8575f94622eaf76fe) test(ONET): add 4 integration tests — authenticated PING, rejection, NodeId stability, peer cache
- [e5aeedf](https://github.com/NextGenSoftwareUK/OASIS/commit/e5aeedf57834f478569dea52d0f8aa218ba95135) chore: bump ONODE.Core pointer — ONET Holon-backed state persistence
- [5f2b1a0](https://github.com/NextGenSoftwareUK/OASIS/commit/5f2b1a09c93de4099fd84fa8e68e4bc9aab04c5e) chore: bump WEB6 pointer — README provider/tool count corrections (99 providers, 259 tools)
- [1088c81](https://github.com/NextGenSoftwareUK/OASIS/commit/1088c81c21930303ca726ddc36bf55b10e634e7c) chore: bump ONODE.Core pointer — ONET DNA init + PKCS8 key export fix
- [60a5ae5](https://github.com/NextGenSoftwareUK/OASIS/commit/60a5ae508a5184649446e1941f62197c4a7f3116) chore: bump submodule pointers for OASIS-API-Core, ONODE.Core, WEB6, OASIS Hub
- [18e82fb](https://github.com/NextGenSoftwareUK/OASIS/commit/18e82fb42d2786512785e3b397dd4be46ff466e7) feat: add FhenixOASIS provider (FHE L2 blockchain) — provider 173
- [c3040a4](https://github.com/NextGenSoftwareUK/OASIS/commit/c3040a4e9e6038e3b722819423f09c1461984244) feat(herzid): bump submodule pointers for HerzID Phase 2 implementation
- [03198a8](https://github.com/NextGenSoftwareUK/OASIS/commit/03198a87d8d8b533f6fefa8e24516ec42b496dd7) feat(herzid): implement full HerzID Phase 2 — controller, services, DI registration
- [ae85936](https://github.com/NextGenSoftwareUK/OASIS/commit/ae859361e15d8004d320950d2024a072514446e0) feat(herzid): add HerzIdSettings to OASISDNA and default DNA config
- [5373c8c](https://github.com/NextGenSoftwareUK/OASIS/commit/5373c8cbf4be09eca0ac8d1284ce32d78df36fa2) fix: resolve all build errors in 11 new OASIS providers
- [f2e3f1f](https://github.com/NextGenSoftwareUK/OASIS/commit/f2e3f1f3474cb8c53d78f61ad9d5d43046701630) test(ONODE): fix 11 failing unit tests; bump ONODE.Core submodule pointer
- [941f954](https://github.com/NextGenSoftwareUK/OASIS/commit/941f954ed888995bf77d7289b626693794cf8230) feat(herzid): add OIDC/OAuth 2.0 provider endpoints for HerzID white-label SSO
- [a2c3471](https://github.com/NextGenSoftwareUK/OASIS/commit/a2c34719f2e90fc7bf44eebd7c46ea3aab63ef75) feat(herzid): add OidcSettings to OASISDNA for HerzID SSO configuration
- [00a1483](https://github.com/NextGenSoftwareUK/OASIS/commit/00a148334ad7e4e8fc45f8b4385e7c3fdaa87937) feat: add 11 new OASIS providers — Maps (4), Blockchain (2), Network (3), Storage (2)
- [8760040](https://github.com/NextGenSoftwareUK/OASIS/commit/8760040202219baa6571df68e0b8b31df890a6c5) Bind GeoNFT collection requests
- [c2c2727](https://github.com/NextGenSoftwareUK/OASIS/commit/c2c27278da310033f391d62cda3438117c4c3489) Update STAR ODK submodule
- [96bcea4](https://github.com/NextGenSoftwareUK/OASIS/commit/96bcea48edb4522759c62faf3f52535ef68cecbe) Seed development GeoNFT map demos
- [d7094aa](https://github.com/NextGenSoftwareUK/OASIS/commit/d7094aace0a2ef55ca25a375869715f06f596274) revert: restore NIXPACKS builder in railway.json
- [4637e3d](https://github.com/NextGenSoftwareUK/OASIS/commit/4637e3d8e7d59506544148738897a57ae8adccf4) fix(railway): switch builder from NIXPACKS to DOCKERFILE using Docker/Dockerfile.web4
- [6523dbc](https://github.com/NextGenSoftwareUK/OASIS/commit/6523dbcecfdc84faf97a39156df25695091b119d) fix(build): inline VerifyAvatarAsync and IsEmailConfigured — Railway uses NuGet fallback not submodule
- [2efb589](https://github.com/NextGenSoftwareUK/OASIS/commit/2efb589d3520b1c5f031d9c5effb114a8ed7f4c4) chore: sync submodule pointers and update test harness
- [c0d839d](https://github.com/NextGenSoftwareUK/OASIS/commit/c0d839d46156c2843b0bd3a06c0583c65925e75c) chore: bump OASIS.API.Core submodule (restore VerifyAvatarAsync + IsEmailConfigured)
- [13edc84](https://github.com/NextGenSoftwareUK/OASIS/commit/13edc8402399bcbf3e4d3eee93ea072b3dfff33a) chore: bump OASIS.API.Core submodule (deterministic settings holon ID fix)
- [f143472](https://github.com/NextGenSoftwareUK/OASIS/commit/f1434725335d9c6334a21b10f28fced2f3625037) Update STAR ODK
- [3a5dd5b](https://github.com/NextGenSoftwareUK/OASIS/commit/3a5dd5bfb49c2b078fc26605c25809787f169180) Sync STAR ODK submodule to Development
- [8891307](https://github.com/NextGenSoftwareUK/OASIS/commit/8891307c803a2d2798899149a2af8f5854de3500) Ignore local build and API state artifacts
- [0076e37](https://github.com/NextGenSoftwareUK/OASIS/commit/0076e37304d4aaf9df0bcb574e0a490844084d22) Fix local API Kestrel port defaults
- [494e3df](https://github.com/NextGenSoftwareUK/OASIS/commit/494e3df9150c9ddc4c301f9f8c118f113452f4fc) fix(webhook): proceed with null stripeEvent when SDK deserialization fails; handler uses raw body
- [7e808d1](https://github.com/NextGenSoftwareUK/OASIS/commit/7e808d1975a4eadd7f3b00fb90e655e80e49544e) diag: return webhook handler diagnostic in response body
- [7fbde39](https://github.com/NextGenSoftwareUK/OASIS/commit/7fbde39f9d58141dc58a3451b53c47f7f140e1c5) fix(webhook): remove early-return on null Data — always process via raw JSON
- [e02a03a](https://github.com/NextGenSoftwareUK/OASIS/commit/e02a03a2334cf533138a4c6f47713661ec7a9d99) fix(webhook): catch NRE from Stripe.net EventConverter and fall back to raw JSON parsing
- [c159a1a](https://github.com/NextGenSoftwareUK/OASIS/commit/c159a1a74dd8cc0b3bcd625f8f3eae77a272c59a) fix(webhook): always fall back to raw JSON for metadata extraction
- [3df46e3](https://github.com/NextGenSoftwareUK/OASIS/commit/3df46e31ab03f378caceac50b9e2f93ed7979882) docs: bump Provider-Summary last-updated date; bump STAR ODK submodule pointer
- [f273259](https://github.com/NextGenSoftwareUK/OASIS/commit/f2732591f52aab7b4e20f759438b7c7f2494df92) test(harness): fix banner and CheckEnv to show test-token mode
- [6f46463](https://github.com/NextGenSoftwareUK/OASIS/commit/6f464639b59301dc370c514d5794b46c11b77f2c) test(harness): support X-Webhook-Test-Token bypass to avoid webhook secret mismatch on Railway
- [3f7f123](https://github.com/NextGenSoftwareUK/OASIS/commit/3f7f123713362e332e795024091d99fd3fa39e84) fix(webhook): use raw body JSON fallback instead of RawJObject SDK property
- [d21bef5](https://github.com/NextGenSoftwareUK/OASIS/commit/d21bef5d7be534229b66eb414e3921dc5b820cdb) fix(webhook): add raw JSON fallback for checkout.session.completed and subscription.deleted
- [1a53a42](https://github.com/NextGenSoftwareUK/OASIS/commit/1a53a4282e713184ecc2ac7febf6c67d617e87c6) test(subscription): end-to-end harness tests real portal signup flow
- [4ceb2d0](https://github.com/NextGenSoftwareUK/OASIS/commit/4ceb2d03f0e0aa5ba17472ae79eae0f3bb9b4837) test(subscription): add unit tests, integration tests and expanded test harness
- [8e7a7cd](https://github.com/NextGenSoftwareUK/OASIS/commit/8e7a7cd2f17803f461cb76c00719ced808dc667c) chore: bump ONODE.Core and STAR ODK submodule pointers
- [2fd58c9](https://github.com/NextGenSoftwareUK/OASIS/commit/2fd58c9928e598621a1f46fba03b743598b56d05) refactor: consolidate the three ArweaveOASIS copies into one, under Storage
- [174e1a5](https://github.com/NextGenSoftwareUK/OASIS/commit/174e1a5cef0ca0d1c3c482e3bf4e0bff3f1066a0) fix: Development declared branch = main for half its submodules
- [ca0a26a](https://github.com/NextGenSoftwareUK/OASIS/commit/ca0a26a3598da0b74efc2804d39efd5e1b9b62f7) test: add the missing unit test, integration test and harness for every provider
- [71be1c0](https://github.com/NextGenSoftwareUK/OASIS/commit/71be1c048e8b75b3c1a01096f7a039072c071382) docs: restore all split-lost review files with per-method verdicts
- [39e095a](https://github.com/NextGenSoftwareUK/OASIS/commit/39e095a71f5c0ca8475b0296ab67e3d386eab2c0) ci: fail the build when a project is in no solution
- [a19ca9f](https://github.com/NextGenSoftwareUK/OASIS/commit/a19ca9f1f90ec19a8925ae8148e835a831a34b3a) feat(onode-api): add load-by-id endpoints for Web4 NFT collections
- [258ffbe](https://github.com/NextGenSoftwareUK/OASIS/commit/258ffbe782999bbaefd6fab36fc8fb0522e48a61) fix: put every project in a solution and fix the bugs that exposed
- [1e61acb](https://github.com/NextGenSoftwareUK/OASIS/commit/1e61acb5867cb3d8d2d52b6f3e2aaa933c29baa1) docs: record the split-lost method review outcome — 96 superseded, 2 real
- [fa2d6b8](https://github.com/NextGenSoftwareUK/OASIS/commit/fa2d6b85e803e87b9d3d64202992dfee8cd2e023) feat(onode-api): flag responses that carry fabricated test data
- [bc21310](https://github.com/NextGenSoftwareUK/OASIS/commit/bc21310e8795b504163577db4ead1f982692c22c) feat(providers): add 11 analytical and RESP-compatible storage providers (161 total)
- [4092626](https://github.com/NextGenSoftwareUK/OASIS/commit/4092626898684c3db1cd53e8be668110a5f338e1) fix(submodules): point Development at each submodule's active branch
- [70de0a1](https://github.com/NextGenSoftwareUK/OASIS/commit/70de0a19575aa9b9928ab770353947c410c90eb0) fix: restore everything previously pruned; put the 98 split-lost methods back for review
- [1d7e4e5](https://github.com/NextGenSoftwareUK/OASIS/commit/1d7e4e55d36e060907624958db4769bdc531f480) docs: 150 providers / 65 storage; restore moved ONODE projects to the solutions
- [918812e](https://github.com/NextGenSoftwareUK/OASIS/commit/918812efd30f795e1029adf30d75bbeeabb5cb13) chore: bump Core submodule — durable messaging persistence
- [be4f8d2](https://github.com/NextGenSoftwareUK/OASIS/commit/be4f8d2469e06801b9d87f398ae57287c576e2d0) fix(providers): make all storage providers conform to OASISStorageProviderBase and add them to the solution
- [cc3f847](https://github.com/NextGenSoftwareUK/OASIS/commit/cc3f8472b67d6e7ae3b64dda0927009c9b2d1ec0) fix(api): address the external API health report findings (P0-P2)
- [f6b6f21](https://github.com/NextGenSoftwareUK/OASIS/commit/f6b6f213a18a7260764eda9eaa640f3308e66d2f) feat(onode-api): add GET /api/karma/activity avatar activity feed
- [163d017](https://github.com/NextGenSoftwareUK/OASIS/commit/163d0173b12b4135562dc687a933ee9552896be4) chore: bump STAR ODK submodule — search param normalisation
- [1ebc0f0](https://github.com/NextGenSoftwareUK/OASIS/commit/1ebc0f0a7ef8b47a2c47a7131f994ce6919771ba) feat(providers): add Redis, Memcached, KeyDB, OpenSearch, Algolia, Solr, ArangoDB, QuestDB, CouchDB, and Xata OASIS storage providers with full unit/integration/harness test suites
- [ff8ba9b](https://github.com/NextGenSoftwareUK/OASIS/commit/ff8ba9b6812b9294f5e437295d35396cac9d94f0) fix(webapi): JsonStringEnumConverter allowIntegerValues:true — accept both string and int enums
- [5928472](https://github.com/NextGenSoftwareUK/OASIS/commit/5928472d3af2ce774bd874868148079a76bfdbd0) fix(webapi): JsonStringEnumConverter with allowIntegerValues:true — accept both string and int enums
- [f9f940b](https://github.com/NextGenSoftwareUK/OASIS/commit/f9f940b1a5d372b79cc44842cbf48bef6512b7bb) update submodules
- [a8424bd](https://github.com/NextGenSoftwareUK/OASIS/commit/a8424bdd55dce7d7ab7677780d6c0ec733fbbdeb) feat(webapi): accept both string and integer for ALL enums in JSON (JsonStringEnumConverter)
- [cd2a65c](https://github.com/NextGenSoftwareUK/OASIS/commit/cd2a65cd3fd86a57173c4643d30784880971a09f) fix(webapi): add JsonStringEnumConverter so string enum values (e.g. HolonType='Custom') deserialise correctly
- [1b07310](https://github.com/NextGenSoftwareUK/OASIS/commit/1b0731062f84fb2398ca37a099a7658d271a7759) feat(providers): add ScyllaDB, Litestream, MinIO, Cloudinary, PouchDB, Meilisearch, Typesense, and ClickHouse OASIS storage providers with full unit/integration/harness test suites
- [250e32b](https://github.com/NextGenSoftwareUK/OASIS/commit/250e32ba88873084a7c4ab0098b26098ec3b0576) feat(providers): add Qdrant, Weaviate, InfluxDB, TimescaleDB, Elasticsearch, DynamoDB, Couchbase, and Upstash OASIS storage providers
- [9ff41a0](https://github.com/NextGenSoftwareUK/OASIS/commit/9ff41a0c69867cc0abffdc4fb2c3d0b23b5d1ad8) feat(providers): add CockroachDB, Neon, SurrealDB, RavenDB, Cassandra, CloudflareD1, Convex, and Fauna OASIS storage providers
- [75f936f](https://github.com/NextGenSoftwareUK/OASIS/commit/75f936f2f4ec5d1ccc4a8d49dcd905ceef1c6a53) feat(providers): add PocketBase, Turso, Appwrite, PlanetScale, OrbitDB, and GUN OASIS providers
- [4507b62](https://github.com/NextGenSoftwareUK/OASIS/commit/4507b62b06a65c59ee14a7f69a5e19fbcd6b0f92) feat(providers): add PostgreSQL, Firebase, Supabase, and Cloudflare KV OASIS providers
- [c9da803](https://github.com/NextGenSoftwareUK/OASIS/commit/c9da803b40a72fca998aa27a53e7df3774f0bf76) chore: update submodules — CustomHolonType in core + mongo
- [6ac205c](https://github.com/NextGenSoftwareUK/OASIS/commit/6ac205c2306eb08264f259b676a7c196ee5c2ee1) feat(mongo): persist CustomHolonType on Holon entities and map in DataHelper
- [cd951b6](https://github.com/NextGenSoftwareUK/OASIS/commit/cd951b6065b8ef98a1b27e5766f3c07563c53c09) chore: update OASIS.API.Core submodule (Custom HolonType)
- [f68f0da](https://github.com/NextGenSoftwareUK/OASIS/commit/f68f0da960761a52d83f792899cc436482ecc873) fix: log SaveSettingsAsync result in UpsertSubscriptionAsync to diagnose webhook save failure
- [071269b](https://github.com/NextGenSoftwareUK/OASIS/commit/071269bf8862e97fffd6bf1ede556a1ff83d8b04) fix(avatar): bump OASIS.API.Core submodule — MetaData persisted on AvatarDetail update
- [4b7fa5d](https://github.com/NextGenSoftwareUK/OASIS/commit/4b7fa5de6e2bf9e2f8b2db3fcc9ad5b42fd8efc5) fix: upgrade Stripe.net to 46.3.0 to support API version 2026-04-22.dahlia
- [be04210](https://github.com/NextGenSoftwareUK/OASIS/commit/be04210783e0426a5a43c0e2154ff4146919115c) chore: update submodule pointers (OASIS Architecture, STAR ODK, WEB6) and csproj
- [f81995a](https://github.com/NextGenSoftwareUK/OASIS/commit/f81995a5cfb3cde15176a8f5b3c0c486dd02838a) feat(avatar): expose all fields in UpdateRequest and wire them through all three Update controller methods
- [1d9156e](https://github.com/NextGenSoftwareUK/OASIS/commit/1d9156ec23c5b3974b7a1557f8e2fd12386275fb) fix: disable Stripe API version mismatch exception in webhook handler
- [a9d0247](https://github.com/NextGenSoftwareUK/OASIS/commit/a9d0247f150358f9d9867df658dc501c1d768149) updated submodules
- [1c575b7](https://github.com/NextGenSoftwareUK/OASIS/commit/1c575b7ec55f6427ce0fab7e979d0f52308cb18d) chore: update STAR ODK submodule (STARDNA csproj fix)
- [cd1c807](https://github.com/NextGenSoftwareUK/OASIS/commit/cd1c807efeb2584c3a1bf63791e9e4a22d7c0bfc) feat: add Stripe section to Default OASIS_DNA.json under SubscriptionConfig
- [280c9e9](https://github.com/NextGenSoftwareUK/OASIS/commit/280c9e9e1701d617affc4c82feebd1fa3a981bfb) refactor: move StripeSettings into SubscriptionConfig; add appsettings.Development.json placeholders for Stripe env vars
- [3149d2c](https://github.com/NextGenSoftwareUK/OASIS/commit/3149d2c3ade164dec9079295b74b24af06871fbb) refactor: replace auto-create Stripe prices with explicit Price ID config lookup via env vars/OASISDNA
- [a34e097](https://github.com/NextGenSoftwareUK/OASIS/commit/a34e097c5d0ae8c07541d656c2158f8ab41c24e6) feat: add 7 new providers (102 total); update all docs, websites, investor IP docs with 102+ count and WEB6 AI/orchestrator details
- [5a22fc5](https://github.com/NextGenSoftwareUK/OASIS/commit/5a22fc5fa716f2809a5684f1d4e0cd3d209a86a9) docs: update all provider counts to 100+; add Provider-Summary.md; enhance investor docs
- [263b46c](https://github.com/NextGenSoftwareUK/OASIS/commit/263b46c431c6194bd1c24861151ac2d2881f996f) feat: bump WEB6APIVersion to 3.0.0
- [6fb81b2](https://github.com/NextGenSoftwareUK/OASIS/commit/6fb81b2b06704542012c3b3caf0343a7c88fb57b) fix: replace all stubs in OracleDBOASIS and SQLServerDBOASIS with real ADO.NET implementations
- [1a6d2a5](https://github.com/NextGenSoftwareUK/OASIS/commit/1a6d2a5783fb91d3a8f751259794de71610991f0) feat: complete all 13 unpublished NuGet providers to publish-ready standard — READMEs, release notes, logo, metadata, copyright
- [6514694](https://github.com/NextGenSoftwareUK/OASIS/commit/65146947877294ca54a21a6ac73dddacee6cc8ab) fix: delete BerrachainOASIS (typo duplicate), fix BerachainOASIS ProviderType enum ref, clean up solution and Startup.cs
- [9b253a5](https://github.com/NextGenSoftwareUK/OASIS/commit/9b253a5bcf90d57f0a659cac2e158edd6a2e4dcd) feat: fix OrionProtocolOASIS and OnionOASIS builds; add spatial/gaming providers; update docs
- [d8dc029](https://github.com/NextGenSoftwareUK/OASIS/commit/d8dc029673b3024ba4503503f2c2c17d03b4e527) feat: fix SQLServerDBOASIS and OracleDBOASIS builds; add MoralisOASIS provider
- [7331508](https://github.com/NextGenSoftwareUK/OASIS/commit/7331508f029ba28fa805c2735e0c219ec554fd22) feat: add 5 new OASIS providers — zkSyncOASIS, ScrollOASIS, LineaOASIS, MonadOASIS, AzureStorageOASIS
- [eff8bfa](https://github.com/NextGenSoftwareUK/OASIS/commit/eff8bfa8f65aef233d74f32448dc389e9ee37639) docs: add AbstractOASIS, BerachainOASIS, StellarOASIS to Swagger, roadmap and Providers.tsx
- [783d161](https://github.com/NextGenSoftwareUK/OASIS/commit/783d1617d98ec117cd6d8fbd39f65ab68a71581d) feat: add 3 new OASIS providers — AbstractOASIS, BerachainOASIS, StellarOASIS
- [2e08a04](https://github.com/NextGenSoftwareUK/OASIS/commit/2e08a0437fca595150b35c20822d3f5d1216daa3) docs: add 12 new OASIS providers to Swagger, Provider-Roadmap and Providers.tsx
- [c1fbb3b](https://github.com/NextGenSoftwareUK/OASIS/commit/c1fbb3b9f6c6a27c4545d07bd28d74c991ebb9a1) feat: add 12 new OASIS providers — SeiOASIS, CelestiaOASIS, EclipseOASIS, PushProtocolOASIS, ENSOASIS, AlchemyOASIS, InfuraOASIS, SafeOASIS, TablelandOASIS, WakuOASIS, LivepeerOASIS, AkashOASIS
- [bed462e](https://github.com/NextGenSoftwareUK/OASIS/commit/bed462edb89a98e7d06d1ed442d0abdf12b73e34) feat: add 5 new OASIS providers — Discord, TheGraph, WorldID, LitProtocol, StoryProtocol
- [b2c2bf2](https://github.com/NextGenSoftwareUK/OASIS/commit/b2c2bf24e421fcbff3fef1a6b424582fb0f7c208) docs: add swagger descriptions and roadmap entries for 6 new providers
- [83588cc](https://github.com/NextGenSoftwareUK/OASIS/commit/83588cc5dfd249015b6f9a8a2c78c7f23372d343) feat: add 6 new OASIS providers — BlueSky, Matrix, Filecoin, Algorand, Ceramic, Basechain
- [a300b01](https://github.com/NextGenSoftwareUK/OASIS/commit/a300b01fb396d61ba5f2a3e8f4495e9efe3d9d65) feat: add LoomOASIS provider — Loom video messaging platform

</details>

<details>
<summary>Complete commit inventory (52 distinct changes)</summary>

- [7e60cd1](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/7e60cd1781d7e38b563faae1a818619b3c816ceb) Document WEB5 API 3.2.0 release
- [4f64344](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/4f64344a6769befb78cbccd93ee0717009917c69) Correct MCP tool count in STAR history
- [12591d1](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/12591d1407b0d70311924246a087aa1d3b2161bc) Use AutoMapper integrated service registration
- [47646df](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/47646dff171d60e7ac7f66be342c7c5e931d368b) Patch JSON runtime vulnerabilities
- [c4ffc79](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/c4ffc790fca771f89613b518046495a4eda10811) Patch GraphQL parser vulnerability
- [0c7d1a4](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/0c7d1a4a04a32a1acefe5fadba5e113cacf871c2) Use tracked default DNA in integration tests
- [7e79622](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/7e7962272cea1be6998b198751a18db238a9505b) Use shared subscription usage contracts
- [1d8e5f2](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/1d8e5f27b01bc72b4a77d7f428162b11bf066bfa) Fix packaged STAR DNA default path
- [bf68af9](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/bf68af940486843150e1dee085e20fec03607182) Wire STAR quest lifecycle synchronization
- [e715f99](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/e715f9965f8d7d2db6e71fac39d41a961d03a104) Gate WEB5 subscription ledger activation
- [f885da9](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/f885da928c02618c459fd7f2de1f5940295bb326) Add [Authorize] to all Web5 STAR WebAPI controllers
- [45c33bd](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/45c33bd9ebabd3a6bd79c9cb0f25d835f6e5c206) Migrate WEB5 to durable WEB4 usage execution protocol
- [9ef3b13](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/9ef3b135bed6453a7a96ab5e202cbd8fd4f99ba1) Delegate WEB5 subscriptions to WEB4
- [1c60a03](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/1c60a03d396b22cc99893443c9c53c890e89da23) Revert "Grant configured enterprise avatars unlimited access"
- [dc259aa](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/dc259aaaf931df866d2e255388f11322cc6c7fd1) Grant configured enterprise avatars unlimited access
- [9833ca0](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/9833ca0b05909f0c046934e560369ddfc6a1b520) Fix GeoHotSpot creation persistence
- [8d4669e](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/8d4669e381b0009c1236878d6420309a8abe3099) chore: add __pycache__ and *.pyc to .gitignore
- [f0f9540](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/f0f954028ba476fe308947e707937f7509c8d828) chore: propagate OASIS_DNA security config to STAR ODK DNA files
- [1578418](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/15784182908bb9ff2b61a591cea3f3d7bddf79f1) Test durable GeoHotSpot journal recovery
- [c0517f9](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/c0517f918975a2326fbec87d336d9b49853441a0) Add resumable GeoHotSpot trigger transactions
- [038d615](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/038d6154fdf48ea5268fe8a0b854762971b59214) Validate GeoHotSpot creation contract
- [00f6ab9](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/00f6ab9e4854f9f004d926e550aa51227050aa16) Complete GeoHotSpot eligibility and quest contracts
- [1cdb4ee](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/1cdb4ee0225986be40454051bfc4ff56b37b290b) Reject inactive and invalid GeoHotSpot evidence
- [66958bb](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/66958bbbbd743785ee1505f0eadcbc23c4ec1a80) Apply GeoHotSpot quest and reward effects
- [5532b61](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/5532b61dc84064ecb1493ffe822ff7a6db8be0d1) feat: add authoritative GeoHotSpot trigger endpoint
- [af9f1e8](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/af9f1e8cb0c9a1aca71fa651e7134cc7cbf649c8) docs: clarify WEB4 and WEB5 GeoNFT support
- [ea305fb](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/ea305fbd8e8e67d790a18a24f20ffa0b5854cb4e) feat: define GeoHotSpot trigger contract
- [be1a721](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/be1a721183388f7e5a45c50a2cc0519d0871a468) fix: assign quest creator before persistence
- [96eb13a](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/96eb13a5470c6cb8fb880799391125a71f638c10) docs: document WEB5 objective ordering contract
- [903a1fa](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/903a1fadbabbec7df7f9ab8b0e12e2a52a73ae34) Test ordered objective selection
- [2737bea](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/2737beaa35d55907396341b464543add17be5df6) Expose quest objective completion order
- [dcec195](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/dcec1955560ec4cd5d31ca3401f30ae62fa7a66b) Return quest progress events from inventory sync
- [445c51d](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/445c51d7548e4b8e89b0bb990e12b71a2ec81ad2) Return provider failure details from quest API
- [fbef69b](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/fbef69b95b030c4336e5cb21b2a70635de7f25ef) Use STAR create lifecycle in API creation callers
- [e5f504b](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/e5f504b8b9b1c7c5206e843499d37096cc919f1a) Create API quests through STAR create lifecycle
- [2da8e79](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/2da8e79342ebb345884b028b47b90b3ca8403b26) Test STARNET identity metadata encryption contract
- [c53a715](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/c53a7154166a8b7f25365ed57dee1456b0eb25c1) Persist quest creation identity before save
- [56fe382](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/56fe3825f6ade83b49252ec23fed3bbbf2b651b4) Consume shared IdentityModel package invariant
- [70e2cb6](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/70e2cb607526c17ae54b4353e02b86a3582892fc) Align WEB5 IdentityModel runtime packages
- [9c6976f](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/9c6976f464978f8841e4c26d45a4de9a4627b239) Reconcile GeoNFT inventory with quest objectives
- [bd40ee6](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/bd40ee6843e3a339c3f078c0c57d3e853ea8f666) Bind authored quests to concrete API model and test creator payload
- [6cd0aeb](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/6cd0aeb256f0fe6e2d3651d96b1eaaa9a495404e) Default OAPP builder to hosted STAR API
- [32df869](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/32df869c68033e5c14c9d26e6843ef953ebb9347) Ignore generated STAR build artifacts
- [289a185](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/289a1857315cab3bef255ee985fec84fb61a1eb5) Fix WEB5 local Kestrel port
- [781acec](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/781acec99383236722d92084411f22d7416c1294) fix(star-webapi): boot OASIS at startup so the GetAll endpoints stop returning 400
- [4cef259](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/4cef2595276f43b61ed0ceb1fa426aa6ed44771a) fix: make the OAPP Console DNA templates buildable
- [78ca304](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/78ca30468abb8aa418fed8327ca816c0728d0651) fix(cli): correct the ONODE project path and guard the gitignored OASIS_DNA.json
- [3f8da4e](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/3f8da4edbbac0e9fd807f947455da44b55f2173f) feat(star-api): accept both searchTerm and query on all search endpoints
- [b1b7e5c](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/b1b7e5c8be718dbd6088e5f8f94aee23cfdc5a03) Update .gitignore
- [cc75c3e](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/cc75c3e2d742e79a9f0066c5404295b83beac2bc) updated provider counts
- [b73a4b6](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/b73a4b6a1902a111119f654cb01e8be2bc9668d8) fix: make OASIS_DNA.json content copy conditional in STARDNA csproj
- [207c5dc](https://github.com/NextGenSoftwareUK/STAR-ODK/commit/207c5dc650108e180515c356d459a50cd7bcd576) Create .gitignore

</details>

[Compare the complete OASIS parent range](https://github.com/NextGenSoftwareUK/OASIS/compare/a5657b3bd8eab2f307b69152c016c2b286ec4c08...8b3e8431d88e833ab3fb79d59602e7004a2ad040)

[Compare the dedicated STAR source range](https://github.com/NextGenSoftwareUK/STAR-ODK/compare/fa92ce8e6ec1e5e0a96e9f09875e4294c4126f04...5c53d7fb12f72bade7209fe3223618c8cf9da3c5)
----------------------------------------------------------------------------------------------------------------------------
## 3.2.0 (27/09/26)

- Unified HyperDrive v2, ONET and offline synchronization; cross-game inventory, GeoNFT and quest progress; API, provider, MCP, runtime and release-pipeline improvements.
- Published by the automated OASIS global release process after CI validation.
