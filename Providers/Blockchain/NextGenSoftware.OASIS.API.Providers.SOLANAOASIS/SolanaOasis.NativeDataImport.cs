using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

/// <summary>
/// Reads the accounts other applications keep on Solana so NativeDataImportManager can holonize them. Each program address is
/// a source and each account that program owns a record keyed by its address, paged in address order. Solana's RPC cannot
/// page <c>getProgramAccounts</c>, so one fetch is held for <see cref="SnapshotLifetime"/> and every page of an import comes
/// from that single consistent snapshot; a program with more than <see cref="MaximumAccounts"/> accounts is refused rather
/// than partly imported.
/// <para>
/// Given a <see cref="NativeConnection"/> (<c>rpcUrl</c> and <c>programs</c>) it reads through the caller's RPC node. With
/// none it reads <see cref="NativeImportPrograms"/> through this provider's own node.
/// </para>
/// </summary>
public partial class SolanaOASIS : INativeDataImportProvider
{
    public const int MaximumAccounts = 10000;
    private static readonly Regex AddressPattern = new Regex("^[1-9A-HJ-NP-Za-km-z]{32,44}$", RegexOptions.CultureInvariant);
    private static readonly string[] ConnectionSettings = { "rpcUrl", "programs" };
    private static readonly TimeSpan SnapshotLifetime = TimeSpan.FromSeconds(60);

    private readonly object _nativeSnapshotGate = new object();
    private (string Identity, string Program, DateTime FetchedUtc, IReadOnlyList<NativeSolanaAccount> Accounts)? _nativeSnapshot;

    /// <summary>Comma-separated program addresses whose accounts native import may read (DNA SolanaOASIS.NativeImportPrograms).</summary>
    public string NativeImportPrograms { get; set; }

    /// <summary>Fetches every account a program owns. Replaced in tests; by default it asks the RPC node.</summary>
    internal Func<string, CancellationToken, Task<IReadOnlyList<NativeSolanaAccount>>> NativeProgramAccounts { get; set; }

    /// <summary>The handler used to reach a caller's RPC node. Replaced in tests; by default it checks every address before connecting.</summary>
    internal Func<HttpMessageHandler> NativeConnectionHandler { get; set; } = NativeConnectionGuard.CreateGuardedHttpHandler;

    internal sealed record NativeSolanaAccount(string Address, ulong Lamports, string Owner, bool Executable, ulong RentEpoch, string DataBase64, long DataLength);

    public API.Core.Enums.ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.SolanaOASIS;

    public string DescribeNativeConnection(NativeConnection connection)
    {
        if (connection == null) return null;
        return "solana://" + ParseNativeConnection(connection).Rpc.Authority.ToLowerInvariant();
    }

