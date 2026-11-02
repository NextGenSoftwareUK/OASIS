# Matrix public room-alias qualification

Updated 2 November 2026. Native qualification only; CQL, hosted admission,
Mongo pairing and browser acceptance remain unimplemented/unverified.

## Verified

- Anonymous GETs against `https://matrix.org/_matrix/client/v3/directory/room/{roomAlias}` resolved `#matrix:matrix.org`, `#synapse:matrix.org` and `#element:matrix.org` (HTTP200 each). No login, join, publication, save or retry.
- First response exposed two incorrect qualifier assumptions: room IDs need not contain a domain, and public rooms can list more than256 routing servers. Fixed the qualifier boundary to treat room IDs as opaque and accept up to4096 server entries within the existing64KiB response budget. The captured response has1169 entries; other responses726/80.
- Original failed validation and raw first response are preserved. Explicit continuation validated/reused that captured response and requested only the two previously unrequested aliases. No first-alias refetch or outcome overwrite.
- Four offline tests cover old/new room IDs, large routing lists, malformed/incomplete rows and duplicate JSON properties. Command: `python -m unittest discover -s Scripts/Sandbox -p test_matrix_public_probe.py`.

## Evidence

Raw public evidence stays in `C:/Source/OASIS-cosmic/TestResults/MatrixPublicSourceCurrent/` (90891bytes across7files), not docs/git. No credentials or source owner minted.

| Artifact | SHA256 |
|---|---|
| Original `qualification.json` (validation failure) | `bf53a4581b70f5007222fda3b63d9618b31b5068ac285384f62d13f6dbc1e2b6` |
| Successful `continuation.json` | `0419a7bb9e809773666e99a46e3f3fdcd9ba2524ee321d91f0edf769e7cba690` |
| First public response `alias-0.json` | `5f44b50820443c2e14dfefd356bf02117a0ec897f176db77af098cd3a76b2cf7` |
| Second public response `alias-1.json` | `ddc036c7f7346832ca7f44e687e8383b5a1f39660558def70aaf0609187394fd` |
| Third public response `alias-2.json` | `c3fa3f0cb4e7053481d6cea1160ffa1f59d9714d5bfbb09508c630a0a4a2b451` |

The qualifier uses exclusive intent/outcome files. Both attempts are consumed;
do not rerun them or remove their guards to obtain a new green result.

## Next implementation and limits

Implement a bounded configured-alias read-only source with `MatrixRoomAlias`
equality/membership, complete selection before paging and explicit unsupported
capability rejection. Preserve exact server-reported opaque room IDs. Room-alias
resolution is mutable and server-reported: it does not prove message history,
room state, membership, E2EE content, freshness, common snapshot or independent
federation consensus. No server address returned by the API should be fetched.
Then add C#/HTTP examples, native3/2 and Mongo5/4 presets, ownership/isolation
and replication checks before hosted admission/card publication.

Legacy `MatrixOASIS` activation requires login and does not supply this anonymous
bounded source contract; leave legacy unchanged and document its gaps only.

Official contracts: [anonymous alias resolution](https://spec.matrix.org/latest/client-server-api/#get_matrixclientv3directoryroomroomalias),
[opaque room IDs](https://spec.matrix.org/v1.17/appendices/),
[v1.16 room-ID change](https://matrix.org/blog/2025/09/17/matrix-v1.16-release/).
