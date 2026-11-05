# Algolia bounded CQL MVP continuation

5 November2026. Controlled implementation; native acceptance remains open.
Do not increment100local/60hostedHTTP/28hostedbrowser or publish a demo card.

## Implemented read contract

The existing REST provider now implements the bounded identity CQL interface
and explicit read-only source activation. It uses Algolia's native
`POST /1/indexes/*/objects` API with at most100explicit authorized object IDs,
always targeting the existing `oasis_holons` index. Native results must match
request count/order, each `objectID` and canonical Holon `id`. Null slots mean
missing objects; native status messages, including missing index, are errors.
No search/browse scan, index creation, settings mutation or error-time host
alternative. Only the documented `search` ACL is needed for these reads.

Complete payload bound4MiB, each record1MiB, total request/body deadline15seconds.
Full records are budgeted before projecting out child graphs and materializing
the single Holon. Authority, deletion, deterministic ordering/paging and caller
cancellation apply before publishing any rows. Native/shape/budget errors return
`OASISResult` failure with no partial rows. Historical reads stay unsupported.

Application IDs cannot alter the fixed HTTPS credential destination. Default
redirects are disabled, invalid credentials are rejected without echoing them,
and a provided client cannot combine credentials from multiple applications.

## Evidence

Original controlled run:27pass/2fail out of29, no skips. Focused diagnostic
reproduces `NotSupportedException` at `$.children[0]`: excluding children after
deserialization was too late. Fixed the projection boundary before materializing
the interface graph, rather than catching the error or changing query paths.
Failure tests now assert the intended native/shape exception type, preventing
an unrelated serialization error from making a denial/error case appear green.
Corrected v2:29/29pass. Final credential-boundary controls:31/31pass, zero skips.
Updated native fixture builds0errors/2warnings, including the exact descriptor
field/size guard. Missing-credential precondition rejects before native
qualification or intent-directory creation. No native acceptance claim.
`controlled-summary.json` records the31case TRX hash and actual shared Core
checkout5415961; selected-parent-Core/API deployment qualification is separate.

Evidence is under `LocalState/COSMIC/RecoveryLaptopAlgoliaCql`:
`controlled.trx`, `graph-diagnostic.trx`, `controlled-v2.trx`,
`controlled-v3.trx`/log and `native-build-v2.log`. Initial native fixture build
passes0errors/4warnings. No native account/API request has been made.

## Prepared native gate

The read-only native fixture and
`Scripts/TestHosts/Run-CosmicAlgoliaCqlNative.ps1 -SourceConfig <owned-descriptor-path>`
are prepared. The launcher requires a source descriptor and process-environment
`COSMIC_ALGOLIA_CQL_API_KEY`, takes the shared build lease, preserves a unique
intent/report directory and requires exactly1passed native test with0skips.
It never provisions records or automatically retries a failed native operation.

The descriptor has these exact case-sensitive properties; keep its credential
outside the descriptor:

| Property | Required owned teaching source |
|---|---|
| AppId | Algolia application identifier |
| Actor | Nonempty owner GUID matching the four stored records |
| LiveIds | Three distinct live canonical Holon GUIDs |
| DeletedId | One distinct soft-deleted canonical Holon GUID |
| MissingId | One distinct absent GUID |

Stored records use the provider's existing camel-case Holon JSON plus matching
`objectID`; the fixture independently checks native identity/owner/deletion,
then CQL3/2/0/paging and semantic preservation of all native record fields.
Source descriptor bytes must remain unchanged. No native source, account or
source-scoped key has been supplied on this laptop yet.

After owned-source qualification, complete manager/Mongo/HTTP, actual browser
and explicit hosted enrollment separately. Legacy storage activation, mutation
acknowledgements, conditional writes, graph/history, index/search configuration
and full ORM parity remain outside this controlled read acceptance.

Primary contract: [Algolia getObjects](https://www.algolia.com/doc/rest-api/search/get-objects)
documents request-order results, null missing objects, native index status and
the required search ACL. [Recovery cleanup ledger](RECOVERY_LAPTOP_CLEANUP_LEDGER.md).
