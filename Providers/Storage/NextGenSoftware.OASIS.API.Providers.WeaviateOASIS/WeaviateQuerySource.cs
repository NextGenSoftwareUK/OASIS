using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Weaviate.Client.Grpc.Protobuf.V1;
using NativeClient = Weaviate.Client.Grpc.Protobuf.V1.Weaviate.WeaviateClient;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS;

/// <summary>Explicit query profile using the native protocol over a caller-owned channel.</summary>
public sealed class WeaviateQuerySource : WeaviateOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _probeId;
    private readonly NativeClient _client;
    private readonly string _apiKey;

    public WeaviateQuerySource(HttpClient rest, string origin, CallInvoker channel, string apiKey, Guid probeId)
        : base(rest, origin)
    {
        if (probeId == Guid.Empty)
            throw new ArgumentException("An existing public teaching probe is required.", nameof(probeId));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _client = new NativeClient(channel ?? throw new ArgumentNullException(nameof(channel)));
        _apiKey = apiKey;
        _probeId = probeId;
    }

    public Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
        => VerifyPublicQueryProbeAsync(_probeId, cancellationToken);

    protected override async IAsyncEnumerable<NativeCqlDocument> ReadNativeCqlAsync(
        IReadOnlyList<Guid> ids, [EnumeratorCancellation] CancellationToken cancellation)
    {
        if (ids.Count is < 1 or > 100 || ids.Any(id => id == Guid.Empty))
            throw new ArgumentException("One to one hundred canonical Weaviate identities are required.", nameof(ids));
        var predicates = ids.Select(id => new Filters
        {
            Operator = Filters.Types.Operator.Equal,
            Target = new FilterTarget { Property = "_id" },
            ValueText = id.ToString("D")
        }).ToArray();
        var filter = predicates[0];
        if (predicates.Length > 1)
        {
            filter = new Filters { Operator = Filters.Types.Operator.Or };
            filter.Filters_.AddRange(predicates);
        }
        var request = new SearchRequest
        {
            Collection = "OasisHolon",
            Limit = (uint)ids.Count + 1,
            Filters = filter,
            Metadata = new MetadataRequest { Uuid = true },
            Properties = new PropertiesRequest { NonRefProperties = { "dataJson", "isDeleted" } },
            Uses127Api = true
        };
        using var call = _client.SearchAsync(request, new Metadata { { "authorization", "Bearer " + _apiKey } },
            deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellation);
        var response = await call.ResponseAsync.ConfigureAwait(false);
        foreach (var item in response.Results)
        {
            cancellation.ThrowIfCancellationRequested();
            var properties = item.Properties?.NonRefProps?.Fields;
            if (item.Metadata?.IdAsBytes.Length != 16 || properties == null ||
                !properties.TryGetValue("dataJson", out var json) || json.KindCase != Value.KindOneofCase.TextValue ||
                !properties.TryGetValue("isDeleted", out var deleted) || deleted.KindCase != Value.KindOneofCase.BoolValue)
                throw new InvalidDataException("Weaviate omitted its native UUID or required typed properties.");
            var key = new Guid(item.Metadata.IdAsBytes.Span, bigEndian: true).ToString("D");
            yield return new NativeCqlDocument { Key = key, Id = key, IsDeleted = deleted.BoolValue, DataJson = json.TextValue };
        }
    }
}
