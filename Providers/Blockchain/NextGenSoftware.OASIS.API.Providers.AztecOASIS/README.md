# Aztec OASIS Provider

Aztec privacy provider for OASIS enabling private note creation, STARK proof workflows, and cross-chain bridge operations with Zcash. Built for the Zypherpunk hackathon.

## Features

- ✅ Aztec API integration (sandbox/testnet)
- ✅ Private note creation and nullification
- ✅ STARK proof generation (placeholder workflow)
- ✅ Bridge helpers for Zcash ↔ Aztec demos
- ✅ Holon storage scaffolding

## Configuration

Add to `OASIS_DNA.json`:

```json
{
  "StorageProviders": {
    "AztecOASIS": {
      "ProviderType": "AztecOASIS",
      "IsEnabled": true,
      "IsDefault": false,
      "ApiUrl": "http://localhost:8080",
      "ApiKey": "optional-key",
      "Network": "sandbox"
    }
  }
}
```

Environment variables:

- `AZTEC_API_URL`
- `AZTEC_API_KEY`

## Usage

```csharp
var provider = new AztecOASIS("http://localhost:8080");
await provider.ActivateProviderAsync();

var noteResult = await provider.CreatePrivateNoteAsync(1.23m, "public_key");
var proof = await provider.GenerateProofAsync("bridgeDeposit", new { noteId = noteResult.Result.NoteId });
await provider.SubmitProofAsync(proof);
```

## Hackathon Targets

Supports:
- Aztec Labs – Private bridge
- Aztec Labs – Unified wallet
- Aztec Labs – Zcash-backed stablecoin

## License

MIT

