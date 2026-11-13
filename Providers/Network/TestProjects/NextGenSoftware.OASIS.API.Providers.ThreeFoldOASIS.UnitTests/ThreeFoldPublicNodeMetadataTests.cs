using System.Net;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS.UnitTests;

public sealed class ThreeFoldPublicNodeMetadataTests
{
    private static readonly string[] Keys={"1","2","3"};
    private sealed class Clock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency=>1;
        public override long GetTimestamp()=>Interlocked.Add(ref ticks,2);
    }
    private sealed class Transport : HttpMessageHandler
    {
        public int Requests;
        public string Fault="";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();Requests++;
            Assert.Equal(HttpMethod.Get,request.Method);
            Assert.Equal("gridproxy.grid.tf",request.RequestUri!.Host);
            Assert.Empty(request.RequestUri.Query);
            uint key=uint.Parse(request.RequestUri.AbsolutePath.Split('/').Last());
            Assert.Equal("/nodes/"+key,request.RequestUri.AbsolutePath);
            if(Fault=="cancel-flight")await Task.Delay(Timeout.Infinite,token);
            var fields=new Dictionary<string,object?>{
                ["nodeId"]=Fault=="identity"||Fault=="last"&&key==3?key+1:key,
                ["farmId"]=4294967295U,["gridVersion"]=3U,["country"]="Belgium",["city"]="Ghent",
                ["publicConfig"]=new{ipv4="must-not-be-projected"},["total_resources"]=new{mru=1234}
            };
            if(Fault=="missing")fields.Remove("city");
            if(Fault=="null-city")fields["city"]=null;
            if(Fault=="typed-id")fields["nodeId"]=key.ToString();
            if(Fault=="fractional-farm")fields["farmId"]=1.5;
            if(Fault=="error")fields["error"]="native failure";
            string json=JsonSerializer.Serialize(fields);
            if(Fault=="duplicate")json=json.Replace("\"nodeId\":","\"nodeId\":0,\"nodeId\":");
            if(Fault=="oversize")json=new string(' ',262145);
            return new HttpResponseMessage(int.TryParse(Fault,out var status)?(HttpStatusCode)status:HttpStatusCode.OK)
                {Content=new StringContent(json,Encoding.UTF8,"application/json")};
        }
    }
    private static ThreeFoldPublicNodeMetadataSource Source(HttpClient client,IEnumerable<string>? keys=null)=>
        new(client,new Uri("https://gridproxy.grid.tf/"),keys??Keys,Guid.NewGuid(),timeProvider:new Clock());
    private static CosmicQueryPlan<Holon> Plan(ThreeFoldPublicNodeMetadataSource source,string[] keys,string scenario="")
    {
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ThreeFoldOASIS).Result)
            .Where(row=>keys.Contains(CosmicQueryMetadata.Value<string>(row,ThreeFoldPublicNodeMetadataSource.AddressMetadata))).Result;
        if(scenario=="point")
        {
            string key=keys[0];query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ThreeFoldOASIS).Result)
                .Where(row=>CosmicQueryMetadata.Value<string>(row,ThreeFoldPublicNodeMetadataSource.AddressMetadata)==key).Result;
        }
        if(scenario=="window")query=query.Skip(1).Result.Take(1).Result;
        if(scenario=="empty-window")query=query.Take(0).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.ThreeFoldOASIS,new[]{new CosmicQueryTarget(ProviderType.ThreeFoldOASIS,source.QueryCapabilities)});
        Assert.False(plan.IsError,plan.Message);return plan.Result;
    }
    [Theory]
    [InlineData("normal")][InlineData("point")][InlineData("window")][InlineData("budget")]
    [InlineData("identity")][InlineData("last")][InlineData("missing")][InlineData("null-city")]
    [InlineData("typed-id")][InlineData("fractional-farm")][InlineData("error")][InlineData("duplicate")]
    [InlineData("oversize")][InlineData("403")][InlineData("404")][InlineData("429")][InlineData("500")]
    [InlineData("inactive")][InlineData("cancel")][InlineData("unknown")][InlineData("empty-window")]
    public async Task OnlyCompleteIdentityCheckedNodeSelectionsBecomeVisible(string scenario)
    {
        var transport=new Transport();using var client=new HttpClient(transport);using var source=Source(client);
        if(scenario!="inactive")Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        transport.Requests=0;transport.Fault=scenario=="empty-window"?"last":scenario;
        using var cancellation=new CancellationTokenSource();if(scenario=="cancel")cancellation.Cancel();
        var result=await source.ExecuteCosmicQueryAsync(Plan(source,scenario=="unknown"?new[]{"4"}:Keys,scenario),scenario=="budget"?2:100,cancellation.Token);
        if(scenario is "normal" or "point" or "window")
        {
            Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(scenario=="normal"?3:1,result.Result.Count);
            Assert.Equal(scenario=="window"?"2":"1",result.Result[0].MetaData[ThreeFoldPublicNodeMetadataSource.AddressMetadata]);
            Assert.All(result.Result,row=>{
                Assert.True(row.IsPublic);Assert.Equal("4294967295",row.MetaData["ThreeFoldFarmId"]);
                Assert.Equal("false",row.MetaData["ThreeFoldChainStateVerified"]);
                Assert.False(row.MetaData.ContainsKey("publicConfig"));Assert.False(row.MetaData.ContainsKey("total_resources"));
            });
        }
        else {Assert.True(result.IsError,"No malformed/partial source output may escape.");Assert.Null(result.Result);}
        if(scenario is "inactive" or "cancel" or "unknown")Assert.Equal(0,transport.Requests);
        if(scenario is "403" or "404" or "429" or "500")Assert.Equal(1,transport.Requests);
    }
    [Theory]
    [InlineData("0")][InlineData("01")][InlineData("+1")][InlineData("4294967296")][InlineData("1/2")]
    public void ConfigurationRejectsNoncanonicalNodeKeys(string key)
    {
        using var client=new HttpClient(new Transport());Assert.Throws<ArgumentException>(()=>Source(client,new[]{key}));
    }
    [Theory]
    [InlineData("https://gridproxy.test.grid.tf/")][InlineData("http://gridproxy.grid.tf/")]
    [InlineData("https://gridproxy.grid.tf/?key=secret")][InlineData("https://secret@gridproxy.grid.tf/")]
    public void ConfigurationCannotSwitchNetworkOrInjectEndpointCredentials(string endpoint)
    {
        using var client=new HttpClient(new Transport());Assert.Throws<ArgumentException>(()=>new ThreeFoldPublicNodeMetadataSource(client,new Uri(endpoint),Keys,Guid.NewGuid()));
    }
    [Fact]
    public async Task UInt32MaximumKeyRemainsExact()
    {
        var transport=new Transport();using var client=new HttpClient(transport);using var source=Source(client,new[]{"4294967295"});
        Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        var result=await source.ExecuteCosmicQueryAsync(Plan(source,new[]{"4294967295"}));
        Assert.False(result.IsError);Assert.Equal("4294967295",Assert.Single(result.Result).MetaData[ThreeFoldPublicNodeMetadataSource.AddressMetadata]);
    }
    [Fact]
    public async Task MutationBoundaryRejectsWritesWithoutNetworkOrInputMutation()
    {
        var transport=new Transport();using var client=new HttpClient(transport);using var source=Source(client);
        var row=new Holon{Version=7,Name="Unchanged"};
        Assert.Equal("CQL_READ_ONLY_SOURCE",(await source.SaveHolonAsync(row)).ErrorCode);
        Assert.Equal("CQL_READ_ONLY_SOURCE",(await source.SaveHolonsAsync(new[]{row})).ErrorCode);
        Assert.Equal("CQL_READ_ONLY_SOURCE",(await source.DeleteHolonAsync(row.Id)).ErrorCode);
        Assert.Equal("CQL_READ_ONLY_SOURCE",(await source.DeleteHolonAsync("unknown")).ErrorCode);
        Assert.Equal(7,row.Version);Assert.Equal(0,transport.Requests);
    }
    [Fact]
    public async Task InflightCancellationRemainsExplicit()
    {
        var transport=new Transport{Fault="cancel-flight"};using var client=new HttpClient(transport);using var source=Source(client);
        using var cancellation=new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        Assert.Equal("CQL_QUERY_CANCELLED",(await source.ActivateQuerySourceAsync(cancellation.Token)).ErrorCode);
    }
    [Fact]
    public async Task SharedDeveloperWalkthroughUsesNewAdaptersDeclaredMembershipKey()
    {
        var transport=new Transport();using var client=new HttpClient(transport);using var source=Source(client);
        Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {
            OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2}};
        var registry=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna);
        var manager=new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null,dna,registry);
        using var output=new StringWriter();
        var result=await CosmicCqlDemo.NativeKeySetWalkthrough.RunAsync(manager,source,Keys,Guid.NewGuid(),output);
        Assert.Equal(3,result.Count);Assert.Contains("Selected native keys: 3; returned holons: 3",output.ToString());
    }
}
