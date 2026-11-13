# WEB4 demo session renewal

Updated 1 November 2026. **Live HTTP lifecycle and frontend publication verified.** Actual browser interaction remains open. Hosted provider35/browser5/open30counts unchanged.

## Live lifecycle and page publication complete

- Backendd9332469ddf4587ebca3d74ed3ccec914bad2ef4/Coreb835014a16748d745346f1265b0753e911dbd64d; exact deployment9724c407-ac0c-4624-ac5e-fed6c8dec493 SUCCESS.
- Original owned avatar normally rotated with a Secure/HttpOnly/Lax/path-correct cookie; same identity retained. Corrected revoke succeeded, cookie cleared, and known revoked refresh was rejected.
- Zero provider holon saves. Original insecure issuance and Unauthorized revoke rejection remain sealed, with explicit continuation intents/ACKs. LocalState/COSMIC/SandboxSessionRenewalCurrent/flow.acknowledged.json now blocks reruns. **Do not create another cohort or replay completed phases.**
- No pre-rotation fault baseline was collected, so faultScopePreserved=false is retained honestly. Post-rotation repeated fault reads agree. No real expiry wait or browser interaction claim.
- Website main9e18b7dedda7c4e6cfaa358fa8987774616deaac published. HTML and both versioned JS assets return HTTP200and exactly match committed bytes; cache policy public,max-age=0,must-revalidate.

## Root causes fixed

1. Demo authentication omitted the generated refresh cookie. Normal/demo issuance now share AvatarSessionCookie; incomplete credentials cannot publish a successful session.
2. Wildcard CORS cannot support cookie credentials. Exact https://web4.oasisomniverse.one receives credentialed explicit-origin CORS; other origins retain public bearer CORS without cookie grants. Both expose operation IDs; responses vary by Origin.
3. Empty collection initializers did not clear default loopback proxy trust. Startup now explicitly clears the existing Railway edge-trust lists before processing one forwarded hop. This deployment policy requires an edge-protected application listener.
4. Logout checked OwnsToken on a deliberately redacted request avatar. Core's requester-aware overload checks the exact authoritative token owner before mutation/save, preserving normal owner/Wizard rules and rejecting foreign/missing/empty identities. Request token lists stay hidden.
5. Browser/tooling omitted credential retention and used JWT string changes as identity changes. Serialized cookie ownership and stable avatar/environment/generation identity now survive rotation. JWT stays in browser memory; uncertain outcomes stop without automatic fresh avatars, token retries or replayed writes.

## Verification and compact evidence

- API16/16including non-loopback forwarded-HTTPS/cookie middleware, CORS and GraphQL shipping schema; Core revocation ownership5/5; client80/80; credential tool9/9. Zero skips in these selected checks.
- Client tests cover controlled-clock expiry preflight, coalesced refresh, logout, identity/environment changes and late responses. They are not browser certification.
- HTTP tools retain encoded wire cookies and access tokens together in versioned DPAPI bundles. Rotation journals intent/response and atomically replaces one bundle. Legacy JWT-only cohorts are never silently replaced.
- Raw evidence stays in TestResults/SandboxSessionRenewalCurrent; durable identity/rotation/ACK material stays in LocalState/COSMIC/SandboxSessionRenewalCurrent.

| Evidence | SHA256 |
|---|---|
| live-session-protocol.json | af9cdf4e7d4c64b1ba081c9f72b17c24231b369c8a44a8168604862a3acee399 |
| site-publication.json | 66a750ebd607c59cba5b5c7cdd9385019d03a9012981ccb72add0e771a413d54 |
| revoke-owner.trx | 3e14f1f9b25bdb7e7bdeb23c5ebe10b72ccb6d9474048b846f5c38d377919b40 |
| revoke-api-shipping.trx | 5746bbdaa744350648e1af24e229437b232d1dccf688c835c1b6eb6400ebaa82 |

| Published file | Bytes | SHA256 |
|---|---:|---|
| web4-sandbox.html | 150878 | 4a046882f2afb01373f2077329cb789fdb3ccd60b55f42c4543c9a695e0cf26a |
| web4-session.js | 6533 | 41f7adf87786fc3071b23377eb1278fb4d41ce6f17b8519b19cc8a97acafc856 |
| cql-playground.js | 21279 | 4a3b036211e281941d4815d157a17e880c310707031ef3ce774402fdae644def |

## Next

Continue Starknet selected release and hosted acceptance. Browser renewal/expiry interaction stays separately open while CUA is unavailable; do not resume OS control or ask David to reopen the browser. Preserve the completed session cohort and all original failure evidence.

[Current status](CURRENT_STATUS.md) / [protocol](../../Devs/WEB4_SANDBOX_SESSION_AUTH.md) / [rollout history](Archive/SESSION_RENEWAL_ROLLOUT_HISTORY_2026-11-01.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
