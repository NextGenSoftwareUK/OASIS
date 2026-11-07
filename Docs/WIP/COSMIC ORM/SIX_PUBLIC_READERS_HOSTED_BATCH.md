# Six public readers: Development batch

Updated 7 November 2026. Qualification in progress; no live batch acceptance yet.

Candidate parent `88b896815eb41e14b5deec3cb3aeae54fb431a5d`, Core
`944d380c16d2831d567e5b76f89e6b1c6dd2e051`, based on accepted
`15eb486735a3eb80cae25142f1f0a839918abff5` / `85278a4`.
Existing release-candidate refs advanced normally; shared checkouts untouched.
Manifest/gitlink validation passes. Only six named reader implementations,
API registrations/references, compiler/admission and qualification tests added.
Miden protobuf input is included. One qualification workflow avoids competing
producer builds on the runner.

| Provider | Bounded native scope | Existing source report |
| --- | --- | --- |
| Miden | Testnet gRPC block headers | [Miden](MIDEN_PUBLIC_SOURCE_STATUS.md) |
| Espresso | Mainnet query API block headers | [Espresso](ESPRESSO_PUBLIC_SOURCE_STATUS.md) |
| EigenLayer | Operator registration at a finalized Ethereum block | [EigenLayer](EIGENLAYER_PUBLIC_SOURCE_STATUS.md) |
| Fhenix | CoFHE AddressBook TaskManager reads | [Fhenix](FHENIX_PUBLIC_SOURCE_STATUS.md) |
| Axelar | Mainnet LCD block reads | [Axelar](AXELAR_PUBLIC_SOURCE_STATUS.md) |
| Ritual | Chain 784 RPC block reads | [Ritual](RITUAL_PUBLIC_SOURCE_STATUS.md) |

Original native/local qualifications reused. Thirteen composed settings retain
all unrelated values; protected before/candidate snapshots are outside Git.
No source fixture replay, settings application, new cohort or source writes.
These scopes do not establish wallet execution, Axelar GMP, Ritual Infernet or
full provider/ORM parity.

- [Exact API qualification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42408837581):
  observed running, job128512356194. Requires25pass/0skip: six new facts,
  five previously accepted cross-chain facts, fourteen NATS cases.
- [Exact image qualification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42408951517):
  observed running, job128512687059; one dispatch acknowledged, exact candidate
  input, teaching sidecars enabled. No image downloaded locally.
- Shared publisher: six new plus four retained controls pass. HTTP verifier
  accepts all six provider names;28 response/recovery controls pass offline.

Next: retain terminal qualification evidence and exact image digest, inspect
published dependency pins and current accepted baseline, then integrate only
this qualified batch. Verify actual runtime before thirteen journalled settings
writes and one activation. First hosted cohorts must prove native3/2, Mongo5/4,
private snapshot reuse, isolation and exact replicas before public cards.
Never replay existing accepted cohorts. Browser acceptance remains separate.
