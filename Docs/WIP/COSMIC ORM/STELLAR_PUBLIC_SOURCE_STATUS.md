# Stellar public header readiness

Updated1November2026. Complete native decoder/CQL/C#/CLI and local HTTP/Mongo verified; hosting/browser remain red. XRPL hosted acceptance is complete within its recorded scope.

- Reuses the existing StellarOASIS Horizon endpoint https://horizon.stellar.org/ and Python stdlib; two anonymous bounded GETs, no account, SDK install or source writes.
- Root explicitly reports the public-network passphrase. A three-record ledger page has contiguous API-reported sequences and linked parent hashes.
- Canonical bounded base64 XDR hashes match native ledger hashes; fixed XDR parent-hash prefixes match the reported parents. Seven deterministic controls pass.
- This does not decode/validate complete LedgerHeader/variable StellarValue unions or bind every metadata field to XDR. Sequence/network are API-reported; SCP/validator consensus, transaction inclusion and full state proof are unverified.

## Retained evidence

- Original public catalogue: `LocalState/COSMIC/HostedStellarPublicSourceCurrent/catalogue.json`, 2674bytes. Preserve original headers/hashes for the upcoming complete decoder; no source-owner/admission created yet.
- Raw preflight: `TestResults/StellarPublicSourceCandidateCurrent/readiness.json`, 3163bytes; SHA256 `184d5e100f05f9d2e3721a3a654a0f4248f47c9377200e8c5366d96a4cdf903c`.
- Probe: `python -B Scripts/Sandbox/Probe-StellarPublicReadiness.py`.
- Controls: `python -B -m unittest discover -s Scripts/Sandbox -p test_stellar_public_readiness.py`.

## Next after XRPL hosted acceptance

Inspect/reuse the existing SDK or implement the exact native schema decoder, including bounded upgrade vectors and supported StellarValue/header extension unions, sequence and complete-input validation. Unknown variants must fail explicitly. Then capability-aware CQL/native tests, readable C#/CLI3/2, local HTTP/Mongo5/4, four ready presets, explicit admission and hosted/browser gates. No Stellar live/CQL count increase from this preflight.

