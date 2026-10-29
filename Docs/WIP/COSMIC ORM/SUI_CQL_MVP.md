# Sui CQL MVP - adapter controls pass, dedicated native source needed

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

## Verified and observed evidence

SuiCqlTests:7/7 zero skips,180ms. Frozen selection/catalogue intersection, authority,
unsupported shapes, inactive provider and pre-cancellation. These use no SDK workers.
`TestResults/SuiCqlCurrent/sui-cql-controls.trx`, SHA256
`D6B3728766D61188BFB14CA6A07058A4455E7D070ED0D6664C25E67413572F65`.

Native original-source inspection actually ran through encrypted activation and
LoadAllHolons on the retained deployment. It found **zero live holons**, not three.
`SuiCqlNativeTests` therefore failed its required-three-record assertion before any
catalogue/source write. No native CQL3/2/missing or paired proof is claimed.
Report `TestResults/SuiCqlCurrent/sui-cql-native.trx`; original source not replaced.
The new fixture contains no provider Save/Delete call or native mutation operation.

Existing protected source: `/var/tmp/oasis-sui/deployment.json` and network. Original
private identity is referenced in place and never printed/copied into docs. Existing
fixture encryption label comes from SuiRealProviderRoutingTests. Native package's
storage initializer creates its Storage object only at package publication; it
has no public factory for another independent storage object.

## Reused resources and cleanup

Runtime `/var/tmp/oasis-runtimes/sui-1.81.1/sui`217147032bytes; runtime directory
218470781bytes. Original source network1450753476bytes before resume. No new chain,
genesis, keys or source data created. Reuse-only launch owned Linux PID108882;
loopback9000 identity/socket verified, terminated with existing exact-owner stop
helper. Shell8728 ended after termination, no Sui processes or9000/9123 listeners.

Pinned bridge @mysten/sui2.35.0 and @scure/bip392.4.0: existing source installs were
absent. Offline restore lacked valibot1.5.0; one prefer-offline locked restore fetched
missing official packages,21packages48091695bytes. Lock SHA256
`919F1FAA1BEE8846C98617EB214588D68AE0D4531A0B35F9E1E259F2CACEEC7C`.
Keep this one source node_modules/cache reusable; logs/TRX only TestResults.

## Exact next step

Prepare one separate COSMIC demo package/Storage object on the same retained local
chain; preserve original deployment/source and all older uncertain outcomes. Persist
original signed/setup intent before submission and acknowledged package/object IDs
before writing3 source holons. Reuse original runtime/cache, no new network/genesis.
Then prove3/2/missing, encrypted source version/digest/content unchanged, full restart
and Mongo-paired C#/HTTP/browser5/4. Do not relax the failing three-record criterion
or silently seed the original empty storage object to obtain green evidence.

[Status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
