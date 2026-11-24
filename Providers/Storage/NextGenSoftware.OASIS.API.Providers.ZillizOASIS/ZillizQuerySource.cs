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
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.API.Providers.ZillizOASIS;

public sealed class ZillizQuerySource : ZillizOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    private static readonly HttpClient Http = new(CreateTransport());

    private static HttpMessageHandler CreateTransport()
    {
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseProxy = false;
        handler.UseCookies = false;
        return handler;
    }

    public ZillizQuerySource(string endpoint, string token, Guid probeId) : base(endpoint ?? "", token)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) ||
            origin.Scheme != Uri.UriSchemeHttps || origin.Port is not (443 or 19530) ||
            origin.UserInfo.Length != 0 || origin.PathAndQuery != "/" || origin.Fragment.Length != 0 ||
            !Regex.IsMatch(origin.IdnHost, @"\A[a-z0-9-]+\.(?:serverless\.)?[a-z0-9-]+\.vectordb\.zillizcloud\.com\z") ||
            !Regex.IsMatch(token ?? "", @"\A[A-Za-z0-9._~-]{1,4096}\z") || probeId == Guid.Empty)
            throw new ArgumentException("Zilliz requires a managed cluster endpoint, restricted API key and public probe.");

        this.probeId = probeId;
        query = new BoundedPublicKeyValueQuery(QueryProviderType, ReadAsync);

        async Task<string> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            var key = id.ToString("D");
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(origin, "/v2/vectordb/entities/get"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Request-Timeout", "5");
            request.Content = JsonContent.Create(new
            {
                collectionName = "oasis_holons",
                id = new[] { key },
                outputFields = new[] { "id", "doc" },
                consistencyLevel = "Strong"
            });
            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await response.Content.LoadIntoBufferAsync(512 * 1024, cancellationToken);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
            var envelope = document.RootElement;
            if (envelope.GetProperty("code").GetInt32() != 0)
                throw new InvalidDataException("Zilliz rejected the exact entity read.");
            var rows = envelope.GetProperty("data");
            if (rows.GetArrayLength() == 0) return null!;
            if (rows.GetArrayLength() != 1 || rows[0].GetProperty("id").GetString() != key)
                throw new InvalidDataException("Zilliz returned an entity outside the requested primary key.");
            var payload = rows[0].GetProperty("doc").GetString();
            if (string.IsNullOrEmpty(payload) || Encoding.UTF8.GetByteCount(payload) > 65535)
                throw new InvalidDataException("Zilliz returned an invalid stored document.");
            return payload;
        }
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.ZillizOASIS;
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