Protocol: [Stellar ledger XDR](https://github.com/stellar/stellar-xdr/blob/master/Stellar-ledger.x), [Horizon ledgers](https://developers.stellar.org/docs/data/apis/horizon/api-reference/resources/ledgers).

## Complete header decoder milestone

`StellarLedgerHeaderDecoder` now consumes the entire native LedgerHeader XDR and binds expected SHA256 and sequence. It decodes all five currently specified StellarValue variants, bounded upgrade/signature opaque values with canonical padding, ED25519 NodeID form, known header extensions/flags and exact signed/unsigned64bit fields. Millisecond forms must agree with seconds; unknown variants, truncation, trailing bytes and malformed canonical inputs fail explicitly.

20controlled cases pass/no skips: three original real headers (protocol29), five independently Python-encoded union fixtures, invalid structures with matching recomputed hashes, exact Int64/UInt64 limits, nonzero padding, milliseconds mismatch and every one of428truncation points. This is native format/content validation, not SCP validator-signature/UNL consensus or full state proof. Initial prefix-only probe remains a narrower historical gate; its limitations are not silently relabelled.

- `TestResults/StellarPublicSourceCandidateCurrent/stellar-decoder.trx`:20passed/0skips, SHA256 `6bc66786a559f64bcaea8e0ffefc81d55c3560617d946e4dcbd18415ece24e48`.
- Build/test logs stay under TestResults/StellarPublicSourceCandidateCurrent; public golden vectors reside in the test source, not raw evidence in docs.

Next: implement bounded Stellar public-header CQL adapter using this decoder, binding Horizon sequence/hash/parent/protocol to the decoded header. Use one immutable configured catalogue and stable source owner, equality/membership over native ledger sequences; then readable C#/CLI3/2, local HTTP/Mongo5/4, four presets and explicit hosted admission/acceptance. No Stellar CQL/provider-count increase yet.

## Native CQL / developer demo complete

`StellarPublicHeaderMetadataSource` provides explicit sequence-key equality/membership over one to eight pinned sequence/hash pairs. Each row binds Horizon hash/sequence/parent/protocol to the verified complete native XDR. Public-network identity is explicit; metadata uses exact signed/unsigned decimal strings. Access/window/budget/cancellation and whole-selection failures are checked, with no partial rows or source writes.

21controlled cases,1real shared-manager C# case and actualCLI3/2 pass. [Developer guide](../../Devs/STELLAR_PUBLIC_HEADER_CQL_GUIDE.md).

- `TestResults\StellarPublicSourceCandidateCurrent\stellar-source.trx`: 21passed/0skips, SHA256 `9e703676b959313b3951d673d3ec7010ad28a8692ad4e3ce9a9d378f44f7747e`.
- `TestResults\StellarPublicSourceCandidateCurrent\stellar-native.trx`: 1passed/0skips, SHA256 `11a3cccfa28b9e48ab75a748f95ca6fe6a5d3af58bc53d1657d05ef78a3c9d78`.
- ActualCLI3/2 exit0; `TestResults\StellarPublicSourceCandidateCurrent/console-run.log` SHA256 `1a914e68b73b582f928ff4ea3a305a1b12d166de9f1bda398097b2f0b7475f2b`.

Existing hosted container read-only egress probe returns200 for Horizon root and one retained ledger, matching public network/sequence/hash/XDR SHA256. `TestResults/StellarPublicSourceCandidateCurrent/hosted-egress.json` is reachability evidence only, not CQL, auth, replicas or browser acceptance.

Stable source `LocalState/COSMIC/HostedStellarPublicSourceCurrent/source.json` created once, original three sequence/hash pairs retained. Next: lazy API profile/independent read tariff, four exact shipping-planned presets, local HTTP/Mongo5/4/private isolation, guarded hosted rollout and browser gates. Counts31implemented query types (Stellar native-only),30deployed tariff types,28hosted HTTP/5browser; no Stellar live permission/count.

## API / local HTTP complete

Explicit lazy CQL_STELLAR_PUBLIC_SOURCE_JSON read profile, compiler scope and independent unsigned tariff/admission implemented. Three pure configuration controls and one generic client preparation control pass. Exact protected three-setting candidate/four presets pass the shipping native+Mongo planner, preserving original source owner/catalogue, wallet DNA and write admission.

Local loopback HTTP proves native3/2, two private SDK Mongo creates, paired5/4, nine query receipts, foreign-private0/public3 and exact physical IDs/provider/owner/visibility. Auth/billing is controlled fixture infrastructure; no production billing/hosted/replication claim. Original config unchanged, exact marker-verified database dropped, owned Mongo stopped/27029 absent. Durable StellarMongoHttpCurrent intents/ACKs/cleanup confirmation retained; never replay the successful fixture.

- `TestResults\StellarPublicSourceCandidateCurrent\stellar-admission-prepared.trx`: 3passed/0skips, SHA256 `186fcfa72b4d0ae7138705b0132d1d7baa355efb1fed946ce7e0f768b5faed50`.
- `TestResults\StellarPublicSourceCandidateCurrent\stellar-tariff.trx`: 1passed/0skips, SHA256 `674c2d33dbbe5f6966d447c1c23558c4ddba0be414ff56419730aa2b57e7fcc6`.
- `TestResults\StellarPublicSourceCandidateCurrent\stellar-mongo-http.trx`: 1passed/0skips, SHA256 `7922a183eaa76ab223586b1c01105316359e80fd40b7b8cba45de87a07d8e0ae`.

Core2dd3c37 task branch pushed/remotely verified; parent gitlink/dependency manifest advance together. Candidate is prepared only, no settings applied. Next verified Development publication, exact app SUCCESS, guarded3setting application, one journalled settings rollout, then normal visitor native/Mongo/isolation/replica acceptance and website links. Wait for the settings rollout before creating a visitor; require discovery readiness before any save. Counts31implemented/candidate31tariff/deployed30tariff/28hosted HTTP/5browser; no Stellar hosted grant.
