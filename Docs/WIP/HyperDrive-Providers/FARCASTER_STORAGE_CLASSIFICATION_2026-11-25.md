# FarcasterOASIS: native storage classification (2026-11-25)

**Finding: Farcaster cannot hold arbitrary keyed, replaceable, removable records within protocol rules.
FarcasterOASIS should be classified as a social/network (non-storage) provider and excluded from the
shared native core-storage suite.** No provider code was changed and no fixture was built, so this
records no pass or fail result for the suite.

## Evidence (Farcaster protocol specification and hub validators)

The sources are `farcasterxyz/protocol` `docs/SPECIFICATION.md`, `hub-monorepo` `packages/core/src/validations.ts`
and `snapchain` `src/core/validations/message.rs`, all read on 2026-11-25. Hubs store only these CRDTs:
UserData, Cast, Reaction, Verification, Link and UsernameProof.

| Message type | Keyed/replaceable? | Can it hold an arbitrary value? |
|---|---|---|
| UserDataAdd | Yes, one value per `(fid, UserDataType)` | No. `UserDataType` is a closed enum (PFP, DISPLAY, BIO, URL, USERNAME, LOCATION, TWITTER, GITHUB, BANNER, ...), each validated for size and format (for example BIO is at most 256 bytes and USERNAME must be a valid fname). There are about 10 slots and no user-defined keys. |
| LinkAdd/LinkRemove | Keyed by `(fid, type, target_fid)` | No value field. `type` is at most 8 bytes and `target` must be a known fid. |
| Reaction | Keyed by `(fid, type, target)` | No value field. Type is LIKE/RECAST. |
| CastAdd/CastRemove | No. Content-addressed by hash, so it cannot be replaced by key | Text of at most 1024 bytes, 2 embeds of 256 bytes each and a 256-byte parent URL. Casts are public social posts that clients render in feeds, so encrypted blobs there would be spam, not honest app data. |
| FrameAction | Validated but has no CRDT/store | No. It is never persisted by hubs. |
| Verification / UsernameProof | Fixed semantic payloads (address proofs, fname proofs) | No. |

Durability also fails at the protocol level. Section 3.1 says each CRDT has a per-storage-unit message limit
(Cast 5,000; Link 2,500; UserData 50). When a new message crosses that limit, the oldest message is pruned
silently. If storage units expire, all messages are pruned after a 30-day grace period. An acknowledged
write therefore has no guarantee of being kept, which breaks the suite's persistence invariant. Storage
units are also on-chain rent in the Storage Registry.

Unlike Nostr (NIP-78 kind 30078 application data), Farcaster has no application-data message type.
Putting records into casts, chunking them across casts, or overloading link types would be a protocol
abuse built on top of the protocol, not native storage.

## Recommendation

- Keep FarcasterOASIS as a social/network provider (cast publishing and reads, user lookup) and declare it
  non-storage in the provider registry/matrix.
- Do not add a `farcaster` case to `.github/workflows/popular-providers-native-storage.yml`.
- Separate issue, not fixed here: `FarcasterOASIS.cs` currently claims `ProviderCategory.StorageAndNetwork`
  and depends on Neynar's hosted API (`https://api.neynar.com`). Correcting the category belongs in the
  provider-classification batch.
