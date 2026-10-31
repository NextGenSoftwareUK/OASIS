using System.Net;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public sealed class SolidCqlKeySetTests
{
    private static readonly Uri Root = new("https://pod.invalid/demo/");
    private static readonly byte[] Key = Enumerable.Range(1,32).Select(value => (byte)value).ToArray();

    [Theory]
    [InlineData("selection")] [InlineData("window")] [InlineData("denied")]
    [InlineData("corrupt")] [InlineData("budget")] [InlineData("missing")]
    [InlineData("forbidden")] [InlineData("invalid")] [InlineData("cancelled")]
    public async Task BoundedEncryptedResourceSetValidatesBeforePagePublication(string scenario)
    {
        using var codec = new SolidPrivateRecordCodec(Root, Key);
        var records = Enumerable.Range(0,3).Select(index => new Holon
        {
            Id = Guid.NewGuid(), Name = "Native record " + index,
            IsPublic = scenario != "denied" || index != 0
        }).ToArray();
        var transport = new Resources();
        foreach (var record in records)
            transport.Bodies[codec.Resource<IHolon>(record.Id)] = codec.Encode<IHolon>(record);
        using var provider = new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport) { EnableCqlRecordKeySets = true };
        Assert.False((await provider.ActivateProviderAsync()).IsError);

        // Select two native resources out of three; the unselected resource is never read.
        var keys = new[] { records[0].Id.ToString("D"), records[2].Id.ToString("D") };
        if (scenario == "invalid") keys[1] = Guid.Empty.ToString("D");
        if (scenario == "corrupt") transport.Bodies[codec.Resource<IHolon>(records[2].Id)] = "broken RDF";
        if (scenario == "missing") transport.Bodies.Remove(codec.Resource<IHolon>(records[2].Id));
        if (scenario == "forbidden") transport.Forbidden = codec.Resource<IHolon>(records[2].Id);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SOLIDOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row,SOLIDOASIS.CqlRecordIdKey))).Result;
        query = query.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        if (scenario == "window") query = query.Skip(1).Result;
        if (scenario == "corrupt") query = query.Take(0).Result;
        var planned = CosmicQueryPlanner.Plan(query,ProviderType.SOLIDOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SOLIDOASIS,provider.QueryCapabilities) });
        Assert.False(planned.IsError,planned.Message);
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancelled") cancellation.Cancel();
        var result = await provider.ExecuteCosmicQueryAsync(planned.Result,
            maximumResults: scenario == "budget" ? 1 : 10,cancellationToken:cancellation.Token);
        if (scenario is "corrupt" or "budget" or "forbidden" or "invalid" or "cancelled")
        {
            Assert.True(result.IsError); Assert.Null(result.Result); Assert.False(result.IsLoaded);
            Assert.Equal(scenario switch { "budget" => "CQL_QUERY_LIMIT", "invalid" => "CQL_INVALID_SOURCE_KEY", "cancelled" => "CQL_QUERY_CANCELLED", _ => "CQL_QUERY_FAILED" },result.ErrorCode);
        }
        else
        {
            Assert.False(result.IsError,result.Exception?.ToString());
            var expected = scenario == "selection" ? new[] { records[0].Id,records[2].Id }
                : scenario == "missing" ? new[] { records[0].Id } : new[] { records[2].Id };
            Assert.Equal(expected,result.Result.Select(row => row.Id));
            Assert.All(result.Result,row => { Assert.Empty(row.Children); Assert.Equal(row.Id.ToString("D"),row.MetaData[SOLIDOASIS.CqlRecordIdKey]); });
        }
        Assert.Equal(scenario is "invalid" or "cancelled" ? 0 : 2,transport.Reads.Count);
        Assert.DoesNotContain(codec.Resource<IHolon>(records[1].Id),transport.Reads);
    }

    private sealed class Resources : HttpMessageHandler
    {
        public Dictionary<Uri,string> Bodies = new();
        public List<Uri> Reads = new();
        public Uri? Forbidden;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Assert.Equal(HttpMethod.Get,request.Method);
            var uri = request.RequestUri!;
            if (uri == Root) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            Reads.Add(uri);
            if (uri == Forbidden) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
            return Task.FromResult(Bodies.TryGetValue(uri,out var body)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body,System.Text.Encoding.UTF8,"text/turtle") }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
