# Development integration — 30 October 2026

Inbound parent835653f31/Coreb60796d integrated into work/cosmic-cql. Task Core0b69338
and incoming Core merged as d9f5ec105b43060debff30fe65d37efde9d02af5, pushed and
verified with git ls-remote. Parent merge cc232e21d preserves both histories and
resolves its Core gitlink/deployment manifest to the merged revision.

Incoming scope: transitive group lookup, host shared/inherited read policy,
SQLite/Neo4j ancestor lowering, related docs/tests. No outgoing Development deployment
or claim that all deferred migration/provider work is ready to ship.

## Verification

- DNA/Core/SQLite/Neo4j/Core tests/WEB4/WEB4 tests bounded builds pass.
- Full Core4,552passed,0failed/0skipped (including incoming shared-read tests).
- WEB4 shipping GraphQL schema/required-policy hosting4passed,0failed/0skipped.
- Railway manifest/seven Dockerfiles/committed gitlinks pass; pin-sync --check passes.
- Native ancestor tests were preserved from incoming commit; this integration did
  not independently rerun native backends or establish complete provider parity.

First batch selected the older ONODE/TestProjects unit project, whose assets file
is absent. It stopped before tests; corrected to maintained
ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests, without restore/download.
Initial pin validation compared the uncommitted merge against old HEAD and failed;
committed merge passes exact HEAD manifest validation. No validation bypass used.

Raw logs/TRX only TestResults/DevelopmentSync20261030; retained summaries/hashes:

- `core.trx`: SHA256 `71c7e96f5058bcc12a3ee8d60203cef83629aa9ce822b760636d6f87ff5e64f7`.

- `web4.trx`: SHA256 `197f0f93dc05d117c442d9fac8b6c467ad6c75fb49de9fc85542ac213f3896a1`.

- `manifest.log`: SHA256 `7775487cd6888593475a25a0232defb6ce54527677938f8fe964cd7ddc5fe839`.

- `pin-sync.log`: SHA256 `601576d7850b2a93f8a350ba549938cc167f6965f25f634418af65997edc17c5`.

One build lease per batch, /m:2 /nr:false /p:UseSharedCompilation=false and existing
outputs/dependencies. No new native services/runtime/download/data. Test outputs
regenerable; reusable source/keys/stores remain untouched. Next Arweave loopback-only
gateway startup and native CQL acceptance; other red programme gates remain open.
