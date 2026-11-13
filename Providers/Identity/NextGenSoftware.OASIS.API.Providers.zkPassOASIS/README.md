# zkPassOASIS

Verifies recipient-bound EVM attestations issued by zkPass TransGate. Applications use the official TransGate SDK to obtain the attestation, then pass it to the shipping .NET verifier.

Construct `zkPassOASIS`, activate it, and call `Verify` with the proof, the schema ID selected by the server, and the wallet address bound to the current request. The verifier checks the allocator's signature over the task and validator assignment, then checks the validator's signature over the attestation and recipient. The default allocator is the address published by zkPass. An explicit replacement is a server-side trust setting.

The input maps TransGate's `uHash` to `ZkPassEvmProof.UserHash`. Applications that grant one-time access must record the accepted task ID in their own replay protection. Applications that consume disclosed field values must verify them against the signed public-fields hash using the schema's encoding.

The CI verification project reads a fixed public Sepolia attestation from the official tutorial contract. The proof payload is fetched at runtime.

- [Official TransGate SDK](https://github.com/zkPassOfficial/Transgate-JS-SDK)
- [Official EVM verification guide](https://docs.zkpass.org/developer-guides/js-sdk/generate-proof-and-verify-the-result/evm)
