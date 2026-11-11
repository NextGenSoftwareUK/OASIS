# Oracle Object Storage public metadata CQL

The teaching profile maps exact object keys from Oracle's published
`workshop-data` bucket into public logical Holons. It uses native
anonymous HEAD requests and returns the object size, ETag, last-modified
time and media type. A reviewed key catalogue bounds CQL equality and
membership. The scoped profile is independent of owned OCI SDK storage.
[Oracle workshop objects](https://docs.oracle.com/en-us/iaas/Content/bigdata/get-started-odh/get-started.htm) ·
[Oracle HeadObject](https://docs.oracle.com/en-us/iaas/Content/Object/Tasks/managingobjects_topic-To_get_object_details.htm)

6 November 2026 native qualification returned HTTP 200 for these three
published objects in the Phoenix region.

| Key | Bytes | ETag |
| --- | ---: | --- |
| `bds-livelabs/env.sh` | 534 | `ee15eb88-80e0-45a1-b8a1-a3121e1477fe` |
| `bds-livelabs/download-all-hdfs-data.sh` | 353 | `5aaf4866-3849-4d93-927a-ee2f9065fb27` |
| `bds-livelabs/download-all-objstore.sh` | 745 | `ff292829-0dff-4b3f-85fb-7a538e880ebf` |

The list request returned 404. The teaching profile uses the three exact
object keys through HEAD. Eight controlled cases and one real CQL 3/2
test passed at Core `e400afd53cef4997e8bfcabc4b34b3f84a854626`.
The TRX SHA-256 is
`ef955952294fb29ffa205baa95ae8c205fef7bab063ba011651e2f590c5475a5`.
The combined opt-in WEB4 admission passed seven new profile checks and
12 Cloudinary and pgvector regressions at Core
`c2a8b2303264bd159ac55d71b684c712647c6d3b`. Live verification remains
open. LocalFile remains first for live rollout.

The reviewed local WEB4 HTTP batch verified native 3/2, paired MongoDB
5/4, and visitor isolation. OCI provided native HEAD metadata and the
owned two-record Mongo fixture was removed after verification.
[Local acceptance scope](PROVIDER_CQL_AND_SANDBOX_STATUS.md#reviewed-public-source-http-on-6-november-2026)

## Joint hosted candidate

The scoped candidate dcae58d976d2af04800d1670fc1e4fe3979b5608 and Core
f695b1cd8880b3009b7b14190ab41867fb38ee65 are published on
work/cosmic-registry-object-release-candidate. This batch adds the public
Oracle object HEAD profile, Cloudinary image metadata and Proof of
Humanity registry reads. The shared registration includes only those
reviewed profiles. The existing native and local WEB4 proofs remain
the source acceptance. [Image CI 42336260220](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42336260220)
must pass before promotion and hosted acceptance.

The eight-value protected candidate and twelve presets use prefix
registry-object-public under LocalState/COSMIC/HostedSourceRolloutCurrent.
No hosted settings were applied. Oracle database hosting remains deferred.

The guarded public publisher and deployment-scoped visitor now explicitly
admit Oracle Object Storage, Cloudinary and Proof of Humanity. Use
Publish-HostedFixedPublicCatalogue.py with all three provider names,
prefix registry-object-public, the reviewed candidate ref and the exact
successful code deployment. Preflight first, then --apply.
The three visitor scopes reject stale deployments. All 25 response
evidence tests and five publisher tests passed.

Image CI 42336260220 passed for the exact candidate. Published image digest:
`sha256:90636c35d33788eeab3d2993a04ad551aacd4398bc11db91029925af46277692`.
Dependency pins, remote Core and live settings baseline were verified.
Development advanced to dcae58d97. Railway code job
900d0fe7-9dc9-489c-a12b-7df8ef84fea8 is queued; settings remain unapplied.

## Hosted HTTP acceptance

Verified on settings deployment `fb589d55-f737-4622-9160-6e63e0e55099`,
parent `dcae58d976d2af04800d1670fc1e4fe3979b5608`, Core
`f695b1cd8880b3009b7b14190ab41867fb38ee65`. Native selections returned
3/2 exact records. MongoDB federation returned 5/4. First preparation saved
two version-one private records; repeat preparation saved zero and reused two.
SQLite and Neo4j each returned both replicas. An independent visitor saw
three public records and zero private copies on all three storage providers.
The original cohort and save receipts are retained; acceptance must not be replayed.
Browser interaction remains unverified.

Evidence under `TestResults/HostedCqlIntegrationCurrent/`:

| Report suffix | SHA-256 |
| --- | --- |
| `OracleObjectStorageOASIS-native-http.json` | `790d5e0ef4adb3584bd4354b2483c6d41719659a3f784b8630d57c795f9849fb` |
| `OracleObjectStorageOASIS-client-http-flow.json` | `e4a486e42fc57ec7f9e31aae5979e993247afb1f0a046546b629e83b0733ff48` |
| `OracleObjectStorageOASIS-isolation-http.json` | `864db45c9d42ad674d089da60437bb682564031690341dd5786596d06fb71b36` |

Website `a88076d` publishes the hosted playground instructions and pinned
local C# example. All 114 client tests passed. Public HTML was HTTP 200,
239108 bytes, identical to the exact commit, SHA-256
`8773ee77638a24988968db32a15d58069424ac2e03604639a03d244198e65666`.
This verifies publication; functional browser interaction remains open.

11 November UTC: OracleObjectStorageOASIS public object metadata presets returned 3 full and 2 filtered rows in the live Development WEB4 CQL Playground. Original-owner paired browser verification and owned SDK storage acceptance remain open.
