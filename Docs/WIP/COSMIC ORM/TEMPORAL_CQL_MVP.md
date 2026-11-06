# Temporal workflow-run CQL MVP

The adapter uses the existing official Temporal .NET SDK 1.20.0 for
`DescribeWorkflowExecution` against a reviewed teaching namespace. The
catalogue binds the exact workflow ID and run ID. A newer run sharing a
workflow ID cannot satisfy the request for an absent run. The projection
contains the teaching workflow name, namespace, run ID, status and start
time. It omits workflow arguments, memo, search attributes and history.

The first native run is pending. The isolated Linux runner uses the
official Temporal CLI 1.10.0 image at Linux x64 digest
`sha256:982e53357ed4a8bdfcb5e3593620120834a71a9cbec4d78d99ff7f64010efb9c`.
The test seeds three synthetic runs, checks exact 3/2/0 CQL reads and
terminates the owned runs. The operator catalogue and query authority
are separate from the existing workflow execution provider.

WEB4 admission, federation and hosted acceptance remain open.

[Temporal development server](https://docs.temporal.io/cli/command-reference/server) ·
[Official Temporal .NET SDK](https://github.com/temporalio/sdk-dotnet/tree/1.20.0).
