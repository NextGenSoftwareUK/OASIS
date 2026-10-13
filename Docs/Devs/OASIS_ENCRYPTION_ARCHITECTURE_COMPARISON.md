# OASIS encryption: existing DNA layers and private whole-record storage

Updated 2026-10-12. This is a source-backed architecture comparison, not a claim that every provider or deployed configuration is verified. No migrations of existing user records are currently required according to the user; owned test fixtures still exist and must be preserved.

## Summary and recommendation

You are correct: plaintext database queries need readable fields or a deliberately designed searchable index. Encrypting every field and then asking Mongo to query those fields directly will not work.

The private key/value implementation does something different: it hides both logical record keys and serialized values, computes keyed physical addresses for exact lookup, and performs broader filtering after authorized decryption. It preserves application lookup behavior, but loses native database query efficiency. It is not a transparent encrypted replacement for Mongo indexes.

Recommended direction: unify policy/configuration and key ownership, reuse the existing shared cryptographic infrastructure, but keep distinct protection profiles for private public-ledger storage and queryable database storage. Do not replace the existing metadata exemption mechanism with universal ciphertext, and do not expose exempt private fields on a publicly readable chain. This recommendation is not yet implemented as a unified policy.

## Existing DNA-controlled encryption

Source: `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json`, `OASISDNA.cs`, Core `Helpers/PasswordEncryptionHelper.cs`, `Managers/KeyManager.*`, and `Managers/HolonManager/HolonManager-Private.Helpers.cs`.

| Purpose | Existing behavior | Important limitation |
| --- | --- | --- |
| `Security.AvatarPassword` | BCrypt hashing, optionally wrapped in AES-256-CBC (`r256:`) then AES-256-GCM (`qpq:`) | BCrypt is one-way password hashing, not reversible data encryption |
| `Security.OASISProviderPrivateKeys` | Wallet/key paths use Rijndael library encryption and optional helper quantum wrapping | Do not infer identical behavior from all three flags; trace each caller |
| `Security.HolonDataEncryption` | Manager serializes non-exempt metadata into encrypted `__oasis_enc__`; optional CBC/GCM layers; per-save `DataEncryptionOverride` exists | Not all ordinary holon/avatar fields are encrypted; direct provider calls can bypass this manager stage |

The password/data helper calls its CBC layer Rijndael-256 but implements AES with a 256-bit key and 128-bit block. The quantum helper layer is AES-256-GCM symmetric authenticated encryption, not a separate post-quantum public-key algorithm. Wallet code also calls the existing Rijndael library; these formats should not be assumed interchangeable without tests.

The checked-in default DNA enables BCrypt for passwords/private-key settings while reversible layers are disabled; holon data layers are disabled. Those defaults do not establish the live Railway JSON settings. No secret values are included here.

### Exact metadata exemption list

The built-in list is `_systemMetaKeys` in `HolonManager-Private.Helpers.cs`:

```text
CreatedByAvatarId, Active, HolonType, STARNETHolonId, _versionStamp, data,
Username, Email, Name, Description,
ParentId, ParentProviderKey, SearchQuery, parkType, env,
web3TokenId,
NFT.MintedByAvatarId, NFT.MintWalletAddress, NFT.ParentWeb4NFTId,
GEONFT.PlacedByAvatarId, GEONFT.MintedByAvatarId,
GEONFT.OriginalOASISNFT.MintWalletAddress, GEONFT.LatLong,
Status, ActiveQuestId, ActiveObjectiveId, ParentMissionId, Avatar
```

`HolonDataEncryption.AdditionalQueryableKeys` extends this set. These keys remain plaintext inside metadata for framework/provider lookups. Ordinary top-level IDs and fields are a separate issue: metadata encryption does not automatically encrypt them. Exemption is a functionality decision, not proof that a field is safe to publish; email/location and other exempt fields can be sensitive.

The manager-level processing is upstream of providers such as Mongo. This document does not certify every Mongo save/load route, direct-provider path, or deployed configuration.

## Private whole-record key/value storage

Source: `Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/`:
`PersistedPrivateKeyValueBackend.cs`, `PrivateKeyValueBackend.cs`, `EncryptedKeyValueBackend.cs`, and `KeyValueStorageProviderBase.cs`.

