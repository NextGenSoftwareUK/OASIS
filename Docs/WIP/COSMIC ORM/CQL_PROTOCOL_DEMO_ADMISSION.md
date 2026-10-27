# Protocol demo readiness and next implementation

27 October 2026. These are admission findings, not CQL certification.
Priority remains cloud, chain, IPFS, SOLID, ActivityPub; unavailable environments
must not cause silent replacement or stop independent implementation.

| Provider | Current source/environment evidence | CQL implication |
|---|---|---|
| IPFS | Cached Kubo exists. Original published `C:/Source/OASIS-hyperdrive-v2-gaps/TestResults/IPFSCurrent/repo/config` and `storage-key.dpapi` are absent, as are the known cosmic worktree locations. | Native storage acceptance remains blocked. Encrypted catalogue download plus managed selection is not native CQL metadata filtering. See [IPFS admission](CQL_IPFS_DEMO_ADMISSION.md). |
| SOLID | Retained `oasis-solid` image58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a exists and is exited. Known cosmic `TestResults/SolidCurrent` and `LocalState/COSMIC/SolidCurrent` locations lack private-local-client.dpapi, private-local-pod.dpapi and storage-key.dpapi. | Do not regenerate identities/keys over retained Pod data. Locate the retained configuration first. Existing controlled/native histories do not prove current CQL acceptance. |
| ActivityPub | Published matrix records native capability-v1 Social/Network5/5 and291 controlled regressions. Source uses the Mastodon HTTP API to project accounts/statuses. | Public read-only capability offers an independent implementation path. It does not establish generic ActivityPub storage, arbitrary LINQ or native CQL enrollment. |

Only exact configuration-path existence and the SOLID container's image/state
were inspected. No credential contents, keys, private Pod resources, daemon
starts, native reruns, downloads or replacement environments were involved.
An optional retained-configuration location question is pending with the user.

## SOLID query boundary

`SOLIDOASIS.RecordHttp.cs` performs a deterministic resource GET for a record ID,
then canonical encrypted RDF decoding. Collection reads enumerate resources.
`SOLIDOASIS.Queries.cs` applies metadata selectors to those decoded collections.
That is useful storage behavior, but is not evidence of server-side metadata
querying, SPARQL availability or CQL predicate pushdown on a Pod.

An initial native point-read profile is possible only if it admits its exact ID
shape, preserves cancellation/bounds/access rules and rejects every unsupported
shape before resource I/O. Richer metadata/range/ordering queries need an explicit
server query extension or separately admitted index; do not assume every Pod
supports a query service. Hosted identity/access/native gates remain open.

## ActivityPub implementation boundary

`ActivityPubOASIS.StatusReads.cs` owns native Mastodon status reads and projection.
The native status ID is a provider key; the projected OASIS GUID is deliberately
not reversible. The same source owner validates returned native identity and
projects author, original object URI, content and visibility.

Consequently an initial CQL native status-point profile must select the **native
status key**, not reverse/hash-guess a projection GUID. It must expose this key
as an explicit queryable source field, admit only the actual supported predicate
shape, and preserve the existing projection owner. Unsupported predicates must
reject before HTTP. This is a Mastodon API profile, not proof that every
ActivityPub server implements that API.

Required first tests: exact native identity, missing/private/denied status,
malformed source evidence, wrong provider, unsupported query with zero HTTP,
cancellation, bounded body/result size and no automatic activation/submission.
Then prove an owned or existing public status through actual GETs, with accurate
instance/object provenance. No posting, federation messages or new account is
authorized by this read-only demo step.

Projection/scalar/timeline search and complete common-demo shapes remain separate
red gates. Do not mark a protocol's full demo green after a point-read milestone.
Logical source and execution profile must remain explicit throughout WEB4.

## Next action

While retained IPFS/SOLID configuration is unresolved, implement the exact native
status-key query contract for ActivityPub/Mastodon with controlled HTTP evidence.
Do not rerun its already-green social baseline without a reproduced bug.
Resume IPFS/SOLID native acceptance when the original environment is located.
