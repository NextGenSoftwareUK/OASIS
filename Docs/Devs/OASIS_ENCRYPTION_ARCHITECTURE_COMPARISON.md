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

The protection covers values passing through this backend, including logical indexes—not public native transactions, public NFTs/metadata, or other SDK operations outside it. Chain wallet addresses, transactions, record counts, sizes, timing, and repeated access to the same physical key remain observable. Compression and deterministic addresses leak structure; there is no ORAM, padding, or general searchable-encryption scheme here.

### Why IDs and metadata lookups still work

An authorized application knows the logical ID and key, so it can compute the physical lookup address without storing the ID in plaintext. Exact logical index lookups work similarly. A chain observer cannot directly read the ID/email from that physical address without the secret index key, although equality/access patterns remain visible.

For arbitrary metadata search, the application decrypts and filters. That solves correctness for current key/value tests, not scalability. Enumeration is at least linear in the private physical record count and requires record reads/decryption; histories/index entries can increase that count. Activation currently validates existing private records and can therefore also become expensive. Concurrent writes can invalidate enumeration. Pagination/resource caps and measured large-store performance remain gates.

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
