Source-guided diagnosis, not root-cause attribution.

Managed configuration explicitly requests Holochain0.7.1. The official tagged source adds newly created cells to running_cells after awaiting all network joins:
https://github.com/holochain/holochain/blob/holochain-0.7.1/crates/holochain/src/conductor/conductor.rs#L2193

The matching join owner first awaits Kitsune space creation and then local-agent registration:
https://github.com/holochain/holochain/blob/holochain-0.7.1/crates/holochain_p2p/src/spawn/actor.rs#L1579

Inference: responsive admin with zero running cells and the retained network-join marker is consistent with an incomplete native join phase. It does not distinguish space creation from agent registration, remote transport from local synchronization/storage, or establish an upstream defect. A root-path HTTP404 is not protocol health proof. Next diagnosis needs retained phase-level native evidence or an isolated retained-state reproduction, preserving the actual cell and records. Do not declare the timeout solved by extending a deadline.

Follow-up: the existing configured HoloNET conductor cache binary was found and its read-only --version returned holochain 0.7.1 with exit0. No startup, data/state writes, runtime download or raw SSH output retention. This confirms the observed version matches the configured version.
