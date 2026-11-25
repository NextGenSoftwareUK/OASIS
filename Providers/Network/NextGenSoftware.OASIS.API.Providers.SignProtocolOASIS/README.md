# SignProtocolOASIS

Sign Protocol — Multi-chain attestation protocol for verifiable credentials.

Part of the [OASIS Omniverse](https://oasisomniverse.one) HOT-swappable provider architecture.

## Storage

`SignProtocolOASIS` stores avatars, avatar details and holons as attestations on a Sign Protocol `SP` contract
(sign-protocol-evm `version()` 1.1.4), through `KeyValueStorageProviderBase`:

- One revocable, hook-less, on-chain schema registered by the provider's wallet holds the store
  (`SignProtocolKeyValueBackend.RegisterSchemaAsync` returns its id and registration block).
- A save attests `abi.encode(string key, uint64 seq, string value)` with indexing key `oasis-kv:v1:keccak256(key)`;
  the highest `seq` for a key is its current value. A delete revokes that attestation.
- Reads rebuild the store from `AttestationMade` / `AttestationRevoked` events and `getAttestation`, accepting only
  attestations of the configured schema made by the configured wallet.
- Writes are acknowledged only after the transaction is mined with status 1 and the attestation or revocation is read back.
- Attestation data is public, so keys are HMAC-indexed and values compressed and encrypted
  (`PrivateKeyValueBackend`, 32-byte record key).

```csharp
var schema = await SignProtocolKeyValueBackend.RegisterSchemaAsync(new SignProtocolConnection(rpc, chainId, spAddress, walletKey, 0, 0));
var provider = new SignProtocolOASIS(new SignProtocolConnection(rpc, chainId, spAddress, walletKey, schema.SchemaId, schema.RegisteredBlock), recordKey);
```

The native storage suite runs against the real SP contract on a local Hardhat chain:
`../TestProjects/NextGenSoftware.OASIS.API.Providers.SignProtocolOASIS.IntegrationTests/fixture/start-sign-protocol.sh`.

`SignAttestationQuerySource` is a separate read-only CQL source over reviewed Base attestation metadata.
