# AWS direct configuration checkpoint — 2026-10-20

Status: partial provider verification; not hosted AWS or full HyperDrive certification.

## Configuration boundary

The production boot factory already rejects missing explicit settings. Direct public DynamoDB/AWS construction did not consistently enforce that contract: blank access keys, whitespace secrets, blank signing regions with endpoint overrides and whitespace session tokens could reach SDK client construction. An empty secret was rejected by the SDK itself rather than the provider boundary.

The DynamoDB constructor now validates region, explicit access/secret and a supplied nonempty session token before constructing the SDK client. AWSOASIS inherits this constructor boundary. Empty/null session tokens retain basic-credential semantics; explicitly selected SDK default credentials remain supported, with mixed explicit/default credentials still rejected. No credential trimming, guessed region or credential fallback was added.

Seven controlled regression cases initially failed (six accepted invalid configuration; one rejected only through the SDK's ArgumentNullException). After correction, the full DynamoDB controlled suite passed **306/306**, zero skipped. Producer and consumer builds succeeded with zero errors, references disabled; captured Core SHA256 `C643340449BAF3DA817BD2A0FE4B394DB36682E2A6C735DDB9573AC9BCF6261E`. No native service, network operation, download or new environment was needed.

Evidence under `Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests/TestResults/`:

- Before: `david_OASISOMEGA_2026-10-20_11_50_46_net10.0.trx` (seven controlled failures), SHA256 `5CBCC7E5277FC8BF0C01DC81C95CA92449DE5A536801A61936842B830390414D`.
- After: `david_OASISOMEGA_2026-10-20_11_51_09_net10.0.trx` (306 passed), SHA256 `0A60DCE5719947AFFBA98A6EBE4029B051F25D687EA3B5E4CE86D6D8DD4D70C7`.

## Read cost/performance finding

Source inspection confirms username/email identity lookups and parent/collection reads use paginated, strongly consistent table scans. Activation creates username/email and parent secondary indexes, but current read paths do not query those indexes. Therefore scan cost grows with table size, and those indexes also consume storage/write capacity without serving these current reads. AWSOASIS is a DynamoDB facade, not an implementation of every AWS storage product.

Do not replace authentication scans with eventually consistent secondary-index queries without preserving ambiguity detection and current credential authority. A scalable authoritative identity-index design, its concurrency/recovery semantics and real hosted cost measurements remain open. This checkpoint does not remove existing indexes or modify deployed schemas.

## Remaining proof

Existing SDK session/default-chain support is not a new implementation here. Hosted IAM/STS role refresh, SSO, permissions/TLS, current native round trips and complete interfaces/unknown-outcome recovery still need their respective evidence. Earlier local native tests do not establish all AWS or all V2 policies. No merge, push or Railway deployment in this checkpoint. Free C before builds: 68,810,088,448 bytes; the requested disk-space target remains unmet.

## Follow-up native evidence

The same retained DynamoDB runtime was subsequently used for **95/95 native cases**, zero skipped, eight seconds, runner terminal exit zero. Six existing create/replace/history cases now additionally verify exact 28-digit decimal metadata and numeric-looking string preservation in current and archived records, across avatar/avatar-detail/holon and both DynamoDB/AWS facades. This expands assertions, not the case count. The test exercises real official SDK requests against DynamoDB Local; it is not hosted AWS/IAM or universal HyperDrive policy proof.

Report: `TestResults/DynamoHistoryCurrent/dynamo-history-native-current.trx`. Previous report was SHA-verified then moved in place to `dynamo-history-pre-native-decimal.trx`, with no duplicate runtime/data environment or ledger reset. Captured Core SHA256 `03766A44A7A20F6452CDFC22D14B1749FD0B85DC32B4914F1A90468A911A521A`, Shared SHA256 `A8AAF9C149B61BBE64E0C16AAB7CDACA2B5B80E12C2C1745B032963E6A893095`; this is a later dependency epoch than the controlled306 run above. Do not present the two as one immutable artifact certification.

Report SHA256 `6F593A81476C49C7CCBC3279C641FE01DE232DFFC2538111BB3A1311F6CE9BE1`. Independently checked afterward: zero pending native journals, no listeners on 18000/18001. Free C after run: 68,801,224,704 bytes. Runner stopped its owned service; no disposable fixture database copies were created. Compact old/new evidence is retained.
