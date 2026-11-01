# Stellar public header readiness

Updated1November2026. Qualification only; full native decoding, CQL, HTTP/Mongo and hosting remain red. XRPL rollout remains the immediate priority.

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
