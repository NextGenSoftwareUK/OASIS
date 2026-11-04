# Espresso public header source

4 November 2026. The official mainnet query service answered anonymous
JSON GETs at /v1/availability/header/{height} for heights 1, 2 and 3.
All returned the requested height, embedded chain ID 1, and a canonical
32-byte L1 finalized hash.

| Height | Unix timestamp | Reported finalized L1 block |
| --- | --- | --- |
| 1 | 1730830678 | 21123092 |
| 2 | 1730830688 | 21123124 |
| 3 | 1730830697 | 21123124 |

Use the pinned mainnet query-service endpoint and strict height and schema
validation for a bounded read profile. Report the L1 data as returned by the
query service. Independent L1 and Espresso finality verification remains
outside this probe. CQL adapter, local WEB4 federation, and hosted acceptance
remain open. No runtime or output files were needed for the probe.

[Official Espresso query API documentation](https://docs.espressosys.com/network/developer/espresso-api)

The guarded .NET client builds and reads all three headers live. It bounds
the response to 16 KiB and the request to 12 seconds, checks the requested
height and the returned chain ID, and pins block 1's reported L1 hash as a
source identity anchor. Source activation should request block 1 even when
the configured catalogue excludes it.

The read-only adapter built and activated live with heights 2 and 3,
requiring the separate block 1 identity check. It projects stable public
holons with exact height equality or bounded membership. WEB4 admission,
end-to-end CQL execution and federation remain pending.

The parent WEB4 registration, compiler and admission/federation tests are
prepared. Before running them, coordinate the shared Core checkout: the
parent pins 2acffcb0, while the shared checkout is detached at 54159614.
Add EspressoSystemsOASIS to the query-only execution allowlist on the task
Core branch when the checkout is available, then run the leased build and
the owned Mongo gate. The local catalogue and report directory are ready.

The guarded rollout publisher and its three configuration tests are ready.
It requires the exact source-bound local HTTP and cleanup receipt, which is
currently absent. It refuses to prepare before that evidence exists and
refuses to apply before the expected Development deployment is live.

WEB4 admission and local federation now pass on Core 63d8851. Live Espresso
returned 3/2 native rows and Mongo federation returned 5/4. The foreign
actor saw 0 private rows and 3 public rows. The run verified 2 SDK saves,
9 usage receipts and removal of its exact Mongo fixture database. The
publisher accepted the source-bound local receipt. Hosted acceptance is open.
