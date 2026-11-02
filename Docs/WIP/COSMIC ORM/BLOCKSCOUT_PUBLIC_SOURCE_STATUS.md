# Blockscout public address CQL — native qualification

2 November 2026. Native reads qualified; CQL/source owner/hosted admission/card
remain red. Counts unchanged:56integrated/51hosted/5browser46open.

## Contract and native evidence

Existing provider default is https://eth.blockscout.com/api/v2/ with optional
API key. Legacy adapter remains unchanged. Its null balance default must not be
copied into a new read profile: unknown balance is not zero or indexed-account
existence. Official current source explicitly returns200for valid unindexed
addresses as well. The public explorer documentation URL /api-docs returned404
in web inspection; that was not a native address API failure.

[Official address controller](https://github.com/blockscout/blockscout/blob/master/apps/block_scout_web/lib/block_scout_web/controllers/api/v2/address_controller.ex)
· [Official documentation index](https://docs.blockscout.com/llms.txt).
Current Pro endpoints use a separate authenticated interface; no Pro account,
key, payment or endpoint substitution was used for this qualification.

One exclusive native qualification used the production guarded .NET transport,
no authorization/cookies/user-agent override and three bounded anonymous GETs.
All returned200with matching address hashes, boolean contract flags and exact
string balances. Original observations include an individually reported balance
update block number; the three records are not a shared finalized snapshot.
Six controlled cases cover exact/nullable balance, wrong address, numeric and
noncanonical balance, and missing identity. One explicit native test passes.
Native tests skip without their explicit environment gate; skipped tests are not
native proof. No source writes/accounts/transactions were created.

Original addresses:

- 0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2
- 0x6b175474e89094c44da98b954eedeac495271d0f
- 0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48

Proving test: OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/BlockscoutPublicNativeQualificationTests.cs.
Intent/outcome: LocalState/COSMIC/BlockscoutPublicQualificationCurrent.
Preserve original responses and consumed intent; no replay/requalification.

## Next red scope after Celestia publication

Implement an explicit query-only explorer-address source with equality/membership
and paging over the original three addresses. Retain exact balances as strings,
model nulls as absence, preserve per-record update heights and indexed names,
and exclude private/watchlist/tag fields from holon projections. No independent
consensus/finality or canonical provider writes are established by this data.
Require capability/error/precision/budget controls, actual C# walkthrough,
HTTP/Mongo/privacy/replicas and original hosted acceptance before a public card.

| Evidence | Bytes | SHA256 |
|---|---|---|
| `LocalState/COSMIC/BlockscoutPublicQualificationCurrent/qualification.intent.json` | 332 | `53bac5cca7e8985a5ed26e4a433dfe2157e09b514ff238fe4c698cd02621b314` |
| `LocalState/COSMIC/BlockscoutPublicQualificationCurrent/qualification.observed.json` | 618 | `95033c17eda549a0f4d1265c712116e33a23abe8e9592beb0fefb5b780481f3f` |
| `TestResults/BlockscoutPublicSourceCurrent/qualification-controls.trx` | 12113 | `138b9d390e65c18514204588624438b24476b041d3db8b1129cf493a5f07b47c` |
| `TestResults/BlockscoutPublicSourceCurrent/native-qualification.trx` | 3351 | `f8e0bb3c054565cb3e3b725f6623b3a0b7e67db18bd3cf2d6224fce02dc92dfb` |
| `TestResults/BlockscoutPublicSourceCurrent/address-0.json` | 1169 | `2283dd252446126841e786cf07185cae34ce8fa9715cfc396a8ba5acf40f6dff` |
| `TestResults/BlockscoutPublicSourceCurrent/address-1.json` | 1144 | `1971218ecb5f6ee19be260c4ceac7fa7212de7bdef26b2e88925d571ea8f7ac1` |
| `TestResults/BlockscoutPublicSourceCurrent/address-2.json` | 1283 | `dad217c59852abdbf34cf49376acd83ce1402f61b631b14c808f375072b7fe72` |
