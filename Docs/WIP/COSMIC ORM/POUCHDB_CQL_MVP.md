# PouchDB CQL MVP continuation

5 November2026. Active, no new accepted profile/card or hosted count.

PouchDBOASIS currently serializes Pascal-case canonical Holons plus CouchDB
_id/_rev fields. New bounded native ID CQL uses the shared authoritative
catalogue compiler and actual GET document/conflicts reads. Query activation
reads the existing database, without provisioning. Historical queries reject.
All requests have aggregate15second deadline,1MiB record/4MiB total payload
budget. Native/canonical identity and revision, conflicts, duplicate properties,
authority/deletion and paging are checked before output. Embedded children are
projected out before materialization, with original bytes still budgeted.
Document404 must explicitly prove missing/deleted; missing DB, auth and native
errors remain errors, never empty successful rows. Constructor now requires
HTTPS or HTTP loopback, dedicated no-redirect default HTTP client and paired
Basic credentials; injected clients are caller-configured.

Controlled cases pass18/18, zero skips (controls-v2/controls.trx). First17case
result retained; v2 also rejects native tombstones and budgets missing responses.
Native HTTP service/source remains pending. One pinned npm environment with PouchDB9.0.0 and
express-pouchdb4.2.0 official server middleware, installed Node18.18.0 reused.
No generic CouchDB substitute: qualify actual PouchDB LevelDB backend, version,
store reopen, original records/revisions and CQL before manager/HTTP/browser.
No native source initialized yet. No memory backend or simulated native data.
Owned cache: LocalState/COSMIC/RecoveryLaptopPouchDBCql; install and test logs,
package-lock/manifest are laptop-specific evidence. Shared build lease required.
Next consumer UI5141, actual native service19600 (both must be free).

Official references: https://pouchdb.com/api.html and
https://github.com/pouchdb/pouchdb-server . Legacy mutation acknowledgements,
all provider interfaces, replication/conflict resolution, graph/history,
full EF/NHibernate parity and hosted enrollment remain separate open gates.

Runtime qualification now passes actual PouchDB9.0.0 LevelDB empty-store
close/reopen, stable native DB identity, zero documents/update sequence. This
is independent runtime evidence, not CQL/native Holon acceptance. One retained
runtime-probe store, intent and receipt; no business records yet. Native
packages2821files/37236717B; runtime-files.json SHA256
`60f79bf3018a5dfb578097f4f3aab298fa02632f2dfbceb29d0162df4ac04684`.
package-lock SHA256
`8545384a8ede53f68f52614f449f0c690c1826c3cea1693ad876b20abf7c9bc5`.
Initial npm9 and local npm10 --prefix installs both failed saving the dependency
tree; full original logs retained. Executing pinned npm10.9.4 from the actual
package directory resolved context and produced a lockfile, same environment.
No source replay, global install or alternate backend. One local npm tool
(~11.6MB) and2714849B archive retained; no second Node. Next create-once native
HTTP source19600 with original revisions/hash preserved through restart, then
C#/manager/HTTP/browser5141. Hosted enrollment remains separate.
