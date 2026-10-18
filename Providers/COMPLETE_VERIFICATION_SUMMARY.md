# Provider Verification Summary

This file is not a blanket production-readiness certification.

The previous version claimed that every provider was 100% complete after searching source text. That claim was not supported by executable runtime evidence and incorrectly described several implementations, including the former AWS path. Source inspection alone cannot prove activation, persistence, failover, replication, or load balancing.

The authoritative provider evidence is maintained in:

- `Docs/Devs/HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md`
- the executable integration-test projects referenced by that document
- the checked-in provider/runtime runner scripts under `Scripts/`

A provider is verified for HyperDrive V2 only when all applicable gates pass:

1. Activation performs a real connectivity/readiness probe.
2. Persistence uses the provider's real SDK, protocol, or API; it does not use an in-memory substitute, fabricated response, mock, or placeholder.
3. A repeatable integration test runs against the real service or an official emulator/node and completes with zero skipped tests.
4. The provider participates successfully in real-provider HyperDrive V2 failover, replication, and load-balancing tests when configured for those modes.
5. Capabilities the upstream service cannot support fail explicitly and are documented; they are never reported as successful no-ops.

The coverage document is the authoritative matrix. A registered provider without a `PASS` row there remains unverified and must not be described as production ready.
