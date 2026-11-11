# Hyperlane public source qualification

4 November 2026. Open. No native CQL or hosted acceptance.

Hyperlane documents anonymous indexed message reads and a message ID equality
query. Its operations guide identifies the Hasura Explorer endpoint.
The scoped candidate is read-only lookup over a fixed catalogue of distinct
32-byte message IDs, using exact ID equality and bounded membership. Project
origin/destination domain IDs and indexed delivery state. Keep message body,
wallet addresses and payment data outside the demo. Delivery is explorer-reported.

- [Hyperlane GraphQL contract](https://docs.hyperlane.xyz/docs/reference/explorer/graphql-api)
- [Explorer operations guide at inspected source pin](https://github.com/hyperlane-xyz/hyperlane-monorepo/blob/46bd67392990755ef5220e7622446a56fdd918f6/docs/ai-agents/operational-debugging.md)

First qualification requested three delivered message IDs ordered by send time
from the documented Hasura endpoint. HTTP 200 carried a GraphQL
time-limit-exceeded error at the project's five-second cap. Zero records were
qualified. Preserve the original request and response under
TestResults/HyperlanePublicSourceCurrent. Response 150 bytes, SHA256
3296b5315701cad409882cd4dce9f75fbd6a6b248fc478e60981b634b87e51a6.

Next qualify known IDs using the documented exact-key operation with a separate
recorded intent. Preserve the original failed discovery. Require matching IDs
and exact three/two coverage before implementing the provider profile. No account,
write, package, runtime, tariff or hosted setting was created.

## Exact message IDs qualified

A separate lineage-bound discovery selected at most three IDs without sorting
or filtering the whole index. Three distinct 32-byte IDs returned. Exact-key
membership requests for all three and the first two returned exactly the
requested sets, both HTTP 200. This qualifies indexed source access only.
The first timeout remains recorded. Raw evidence and intent hashes are retained
under LocalState/COSMIC/HyperlanePublicQualificationCurrent.
Next implement the bounded read-only provider profile against this catalogue.

## Native CQL profile and WEB4 build

Five focused tests passed with zero skips. Native provider activation and CQL
execution over the preserved Explorer catalogue passed one test with exact
3/2 results and zero skips. Native TRX SHA256 16315AE133D09811FDDD764713F2B23CC7F7854DCC16A2624F30856FF71394C5.
WEB4 builds with explicit query-source registration. Next verify the HTTP
presets and Mongo federation/privacy, then prepare guarded hosted admission.
No deployment or hosted coverage increment.

## WEB4 preset and rollout controls

All four presets plan against shipping native and Mongo capabilities in one
passing test. TRX SHA256 FF040A9DA82CB8C46E2D64A627DD6BB89A01B540D11ABE27D3B93A0F1107C2C1. The published client derives
the exact two-message preparation request. Two offline rollout checks pass.
The source-bound native receipt is retained. Local HTTP acceptance requires
an owned loopback Mongo replica set. This laptop has MongoDB 8.2 installed
and an unrelated service on 27017. The former laptop's 8.3.11 binary and
replica store are absent. Use a separately owned reusable local test store
with explicit identity and keep the existing service outside the test.
No WEB4 configuration or deployment has changed.

## Local HTTP continuation passed

The original two saves were acknowledged before HTTP 500. The read-only
continuation isolated the missing Core tariff allowlist entry, then passed
native 3/2, Mongo 5/4, foreign-private 0/public 3, and nine measured receipts
with zero additional saves. Exact fixture database cleanup verified. Original
failed reports retained. Final TRX SHA256 257B6A7187172CBCDE886F7727CF3DF30DBD6BA7FDCB05C24F809589256672A6.
The source-bound local receipt now permits protected candidate preparation
after Railway sign-in. Hosted settings, deployment and visitor proof remain open.

## Development batch preflight

On 4 November, GitHub confirmed Core `89d39f6c2` on
`work/cosmic-hyperlane-admission` and parent `4f8118437` on
`work/cosmic-cql-ton-public`. Development `b718f2879` is an ancestor of
the verified task head and has no Development-only commits in this snapshot.
Recheck ancestry at publication time. The LocalFile qualification bundle has
since been recovered and its candidate image published. Railway still returns
Unauthorized. Follow the [LocalFile rollout](LOCALFILE_CQL_MVP.md) first, then
prepare the protected Hyperlane candidate and verify its hosted acceptance.

Offline sequential assembly using both retained qualified bundles passed.
Starting from synthetic enabled-sandbox settings with the free IPFS tariff,
the LocalFile candidate followed by Hyperlane preserved the existing LocalFile
source, bootstrap, catalogue and presets. The shared admission retained
IPFS and LocalFile and appended Hyperlane at zero cost. Prepare Hyperlane
from the fresh live baseline after LocalFile acceptance so the shared
admission includes the accepted rollout. Live compatibility remains open.

The reviewed LocalFile image source `c64adf474` contains the Hyperlane WEB4
registration. Its Core pin `a9a238371` descends from the verified Hyperlane
admission commit `89d39f6c2`. Review those source and Core pins when
considering reuse of the image for Hyperlane after LocalFile acceptance.
Hyperlane settings and runtime acceptance still require separate verification.

With the retained qualified catalogue, both generated paired presets passed
the published Web4Site `2170b67` preparation recipe offline. It selected
the first two message keys through `HyperlaneMessageId` and retained the
three- and two-key federation selections. No API calls or saves were made.

## Development enrollment resumed - 7 November 2026

Original source-bound native3/2 and local HTTP5/4/privacy receipts remain
present under HyperlanePublicQualificationCurrent; no fixture replay. Current
82428c7a6 already ships the Hyperlane reader and Core2a09c9be admits its free
query tariff. Against the actual accepted NATS baseline, the existing publisher
changes exactly three values and preserves all other settings. Its four presets
retain the exact three/two message IDs and explorer-reported-delivery-state
policy; this does not establish independent cross-chain finality.

Protected snapshots prepared from the fresh baseline, then three writes were
acknowledged and read back once, with no per-write deployment. Exact healthy
source/image and complete candidate rechecked before one activation request.
Settings jobcf69f1ba-2a18-4389-b658-4ab31d7bee71 isBUILDING at82428c7a6.
Intents/protected response/ACK and compact baseline proof retained in the old
laptop state folder. Next follow that exact job, verify current source/settings
and first hosted native/Mongo/reuse/privacy/replica HTTP scope. No hosted cohort
has been created, no native source writes/wallet/transaction actions occurred.
NATS original acceptance cohort and all owner deferrals remain preserved.

## Hosted HTTP accepted - 7 November 2026

Activationcf69f1ba-2a18-4389-b658-4ab31d7bee71 SUCCESS, instance
f7e77b49-8889-480d-987c-a62e5c537668, source82428c7a6/Core2a09c9be,
image sha256:87d32708257cc79d05959d63d2e629eab205b536681e61824c8e333b261000da.
Exact release, complete settings and all3original ACKs verified before the
first hosted flow; release/settings unchanged afterwards. Native3/2,
Mongo5/4, first2private saves, repeat0new2existing, foreign-private0/public3
and exactSQLite/Neo4j replicas2/2 passed. Original owner/foreign sessions,
2save ACKs, empty pending intents and report-bound acceptance receipt are
sealed under LocalState/COSMIC/HostedQueryAcceptance-HyperlaneOASIS.
Never replay the accepted cohort. Source API reads report explorer delivery
state; no independent finality proof, wallet/transaction or source writes.

Reports in TestResults/HostedCqlIntegrationCurrent:
- HyperlaneOASIS-native-http.json:13401B,
  f8a4a2f3eaad7647c0ce94577d72529cc201e1b5d3fa8fa1d0c538f21e6fbc0b.
- HyperlaneOASIS-client-http-flow.json:35961B,
  0bc7013704835018769687aa338b15541fcf70a6822c9c7829a86ce3f5748306.
- HyperlaneOASIS-isolation-http.json:6065B,
  75a8c69a90d3639c66da2f7053dc52cdc03c95cc4496a0928864a1385f94949e.
Receipt sealing used the normalized discovery operation Rows, not the
configuration default Query label; only report validation was corrected,
without replaying HTTP or saves. Coverage119local/80liveHTTP/28browser.
Next publish the scoped hosted demo card and retain browser as an open gate.

## Public demo published

[Hyperlane card65](https://web4.oasisomniverse.one/web4-sandbox.html#cql-hyperlane-catalogue)
is published atWeb4Sitec34a4b6c21c25f01101ba7593f9ecffac71804bc on the existing
website task/main branches. It describes explorer delivery observations,
native3/2, Mongo5/4 and private snapshot reuse, with real C# query/evidence
links pinned tobackendcc95932bd. All119 website controls pass. Public HTTP200
HTML248913B SHA256e98bb31b143f81791ff94faa96b6459da5ed79bf63adecf9139f7827b2708848
matches the exact committed blob. Compact receipt retained; no downloaded HTML,
new environment or accepted cohort replay. Browser interaction remains open.

11 November UTC: HyperlaneOASIS indexed message observation presets returned 3 full and 2 filtered rows in the live Development WEB4 CQL Playground. Original-owner paired browser verification remains open.
