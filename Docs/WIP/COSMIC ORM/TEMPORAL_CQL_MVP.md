# Temporal workflow-run CQL MVP

The adapter uses the existing official Temporal .NET SDK 1.20.0 for
`DescribeWorkflowExecution` against a reviewed teaching namespace. The
catalogue binds the exact workflow ID and run ID. A newer run sharing a
workflow ID cannot satisfy the request for an absent run. The projection
contains the teaching workflow name, namespace, run ID, status and start
time. It omits workflow arguments, memo, search attributes and history.

The passing native run used the
official Temporal CLI 1.10.0 image at Linux x64 digest
`sha256:982e53357ed4a8bdfcb5e3593620120834a71a9cbec4d78d99ff7f64010efb9c`.
The test seeds three synthetic runs, checks exact 3/2/0 CQL reads and
terminates the owned runs. The operator catalogue and query authority
are separate from the existing workflow execution provider.

WEB4 admission passed locally. Federation and hosted acceptance remain open.

[Temporal development server](https://docs.temporal.io/cli/command-reference/server) ·
[Official Temporal .NET SDK](https://github.com/temporalio/sdk-dotnet/tree/1.20.0).

The native run passed 3/2/0. The missing case used an existing workflow
ID with an unrecognized run ID and returned zero. An out-of-catalogue
key was rejected. The three owned executions were terminated by their
exact workflow and run IDs. One test passed with zero skips. The
1,686-byte CI artifact SHA-256 is
`81118258f1447da523b80bc3611fa604cf710073ebbbee2c8d67a211561f53dc`.
[Passing Linux run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42266007330).

The cloud reader factory derives the TLS target from an exact
`namespace.account` name and accepts a protected API key. The key never
appears in catalogue identities or query output. The cloud binding
control and the real local native test both passed with zero skips.
The 1,871-byte CI artifact SHA-256 is
`fa3778434fc938158b2eb069dfb7c44d92b72c985d28b0ec131d2ce2ac611466`.
Actual Temporal Cloud access and hosted acceptance remain open.
[Cloud binding checks](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42266619694) and
[Temporal Cloud connection guidance](https://docs.temporal.io/cli/cloud).

## WEB4 read-only admission

`CQL_TEMPORAL_QUERY_SOURCE_JSON` binds an approved teaching namespace,
owner, exact workflow/run pairs and one probe. The protected
`CQL_TEMPORAL_QUERY_API_KEY` belongs to the namespace-scoped reader.
Startup registers a lazy, opt-in read profile. Query admission requires
an independent read tariff and rejects write tariffs for this source.

At Core `c682a69c766160f723c391a44c20f9a95d455afa`, seven Temporal checks
and thirteen shared SQL admission checks passed with zero skips.
The 35,618-byte WEB4 TRX SHA-256 is
`0a22ac31689c5bc001372487fc0b6947953da27d50c8f56fd1c303bc24c153f3`.
All 37 protected configuration checks passed. Hosted verification after
LocalFile requires the restricted reader, approved exact runs and a
separate namespace access denial check.