    public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection = null,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
        try
        {
            var sources = new List<NativeDataSource>();
            foreach (var program in AllowedNativePrograms(connection))
                sources.Add(new NativeDataSource(program, "program", (await SnapshotAsync(program, connection, cancellationToken)).Count));
            result.Result = sources;
            result.Message = $"{sources.Count} allowed Solana programs.";
        }
        catch (Exception cause) when (cause is InvalidOperationException or InvalidDataException)
        {
            OASISErrorHandling.HandleError(ref result, $"Listing native Solana programs failed: {NativeConnectionGuard.Explain(cause)}", cause);
            result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_LIST_FAILED";
        }
        return result;
    }

    public async Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize,
        NativeConnection connection = null, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<NativeRecordPage>();
        try
        {
            if (pageSize < 1) throw new ArgumentException("Page size must be at least 1.");
            if (!AllowedNativePrograms(connection).Contains(sourceName, StringComparer.Ordinal))
                throw new ArgumentException($"'{sourceName}' is not a program of this connection.");

            var accounts = (await SnapshotAsync(sourceName, connection, cancellationToken))
                .Where(account => continuationToken == null || string.CompareOrdinal(account.Address, continuationToken) > 0)
                .Take(pageSize + 1).ToList();

            var records = accounts.Take(pageSize).Select(account => new NativeRecord(sourceName, account.Address, new Dictionary<string, object>
            {
                ["lamports"] = checked((long)account.Lamports),
                ["owner"] = account.Owner,
                ["executable"] = account.Executable,
                ["rentEpoch"] = account.RentEpoch.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["dataLength"] = account.DataLength,
                ["data"] = account.DataBase64
            })).ToList();
            result.Result = new NativeRecordPage(records, accounts.Count > pageSize ? records[^1].NativeKey : null);
        }
        catch (Exception cause) when (cause is InvalidOperationException or InvalidDataException or ArgumentException or OverflowException)
        {
            OASISErrorHandling.HandleError(ref result, $"Reading native Solana program '{sourceName}' failed: {NativeConnectionGuard.Explain(cause)}", cause);
            result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_READ_FAILED";
        }
        return result;
    }

    /// <summary>One consistent fetch per (node, program), held briefly so every page of an import comes from it.</summary>
    private async Task<IReadOnlyList<NativeSolanaAccount>> SnapshotAsync(string program, NativeConnection connection, CancellationToken cancellationToken)
    {
        var identity = DescribeNativeConnection(connection) ?? "configured";
        lock (_nativeSnapshotGate)
            if (_nativeSnapshot is { } held && held.Identity == identity && held.Program == program && DateTime.UtcNow - held.FetchedUtc < SnapshotLifetime)
                return held.Accounts;

        var fetch = NativeProgramAccounts ?? (connection != null
            ? (program2, token) => FetchThroughCallersNodeAsync(connection, program2, token)
            : FetchProgramAccountsAsync);
        var fetched = await fetch(program, cancellationToken);
        if (fetched.Count > MaximumAccounts)
            throw new InvalidDataException($"{program} owns {fetched.Count} accounts; native import reads at most {MaximumAccounts} per program.");
        var sorted = fetched.OrderBy(account => account.Address, StringComparer.Ordinal).ToList();
        if (sorted.Select(account => account.Address).Distinct(StringComparer.Ordinal).Count() != sorted.Count)
            throw new InvalidDataException($"{program} returned an account address twice.");

        lock (_nativeSnapshotGate) _nativeSnapshot = (identity, program, DateTime.UtcNow, sorted);
        return sorted;
    }

    private Task<IReadOnlyList<NativeSolanaAccount>> FetchProgramAccountsAsync(string program, CancellationToken cancellationToken) =>
        ReadProgramAccountsAsync(_rpcClient, program, cancellationToken);

    /// <summary>Reads through the caller's own RPC node with a client whose HTTP handler checks every address before connecting.</summary>
    private async Task<IReadOnlyList<NativeSolanaAccount>> FetchThroughCallersNodeAsync(NativeConnection connection, string program, CancellationToken cancellationToken)
    {
        var (rpc, _) = ParseNativeConnection(connection);
        using var http = new HttpClient(NativeConnectionHandler(), disposeHandler: true) { Timeout = TimeSpan.FromSeconds(60), BaseAddress = rpc };
        var client = Solnet.Rpc.ClientFactory.GetClient(rpc.AbsoluteUri, httpClient: http);
        return await ReadProgramAccountsAsync(client, program, cancellationToken);
    }

    private static async Task<IReadOnlyList<NativeSolanaAccount>> ReadProgramAccountsAsync(Solnet.Rpc.IRpcClient client, string program, CancellationToken cancellationToken)
    {
        var response = await client.GetProgramAccountsAsync(program, Solnet.Rpc.Types.Commitment.Finalized).WaitAsync(cancellationToken);
        if (!response.WasSuccessful || response.Result == null)
            throw new InvalidDataException($"The RPC node refused getProgramAccounts for {program}: {response.Reason}");
        return response.Result.Select(pair => new NativeSolanaAccount(pair.PublicKey, pair.Account.Lamports, pair.Account.Owner,
            pair.Account.Executable, pair.Account.RentEpoch, pair.Account.Data?.FirstOrDefault() ?? "",
            pair.Account.Data == null || pair.Account.Data.Count == 0 ? 0 : Convert.FromBase64String(pair.Account.Data[0]).LongLength)).ToList();
    }

    private static (Uri Rpc, string Programs) ParseNativeConnection(NativeConnection connection)
    {
        connection.RequireOnly(ConnectionSettings);
        if (!Uri.TryCreate(connection.Require("rpcUrl"), UriKind.Absolute, out var rpc) || rpc.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(rpc.UserInfo))
            throw new NativeConnectionRefusedException("rpcUrl must be the https address of a Solana RPC node without credentials, for example https://api.mainnet-beta.solana.com/.");
        return (rpc, connection.Require("programs"));
    }

    private IReadOnlyList<string> AllowedNativePrograms(NativeConnection connection)
    {
        var configured = connection == null ? NativeImportPrograms : ParseNativeConnection(connection).Programs;
        var programs = (configured ?? "").Split(',').Select(program => program.Trim()).Where(program => program.Length > 0)
            .Distinct(StringComparer.Ordinal).ToList();
        if (programs.Count == 0)
            throw new InvalidOperationException("Set NativeImportPrograms to the Solana program addresses other applications own before importing native data, or send a connection.");
        var invalid = programs.FirstOrDefault(program => !AddressPattern.IsMatch(program));
        if (invalid != null)
            throw new InvalidOperationException($"'{invalid}' is not a Solana program address.");
        return programs;
    }
}
