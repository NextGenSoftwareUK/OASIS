# WEB4 demo session renewal

Updated 1 November 2026. Local implementation verified; API4434e2c28 deployment6402f4ec-b203-42a5-8508-589f683708fc SUCCESS. Live protocol failed securely at initial cookie validation; frontend publication pending. Provider/browser counts remain35hosted HTTP/5browser/30open.

## Root causes and correction

- Sandbox session authentication omitted its generated refresh cookie. Normal and demo authentication now share AvatarSessionCookie; missing access/refresh credentials cannot publish a successful demo session.
- Wildcard CORS cannot support cookie credentials. Exact https://web4.oasisomniverse.one receives explicit-origin credentialed CORS; other clients retain existing public bearer CORS. Both expose operation IDs and responses vary by Origin.
- Browser auth now sends cookie credentials and serializes login, refresh and logout. Expiry preflight happens before the original request. Uncertain rotation is blocked rather than retried; no automatic fresh avatar or replayed write.
- JWT remains memory-only. Stable avatar/environment/generation identity survives renewal; old identity/environment responses cannot publish into a new session. CQL discovery/results/preparation and HyperDrive/ORM calls share these guards.
- Logout revokes the normal refresh credential and clears its scoped cookie. Genuine failures are reported; no request retry.
- HTTP acceptance harness stores access + encoded cookie header together in one DPAPI bundle, versioned in its existing ACK. Rotation journals intent/response, requires same owner, and atomically replaces one credential bundle. Pending rotations stop further actions. Retained legacy JWT-only cohorts are never silently replaced.

## Local verification

- API16/16:15cookie/session/proxy/preflight checks plus1shipping GraphQL schema check; zero skips.
- Client80/80:12session-state/rotation/logout/context cases, CQL renewal integration and existing affected preparation/UI regressions; zero skips. This is in-memory client testing, not browser certification.
- Credential tool9/9: encoded-cookie retention, same owner, one atomic bundle install, missing/unknown outcomes and no replay. Tiny owned test fixture directory removed at teardown.
- Initial compile had test type aliases wrong (fixed without changing production contracts). Initial UI tests assumed synchronous fetch; replaced assumptions with request-start gates. One stalled owned fake-transport Node runner/child stopped; no production process touched.

- `TestResults/SandboxSessionRenewalCurrent/session-renewal-api-final.trx`, 26165bytes; SHA256 `c84bc6f60c6eec81d38702a797b59913e05deb87f15dbda8beb6af362f2756a2`.
- `TestResults/SandboxSessionRenewalCurrent/browser-unit-suite-final.log`, 7775bytes; SHA256 `9cdd405c8d7a64042c908bd57b4c1dfa6f4563de8349ad54c30f1a5c8aa77798`.
- `TestResults/SandboxSessionRenewalCurrent/credential-unit-tests-final.log`; final9-case run includes successive rotations with unchanged JWT and changed refresh cookie.

## Reproduced hosted blocker

The exact deployed4434e2c28 passed trusted/untrusted preflight, then issued one owned session with HttpOnly/Lax/path-correct cookie but **Secure=false**. Sealed original response and intent remain in LocalState/COSMIC/SandboxSessionRenewalCurrent; no replacement owner, rotation or provider save occurred. Startup used empty collection initializers, leaving default loopback proxy trust intact and ignoring Railway�s non-loopback forwarded HTTPS scheme. Correction explicitly clears those collections under the existing edge-trust policy; new middleware regression exercises the actual proxy/scheme/cookie chain. Preserve and reconcile the original issuance; do not rerun the original flow command.

## Exact next step

1. Server4434e2c28 is deployed; frontend43af923 remains on its task branch. Publish the page only after live cookie protocol verification.
2. Deployment success observed. Create one owned session-verification cohort under LocalState/COSMIC/SandboxSessionRenewalCurrent; capture cookies/bearers only in DPAPI. Prove exact trusted-origin preflight, secure HttpOnly cookie, normal same-avatar rotation and revoke/clear; no provider saves or completed cohort reruns.
3. Publish frontend only after HTTP protocol passes; verify exact HTML and both changed JS assets (versioned URLs). Actual browser interaction remains open while CUA is unavailable; do not ask David to reopen or resume OS control.
4. Update CURRENT_STATUS/provider report/manual/architecture/resource ledgers, then continue Starknet native CQL breadth. Full roadmap remains active.

[Current status](CURRENT_STATUS.md) · [protocol](../../Devs/WEB4_SANDBOX_SESSION_AUTH.md) · [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Proxy regression run:16passed/0failed/0skipped; TestResults/SandboxSessionRenewalCurrent/session-proxy-final.trx SHA256d2ac7c028fe5b47fdda1529564e32fe9ea3daea862fcd00fff58fa93089dd9cb. The prior15-case proof remains historical. Next: deploy tested proxy correction, then run Verify-HostedSessionRenewal.py with exact new commit/deployment and --reconcile-insecure-issuance. Do not use fresh-session mode on the retained cohort.
