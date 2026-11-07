# Pinata public IPFS block CQL MVP

The exact-key profile uses the official Pinata public gateway with
the IPFS raw block representation. It matches each block's bytes
against the requested CID using the existing vendored IPFS library.
The bounded read projects CID, codec, byte length and digest into
an owner-scoped public holon. The block bytes are read in memory and
remain on IPFS for subsequent retrieval.

The catalogue contains the root published in Pinata's gateway
documentation and two linked blocks from that verified root.

| CID | Raw bytes |
| --- | ---: |
| `QmP4eZj61tDxCmtb6aqSwQsrxRQAN2gwxTEXkTed9TujHh` | 344 |
| `QmeBt6f2Dem8QnZuZ8PDRT1iuWpphmza3gP17en2ZoReYR` | 262,158 |
| `Qmb733sded7TRm8U8bM4FHif89jUVKd8yQiurKCKgCLyQk` | 262,158 |

At Core `cec62ced64ddb5b913f595e4ffc901e95f4c86dd`, the actual
gateway returned exact CQL 3/2. Every row's digest, size and codec
matched. Three transport controls rejected the wrong digest, an
oversized block and the wrong media type. Four checks passed with
zero skips. The 7,479-byte TRX SHA-256 is
`43cedefc1ca18665f0491f9fddd815e7ab1609c23dfa45914690f7bb7f500a0b`.

The first test project compile found two outdated legacy test
assertions. The focused consumer passed against the same compiled
provider after the assertions were aligned with its current enum
metadata. WEB4 admission and local federation passed. Protected
Pinata pin management and canonical storage acceptance are separate.

[Pinata gateway guidance](https://knowledge.pinata.cloud/en/articles/6297294-public-gateways-vs-dedicated-gateways) ·
[Trustless gateway specification](https://specs.ipfs.tech/http-gateways/trustless-gateway/).

## WEB4 admission

Set `CQL_PINATA_PUBLIC_BLOCK_QUERY_SOURCE_OWNER` to the reviewed
canonical teaching owner GUID. The profile uses its fixed public
block catalogue and requires an independent read permission and
tariff. The reviewed settings descriptor contains the `Actor` field
and derives exact logical IDs from the three verified CIDs.

At Core `06a4363e1c26e616eddda34c2959acadab8dbba8`, the WEB4
producer passed three Pinata checks and fourteen shared admission
regressions with zero skips. The 31,278-byte TRX SHA-256 is
`e4c52591653685f7ee7bdf880b4baa002e5ed3cdbd572832d36b1984ff2a2df3`.
The settings and response-evidence suites passed 39 and 21 checks.
Development activation follows LocalFile.

The first Pinata local HTTP attempt passed activation and the preliminary
native selection. Mongo acknowledged both private version-one saves, then
the native WEB4 three-row query returned HTTP 503 with `CQL_QUERY_FAILED`.
The source error was unavailable in the initial client log. The failing TRX
SHA-256 is `72bcaed7eae1e5c22fe11ad7576df4b22c992b104542530630b07c862608079c`.
The original synthetic database and write acknowledgements are retained.
Continuation reconciles both acknowledged records by identity and version
before federating the private copies.

The first read-only continuation stopped in the preliminary native
selection with a `TaskCanceledException` while the guarded TLS connection
was waiting for a Pinata response. The independent CID fetches had been
running serially under the shared 15-second query deadline. The 22-second
test included activation and the bounded native selection. No original
write was repeated. The continuation TRX SHA-256 is
`b9430cac7ad8e195c71be4920075978bdb6e9c71125cec2f4bd72b047a1a8c60`.

The repair permits three independent verified block reads to overlap
under the same deadline and row and response budgets. The shared
query helper continues to read serially for its existing profiles.

The bounded read batch passed all five Pinata provider checks, including
native 3/2 and a controlled test that requires all three independent
reads to start before any response is released. The 8,978-byte TRX
SHA-256 is `ea5f69d2d5063d56929caf6c73e6860db3f694fc9bd290c8f54af00ce34f2132`.
The exact local HTTP continuation still needs its rebuilt WEB4 producer.

## Local WEB4 and Mongo federation

At Core `2324c21cfe680f465907e8dd1175b7229d7ef5d3`, WEB4 HTTP
returned native 3/2, paired 5/4, foreign-private 0 and foreign public 3.
The shared run passed three provider cases with zero skips. The
6,208-byte TRX SHA-256 is
`ce187305a3ebafd94bf5fd2fbe2734fb097153f258ebdc5231752f8c869bf6eb`.
The fixture confirmed its exact owned database was absent after cleanup.
Hosted HTTP and browser acceptance remain open.

Pinata reused both acknowledged version-one saves and verified their
identities before federation. The runner issued no repeat saves.

## Follow-on scoped release candidate

Parent 8645794bbac7c7f8b28c51180db5afa60e586721 and Core
4585be145c13e3c3d43cb9a5b205475f126faa8f are published on
work/cosmic-public-three-release-candidate. The batch contains Discord,
Livepeer and Pinata on top of the World ID candidate. Provider sources
match the tested task branch, including Livepeer's native contract reader.
Manifest/gitlink validation passed. [CI 42332826329](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42332826329)
is the image gate. Promotion and hosted settings remain open.

Eight shared batch settings and twelve presets are prepared under
LocalState/COSMIC/HostedSourceRolloutCurrent with prefix public-three.
The protected baseline is the intended World ID candidate, so it is
explicitly future state until World ID deployment and settings finish.
Before applying, the guarded publisher must verify the actual live
variables equal this baseline. No hosted values were written.

The immutable image gate passed for exact parent 8645794bb. The candidate
tag resolves to sha256:9186270c613dd1fb90a71f1edbb894d336f39ed5e1527efb5f523ea7c076722f.
Finish the World ID settings deployment and its provider acceptance
before advancing Development to this batch. Recheck remote ancestry and
the live protected baseline at promotion time.

Development advanced to the exact image-qualified candidate 8645794bb.
The live settings matched the intended World ID baseline before promotion.
Railway code job 052ece00-c14b-4aa7-abeb-f89c43cf0c51 is queued. Observe
this exact job before using the guarded publisher for the three-provider
public-three batch. Settings have not been applied.

Code deployment 052ece00 succeeded. The guarded publisher applied and
read back all eight batch settings. Same-source settings job
0ab0d7c9-45b7-4fcc-b1ea-c79c9d6c0eae is building at parent 8645794bb.
Observe the exact job before live readiness and provider acceptance.

## Development HTTP acceptance

Settings deployment 0ab0d7c9-45b7-4fcc-b1ea-c79c9d6c0eae succeeded and
matched the protected batch. Startup activation completed before acceptance.
Native queries returned 3/2 and MongoDB federation 5/4. First prep saved
two private copies. Repeat saved zero and reused both. Both replicas
held two copies. The independent visitor saw three public records and
zero private copies on MongoDB, SQLite and Neo4j. Sandbox card and
interactive browser verification remain open.

Evidence under TestResults/HostedCqlIntegrationCurrent, SHA-256:
- PinataOASIS-native-http.json d5aab48282bc9b8ee90d0db5f62fcf3461df7bffe10e436d2f772e07a95677d6
- PinataOASIS-client-http-flow.json 8e5d67a2c336b173ce34cba51016097a87c1101b8f6b5829b4ee3501ab5a5074
- PinataOASIS-isolation-http.json b4e9faac38ee37cebfdda351a137eb1dc3630cbd967f277d8af652abbbeecc54
