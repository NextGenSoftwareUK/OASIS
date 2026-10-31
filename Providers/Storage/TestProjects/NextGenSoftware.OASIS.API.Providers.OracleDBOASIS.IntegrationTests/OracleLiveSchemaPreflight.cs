using System.Text.Json;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

/// <summary>Read-only target/schema readiness. This does NOT execute the trigger/DDL-bearing native suite.</summary>
public static class OracleLiveSchemaPreflight
{
    internal sealed record Target(string Database, string Schema, string SessionUser, string Service, string Protocol);
    internal class ExistingSchemaOwner(string connection) : OracleDBOASIS(connection)
    {
        internal int ProvisionCalls;
        protected override Task ProvisionStorageSchemaAsync(OracleConnection connection)
        {
            ProvisionCalls++;
            throw new InvalidOperationException("Read-only preflight cannot provision or repair schema objects.");
        }
    }

    public static Task RunAsync(string connectionVariable, string expectedDatabase, string expectedSchema, string expectedService, string work)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionVariable);
        var connection = Environment.GetEnvironmentVariable(connectionVariable);
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Configure the protected Oracle test connection outside chat/command-line arguments.");
        return RunAsync(connection, expectedDatabase, expectedSchema, expectedService, work, requireTcps: true);
    }

    internal static Task RunLocalControlAsync(string connection, string expectedDatabase, string expectedSchema, string expectedService, string work)
        => RunAsync(connection, expectedDatabase, expectedSchema, expectedService, work, requireTcps: false);

    internal static void ValidateTarget(Target observed, string expectedDatabase, string expectedSchema, string expectedService, bool requireTcps)
    {
        ArgumentNullException.ThrowIfNull(observed);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedDatabase);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedService);
        if (observed.Database != expectedDatabase || observed.Schema != expectedSchema || observed.SessionUser != expectedSchema || observed.Service != expectedService)
            throw new InvalidDataException("Oracle connection reached a different database/schema/user/service than explicitly approved.");
        if (observed.SessionUser is "SYS" or "SYSTEM")
            throw new InvalidDataException("Use the approved non-administrator test schema, not SYS/SYSTEM.");
        if (requireTcps && !string.Equals(observed.Protocol, "tcps", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Hosted Oracle preflight requires observed TCPS; local TCP is not hosted TLS acceptance.");
    }

    internal static async Task<Target> ReadTargetAsync(OracleConnection native)
    {
        await using var command = new OracleCommand("SELECT SYS_CONTEXT('USERENV','DB_UNIQUE_NAME'),SYS_CONTEXT('USERENV','CURRENT_SCHEMA'),SYS_CONTEXT('USERENV','SESSION_USER'),SYS_CONTEXT('USERENV','SERVICE_NAME'),SYS_CONTEXT('USERENV','NETWORK_PROTOCOL') FROM DUAL", native) { CommandTimeout = 10 };
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidDataException("Oracle session target metadata is absent.");
        var target = new Target(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4));
        if (await reader.ReadAsync()) throw new InvalidDataException("Oracle session target metadata is ambiguous.");
        return target;
    }

    internal static async Task<string[]> ReadCanonicalObjectsAsync(OracleConnection native)
    {
        await using var command = new OracleCommand("SELECT OBJECT_NAME,OBJECT_ID FROM USER_OBJECTS WHERE OBJECT_TYPE='TABLE' AND OBJECT_NAME IN ('OASIS_AVATARS','OASIS_AVATAR_DETAILS','OASIS_HOLONS','OASIS_WRITE_RECEIPTS','OASIS_WRITE_OPERATION_LOCKS','OASIS_WRITE_PREPARATIONS') ORDER BY OBJECT_NAME", native) { CommandTimeout = 10 };
        await using var rows = await command.ExecuteReaderAsync(); var result = new List<string>();
        while (await rows.ReadAsync()) result.Add(rows.GetString(0) + ":" + rows.GetDecimal(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (result.Count != 6) throw new InvalidDataException("All six existing canonical tables are required; no provisioning is allowed.");
        return result.ToArray();
    }

    private static async Task RunAsync(string connection, string expectedDatabase, string expectedSchema, string expectedService, string work, bool requireTcps)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedDatabase);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedService);
        if (!Directory.Exists(work)) throw new DirectoryNotFoundException("Reuse the existing Oracle evidence directory.");
        var report = Path.Combine(work, "oracle-schema-preflight.current.json");
        var next = report + ".next";
        void RefuseExisting()
        {
            if (File.Exists(report) || File.Exists(next)) throw new InvalidOperationException("Preserve original/interrupted Oracle preflight evidence before another run.");
        }
        RefuseExisting();
        await using var lease = new FileStream(Path.Combine(work, "oracle-schema-preflight.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        RefuseExisting();
        await using var native = new OracleConnection(connection); await native.OpenAsync();
        var target = await ReadTargetAsync(native);
        ValidateTarget(target, expectedDatabase, expectedSchema, expectedService, requireTcps);
        var before = await ReadCanonicalObjectsAsync(native);
        var provider = new ExistingSchemaOwner(connection);
        try
        {
            var active = await provider.ActivateProviderAsync();
            if (active.IsError) throw new InvalidDataException("Existing-schema production validation failed; no repair is permitted.", active.Exception);
            if (!active.Result || !provider.IsProviderActivated || provider.ProvisionCalls != 0)
                throw new InvalidDataException("Read-only preflight did not activate solely through existing-schema validation.");
            var after = await ReadCanonicalObjectsAsync(native);
            if (!before.SequenceEqual(after)) throw new InvalidDataException("Canonical table identities changed during read-only validation.");
            await using (var file = new FileStream(next, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, new
                {
                    state = "verified-read-only", target, canonicalObjects = after, provisioningCalls = provider.ProvisionCalls,
                    scope = requireTcps ? "hosted target/protocol/existing-schema preflight" : "LOCAL existing-schema control; NOT hosted/TLS proof",
                    tcpsRequired = requireTcps, crudVerified = false, walletPolicyVerified = false, certificatePolicyVerified = false,
                    protocolObservedOnInspectionSessionOnly = true, providerSessionBindingVerified = false,
                    durabilityVerified = false, cloudCostMeasured = false, fullProviderVerified = false, hyperDriveVerified = false,
                    completedUtc = DateTime.UtcNow
                });
                await file.FlushAsync(); file.Flush(true);
            }
            File.Move(next, report);
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
