# Provider completion continuation

2026-11-09. Isolated worktree C:/Source/OASIS2-hyperdrive, branch work/hyperdrive-provider-completion, based on Development 871e3d7184. The previous provider branch had already been merged into Development and its remote head is no longer advertised. Start from CURRENT_STATUS.md and PROVIDER_MATRIX.md. Treat their evidence as historical until matched to the current provider and dependency revisions.

1. Verify the current matrix against the production provider registrations and each provider's advertised interfaces. Preserve separate states for missing code, controlled tests, native conformance and full interface coverage.
2. Continue SQL and document provider baselines. Review the Neon native gate and the missing Convex server functions first. Use one reusable test environment and run native lifecycle conformance through the shipping adapters. Record external access decisions in the existing David action list when needed.
3. After provider acceptance, exercise HyperDrive auto failover, auto replication and auto load balancing across eligible providers with real backends. Verify operation outcomes, persistence after failover and replication convergence. Use capability checks for non-storage providers.
4. Commit scoped verified batches on the task branch. Keep the COSMIC/CQL checkout and its pending changes separate.
