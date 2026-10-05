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

