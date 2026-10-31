# Aptos OASIS storage contract

This package is the on-chain storage contract used by `AptosOASIS`. It stores
typed OASIS JSON records in an Aptos table owned by the publishing account.
Writes and deletes are entry functions and therefore require signed Aptos
transactions; reads use Aptos view functions.

The contract exposes `initialize`, `upsert_record`, `delete_record`,
`has_record`, `get_record`, and `get_record_keys`.

Compile and test with the official Aptos CLI:

```powershell
aptos move test --package-dir . --named-addresses oasis=0x123
```

The complete owned-runtime provider test is:

```powershell
Scripts/run_aptos_provider_evidence.ps1
```

That runner checksum-verifies Aptos CLI 9.6.0, starts the CLI's native
localnet, deploys and initializes this contract, and executes the .NET
provider integration tests against the real node. It requires no public APT
and cannot spend production funds.
