# Sui + Mongo CQL MVP - native/restart/paired/browser verified

## Current-branch integration —30 October2026

Selective port from preserved parent f89cf0085. Seven controls, one retained-native
scenario and one Mongo federation scenario passed with zero skips. Independent
browser all5/subset4 verified, preserving provider labels. Original and dedicated
Storage object versions/digests/content stayed unchanged. All seed/deployment/
signed-preparation/acknowledgement fingerprints remained unchanged.

```powershell
Scripts/TestHosts/Run-CosmicSuiCqlNative.ps1 -Federated -Serve
```

Open http://127.0.0.1:5112/. Use both query buttons, then the local stop button and
wait for cleanup. Dedicated5112 avoids LocalFile5110. Runner builds producer before
consumer, reuses the original node without provisioning, archives successful reports
by hash and refuses unresolved prior failures. No source write/publication replay.

Current evidence under `TestResults/SuiCqlCurrent/`:

| Report | SHA256 |
|---|---|
| sui-cql-current-controls.trx (7passed) |309CBD685DCBCE28CB5AB8998401670081F08933E28AB3D34E3B4A46FCDC4DCA |
| sui-cql-native.trx (1passed) |FF79C91FF9760D5C7D5BD2CEEC24D956288284A0CDF096B96F5FEF8700F7397D |
| sui-mongo-federation.trx (1passed) |93974D9882D3A1364885BA9CEEE18526D183857359403CFA018ABB803FBB70A3 |

Native browser proof: `sui-mongo-subset-current.png`. Exact temporary Mongo fixture
verified/dropped; pending/ready gone, runner exit0. Native process inventory empty;
9000/9123 and27029/5112 stopped. Source metadata remains15files15045B; retained ledger
1469253016B after normal node execution. No runtime install/download/new environment.

Public sandbox section18 verified: Web4Site mainf4639060d5331b6bce09cf4e7aea7818c2804f66,
Production7452544690success. Expanded command/fixed5112link visible. Screenshot
`TestResults/SuiCqlCurrent/sui-catalogue-public.png`, SHA256
26D6D36454A48E2740069155911D34824C263BD9CC5CA5161DA7505CA6DD0B5E.
Backend remains task-branch only, no new Railway deployment. Hosted selector remains
Mongo/SQLite/Neo. Historical proof below retains its old5110 port, superseded by5112.
All stated bounded-query/encryption-fixture/HyperDrive limits below still apply.

29 October 2026. SuiOASIS implements ICosmicQueryStorageProvider using its existing
persisted encrypted backend and official Mysten SDK bridge. No parallel transport.

## Scope

Shared bounded-ID algebra with exact SuiOASIS authority/catalogue, ID equality and
membership, Boolean algebra and paging, at most100 selected keys. Unsupported
content/metadata/order/children reject before I/O. Canonical RecordKey matches saves;
private backend owns opaque keys/authenticated decryption. CQL never enumerates
source records to filter a predicate. Any failure/cancellation discards all rows.
Separate reads may observe different checkpoints; no distributed snapshot claim.
Result and4MiB decoded canonical payload budgets apply; underlying SDK allocations
are outside that post-decode budget. Full history/children/atomic writes/HyperDrive
acceptance remains separate.

## Verified evidence

SuiCqlTests:7/7 zero skips,180ms. Frozen selection/catalogue intersection, authority,
unsupported shapes, inactive provider/pre-cancellation; no workers in these controls.
`TestResults/SuiCqlCurrent/sui-cql-controls.trx`, SHA256
`D6B3728766D61188BFB14CA6A07058A4455E7D070ED0D6664C25E67413572F65`.

The first original-source inspection found0 live holons and failed its3-record
criterion. That source was not seeded. A separate package/Storage was admitted on
the same chain through the shipping bridge publication/preparation path instead.
The Move package's18 tests passed using the cached1.81.1 framework; no downloads.

Dedicated native3/2/missing query now passes1/1 zero skips,9s. Source setup persists
three original IDs before writing; each physical native write's signature/identity/
chain is verified through the existing SDK boundary and fsynced before submission.
Three encrypted SaveHolon acknowledgements and three signed source preparations
are retained. CQL preserves dedicated object version/digest/content; the original
Storage object also stays exactly unchanged across dedicated-source setup/reads.
`TestResults/SuiCqlCurrent/sui-cql-native.initial.trx`, SHA256
`CFD2A6C1193D22EFDF53D42D42A9E836F73756904579B66B5436A89EA900A710`.

