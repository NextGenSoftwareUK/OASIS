# Manager graph preparation

Working checkpoint, 19 October 2026. Core1079 and SQLite705 pass, zero failures/skips; both Core target frameworks build. Full Shared732 also passes against the final Core snapshot.

The manager preparation owner no longer recursively calls parent assignment and child preparation. Explicit enter/exit frames preserve the existing order:

1. Establish the staged node's parent context and audit/version/custom metadata.
2. Prepare its selected semantic descendants in depth-first order while ancestor context remains available.
3. Strip that node's navigation and encrypt its metadata after descendants finish.

This order matters: child custom-property extraction can read the parent's prepared revision and plaintext metadata. Moving encryption or navigation stripping before descendants would change behavior. Sync/async V2 tests explicitly observe parent version 1 and unencrypted parent metadata during child extraction, then verify encrypted provider input with stripped navigation and preserved caller parent references after acknowledgement.

The traversal rejects null/repeated nodes/cycles before executing the provider. It captures each selected AllChildren view once during preparation and introduces no depth cap, alternative store or retry. Existing save-plan selection still decides the detached graph; typed semantic/physical membership ownership remains a separate gap.

Logical revision increment is checked. Two red-before V2 cases show the old unchecked counter could pass a wrapped negative revision to a provider. Version exhaustion now returns a preparation error before provider I/O, retains pending caller values and releases operation ownership.

Six new Core cases cover sync/async version exhaustion, 2,048-node public V2 preparation/audit/navigation ownership and parent-context/encryption ordering. Four real SQLite cases cover sync/async × saveChildrenOnProvider flags using 1,024-node public V2 saves, reloads, clean tracking and no-op saves with no additional revision. Full Core1079 and SQLite705 pass; exact binary and downstream results are recorded in VERIFICATION.md.

This closes manager preparation recursion for these routes. It does not certify arbitrary callbacks/constructors, every typed semantic aggregate, per-holon key-resolution policies, coherent historical graphs, all query/cancellation/provider combinations or the remaining durable recovery/security/migration/delta/CQL roadmap. Legacy Hyperdrive routing is unchanged.
