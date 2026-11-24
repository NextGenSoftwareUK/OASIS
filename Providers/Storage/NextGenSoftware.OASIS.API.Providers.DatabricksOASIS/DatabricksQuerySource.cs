using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.DatabricksOASIS;

public sealed class DatabricksQuerySource : DatabricksOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });

    public DatabricksQuerySource(string workspaceUrl, string token, string warehouseId,
        string catalog, string schema, Guid probeId) : base(workspaceUrl, token, warehouseId)
    {
        if (!Uri.TryCreate(workspaceUrl, UriKind.Absolute, out var workspace) ||
            workspace.Scheme != Uri.UriSchemeHttps || !workspace.IsDefaultPort ||
            workspace.UserInfo.Length != 0 || workspace.PathAndQuery != "/" || workspace.Fragment.Length != 0 ||
            !Regex.IsMatch(workspace.IdnHost, @"\A[a-z0-9-]+\.(?:cloud\.databricks\.com|azuredatabricks\.net|gcp\.databricks\.com)\z") ||
            !Regex.IsMatch(warehouseId ?? "", @"\A[0-9a-f]{16}\z") ||
            !Regex.IsMatch(catalog ?? "", @"\A[A-Za-z_][A-Za-z0-9_]{0,127}\z") ||
            !Regex.IsMatch(schema ?? "", @"\A[A-Za-z_][A-Za-z0-9_]{0,127}\z") ||
            !Regex.IsMatch(token ?? "", @"\A[A-Za-z0-9._~-]{1,8192}\z") || probeId == Guid.Empty)
            throw new ArgumentException("Databricks requires a managed workspace, restricted reader, warehouse and public probe.");

        this.probeId = probeId;
        query = new BoundedPublicKeyValueQuery(QueryProviderType, ReadAsync, maximumConcurrentReads: 1);

        async Task<string> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            const int maximumResultBytes = 256 * 1024;
            var key = id.ToString("D");
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(workspace, "/api/2.0/sql/statements"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new
            {
                warehouse_id = warehouseId,
                catalog,
                schema,
                statement = "SELECT id, doc FROM oasis_holons WHERE id = :id LIMIT 2",
                parameters = new[] { new { name = "id", value = key, type = "STRING" } },
                disposition = "INLINE",
                format = "JSON_ARRAY",
                byte_limit = maximumResultBytes,
                wait_timeout = "5s",
                on_wait_timeout = "CANCEL"
            });

            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await response.Content.LoadIntoBufferAsync(2 * maximumResultBytes, cancellationToken);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
            return ReadInlineResult(document.RootElement, key)!;
        }
    }

    internal static string? ReadInlineResult(JsonElement root, string key)
    {
        if (root.GetProperty("status").GetProperty("state").GetString() != "SUCCEEDED")
            throw new InvalidDataException("Databricks did not complete the teaching statement.");

        var manifest = root.GetProperty("manifest");
        var rowCount = manifest.GetProperty("total_row_count").GetInt32();
        if (manifest.GetProperty("truncated").GetBoolean() || rowCount is < 0 or > 1)
            throw new InvalidDataException("Databricks returned a truncated or duplicate teaching result.");
        var result = root.GetProperty("result");
        if (result.TryGetProperty("next_chunk_index", out _) ||
            result.TryGetProperty("next_chunk_internal_link", out _) ||
            result.TryGetProperty("external_links", out _))
            throw new InvalidDataException("Databricks did not return a complete inline result.");
        var rows = result.GetProperty("data_array");
        if (rows.GetArrayLength() != rowCount)
            throw new InvalidDataException("Databricks returned an incomplete teaching result.");
        if (rowCount == 0) return null;

        var row = rows[0];
        if (row.GetArrayLength() != 2 || row[0].GetString() != key || row[1].GetString() is not string json)
            throw new InvalidDataException("Databricks returned an invalid teaching record.");
        return json;
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.DatabricksOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(probeId, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);
}
