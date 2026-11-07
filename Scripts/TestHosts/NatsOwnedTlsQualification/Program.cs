using System.Security.Authentication;
using System.Text.Json;
using NATS.Client.Core;
using NATS.Net;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.NATSJetStreamOASIS;

var directory = Path.GetFullPath(Required("OASIS_NATS_OWNED_QUALIFICATION"));
if (!Directory.Exists(directory)) throw new InvalidOperationException("An explicit owned qualification directory is required.");
var writerPassword = Required("OASIS_NATS_OWNED_WRITER_PASSWORD");
var readerPassword = Required("OASIS_NATS_OWNED_READER_PASSWORD");
using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
var token = deadline.Token;
var actor = Guid.NewGuid();
var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
var descriptor = new { schema = 1, endpoint = NatsPublicCqlProfile.OwnedEndpoint,
    bucket = NatsPublicCqlProfile.OwnedBucket, sourceOwner = actor, ids };
WriteNew("source.intent.json", new { descriptor, nativeVersion = "2.15.2", operation = Guid.NewGuid() });
var writer = new FixtureWriter(writerPassword);
try
{
    var activated = await writer.ActivateProviderAsync();
    Require(!activated.IsError, "Native writer activation failed.");
    var names = new[] { "Espresso", "Latte", "Tea" };
    for (var index = 0; index < ids.Length; index++)
    {
        var saved = await writer.SaveHolonAsync(new Holon { Id = ids[index], Name = names[index],
            CreatedByAvatarId = actor, IsPublic = true, IsNewHolon = true }, saveChildren: false);
        Require(!saved.IsError && saved.IsSaved && saved.SavedCount == 1, "Native source save was not acknowledged.");
        WriteNew($"save-{index}.acknowledged.json", new { id = ids[index], saved = true });
    }

    var options = new NatsOpts { Url = NatsPublicCqlProfile.OwnedEndpoint,
        TlsOpts = new NatsTlsOpts { Mode = TlsMode.Require, CaFile = NatsPublicCqlProfile.OwnedCaFile },
        AuthOpts = new NatsAuthOpts { Username = "writer", Password = writerPassword },
        ConnectTimeout = TimeSpan.FromSeconds(3), RequestTimeout = TimeSpan.FromSeconds(3) };
    await using var admin = new NatsConnection(options);
    await admin.ConnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), token);
    var context = admin.CreateKeyValueStoreContext();
    await context.CreateStoreAsync("cosmic_cql_foreign", cancellationToken: token);
    var store = await context.GetStoreAsync(NatsPublicCqlProfile.OwnedBucket, token);
    var originals = new Dictionary<Guid, (string? Value, ulong Revision)>();
    foreach (var id in ids)
    {
        var entry = await store.GetEntryAsync<string>(writer.Key(id), cancellationToken: token);
        originals.Add(id, (entry.Value, entry.Revision));
    }

    var json = JsonSerializer.Serialize(descriptor);
    using (var source = NatsPublicCqlProfile.FromOwnedSandboxJson(json,
        NatsPublicCqlProfile.OwnedReader, readerPassword, sandboxEnabled: true))
    {
        var active = await source.ActivateQuerySourceAsync(token);
        Require(!active.IsError, "Trusted TLS public source activation failed.");
        foreach (var selected in new[] { ids, ids.Take(2).ToArray() })
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.NATSJetStreamOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),
                    ProviderType.NATSJetStreamOASIS, selected).Result).Result
                .Where(row => selected.Contains(row.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.NATSJetStreamOASIS,
                new[] { new CosmicQueryTarget(ProviderType.NATSJetStreamOASIS, source.QueryCapabilities) });
            Require(!plan.IsError, "Native NATS CQL plan was rejected.");
            var result = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: token);
            Require(!result.IsError && result.Result.Select(row => row.Id).Order().SequenceEqual(selected.Order()),
                "Native TLS CQL did not return the exact selected records.");
        }
    }

    var readerOptions = options with { AuthOpts = new NatsAuthOpts {
        Username = NatsPublicCqlProfile.OwnedReader, Password = readerPassword } };
    await using var reader = new NatsConnection(readerOptions);
    await reader.ConnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), token);
    var deniedSubject = "$KV." + NatsPublicCqlProfile.OwnedBucket + ".denial_probe_" + Guid.NewGuid().ToString("N");
    await DeniedPublish(reader, deniedSubject, token);
    await DeniedPublish(reader, "$JS.API.STREAM.INFO.KV_cosmic_cql_foreign", token);

    await using var untrusted = new NatsConnection(readerOptions with {
        TlsOpts = new NatsTlsOpts { Mode = TlsMode.Require } });
    var rejectedCertificate = false;
    try { await untrusted.ConnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), token); }
    catch (NatsException error) when (AuthenticationFailure(error)) { rejectedCertificate = true; }
    Require(rejectedCertificate, "An untrusted CA was not rejected with a TLS authentication failure.");
    foreach (var id in ids)
    {
        var entry = await store.GetEntryAsync<string>(writer.Key(id), cancellationToken: token);
        Require(entry.Value == originals[id].Value && entry.Revision == originals[id].Revision,
            "The native source value or revision changed.");
    }
    WriteNew("source.json", descriptor);
    WriteNew("qualification.json", new { schema = 1, nativeVersion = "2.15.2", sdkVersion = "3.2.0",
        nativeCounts = new[] { 3, 2 }, deniedWrite = true, deniedForeignBucket = true,
        untrustedCaRejected = true, sourceUnchanged = true, sourceSeededOnce = true });
    Console.WriteLine("PASS: real NATS TLS/CQL3/2, explicit write/foreign permission denials, untrusted CA rejection and unchanged source.");
}
finally { await writer.DeActivateProviderAsync(); }

void WriteNew(string name, object value)
{
    using var file = new FileStream(Path.Combine(directory, name), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
    JsonSerializer.Serialize(file, value); file.Flush(true);
}
static string Required(string key) => Environment.GetEnvironmentVariable(key)
    ?? throw new InvalidOperationException("An explicit native qualification setting is missing: " + key);
static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
static bool AuthenticationFailure(Exception error)
{
    for (Exception? current = error; current != null; current = current.InnerException)
        if (current is AuthenticationException) return true;
    return false;
}
static async Task DeniedPublish(NatsConnection connection, string subject, CancellationToken token)
{
    var denied = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    ValueTask OnError(object? sender, NatsServerErrorEventArgs error)
    {
        if (error.Kind == NatsServerErrorKind.PermissionsViolation &&
            error.Error.Contains('"' + subject + '"', StringComparison.Ordinal)) denied.TrySetResult(true);
        return ValueTask.CompletedTask;
    }
    connection.ServerError += OnError;
    try
    {
        await connection.PublishAsync(subject, "must-not-write", cancellationToken: token);
        await connection.PingAsync(token);
        await denied.Task.WaitAsync(TimeSpan.FromSeconds(3), token);
    }
    finally { connection.ServerError -= OnError; }
}

sealed class FixtureWriter(string password) : NATSJetStreamOASIS(NatsPublicCqlProfile.OwnedEndpoint,
    NatsPublicCqlProfile.OwnedBucket, readOnly: false,
    new NatsAuthOpts { Username = "writer", Password = password },
    new NatsTlsOpts { Mode = TlsMode.Require, CaFile = NatsPublicCqlProfile.OwnedCaFile })
{
    public string Key(Guid id) => RecordKey("holon", id);
}
