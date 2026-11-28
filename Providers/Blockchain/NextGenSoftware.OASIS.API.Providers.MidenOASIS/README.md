# Native core storage configuration

Core avatar/avatar-detail/holon storage uses the locked official Miden SDK0.18.1, a public account with the `oasis::kv` component, and encrypted Kubo record envelopes. Claude’s implementation passed26sync+26async checks on the Miden0.18.3 local network (CI45753725053, commit0289f57c). Current Development integration is verified separately; this is not a claim that every optional provider capability or hosted account is accepted.

The normal constructor used by the BootLoader reads these environment/secret-store fields when explicit constructor arguments are absent:

- `OASIS_MIDEN_RPC_URL`: native node RPC endpoint.
- `OASIS_MIDEN_STORAGE_ACCOUNT`: deployed public storage-account ID.
- `OASIS_MIDEN_STORAGE_KEY_SEED`: signing seed; secret store only.
- `OASIS_MIDEN_KUBO`: Kubo HTTP API for encrypted envelopes.
- `OASIS_MIDEN_STORAGE_KEY`: base64 record-encryption key, exactly32bytes; secret store only.

Configure a coherent set. Partial/invalid settings fail explicitly. An unconfigured provider cannot activate storage; public read-only account/block source configuration is separate. No deployment secret belongs in OASIS DNA or this README.

WEB4–WEB10 Docker builds install and verify the locked SDK beside the published bridge via `Docker/install-miden-sdk.sh`. Installation occurs at build time, and its temporary npm cache is removed. The long-lived worker starts only when the provider is used. For standalone managed/NuGet consumers, Node24 and the bridge assets are prerequisites: run `sh MidenSdkBridge/install-sdk.sh "$PWD/MidenSdkBridge"` once after publishing. The package includes its worker, component, locked manifest and installer.

The native CI publishes the test consumer, provisions the same SDK installer, and exercises the shipping constructor against isolated real Miden/Kubo services. It removes owned containers/volumes, fixture and published output. Hosted node/account credentials and deployment acceptance remain separate.

---

# Miden OASIS Provider

Miden privacy provider for OASIS offering private note management, STARK proof workflows, and zkVM interactions.

## Overview

This provider enables OASIS to interact with the Miden blockchain, a STARK-based L2 with private state capabilities. Built specifically for **Zypherpunk Hackathon Track 4: Private Bridge Zcash ↔ Miden** ($5,000 prize).

## Features

- **Private Note Management**: Create, manage, and nullify private notes on Miden
- **STARK Proof Generation**: Generate and verify STARK proofs for private operations
- **Zcash ↔ Miden Bridge**: Bi-directional private bridge operations
  - Lock on Zcash, mint on Miden
  - Lock on Miden, mint on Zcash
- **Privacy Preserved**: All operations maintain privacy through STARK proofs

## Configuration

Set environment variables:
- `MIDEN_API_URL`: Miden API endpoint (default: `https://testnet.miden.xyz`)
- `MIDEN_API_KEY`: API key for authentication (optional)

## Usage

```csharp
var midenProvider = new MidenOASIS();
await midenProvider.ActivateProviderAsync();

// Create a private note
var noteResult = await midenProvider.CreatePrivateNoteAsync(
    value: 100m,
    ownerPublicKey: "0x...",
    assetId: "ZEC"
);

// Generate STARK proof
var proofResult = await midenProvider.GenerateSTARKProofAsync(
    programHash: "bridge_program_hash",
    inputs: new { amount = 100m, source = "Zcash" },
    outputs: new { noteId = noteResult.Result.NoteId }
);

// Bridge operations
var mintResult = await midenProvider.MintOnMidenAsync(
    midenAddress: "0x...",
    amount: 100m,
    zcashTxHash: "tx_hash",
    viewingKey: "viewing_key"
);
```

## Bridge Architecture

The provider supports bi-directional private bridges:

### Zcash → Miden
1. Lock ZEC on Zcash (shielded transaction)
2. Generate viewing key for auditability
3. Generate STARK proof verifying the lock
4. Mint private note on Miden

### Miden → Zcash
1. Lock on Miden (private note)
2. Generate STARK proof
3. Verify proof and mint on Zcash (shielded)

## Status

✅ **Provider Structure**: Complete
✅ **STARK Proof Support**: Implemented
✅ **Bridge Operations**: Implemented
⏳ **Testing**: Pending Miden testnet access
⏳ **Integration**: Pending bridge manager implementation

## Related Files

- `MidenZcashBridgeManager.cs`: Bridge manager for coordinating Zcash ↔ Miden transfers
- `ZYPherPUNK_TRACK_SPECIFIC_BRIEFS.md`: Full track requirements

## License

MIT

