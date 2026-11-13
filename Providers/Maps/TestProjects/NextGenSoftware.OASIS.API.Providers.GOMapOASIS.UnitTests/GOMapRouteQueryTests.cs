using System.Net;
using System.Text;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.UnitTests;

[TestClass]
public sealed class GOMapRouteQueryTests
{
    private static readonly Guid Owner=Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Keys={"west-north","north-east","east-west"};
    private static readonly Guid[] Ids=Keys.Select(x=>GOMapRouteQuerySource.LogicalId(Owner,x)).ToArray();
    private const ProviderType Provider=ProviderType.GOMapOASIS;
    private sealed class Replies : HttpMessageHandler
    {
        public int Calls; public string? Fault; public bool FailSecond;
        public string ExpectedHost { get; init; } = "router.project-osrm.org";
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();Calls++;
            Assert.AreEqual(ExpectedHost,request.RequestUri!.Host);
            Assert.IsTrue(request.RequestUri.AbsolutePath.StartsWith("/route/v1/driving/"));
            Assert.IsNull(request.Headers.Authorization);
            var body="{\"code\":\"Ok\",\"routes\":[{\"distance\":12,\"duration\":4,\"geometry\":{\"type\":\"LineString\",\"coordinates\":[[13,52],[13.1,52.1]]},\"extra\":\"retained\"}],\"waypoints\":[{\"location\":[13,52]},{\"location\":[13.1,52.1]}]}";
            if(Fault=="missing-code")body=body.Replace("\"code\":\"Ok\",","");
            if(Fault=="duplicate")body=body.Replace("\"code\":\"Ok\"","\"code\":\"Ok\",\"code\":\"Ok\"");
            if(Fault=="bad-geometry")body=body.Replace("LineString","Point");
            if(Fault=="no-route")body=body.Replace("\"Ok\"","\"NoRoute\"");
            var response=new HttpResponseMessage(Fault=="http" || (FailSecond && request.RequestUri.AbsolutePath.Contains("13.397634,52.529407;"))?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK){RequestMessage=request,Content=new StringContent(body,Encoding.UTF8,Fault=="content-type"?"text/plain":"application/json")};
            return Task.FromResult(response);
        }
    }
    private static Task<OASISResult<IReadOnlyList<IHolon>>> Read(GOMapRouteQuerySource source,Guid[] ids)
    {
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner,Provider,ids).Result).Result
            .Where(row=>ids.Contains(row.Id)).Result;
        var plan=CosmicQueryPlanner.Plan(query,Provider,new[]{new CosmicQueryTarget(Provider,source.QueryCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);return source.ExecuteCosmicQueryAsync(plan.Result);
    }
    private static void Check(OASISResult<IReadOnlyList<IHolon>> result,Guid[] ids)
    {
        Assert.IsFalse(result.IsError,result.Exception?.ToString());CollectionAssert.AreEquivalent(ids,result.Result.Select(x=>x.Id).ToArray());
        foreach(var row in result.Result)
        {
            Assert.AreEqual(Owner,row.CreatedByAvatarId);Assert.IsTrue(row.IsPublic && row.IsActive);Assert.IsFalse(row.Children.Any());
            var key=row.MetaData["GOMapRouteKey"].ToString()!;Assert.AreEqual(GOMapRouteQuerySource.LogicalId(Owner,key),row.Id);
            Assert.AreEqual(key,row.ProviderUniqueStorageKey[Provider]);
            var raw=row.MetaData["GOMapRouteJson"].ToString()!;
            Assert.AreEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant(),row.MetaData["GOMapResponseSha256"].ToString());
            Assert.AreEqual("https://www.openstreetmap.org/copyright",row.MetaData["GOMapAttributionUrl"].ToString());
            Assert.AreEqual("https://www.openstreetmap.org/fixthemap",row.MetaData["GOMapFixMapUrl"].ToString());
        }
    }
    [TestMethod]
    public async Task FullAndSubsetRetainNativePayloadIdentityAndAttribution()
    {
        var origin = new Uri("https://routing.example/");
        using var replies = new Replies { ExpectedHost = origin.Host };
        using var source = new GOMapRouteQuerySource(Owner, Keys, replies, origin);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        var full = await Read(source, Ids);
        Check(full, Ids);
        foreach (var row in full.Result)
            Assert.AreEqual(origin.AbsoluteUri, row.MetaData["GOMapEndpoint"].ToString());
        Check(await Read(source,Ids.Take(2).ToArray()),Ids.Take(2).ToArray());
        Assert.AreEqual(6,replies.Calls);
    }
    [DataTestMethod]
    [DataRow("http")][DataRow("missing-code")][DataRow("duplicate")][DataRow("bad-geometry")][DataRow("no-route")][DataRow("content-type")]
    public async Task FailedNativeReadRefusesActivationWithoutRetry(string fault)
    {
        using var replies=new Replies{Fault=fault};using var source=new GOMapRouteQuerySource(Owner,Keys,replies);
        var result=await source.ActivateQuerySourceAsync();Assert.IsTrue(result.IsError);Assert.IsFalse(source.IsProviderActivated);Assert.AreEqual(1,replies.Calls);
    }
    [TestMethod]
    public void CataloguesAndOwnersAreExplicitBeforeTransport()
    {
        CollectionAssert.AreEqual(new[]{"f2c0c826-066e-8a9c-aaec-edafe1a16fdf","ba0c3bc1-920a-8f73-8c34-51a1cfc9d6e3","d25cf63e-14cb-8a85-aa80-c1ee36b1f1b2"},Ids.Select(x=>x.ToString()).ToArray());
        foreach(var keys in new[]{Array.Empty<string>(),new[]{"west-north","west-north"},new[]{"visitor-route"},new[]{"WEST-NORTH"}})
            Assert.ThrowsException<ArgumentException>(()=>new GOMapRouteQuerySource(Owner,keys));
        Assert.ThrowsException<ArgumentException>(()=>new GOMapRouteQuerySource(Guid.Empty,Keys));
        Assert.AreNotEqual(Ids[0],GOMapRouteQuerySource.LogicalId(Guid.NewGuid(),Keys[0]));
    }
    [TestMethod]
    public async Task InactiveAndCancelledReadsDoNotDispatch()
    {
        using var replies=new Replies();using var source=new GOMapRouteQuerySource(Owner,Keys,replies);
        Assert.IsTrue((await Read(source,Ids)).IsError);using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Assert.IsTrue((await source.ActivateQuerySourceAsync(cancellation.Token)).IsError);Assert.AreEqual(0,replies.Calls);
    }
    [TestMethod]
    public async Task UnknownSelectionIsPreflightedAsAWhole()
    {
        using var replies=new Replies();using var source=new GOMapRouteQuerySource(Owner,Keys,replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);var before=replies.Calls;
        var result=await Read(source,new[]{Ids[0],Guid.NewGuid()});Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(before,replies.Calls);
    }
    [TestMethod]
    public async Task OneFailedRoutePublishesNoPartialRows()
    {
        using var replies=new Replies();using var source=new GOMapRouteQuerySource(Owner,Keys,replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);replies.FailSecond=true;
        var result=await Read(source,Ids);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
    }
    [TestMethod]
    public void UnsupportedContentFilterIsNotAdvertisedAsNativeSupport()
    {
        using var source=GOMapRouteQuerySource.ForReviewedTeachingCatalogue(Owner);
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result).Where(row=>row.Name=="visitor-route").Result;
        var result=CosmicQueryPlanner.Plan(query,Provider,new[]{new CosmicQueryTarget(Provider,source.QueryCapabilities)});
        Assert.IsTrue(result.IsError);
    }
    [TestMethod,TestCategory("GOMapRouteNative")]
    public async Task FixedPublicNativeCqlReturnsThreeAndTwoAttributedRoutes()
    {
        Assert.AreEqual("fixed-public-berlin",Environment.GetEnvironmentVariable("COSMIC_GOMAP_NATIVE_CQL"));
        Assert.AreEqual("true",Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory=Path.GetFullPath(Environment.GetEnvironmentVariable("COSMIC_GOMAP_NATIVE_CQL_EVIDENCE_DIRECTORY")!);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE")!,"OASIS","TestResults","GOMapRouteQueryCurrent","native")),directory);
        Assert.IsFalse(Directory.Exists(directory));Directory.CreateDirectory(directory);
        using var source=GOMapRouteQuerySource.ForReviewedTeachingCatalogue(Owner);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        var full=await Read(source,Ids);Check(full,Ids);
        var subset=await Read(source,Ids.Take(2).ToArray());Check(subset,Ids.Take(2).ToArray());
        object[] Observations(IReadOnlyList<IHolon> rows)=>rows.Select(row=>(object)new{
            id=row.Id,key=row.MetaData["GOMapRouteKey"].ToString(),rawJson=row.MetaData["GOMapRouteJson"].ToString(),
            sha256=row.MetaData["GOMapResponseSha256"].ToString(),attribution=row.MetaData["GOMapAttribution"].ToString(),
            attributionUrl=row.MetaData["GOMapAttributionUrl"].ToString(),fixMapUrl=row.MetaData["GOMapFixMapUrl"].ToString()}).ToArray();
        await File.WriteAllTextAsync(Path.Combine(directory,"acknowledged.json"),System.Text.Json.JsonSerializer.Serialize(new{
            source=Environment.GetEnvironmentVariable("GITHUB_SHA"),provider=Provider.ToString(),sourceOwner=Owner,
            nativeCounts=new[]{3,2},full=Observations(full.Result),subset=Observations(subset.Result),
            sourceWrites=0,developerQualificationOnly=true,liveDeployment=false,web4Qualified=false}));
    }

}
