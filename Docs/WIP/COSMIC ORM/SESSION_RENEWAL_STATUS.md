# WEB4 demo session renewal

Updated 1 November 2026. Local implementation verified; API deployment and frontend publication pending. Provider/browser counts remain35hosted HTTP/5browser/30open.

## Root causes and correction

- Sandbox session authentication omitted its generated refresh cookie. Normal and demo authentication now share AvatarSessionCookie; missing access/refresh credentials cannot publish a successful demo session.
- Wildcard CORS cannot support cookie credentials. Exact https://web4.oasisomniverse.one receives explicit-origin credentialed CORS; other clients retain existing public bearer CORS. Both expose operation IDs and responses vary by Origin.
- Browser auth now sends cookie credentials and serializes login, refresh and logout. Expiry preflight happens before the original request. Uncertain rotation is blocked rather than retried; no automatic fresh avatar or replayed write.
- JWT remains memory-only. Stable avatar/environment/generation identity survives renewal; old identity/environment responses cannot publish into a new session. CQL discovery/results/preparation and HyperDrive/ORM calls share these guards.
- Logout revokes the normal refresh credential and clears its scoped cookie. Genuine failures are reported; no request retry.
- HTTP acceptance harness stores access + encoded cookie header together in one DPAPI bundle, versioned in its existing ACK. Rotation journals intent/response, requires same owner, and atomically replaces one credential bundle. Pending rotations stop further actions. Retained legacy JWT-only cohorts are never silently replaced.

## Local verification

- API15/15:14cookie/session/preflight checks plus1shipping GraphQL schema check; zero skips.
- Client80/80:12session-state/rotation/logout/context cases, CQL renewal integration and existing affected preparation/UI regressions; zero skips. This is in-memory client testing, not browser certification.
- Credential tool8/8: encoded-cookie retention, same owner, one atomic bundle install, missing/unknown outcomes and no replay. Tiny owned test fixture directory removed at teardown.
- Initial compile had test type aliases wrong (fixed without changing production contracts). Initial UI tests assumed synchronous fetch; replaced assumptions with request-start gates. One stalled owned fake-transport Node runner/child stopped; no production process touched.

- `TestResults/SandboxSessionRenewalCurrent/session-renewal-api-final.trx`, 26165bytes; SHA256 `c84bc6f60c6eec81d38702a797b59913e05deb87f15dbda8beb6af362f2756a2`.
- `TestResults/SandboxSessionRenewalCurrent/browser-unit-suite-final.log`, 7775bytes; SHA256 `9cdd405c8d7a64042c908bd57b4c1dfa6f4563de8349ad54c30f1a5c8aa77798`.
- `TestResults/SandboxSessionRenewalCurrent/credential-unit-tests.log`, 111bytes; SHA256 `f190963e561409770896f1c33b141e3abee144ddc2f00d5dbbfd92537821a5b0`.

## Exact next step

1. Commit/push API and frontend task branches; deploy server first. Do not publish the new page while the old wildcard-only server is live.
2. Observe exact deployment success. Create one owned session-verification cohort under LocalState/COSMIC/SandboxSessionRenewalCurrent; capture cookies/bearers only in DPAPI. Prove exact trusted-origin preflight, secure HttpOnly cookie, normal same-avatar rotation and revoke/clear; no provider saves or completed cohort reruns.
3. Publish frontend only after HTTP protocol passes; verify exact HTML and both changed JS assets (versioned URLs). Actual browser interaction remains open while CUA is unavailable; do not ask David to reopen or resume OS control.
4. Update CURRENT_STATUS/provider report/manual/architecture/resource ledgers, then continue Starknet native CQL breadth. Full roadmap remains active.

[Current status](CURRENT_STATUS.md) · [protocol](../../Devs/WEB4_SANDBOX_SESSION_AUTH.md) · [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