Full stop/start same ledger/package/storage/3IDs passes1/1 zero skips,4s, without
repeating publication or record writes. `sui-cql-restart.trx`, SHA256
`30AB7AE82B48D1A0D8B681CB8D52330F875BBD95C25BF7EC81E1A20E76330BFB`.
Both reports are under TestResults/SuiCqlCurrent; no broader native parity claim.

## Dedicated source admission and retention

One-time `Scripts/TestHosts/admit-cosmic-sui-source.mjs` imports the shipping bridge,
uses its publication/signature verification, and fsyncs the original signed package
publication and registration preparations before submission. Two preparations and
one acknowledged publication receipt are retained. It refuses existing evidence,
never replaces a deployment or replays failed publication, and preserves errors.
Do not rerun it against this admitted source. It does not implement a second signer.

Dedicated package:
`0x16ea6aa176ae03950b44870f3e661e397910949f2d6f7cc30e83d2c622825c65`.
Dedicated Storage:
`0xf71897f5cbc35ea6c3399eba65bdbd1698eb57ae900182d5e6c496829e067607`.
Public metadata/journals/signatures/acknowledgements/catalogue are retained in
`LocalState/COSMIC/SuiCql`,15045bytes. No private key was copied into that metadata;
original private identity remains referenced in place. Seed SHA256
`8CD5BCE33245FD37ACE2EC771C352AD5E5AFBA88FD6782FAE54A69B09309D85F`;
public deployment SHA256
`F551FECE87F844A44DF907D790C29A9C6AD70D4E6EA936C382EFB3061E96794B`.
The catalogue flag ReadOnlyOriginal means the old deployment Storage is untouched.

Original `/var/tmp/oasis-sui/deployment.json` and network are preserved. Existing
fixture encryption-key resolver is reused for this isolated synthetic source;
public fixture key material is not a production secrecy claim. Native fixture uses
the shipping internal preparation scope via reflection only to attach durable test
persistence; real SDK signing/submission/decryption remain the shipping path.

## Environment and cleanup

Runtime `/var/tmp/oasis-runtimes/sui-1.81.1/sui`217147032bytes; runtime directory
218470781bytes. Original source network1450753476bytes before this programme resume.
SDK21packages48091695bytes reused; lock SHA256
`919F1FAA1BEE8846C98617EB214588D68AE0D4531A0B35F9E1E259F2CACEEC7C`.
Move build output4975bytes plus source build/dependency links are regenerable;
never delete shared framework targets when cleaning source links.

Owned nodes110899 and112740 were stopped using the exact-owner termination helper;
launch shells73028/52392 are terminal. No Sui processes/9000/9123 listeners remain.
Retain original ledger/deployment and new source/preparations together. Generated
logs/TRX/compiled artifacts stay in TestResults/source build output; no raw docs.

## Paired SDK/HTTP/browser proof and demo

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Sui -Federated
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Sui -Federated -VerifyOnly
```

The runner owns reuse-only startup/shutdown of the existing chain and exact Mongo
fixture. Browser localhost5110. One HolonManager query returns Sui3+Mongo2=5 physical
rows; selecting two IDs returns4, preserving source copies. Queries are bounded
identity predicates; arbitrary metadata/content queries remain outside this MVP.

SuiMongoCqlNativeTests passes1/1,zero skips,48s. SDK/HTTP verify5/4, physical provenance,
foreign Mongo ID exclusion, invalid actor/preset rejection and inactive-Sui HTTP503
without rows. Browser independently verified full/subset counts and provenance.
`TestResults/SuiCqlCurrent/sui-mongo-federation.trx`, SHA256
`FEBA1C7721118D18973234821E09B3B46FA3745DF1DFD5BC8057BF2E842359DC`.
Native preflight1/1,4s, `sui-cql-native.trx`, SHA256
`F054E1E6DA341417AD8E587A434948DB03F1AC8FC4F663FFB1849AD091FABD2B`.
Screenshot `sui-mongo-browser.jpg` in the same TestResults directory.

Hub routing check passes8 single/16 plans,19 paired/38 plans,24 paired rejections,
3 archive rejections, listeners unchanged (`hub-routing.log`). The shared C# and
HTTP teaching files provide spaced code/comments and explicit Sui provider scope.

After browser shutdown same runner32601 exited0; exact temporary Mongo DB
`cosmic_cql_mongo_sui_demo` was content/identity-validated and dropped, pending/ready
journals removed. Owned Sui stopped through the exact-owner helper; no Sui process,
9000/9123 native sockets or27029/5110 teaching listeners remain. Seed/source/5 signed
preparations retained unchanged; source metadata remains15045bytes. No new downloads.

Continue other red provider MVPs. Preserve scope: no mainnet/finality/full ORM,
child/history/atomic-write/HyperDrive certification follows from this bounded demo.

[Status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
