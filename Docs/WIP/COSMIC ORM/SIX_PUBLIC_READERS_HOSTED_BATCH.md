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
  SUCCESS, job128512356194. All25pass/0skip: six new facts,
  five previously accepted cross-chain facts, fourteen NATS cases.
- [Exact image qualification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42408951517):
  SUCCESS, job128512687059; one dispatch acknowledged, exact candidate
  input, teaching sidecars enabled. No image downloaded locally.
- Shared publisher: six new plus four retained controls pass. HTTP verifier
  accepts all six provider names;28 response/recovery controls pass offline.

Next: retain terminal qualification evidence and exact image digest, inspect
published dependency pins and current accepted baseline, then integrate only
this qualified batch. Verify actual runtime before thirteen journalled settings
writes and one activation. First hosted cohorts must prove native3/2, Mongo5/4,
private snapshot reuse, isolation and exact replicas before public cards.
Never replay existing accepted cohorts. Browser acceptance remains separate.

API artifact13588752684 retained remotely:4390B, SHA256
`eff9cb60dac1d4b0e6b70148bf8cc2843a1c2809476f8d4a3c29b52639d3677c`,
expires7December2026. No local artifact download. Seven private pins freshly
confirmed on advertised remote refs; compact receipt retained. Image SUCCESS.

Image digest `sha256:e705f5cf4d1345d85ce8e4d1de970e1e37656d79c9a9d98aaf3e52541187dec3`.
One scoped Development push acknowledged after exact qualification and fresh
complete settings/baseline checks. Code deployment
`88dcff57-010b-4ab5-9dee-db96f1328cb7` observed QUEUED for88b896815.
No thirteen-setting application, activation or first cohort yet. Next: terminal
code deployment and actual runtime verification before settings enrollment.
