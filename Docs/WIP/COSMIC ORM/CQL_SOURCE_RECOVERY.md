# Explicit CQL read-source recovery

Implemented and controlled-tested, 2 November 2026. This addresses a reproduced
demo outage. Hosted operator recovery is not yet verified.
Code b718f2879 published to Development;exact deployment
33c9ef7d-b8b3-4403-9109-32ce47c4d7a8 QUEUED. Follow this job without redeploy.

The sixth release is healthy, but Nostr activation failed at connect with
HTTP503/NotAWebSocket. The startup failure latch previously required a whole-host
restart. Original safe evidence is retained in
LocalState/COSMIC/HostedLauncherRecoverySixthReleaseCurrent.

## Contract

Recovery uses the same enrolled read-only profile, endpoint, record catalogue
and activation policy. It does not create a source, substitute a provider,
sign, save, replay a CQL query, reseed demo data or alter HyperDrive routing.
Activation itself can perform bounded native health/verification reads.

Only an authenticated Wizard can call:

```http
POST /api/admin/cql/sources/NostrOASIS/recover
Authorization: Bearer <existing operator credential>
Content-Type: application/json

{
  "operationId": "<new operation GUID>",
  "expectedGeneration": 1,
  "hostInstanceId": "<activationHostInstance from current discovery>"
}
```

Obtain the exact current `activationGeneration` and `activationHostInstance`
from `GET /api/data/query-cql/providers?examplesVersion=2`. The numbers above are
illustrative. Preserve request identity and the original response; never put
credentials into reports, command arguments or chat. Temporary demo avatars
cannot operate shared source lifetime.

Each enrolled source has one activation gate shared by startup and recovery.
A concurrent request returns409 instead of queuing another attempt. Only a
failed source can recover; healthy/pending/canonical/unenrolled sources cannot
be restarted through this route. Discovery remains unavailable while pending.

The response contains the operation ID, completed generation, host instance,
availability and safe error code. A duplicate completed request returns its
historical receipt without native activation; discovery gives current readiness.
Failed/cancelled receipts are also retained;
changing the expected generation on the same receipt returns409. Old/reordered
requests are fenced by generation. Receipts are bounded in-memory per source,
not durable transaction history. A different host instance returns409 rather
than silently repeating an old request after restart.

The complete owning native call is awaited, including cancellation; no detached
timeout task or overlapping activation is created. Host shutdown cancels the
same operation. Existing profile health-check budgets remain in force. A failed
attempt stays unavailable and requires a separately explicit operator decision.
Normal queries and discovery never initiate recovery or native retries.

## Verification gates

- Controlled tests: startup/recovery overlap, duplicate success/failure receipts,
  generation/body/host fences, cancellation, canonical-source rejection and
  unauthenticated/non-Wizard denial: pass, including a non-cooperative native
  call that retains ownership after cancellation and host-shutdown cancellation.
- API producer/consumer compilation and shipping GraphQL schema: pass.
- Combined actual test run: **27passed,0failed,0skipped**. Original compressed
  TRX/source hashes/counts:LocalState/COSMIC/HostedSourceRecoveryCurrent/
  source-recovery-final-evidence.json. Initial test-only CS0029 name collision and
  original failed compile log preserved separately;no native operation occurred.
- Live route authorization and operator-owned recovery: pending. A controlled
  provider is not proof that the upstream relay has recovered.
- Original acknowledged Nostr query/save cohort remains frozen. No browser,
  uptime, all-provider health or full ORM certification follows from recovery.

Source: CqlHostedSources.cs and CqlSourceRecoveryController.cs in the shipping
WEB4 API; CqlSourceRecoveryTests.cs exercises the actual controller/service.
Raw output: TestResults/CqlSourceRecoveryCurrent; required compact evidence
belongs in LocalState/COSMIC, with hashes/counts reported here at the milestone.

This repairs recovery after recorded activation failure. A runtime query failure
does not automatically change the activation generation or initiate recovery;
continuous source-health management and runtime-query outage transitions remain
separate work. Operator receipts are not a durable business-operation outbox.