1. Provider base creates logical record/index keys and serialized record values.
2. Private backend derives a separate index key using HMAC-SHA256 with a fixed domain label and the storage encryption key.
3. Physical address is `oasis/private/v1/` plus HMAC-SHA256 of the logical key. The actual logical key and value are placed inside the encrypted payload.
4. AES-256-GCM uses a random nonce and authenticates the physical key/format as associated data. Optional compressed records have a versioned envelope and bounded decompression. Wrong keys, invalid authentication and plaintext records are rejected.
5. Exact reads compute the same physical address, decrypt, and verify the embedded logical identity. Exact index entries (username/email etc.) also pass through the private backend.
6. Prefix/list/search operations enumerate physical records, decrypt their embedded logical keys, filter/sort in the application, and read/filter the resulting records. They are not server-side plaintext metadata queries.

Cosmos/Sui now use mandatory private composition, with separately persisted 32-byte keys currently supplied through fixed environment settings or explicit constructor resolvers. TRON has its own mandatory private wrapper using the shared primitives. Hedera wiring is separately configured; its complete live contract coverage still requires funding. Do not assume all providers use this backend.

Cosmos/Sui BootLoader registration now reads `StorageEncryptionKeyEnvironmentVariable` from each provider's DNA settings, captures that variable name when creating the provider, and resolves key bytes lazily through the existing private backend. The checked-in default JSON explicitly lists these names. This shares the secret-reference configuration pattern with TRON/Hedera but does not merge the metadata/password and whole-record encryption policies. Existing direct constructors retain their default references or explicit key resolvers.

Example configuration references (not key material):

```json
{
  "CosmosBlockChainOASIS": {
    "StorageEncryptionKeyEnvironmentVariable": "OASIS_COSMOS_STORAGE_ENCRYPTION_KEY"
  },
  "SuiOASIS": {
    "StorageEncryptionKeyEnvironmentVariable": "OASIS_SUI_STORAGE_ENCRYPTION_KEY"
  }
}
```

These are fields under `OASIS.StorageProviders`, not a complete DNA document. Configure each referenced variable securely as canonical base64 encoding of an independently persisted 32-byte storage key. Do not put secrets in documentation/source or substitute a signing key. Back up the key; do not regenerate it when reads fail. Railway's environment JSON/settings have not been changed by this work.

The protection covers values passing through this backend, including logical indexes—not public native transactions, public NFTs/metadata, or other SDK operations outside it. Chain wallet addresses, transactions, record counts, sizes, timing, and repeated access to the same physical key remain observable. Compression and deterministic addresses leak structure; there is no ORAM, padding, or general searchable-encryption scheme here.

Cosmos/Sui NFT minting now has a separate explicit public projection for `StoreNFTMetaDataOnChain=false`: native/OASIS identity, standard/provider/collection, public wallets/recipient and the existing supplied metadata URI. It does not publish request JSON, custom metadata, images or avatar IDs, and does not fetch external URLs. This is **not whole-record encryption** or unified privacy policy; the pointer itself is public and any externally hosted data has its own access/protection requirements. Existing metadata exemptions are not copied into the NFT public projection.

### Why IDs and metadata lookups still work

An authorized application knows the logical ID and key, so it can compute the physical lookup address without storing the ID in plaintext. Exact logical index lookups work similarly. A chain observer cannot directly read the ID/email from that physical address without the secret index key, although equality/access patterns remain visible.

For arbitrary metadata search, the application decrypts and filters. That solves correctness for current key/value tests, not scalability. Enumeration is at least linear in the private physical record count and requires record reads/decryption; histories/index entries can increase that count. Activation currently validates existing private records and can therefore also become expensive. Concurrent writes can invalidate enumeration. Pagination/resource caps and measured large-store performance remain gates.

Current retained-chain tests now verify string-equality filters for every built-in exemption key plus a configured application key against both actual Cosmos and Sui providers/official SDKs, with encrypted storage and reopening. This strengthens the shared-backend tests, but does not certify typed/compound/nested queries, Mongo feature parity, every provider, or combined manager+provider deployed encryption. See the dated coverage log for exact reports and cleanup evidence.

