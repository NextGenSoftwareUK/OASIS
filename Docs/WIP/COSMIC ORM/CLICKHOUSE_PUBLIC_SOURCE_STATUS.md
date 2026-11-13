# ClickHouse public source qualification

Updated2November2026. No CQL/native-write/hosted/browser acceptance yet.

## Free public access

[Official playground documentation](https://github.com/ClickHouse/clickhouse-docs/blob/89036285e33b88b424ebc397cce3cb70e9c2b5a9/docs/getting-started/playground.md)
publishes HTTPS play.clickhouse.com, userexplorer/play, empty password, read-only
SQL and usage quotas. No new account/server/install is needed. This is a public
operator dataset, separate from owning/saving COSMIC holons in ClickHouse.

## Original qualification and preserved failure

Probe-ClickHousePublicReadiness.py has7controlled tests passing. First native
identity query returnedHTTP200 and verified explorer/read-only mode. The selected
stackoverflow.posts schema query returnedHTTP200 with no matching columns, so the
qualifier stopped before either post query. Original qualification remains failed;
never replay it or relabel it successful. Native writes0.

A separate explicit public catalogue discovery returnedHTTP200 with at most64
system.tables names, not user content. It includes blogs.countries and
blogs.country_codes, as well as default.hackernews and other public datasets.
The original absent-table gate is preserved; choosing an actual catalogue table
requires a new source-specific qualification, not a silent producer fallback.

## Durable evidence and next step

Original intents/reports/compressed raw bodies/SHA256manifest:
LocalState/COSMIC/ClickHousePublicQualificationCurrent.
Raw results: TestResults/ClickHousePublicSourceCurrent.

Next inspect the exact blogs.countries schema using a bounded metadata query,
then qualify three existing country keys and a two-key filtered subset. Keep
SQL templates fixed/parameterized, bound reads/time/body size, preserve Unicode
and integer precision, reject native errors/partial pages. Add a separate
read-only CQL profile only after that source contract passes. Legacy provider
implementation remains unchanged/docs-only. No CQL/provider count increase.

## Countries source qualified - 2 November 2026

Exactblogs.countries schema: nameString, coordinatesMultiPolygon. New source-specific original3/2queries pass forFrance/Germany/Italy thenFrance/Germany, identical subset projections. Polygon counts21/22/29 remain canonical UInt64decimalstrings; no raw geometry or personal/contentpayload copied.4newcontrolled tests pass, including UInt64max/invalidnumeric/duplicate/oversize/keyset cases. The previous absentStackOverflowtable qualification stays failed and is not replayed. No writes/account/install. Durable countriesintent/report/compressed originals/SHAmanifest retained beside original failures. No CQL/provider count increase yet.

countries-schema.json:374bytes SHA256 `61e838f12ca06ff1d444eee333a46068c009d6d67bb2ff7905cef68b73f6600c`.

countries-1.json:434bytes SHA256 `8e5bd2001beeeaf381110515db57c752849364f8eacc978a38355a8ad3bbd473`.

countries-2.json:382bytes SHA256 `8fd47281ba99e60c6f0d39f02c32c451a34f8072f90e8276366e262a1bd0e613`.

Exact next step: implement explicit read-only ClickHouse country metadata source, configurednameeq/IN+paging using fixed parameterizedSQL and guardedHTTP; reject writes/nativeerrors/incompleteselections before publication. Then manager/CLI/localHTTP+Mongo gates, tariff/hosted integration and sandbox card. Keep legacy ADO/storage paths unchanged; no fullSQLparity claim.

## Read-only CQL adapter controlled milestone

ClickHousePublicCountryMetadataSource builds using existing cacheddependencies.25/25new controlled tests pass, zero skipped. Supports ClickHouseCountryName equality/IN+paging, result budgets, fixed typedStringparameter SQL, schema/readonlyuser checks, complete-keyset validation, canonical UInt64polygoncounts, malformed/oversize/nativeHTTPerrors/exceptionheaders, cancellation and explicit no-write boundary. Native parameter values never enter SQL text. No native CQL/manager/CLI/HTTP/hosted proof yet; counts unchanged.

Initial run22pass/1fail revealed the existing shared key strategy excludes Unicode/spaces. Adapter configuration now enforces that same printableASCII33-126 contract rather than accepting unreachable keys; controlled quote/backslash/injection values remain safely parameterized. Unicode/spaced country keys are explicitly unsupported by this profile, not encoded/rewritten. Original failed TRX retained.

Final evidence TestResults\ClickHousePublicSourceCurrent\adapter-point.trx SHA256 `ac03c1def60e6f3ea8ffbdb5dbe96fccf5ecae33ad5a3c05db23e6aa4d61d040`. Next native compiled manager/LINQ3/2 over original France/Germany/Italy source and exact parameterized HTTP contract, then localHTTP/Mongo/hosted admission.

## Actual compiled manager/LINQ/CLI acceptance

Full shared demo cachedrestore/build passed1m48s. Original --clickhouse-public run passed3full/2filtered countryrecords through HolonManager/deferredLINQ and nativeHTTPtypedStringparameters; originalFrance/Germany/Italy counts21/22/29 matchqualification. No writes/newDB/account. Stable sourceowner/config and source/qualification/binary-bound intent/ACK/compressed output retained HostedClickHousePublicSourceCurrent. Output SHA256 `bdf840e82c4f48b178b3e5bd7033bf72fc9f53cc2a8865fd30f3fa2a508e79a4`. Accepted CLI/source frozen, no replay. Next shippingHTTPregistration/compiler/readtariff/presets and localMongo/privacy gates before hostedpublication. This is63rd locallyintegratedtype; hosted58unchanged.

## HTTP admission controls

ExplicitCQL_CLICKHOUSE_PUBLIC_SOURCE_JSON registration/compiler/independent readtariff added.3/3APItests+1/1Coretariff pass, zero skipped;4native/Mongo3/2presets plan, missingpermission/tariff and writetariff refused. Coretask61f1ff288462518abef1208cc62dfb186f93cd1f pushed/remoteverified; parentpin/manifest updated together.10newpurehostedpublisher controls pass; no livebaseline/candidate/settings/deployment yet. Original localHTTP/Mongo/privacy gate remains next and required before publisher preparation.

TestResults\ClickHousePublicSourceCurrent\api-admission.trx SHA256 `9363a515a8a0e669cc1b9a0544d1028f0908d59dd3fc4ae6fe02be224f68b9fd`.

TestResults\ClickHousePublicSourceCurrent\core-tariff.trx SHA256 `3f8c0ecba67b6fe563e4ea1e1bef9a1ebc308c9a52c5145c3b6f16ec3fa26f91`.

## Original local HTTP/Mongo gate and hosted candidate

Shipping endpoints passnative3/2+Mongo5/4, foreignprivate0/public3,2SDKownedversion1saves/9queryreceipts. Exactly1nativetestpassed14s, no skips. HTTPconsumers stopped; exactnonceownedDBcosmic_cql_clickhouse_http_demo removed/absenceverified, ownedMongo stopped/shareddataretained. Sourceconfigunchanged. JournalClickHouseMongoHttpCurrent and source-bound web4localACK/compressedoriginalevidence underHostedClickHousePublicSourceCurrent frozen; no replay.

Hosted protected baseline/candidate prepared:exactly3changedvalues/freequerytariff/4native+Mongopresets/DNAunchanged; no settings/deployment. Actual shippingpreparedpresettest passes1/1 covering4presets. Generic hostedverifier now admitsClickHouse CLIoption. Next independenthostedacceptanceaudit controls, fetch/merge/preserveupstream, verifycombinedcode/manifest, publishDevelopment then exactsourceactivation/hostedcohort. Counts63integrated/58hosted remain.

TestResults\ClickHousePublicSourceCurrent\mongo-http-native.trx SHA256 `74dcb24d59fd5fed8ab823f7d5deb8e62962059899b2800eb586f2197f547c47`.

TestResults\ClickHousePublicSourceCurrent\prepared-presets.trx SHA256 `d1603920c515b3f407f86021e82b355af8ea7913d31de313027c7968abc5dfe3`.

## Merged release qualification

Fetched/merged latestDevelopment8bfc03c82/Core87a63bb, preserving GeoNFT/accountauthority/place-reward fixes. CombinedCore0659aa00ba26260ec4c06f196df7b60ee6c0df01, parent237d90dfa; manifest7Dockerfiles/gitlinks pass.3ClickHouseAPI+1Coretariff+4actualGeoNFT/place-rewardmethods pass. One separateWorldscapeNativeCreditBudget Mongo test skipped because its explicitnativeURI is absent; not counted as pass. Initial filename/classfilters missed partial-class methods; final exactmethodfilter ran4cases successfully, alloriginaloutputs preserved. Independenthostedaudit2tests/16negativecases pass withoutnetwork. No acceptednativefixture replay.

TestResults\ClickHousePublicSourceCurrent\combined-api.trx SHA256 `a78956f2096dc27c0f6a6d47ee4d9f002cae71487004548bb478f2e895b7fa50`.

TestResults\ClickHousePublicSourceCurrent\combined-core.trx SHA256 `201024fb5b0ad2e17bbb7e0267a2cca731e4d04ca5c630ef7f9b033e67bd792b`.

TestResults\ClickHousePublicSourceCurrent\merged-place-guards.trx SHA256 `9e44b9340b7547f4fb6a255af77015ab64cfe85f08a54b3480b090da55780945`.

Next publish exactmergedcode once, observeitsjob, apply original3settingcandidate only afterSUCCESS/fullbaselineequality, activateonce, then original hostedcohort/audit/card.

Developmentpublished parent7f477378b02adda621376f88f309c9bf70675fd9/Core0659aa00ba26260ec4c06f196df7b60ee6c0df01 (ls-remoteverified), after preservedupstreammerge. Exactcodejobaae1a2b0-8eca-43e7-98e8-2bf4a5c9305d INITIALIZING. Originalcodepublicationintent/jobtuple retained HostedClickHouseReleaseCurrent. No3settingsapplied; followexactjob, no duplicatebuild/restart on observationtimeout.

## Code success and settings activation

Codejobaae1a2b0SUCCESS at7f477378b. Original3settings appliedexactlyonce; per-keyACKs/exactcompletecandidateequality/DNAunchanged verified. Oneactivation78d92d9b-8e3f-4ad2-9be0-25b57bd6563a BUILDING atsamecode. OriginalsettingsACK/activationintent/protectedsubmission/jobtuple retained HostedClickHouseReleaseCurrent. No hostedcohort/session/nativequery/save yet. Followexactactivation, no resubmit/restart. Hostedfunctionalcounts58unchanged.

## Activation superseded by newer Development release

Originalactivation78d92d9b becameREMOVED. InventoryshowsnewerDevelopmentjob22cabb60-492a-432b-8d4b-597a068d621b BUILDING atc0edcb0ed0b81e5d5dd20fc99807163d09833037 (sharedgarden/parkwork). Gitancestry confirmsit includesverifiedClickHouse7f477378b. Complete livevariablesstillmatchoriginalcandidateexactly, all3values/DNAunchanged. Durable superseding-deployment.json retainsidentity/ancestry/settingsproof. Follownewexistingjob toSUCCESS; no restart/redeploy/settingsreapply or nativecohort/session created. No assumptionthat removedjob itselffailedsourceactivation.

## Original hosted acceptance independently audited

Supersedingjob22cabb60SUCCESS atc0edcb0ed0b81e5d5dd20fc99807163d09833037 (includesverified7f477378b). Originalsettingsnotreapplied. DiscoverystartedNOT_READY thenavailable4presets; sameoriginalowner reused, no native/private dispatchbeforereadiness. Hostedoriginalnative3/2, Mongo5/4, repeat0new/2existing, foreign0private/3public, exactSQLite/Neo4j2each.2normalAPIversion1saves/pending0; independentofflineaudit passes with no extra reads/saves. No replacementcohort/reseed/continuation. Originalcohort/source/config/operations frozen. Hostedfunctional59; ThreeFoldCLI makesintegrated64. Browser/cardnotyetverified/published forClickHouse. No fullORM/ownedClickHousewrites/availability certification.

ClickHouseOASIS-native-http.json:12183bytes SHA256 `11368e6312f9a4d99fc205cd3b60efdf639c22ee110b7ad943c3e7d1779c0cd7`; compressedoriginal durable HostedQueryAcceptance-ClickHouseOASIS.

ClickHouseOASIS-client-http-flow.json:32819bytes SHA256 `4e6f482a94e114a3d20499f4c3f43048b7e539f25e97f1498f30c70da798450b`; compressedoriginal durable HostedQueryAcceptance-ClickHouseOASIS.

ClickHouseOASIS-isolation-http.json:4497bytes SHA256 `f54b343acb7b72821a5b302bfb350d6bc19a416df2c9e7ac48ad6a9e276476a2`; compressedoriginal durable HostedQueryAcceptance-ClickHouseOASIS.

Next publishClickHousecard withpinnedguide/C#links and byteverifyassets; browserinteractionremainsclosed.

## Sandbox card published and static files verified

Website commit `d3dfe48a4d4e4ff0288180303fbbb06ab88eaebb` pushed to task/main, remote tips verified. Public card has four-preset instructions and pinned guide/commented C# links; existing local ClickHouse card preserved. HTML parses with 155 unique IDs (initial check caught duplicate new/local card ID; corrected before publication). All three public GETs returned 200 and exactly match committed Git blobs: HTML 201382 bytes SHA256 `97b17c1e4532a464783989fb198e957c62d460e6059f7abda29f21e51d34209b`; preparation JS 10241 bytes SHA256 `c1006abf6074616428f93e83e13774ba8a6198f35d8ed097d84323466a8b309a`; playground JS 21871 bytes SHA256 `7cb6210b9adda1e83630fc966926772f8338c43163c24d1abd88c626ad1ad649`. Compact acknowledgement: `LocalState/COSMIC/HostedClickHousePublicSourceCurrent/website-publication.json`; raw static copies: `TestResults/ClickHousePublicSourceCurrent/website`. No native query/save replay. Browser interaction remains unverified. Next: ThreeFold HTTP/Mongo admission.
