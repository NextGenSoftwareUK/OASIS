using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NATS.Client.Core;
using NATS.Net;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.NATSJetStreamOASIS;

if (args.Length != 3 || args[2] is not ("before" or "after"))
    throw new ArgumentException("Source JSON, proof directory and before/after phase are required.");
var json = await File.ReadAllTextAsync(args[0]);
var password = Environment.GetEnvironmentVariable("CQL_NATS_PUBLIC_SOURCE_PASSWORD")
    ?? throw new InvalidOperationException("Explicit reader credential required.");
using var document = JsonDocument.Parse(json);
var ids = document.RootElement.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var token = deadline.Token;
using var source = NatsPublicCqlProfile.FromOwnedSandboxJson(json, "cql_reader", password, true);
var active = await source.ActivateQuerySourceAsync(token);
Require(!active.IsError, "Restored native source activation failed.");
foreach (var selected in new[] { ids, ids.Take(2).ToArray() })
{
    var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.NATSJetStreamOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),
            ProviderType.NATSJetStreamOASIS, selected).Result).Result
        .Where(row => selected.Contains(row.Id)).Result;
    var plan = CosmicQueryPlanner.Plan(query, ProviderType.NATSJetStreamOASIS,
        new[] { new CosmicQueryTarget(ProviderType.NATSJetStreamOASIS, source.QueryCapabilities) });
    Require(!plan.IsError, "Restored native CQL planning failed.");
    var result = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: token);
    Require(!result.IsError && result.Result.Select(row => row.Id).Order().SequenceEqual(selected.Order()),
        "Restored native CQL records differ.");
}
await using var reader = new NatsConnection(new NatsOpts {
    Url = NatsPublicCqlProfile.OwnedEndpoint,
    TlsOpts = new NatsTlsOpts { Mode = TlsMode.Require, CaFile = NatsPublicCqlProfile.OwnedCaFile },
    AuthOpts = new NatsAuthOpts { Username = "cql_reader", Password = password },
    ConnectTimeout = TimeSpan.FromSeconds(3), RequestTimeout = TimeSpan.FromSeconds(3) });
await reader.ConnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), token);
var store = await reader.CreateKeyValueStoreContext().GetStoreAsync(NatsPublicCqlProfile.OwnedBucket, token);
var snapshots = new List<object>();
foreach (var id in ids)
{
    var entry = await store.GetEntryAsync<string>($"oasis/holon/{id:N}", cancellationToken: token);
    Require(entry.Value != null, "Restored source entry is missing.");
    snapshots.Add(new { id, entry.Revision,
        sha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(entry.Value!))) });
}
await Denied("$KV.cosmic_cql_public.denial_probe_" + Guid.NewGuid().ToString("N"));
await Denied("$JS.API.STREAM.INFO.KV_cosmic_cql_foreign");
var snapshot = JsonSerializer.Serialize(snapshots);
var baseline = Path.Combine(args[1], "before.snapshot.json");
if (args[2] == "after")
    Require(await File.ReadAllTextAsync(baseline, token) == snapshot, "Source values/revisions changed across native restart.");
using (var output = new FileStream(Path.Combine(args[1], args[2] + ".snapshot.json"), FileMode.CreateNew))
{ await output.WriteAsync(Encoding.UTF8.GetBytes(snapshot), token); output.Flush(true); }
Console.WriteLine("PASS: restored read-only native CQL3/2 and explicit write/foreign permission denials: " + args[2]);

static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
async Task Denied(string subject)
{
    var denied = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    ValueTask OnError(object? sender, NatsServerErrorEventArgs error)
    {
        if (error.Kind == NatsServerErrorKind.PermissionsViolation && error.Error.Contains('"' + subject + '"', StringComparison.Ordinal))
            denied.TrySetResult(true);
        return ValueTask.CompletedTask;
    }
    reader.ServerError += OnError;
    try {
        await reader.PublishAsync(subject, "must-not-write", cancellationToken: token);
        await reader.PingAsync(token);
        await denied.Task.WaitAsync(TimeSpan.FromSeconds(3), token);
    }
    finally { reader.ServerError -= OnError; }
}
