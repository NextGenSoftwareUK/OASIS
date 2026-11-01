# Arweave public-source qualification

Updated 1 November 2026. Native CQL/API/hosted HTTP acceptance verified; browser gate remains open.

The existing Arweave provider is the candidate after Cardano hosted acceptance. Four bounded anonymous reads against https://arweave.net returned HTTP200: `/info` (292 bytes) identifies `arweave.N.1`; `/block/height/269512` (919 bytes) supplies a historical header. No account, wallet, database, transaction or source owner was created.

Evidence: `TestResults/ArweavePublicSourceCandidateCurrent/shape-observation.json`; info SHA256 `7cdf0be7c9c275ff41dc968ed9f1925117fff24feaff959a3e17ae4617d3bacf`; block SHA256 `ba1f75e157154733d85ffd1a8ed53d45249820e6347963223200b1fb0e51e668`. Raw responses remain outside Docs.

## Next qualification

- Establish one explicit historical wire-format contract and network identity before implementation. Numeric wire types vary by era: the observed difficulty fields are decimal strings, while pool/size fields are JSON integer tokens. Preserve exact values without floating-point conversion.
- Qualify three explicit headers and a two-key subset; validate requested identity, complete sets, deterministic ordering, bounded responses and malformed-input rejection. Do not download the full block index.
- Only then implement the shared CQL read-only path, native tests/demo, API admission, four presets, private Mongo preparation, isolation/reuse/replicas and hosted acceptance.
- Gateway-reported headers do not independently prove consensus or owned native writes. No CQL, HyperDrive or browser completion is claimed.