## Pros and cons

| Aspect | DNA selective metadata protection | Private whole-record key/value protection |
| --- | --- | --- |
| Native queries | Exempt fields remain queryable/indexable | Only physical exact-key lookup is native; broad queries decrypt/filter |
| Privacy | Protects selected metadata; exposed fields remain | Hides serialized records and logical index keys; chain activity still public |
| Performance | Better fit for Mongo/database indexes | More reads/decryption for listing, search and activation |
| Configuration | Existing DNA and per-save override | Currently separate persisted storage-key configuration |
| Integrity | GCM authenticates; CBC alone does not | Mandatory GCM plus physical/logical identity binding |
| Portability | Manager path shared across providers, coverage must be traced | Shared key/value abstraction, not every provider's native query model |
| Key loss/rotation | Key loss prevents recovery; rotation needs planning | Same; changing storage key also changes physical addresses, requiring explicit reindex/re-encryption |

Neither method should hash wallet keys or full records using BCrypt. Neither guarantees transactional multi-record index updates, authorized access control, or safe key rotation by encryption alone.

## How to merge sensibly

User approved this direction on 2026-10-13 with the explicit invariant that existing exemption/configured-key queries must continue to work. Compatibility coverage now fixes the full built-in list plus an application key in golden tests: manager CBC/GCM preservation and shared encrypted provider sync/async string-equality queries after reopening passed. This does not yet prove all live providers or implement unified policy; native Mongo indexes and encrypted key/value decryption/filtering remain distinct execution strategies.

### Concrete lookup example

For a private holon with ID `123`, the application asks for its logical record key (illustratively `holon/123`; not a literal provider key contract). Storage sees only a keyed address and an authenticated ciphertext. Knowing `123` alone is insufficient to compute the address without the index key. The authorized backend computes it without scanning. An existing logical email index can likewise be addressed by its HMAC; its index value is encrypted too.

By contrast, a query such as “all quests in this park with Status=Active” cannot be evaluated against those encrypted values by a native database/chain query engine. The current private backend enumerates/decrypts/filters. The selective metadata system keeps `Status` and configured query keys readable, allowing the relevant provider query implementation to use them. The two approaches solve different query contracts.

The built-in exemption set uses ordinary `HashSet<string>` matching: spelling and casing matter. Adding a key to `AdditionalQueryableKeys` does not create an index or implement a provider query; it only exempts that metadata entry from manager encryption. Similarly, the reserved `__oasis_enc__` marker means “already encrypted” to the current manager. Application code must not treat arbitrary caller-supplied reserved metadata as trusted proof of encryption.

### Proposed single-policy design (not implemented)

| Profile | Intended use | Cleartext projection | Protected payload |
| --- | --- | --- | --- |
| Password | Authentication | Necessary account lookup fields, subject to separate record policy | BCrypt verifier, optional existing wrappers |
| Provider credential | Wallet/signing secrets | Non-secret credential reference | Reversible secret encryption; never BCrypt the secret |
| Queryable private database | Mongo/SQL-style indexed data | Explicitly approved IDs/query fields | Sensitive metadata/fields with authenticated encryption |
| Private public-ledger record | Private records on publicly readable storage | Physical HMAC address and unavoidable chain transport information | Entire serialized record plus logical key/index values |
| Explicitly public object | Public NFT/object data | Reviewed public fields only | Any associated private data stored separately under a private profile |

One policy owner should select the profile, validate keys and classify fields; managers and providers should consume that decision rather than invent competing rules. Reuse the current exemption mechanism for the queryable profile, but do not automatically copy its exempt fields into the public-ledger projection. A searchable blind index could later support selected equality queries, with separate domain keys, normalization rules and a documented leakage budget. Range/geospatial/full-text queries require a different reviewed design, not merely an HMAC for every field.

A safe implementation sequence is: define profile/configuration contracts; map every save/load entry point (including direct provider calls); keep existing password compatibility; centralize authenticated-envelope/key resolution; test metadata exemptions and lookup parity; then integrate provider profiles. Enforce mandatory private-ledger policy even when a per-save metadata override disables selective encryption. Metadata overrides currently affect the manager stage, not the independent mandatory provider envelope.

