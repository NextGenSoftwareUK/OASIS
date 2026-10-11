# Radix on-ledger storage (parked)

Code recovered from the retired `main` branch: a Scrypto component (`contracts/`, `oasis_storage`)
that stores OASIS holons on the Radix ledger, plus a C# service that reads component state through the
Radix Gateway API (`/state/entity/details`).

**Not integrated yet.** It was written against the older single-file RadixOASIS provider. To adopt it:

1. Build and deploy the Scrypto package (needs XRD), and record the component address in OASIS_DNA.
2. Wire `RadixComponentService` into the current partial-class provider (save/load paths).
3. Add tests against Stokenet before mainnet.

This branch must not be merged as-is: the provider project compiles every `.cs` file in its folder.
