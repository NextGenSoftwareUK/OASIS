# Cardano public-source qualification

Updated 1 November 2026. Native CQL/manager/CLI, API admission/local HTTP/Mongo/privacy and actual presets verified; hosting pending. No hosted count grant.

- The [operator's introduction](https://www.koios.rest/guide/introduction.html) lists its Mainnet endpoint and points to the [operator's API artifacts](https://github.com/cardano-community/koios-artifacts) for specifications and SQL contracts.
- One anonymous Node24 GET to https://api.koios.rest/api/v1/genesis returned HTTP200, one record with networkid=Mainnet and networkmagic=764824073. This is an indexer-reported identity observation, not independent consensus proof.
- A proposed OpenAPI description request at /api/v1/ returned404. That path guess was wrong; it does not show a service-wide failure. No retry/fallback endpoint used.
- No wallet, account, key, installation, database, sourceOwner, native .NET guard qualification or mutation. No block catalogue or three/two-record query has been qualified.

Evidence in TestResults/CardanoPublicSourceCandidateCurrent:

| File | Bytes | SHA256 |
|---|---:|---|
| genesis.response.json | 7606 | 418ed31e2dfc84c6e7e47286b99158a204e73eeed9b4e902b426c0b2e032dee8 |
| openapi.response.json | 87 | 20bd41b63d2a9c4f9a1fbb3abf9378e95faa3c9711c2e9ac834d5e93e2477cc9 |

Next: read the documented block-selection/schema contract from the operator's specs, then qualify bounded historical header selections and native numeric/identity/status domains through the actual guarded .NET transport. No implementation/hosting admission until that succeeds. Preserve these original observations; do not install a node or replace endpoint based on the404.


## Guarded native header contract qualified

The actual Core8a5ab45 NativeConnectionGuard handler, loaded from its frozen .NET10 DLL in existing PowerShell7, returned HTTP200for genesis, three explicit historical block hashes and their two-hash subset. No SDK install, build, sourceOwner or mutation. Observed genesis identifies Mainnet/764824073; securityparam is the decimal string2160.

The catalogue comes from operator fixture commit2e8ad77681a29a9a3aca94617acacebbb85d8d59. Full response order differs from requested order, and subset order differs again. Complete unique key sets match exactly. Repeated stable header fields agree; num_confirmations is a per-response observation and must not be treated as immutable or consensus verification.

- Heights6946669/6946670/6946673; observed confirmations7198318/7198317/7198314. Integer tokens checked exactly without float conversion; fee/output fields remain optional decimal-integer strings, not rounded decimals or invented zeroes.
- Operator fixture download9392bytes, SHA256f305b74241c0773e285448b7510c825cf72030c8f1c8a7fb0f42a2897ecc9e10. Native genesis7606bytes; three2234bytes; two1489bytes. HTTP elapsed559/116/320ms in this observation, not a latency guarantee.
- Compact TestResults/CardanoPublicSourceCandidateCurrent/native-header-validation.json SHA2566dbfba1c91fec4b2c8aa0ddd5f5536badbb9607328facd79d6b7a285a6fb08af. Raw response hashes and selected public hashes are in that report; original404preserved.

Next implement the bounded read-only adapter/tests: fixed Mainnet endpoint and genesis identity, exact hash and integer domains, complete-set rejection for missing/duplicate/extra rows, deterministic requested-key ordering after full validation, explicit historical confirmation policy, nullable fee/output preservation, cancellation/result limits and no partial output. Then actual manager/CLI/API/local federation before hosting. CQL implementation remains absent; counts38implemented/36hosted do not increase yet.

Primary contracts: [block endpoints](https://github.com/cardano-community/koios-artifacts/blob/main/specs/fragments/paths/block.yaml), [response fields](https://github.com/cardano-community/koios-artifacts/blob/main/specs/fragments/schemas/block.yaml), [batch request body](https://github.com/cardano-community/koios-artifacts/blob/main/specs/fragments/request-bodies.yaml). These describe an indexer API, not independently verified chain storage.


## Native CQL and developer demo verified

CardanoPublicHeaderMetadataSource passes35controlled tests and one real native3/2test, all selected tests0skips. Actual commented C# console demo passes3/2through shared HolonManager/LINQ. Requested-key order is restored after complete-set validation. Missing/extra/duplicate/substituted rows, immature history, malformed hashes/numbers/amounts, cancellation, response limits and result budgets reject with no partial output. Exact30-digit lovelace and integers above2^53are preserved; absent/null/zero remain distinct.

Fixed Mainnet identity and2160minimum reported-confirmation profile remain separate from independent consensus/finality. No chain/storage writes, Mongo copies, account, wallet or local service. Stable LocalState/COSMIC/HostedCardanoPublicSourceCurrent/source.json403bytes retained; never regenerate its sourceOwner.

- Controls TestResults/CardanoPublicSourceCandidateCurrent/cql-controls.trx SHA25612d7a6cf4e8f4b4582a866ec7fc56c50cca6fbfaf54d91ed1f5d950f2be99509.
- Native cql-native.trx SHA256c5dfeb72ff5e39ac2649406102209d9ea78812e852e3aa8b91bbec58c0550eb1.
- Actual CLI cli-native.log SHA2562ccaea0ce1777d5f82c2c2666ab11c6d69f4d4c9b50936d7852ce51cf1da7d49; build0errors.
- Compact native-cql-milestone.json records result counts/sizes/hashes; no raw evidence committed to docs.

Next explicit read-only Core/API admission, four presets, local HTTP/Mongo federation/privacy and actual candidate checks before selected hosted rollout. [First-checkout setup and commented demo](../../Devs/CARDANO_PUBLIC_HEADER_CQL_GUIDE.md). Native candidate raises implementation inventory to39, but only36hosted and38other API/local query types; do not infer API admission from native proof.


## API/local HTTP and prepared candidate verified

Core read-only admission now includes Cardano with independent permission/tariff; signed/write permissions and canonical wallet DNA remain unchanged. API/schema3/3, real loopback HTTP/Mongo1/1, actual prepared-preset1/1and publisher3/3pass, zero skips. Native3/2, paired5/4, foreign-private0/public3and9controlled billing receipts. Two SDK-owned private copies removed after exact fixture marker/nonce/count checks; fixture.cleaned.json confirms DB absence and runner-owned Mongo stopped/port27029absent.

Three protected settings/four presets prepared without hosted mutation. Original stable source403bytes retained. Next select verified release excluding red TRON, verify shipping artifact/actual presets, publish, then exact guarded three-setting apply/one journalled deployment/fresh hosted acceptance/card. Never replay a fixture or uncertain write.

- TestResults/CardanoPublicSourceCandidateCurrent/api-admission.trx:6559bytes; SHA256dee7c17919c2a9753d8ba26977a781df96c703221261f6f761fb78e8ffe96875.
- TestResults/CardanoPublicSourceCandidateCurrent/cardano-mongo-http.trx:3365bytes; SHA2560d7cdc541fa72592e0b32650106587a9eb4df9751fff0b0f67948cae8e2dc06a.
- TestResults/CardanoPublicSourceCandidateCurrent/actual-prepared-candidate.trx:3335bytes; SHA256554867ed09e81fed9c5f1c1958aef9ead3c30fac06bba3c223e57931d8680387.
- TestResults/CardanoPublicSourceCandidateCurrent/cardano-mongo-native-summary.json:227bytes; SHA25660cf2a9833af5b490d1727eb150d122decf064612c0d3034d790f0205f4ae0b4.

Core task commitbe397f10419cf8b5c0dafc93ff1f4958b88588a2; parent manifest/gitlink updated together. Shipping release and hosted gates remain open.

Selected releasec9410b9b9a578c21bb928e494106934db0e1621b/Corebe397f10419cf8b5c0dafc93ff1f4958b88588a2 passes4/4shipping API/schema/actual-candidate checks,0skips. Actual rebuilt DLL metadata contains CardanoPublicHeaderMetadataSource and excludes unqualified TronPublicHeaderMetadataSource. selected-release.trx SHA256f908892922b195398f7f308c4276197f830476dec3e04da3af872b2e4f6a7f30; exact assembly hashes recorded in TestResults/CardanoPublicSourceCandidateCurrent/selected-release-artifact.json. Ready for scoped Development publication, then exact app/config/hosted acceptance. No broad claims from this shipping gate.

Application batchaffe5719070d7b149d165b901306708e7643fb97/Corebe397f10419cf8b5c0dafc93ff1f4958b88588a2: app1615af0d-5302-42a9-87c3-6285148a6a7b SUCCESS. Three reviewed settings applied once; per-key acknowledgements and exact full candidate verified. One journalled settings deploymentcfe35470-7e92-4ad5-bd53-3b77944df051 is BUILDING on authoritative observation. Preserve rollout intent/protected outcome; do not resubmit. After exact SUCCESS, execute fresh Cardano hosted acceptance once before granting hosted counts or publishing its card.

## Hosted acceptance complete

Settingscfe35470 SUCCESS at parentaffe57190/Corebe397f1. Original cohort native3/2, private Mongo+native5/4, repeat0new/2existing, foreign-private0/public3, matching SQLite/Neo4j2/2 and both acknowledged versions1 pass. Completion ACK LocalState/COSMIC/HostedQueryAcceptance-CardanoOASIS/cql-http-acceptance.acknowledged.json blocks replay. Two private snapshots retained for demo; no native chain write. Hosted37/implemented39/tariffs37/browser5/open32. Frontend publication next; browser not inferred.
- TestResults\HostedCqlIntegrationCurrent\CardanoOASIS-native-http.json: 15588bytes; SHA25692f22d90c9e4fbfd3b096abaaf630862c4925e1a2f081e00aef41c5dadd8a848.
- TestResults\HostedCqlIntegrationCurrent\CardanoOASIS-client-http-flow.json: 41139bytes; SHA256e64217681ae069562d3c128e79b87533c6317b074a8e7c647ec8918875a77af6.
- TestResults\HostedCqlIntegrationCurrent\CardanoOASIS-isolation-http.json: 6291bytes; SHA2560522a921d2e50566c7a284b3af53bd67227e7856c6dc2cb52a656becd8b9d0ee.

Website5d9845c published with guide64f448f84/C#affe57190. Public HTML200/153728bytes/SHA2564ebca2803c9c4739e6ac5a15043b7ca4b8feabf55731dd8868be192c6b59f81f exactly matches committed git blob; working checkout CRLF differs only in line endings. Compact site-publication.json retained outside Docs. Browser gate remains open.