### Security and operational trade-offs to retain

- Whole-record encryption protects plaintext from the storage operator/public observer, not from an authorized application holding its key. API authentication/authorization is still required.
- GCM detects changed ciphertext and wrong physical addresses, but does not by itself reject replay of an older valid record at the same address. Trusted version/concurrency rules and replay protection need separate enforcement.
- The storage envelope contains a format version, not a complete key-ID/rotation protocol. Keys must be backed up independently; silent regeneration makes old data unreadable. Rotation of a whole-record key also changes HMAC addresses and requires an explicit, recoverable rewrite of records and indexes.
- Replication must decrypt through the source provider and re-encrypt through the destination provider's policy/key; copying ciphertext across unrelated keys is not a portable record transfer. Selected V2 roundtrips are evidence, not proof of every route.
- Nested manager metadata encryption and provider whole-record encryption can require two distinct keys to read a record. It can be intentional, but otherwise adds failure modes, size and CPU without fixing queryability. Do not remove existing layers until policy ownership and compatibility are verified.
- The existing “quantum” helper and the new envelope both use symmetric AES-GCM. Neither is a post-quantum key exchange, and stacking algorithms is not a substitute for key management or authenticated encryption.

### Source map for further implementation

Paths below are repository-relative, not new APIs:

- `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json`: existing security settings and provider storage-key references.
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Helpers/PasswordEncryptionHelper.cs`: BCrypt/CBC/GCM formats and validation.
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/HolonManager/HolonManager-Private.Helpers.cs`: `_systemMetaKeys`, manager metadata preparation and decryption.
- `Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/EncryptedKeyValueBackend.cs`: value envelope, 12-byte nonce, 16-byte authentication tag, associated data and 4 MiB decompression limit. This class alone leaves keys visible.
- `Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/PrivateKeyValueBackend.cs`: HMAC addresses and encrypted logical-key binding; supplies the key privacy missing from the value-only class.
- `Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/PersistedPrivateKeyValueBackend.cs`: lazy, separately persisted key resolution and lifecycle.

Unify the policy, not blindly the ciphertext layouts:

- Keep the existing password profile and verification format.
- Keep existing reversible wallet-key handling, with explicit failure semantics.
- Keep selective metadata/exemption configuration for queryable private databases.
- Define a mandatory private-ledger record profile that reuses shared authenticated cryptography and separately persisted keys, with domain-separated encryption/index keys. Do not derive storage keys from blockchain signing credentials.
- Connect that profile to a documented DNA configuration surface and secure secret resolver rather than having independent undocumented environment conventions. Validate enabled layers and required keys before writes.
- Avoid applying both metadata and full-record encryption by accident. Define intentional nesting only where threat models require it; public/queryable projections must be explicitly classified, not inferred from a legacy exemption list.

This can share primitives and policy validation without forcing Mongo's queryable representation into the on-chain private representation. A future blind-index/search design needs its own leakage/performance review, not a quick field-hashing patch.

## Current evidence and remaining work

- Existing DNA helper compatibility: 7 tests passed for password and reversible layer combinations.
- Shared private storage: 101 tests passed, including key configuration, restart, identity/authentication checks and bounded compression tests.
- Actual retained Cosmos/Sui encrypted holon roundtrips and selected V2 replication/load-balancing/failover tests passed; see `HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md` for revision-scoped reports.
- Existing helper audit found silent skips with enabled/missing keys and ciphertext returned after decryption failures. Core fix `691cfcfa5e1e3dcf765cab5e3e751422b9b59cc6` now rejects missing enabled keys during password/data encryption, and surfaces missing-key/authentication failures during data/quantum decryption. Core builds and all 440 unit tests pass, including new negative cases. The fix is pushed but not deployed; end-to-end manager/provider/deployment regressions remain required. Password verification still returns false for invalid passwords or failed verification.
- Unified DNA/private-ledger policy, end-to-end Mongo encryption coverage, live Railway settings, large-store costs, rotation, all-provider/interface coverage and production recovery are not complete. No merge/release claim follows from this comparison.
