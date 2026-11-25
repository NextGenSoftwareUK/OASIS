# ENSOffchain CQL MVP

Progress (25 Nov 2026, cloud-cql-C): read-only name-profile source merged to Development 25a128c2aa; WEB4 tests pass. Live acceptance is pending Railway enrollment: see [enrollment handoff](CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md).

## Scope

- `EnsOffchainPublicNameSource` reads up to eight enrolled lowercase `.eth` names, one bounded GET each (`/ns/ens/{name}` on `https://api.web3.bio/`, 15 s, 16 KiB).
- Each response must name exactly the requested identity, with platform `ens` and a canonical lowercase address. Redirects, other origins, unlisted names and configuration that carries credentials are refused.
- Metadata keys: `ENSOffchainName`, `ENSOffchainAddress` and `ENSOffchainDisplayName`.

## Not claimed

Name search, writes, the CCIP-read off-chain resolver protocol, or independent verification of web3.bio's resolution against chain state (the on-chain ENS source does that).