[Native HTTP contract](https://docs.arweave.org/developers/arweave-node-server/http-api). [Current status](CURRENT_STATUS.md). [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Three headers now observed at269512/269513/269514; consecutive previous_block links match retained indep_hash values. Additional responses:269513881bytes SHA256d7c4afe8f6d4f549ad9997bb67e5d0752ac9090055f5d7f7e36eeb6ca3a3c3b2;2695141020bytes SHA256bb93c22522fd70b150dcf2396248e61a2c22e30ad17c2bc0fd0fb0193c5870f2. selection-observation.json retains HTTP200 statuses. These are native height reads only; hash-subset/CQL/identity guards are still unproved.

## Native wire qualification

Three height responses and the two explicit hash responses now agree exactly; all IDs decode to48bytes, consecutive parent links match, difficulty/cumulative difficulty remain bounded canonical decimal strings, and numeric fields pass exact UInt64 checks. This fixes the candidate wire era to269510<=height<422250, matching the native HTTP manual. No general-era support or independent consensus verification is inferred.

`TestResults/ArweavePublicSourceCandidateCurrent/wire-qualification.json` records native3/2 with CQL=false. Hash responses: hash-269512 919bytes SHA256ba1f75e157154733d85ffd1a8ed53d45249820e6347963223200b1fb0e51e668; hash-269514 1020bytes SHA256bb93c22522fd70b150dcf2396248e61a2c22e30ad17c2bc0fd0fb0193c5870f2. Next: implement and test the read-only CQL adapter using this exact contract, then local manager/CLI before any hosted admission.

## Native CQL adapter verified

The existing Arweave provider now includes an explicit read-only public-header query profile.33controlled checks and1real native CQL test pass with0skips. Shared planner native3/2 selections preserve requested keys, exact values and reject malformed/network/era/identity/partial selections before paging. No chain scan, wallet, database or source write. The actual spaced console walkthrough also passes shared HolonManager/deferred LINQ3/2; build0errors. [Setup, scope and examples](../../Devs/ARWEAVE_PUBLIC_HEADER_CQL_GUIDE.md).
- TestResults\ArweavePublicSourceCandidateCurrent\cql-controls.trx: 33passed, 51822bytes, SHA2566dc1ac9df530c10ac50d4bc126c1009975b334efef744e531af706a906fca12e.
- TestResults\ArweavePublicSourceCandidateCurrent\cql-native.trx: 1passed, 2799bytes, SHA256aa8b77d87fd6658865993ff892d3333215db04a6295a933e88ad183c9b60bbf8.

Actual CLI evidence TestResults/ArweavePublicSourceCandidateCurrent/cli-native.log: 1678bytes, SHA256162eb90f37a545be25726877d52480761652ad6e54f50b6c778731b3f593e996. Source catalogue retained at LocalState/COSMIC/HostedArweavePublicSourceCurrent/source.json; preserve owner. Implemented query types40; hosted37/tariffs37/browser5/open32 unchanged. Next Core/API read-only admission, local HTTP/Mongo/privacy and actual prepared presets before hosting.

## API and local HTTP acceptance complete

Read-only admission, schema boot and actual four-preset planning pass: API3/3, actual candidate1/1, pure publisher3/3. Real local HTTP/Mongo1/1 proves native3/2, paired5/4, foreign-private0/public3 with2SDK-owned snapshots and9controlled billing receipts. This local fixture does not certify production billing or replication. Exact fixture marker/nonce/count checks preceded database removal; fixture.cleaned.json confirms absence and the runner-owned Mongo stopped/port27029absent.

Core5fd361db11f5d54e3e99282536d7c7be842edf86 pushed/ls-remote verified on task and Development; parent gitlink/manifest move together. Actual frozen shipping Arweave DLL contains the adapter; red TRON adapter is absent. Protected three-setting candidate/four presets are prepared, not applied. Hosted37/tariffs37/browser5/open32 unchanged. Next publish selected verified code, observe exact app SUCCESS, guarded apply once, one journalled settings deployment, then fresh hosted acceptance/card.
- TestResults\ArweavePublicSourceCandidateCurrent\api-admission.trx: 3passed, 6559bytes, SHA256f6b190101deb23febed737ea17b06b0eb5b99585c8b50cf5c646d75068f334b0.
- TestResults\ArweavePublicSourceCandidateCurrent\arweave-mongo-http.trx: 1passed, 3365bytes, SHA256c09f4d94e473387b5dc4620f57a2bceae7b18449f7b1cbc373309e33f6afebb0.
- TestResults\ArweavePublicSourceCandidateCurrent\actual-prepared-candidate.trx: 1passed, 3335bytes, SHA256d420137b413d834eb40d5faff605775fe92c7f99acac5aab5fa7490955ac2416.

App releaseb158bda6134d14195cf99dc04695904395dac9b1/Core5fd361d published to Development after fresh ancestry/pin checks. Exact deployment73ec2804-4e32-48de-b367-4ef750a82e9a BUILDING. Observe this job without redeploying. After SUCCESS keep HEAD=b158bda61; set COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT=b158bda6134d14195cf99dc04695904395dac9b1 and apply reviewed Arweave candidate once. Verify all per-key ACKs/whole candidate before one journalled settings redeploy. Checkpoint docs intentionally pending until guarded apply; no host setting applied.

Frontend task4f22ac3 stages the spaced Arweave local command and guide/C# links pinnedb158bda61; not published to main. Await original exact app job, then guarded settings and hosted acceptance before publication.

## Hosted settings rollout

App73ec2804 SUCCESS atb158bda61/Core5fd361d. Three reviewed settings applied once, individual ACK hashes and complete hosted candidate matched. One journalled settings deploymentec59010e-e01a-4606-b91c-371c79a11414 is BUILDING. Original arweave-public-rollout.intent.json/protected outcome retained; do not replay apply/redeploy. After exact SUCCESS run Verify-HostedQuerySource.py ArweaveOASIS --prepare once on its fresh durable cohort, then inspect ACKs/identities/versions/replicas/privacy and write completion ACK before card publication. Hosted37/tariffs37/browser5/open32 remain evidence counts until acceptance. Checkpoint docs may now be committed; exact apply guard is complete.

Settings jobec59010e is now REMOVED. Newer Development descendant37a530a87e601397f187430728bb9fb02b224f87 job3da7f986-6e65-4fcf-b8f8-4916e688a9b6 is BUILDING. Exact ancestry/diff confirms only ACTIONS-FOR-DAVID.md changed; Core/gitlink/manifest/code are identical to testedb158bda61. The complete hosted candidate still matches, so no settings/deployment replay. Observe this replacement; after SUCCESS accept using its exact release identity. Original removed-job evidence retained.

## Hosted acceptance complete

Deployment3da7f986 SUCCESS at37a530a87/Core5fd361d (identical tested codeb158bda61 plus David-list update). Original Arweave cohort proves native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3 and SQLite/Neo4j2/2; both private creates acknowledged version1, no pending intents. Durable completion ACK blocks replay. Two private snapshots retained for demo, no native writes. Hosted38/implemented40/tariffs38/browser5/open33. Publish staged card next; browser not inferred.
- TestResults\HostedCqlIntegrationCurrent\ArweaveOASIS-native-http.json: 15507bytes; SHA256dca50b8eaf4fc8274a0f001a4389b86cfccaa4d0a4560f440133bb795d02f5f2.
- TestResults\HostedCqlIntegrationCurrent\ArweaveOASIS-client-http-flow.json: 40733bytes; SHA25633802c3f6f3b2454f5dd175a3c099eb12b49bf5e0e5942b69cfbb53c69fdd075.
- TestResults\HostedCqlIntegrationCurrent\ArweaveOASIS-isolation-http.json: 6251bytes; SHA2563eead081c4727aebdda90208798a8918476a25e88f1b63c6548da7b96f74aef2.
