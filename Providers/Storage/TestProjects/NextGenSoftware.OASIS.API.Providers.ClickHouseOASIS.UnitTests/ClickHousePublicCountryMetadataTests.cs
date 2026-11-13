using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.UnitTests;

[TestClass]
public sealed class ClickHousePublicCountryMetadataTests
{
    private static readonly string[] Keys = { "France", "Germany", "Italy" };
    private sealed class Clock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => Interlocked.Add(ref ticks, 2);
    }
    private sealed class Transport : HttpMessageHandler
    {
        public int Requests;
        public string Fault = "";
        public string[] LastParameters = Array.Empty<string>();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Requests++; token.ThrowIfCancellationRequested();
            if (Fault == "cancel-flight") await Task.Delay(Timeout.Infinite, token);
            Assert.AreEqual(HttpMethod.Post, request.Method);
            var sql = await request.Content!.ReadAsStringAsync(token);
            Assert.IsTrue(sql.StartsWith("SELECT ") && sql.EndsWith("FORMAT JSON"));
            var parameters = request.RequestUri!.Query.TrimStart('?').Split('&')
                .Select(pair => pair.Split('=',2)).ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]));
            Assert.AreEqual("explorer", parameters["user"]);
            object[] rows;
            if (sql.Contains("system.columns")) rows = Fault == "schema" ? Array.Empty<object>() : new object[] {
                new {name="name",type="String"}, new {name="coordinates",type="MultiPolygon"} };
            else if (sql.Contains("currentUser()")) rows = new object[] { new { version="26.10", user=Fault=="user"?"default":"explorer", @readonly=Fault=="writable"?"0":"2" } };
            else
            {
                Assert.IsTrue(sql.Contains("FROM blogs.countries WHERE name IN ("));
                LastParameters = parameters.Where(pair => pair.Key.StartsWith("param_c"))
                    .OrderBy(pair => pair.Key).Select(pair => pair.Value.Replace("\\\\", "\\")).ToArray();
                foreach (var key in LastParameters) Assert.IsFalse(sql.Contains(key), "Native values must not enter SQL text.");
                rows = LastParameters.Select((key,i) => (object)new {
                    country = Fault=="foreign" && i==LastParameters.Length-1 ? "Foreign" : Fault=="duplicate" ? LastParameters[0] : key,
                    polygons = Fault=="precision" ? "18446744073709551616" : "18446744073709551615" }).ToArray();
                if (Fault=="missing") rows=rows.Skip(1).ToArray();
            }
            string json=JsonSerializer.Serialize(new {data=rows, rows=rows.Length});
            if (Fault=="duplicate-property") json=json.Replace("\"rows\":", "\"rows\":0,\"rows\":");
            if (Fault=="exception") json="{\"exception\":\"failed\",\"data\":[],\"rows\":0}";
            if (Fault=="oversize") json=new string(' ',262145);
            var response=new HttpResponseMessage(int.TryParse(Fault,out var status)?(HttpStatusCode)status:HttpStatusCode.OK)
                { Content=new StringContent(json,Encoding.UTF8,"application/json") };
            if (Fault=="exception-header") response.Headers.Add("X-ClickHouse-Exception-Code","241");
            return response;
        }
    }
    private static ClickHousePublicCountryMetadataSource Source(HttpClient client, IEnumerable<string>? keys=null) =>
        new(client,new Uri("https://example.invalid/"),keys??Keys,Guid.NewGuid(),timeProvider:new Clock());
    private static CosmicQueryPlan<Holon> Plan(ClickHousePublicCountryMetadataSource source, IEnumerable<string> keys, string scenario="")
    {
        var selected=keys.ToArray();
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ClickHouseOASIS).Result)
            .Where(row=>selected.Contains(CosmicQueryMetadata.Value<string>(row,ClickHousePublicCountryMetadataSource.AddressMetadata))).Result;
        if (scenario=="point")
        {
            string key=selected[0];
            query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ClickHouseOASIS).Result)
                .Where(row=>CosmicQueryMetadata.Value<string>(row,ClickHousePublicCountryMetadataSource.AddressMetadata)==key).Result;
        }
        if (scenario=="window") query=query.Skip(1).Result.Take(1).Result;
        if (scenario=="empty-window") query=query.Take(0).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.ClickHouseOASIS,
            new[]{new CosmicQueryTarget(ProviderType.ClickHouseOASIS,source.QueryCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);return plan.Result;
    }
    [DataTestMethod]
    [DataRow("normal")][DataRow("point")][DataRow("window")][DataRow("budget")][DataRow("unknown")]
    [DataRow("user")][DataRow("writable")][DataRow("missing")][DataRow("duplicate")]
    [DataRow("foreign")][DataRow("precision")][DataRow("duplicate-property")][DataRow("exception")]
    [DataRow("oversize")][DataRow("exception-header")][DataRow("403")][DataRow("429")][DataRow("500")]
    [DataRow("empty-window")][DataRow("inactive")][DataRow("cancel")]
    public async Task OnlyCompleteReadonlyNativeSelectionsArePublished(string scenario)
    {
        var transport=new Transport();using var client=new HttpClient(transport);using var source=Source(client);
        if(scenario!="inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        transport.Requests=0;transport.Fault=scenario=="empty-window"?"missing":scenario;
        using var cancellation=new CancellationTokenSource();if(scenario=="cancel")cancellation.Cancel();
        var result=await source.ExecuteCosmicQueryAsync(Plan(source,scenario=="unknown"?new[]{"Unknown"}:Keys,scenario),
            scenario=="budget"?2:100,cancellation.Token);
        if(scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            Assert.AreEqual(scenario=="normal"?3:1,result.Result.Count);
            Assert.AreEqual(scenario=="window"?"Germany":"France",result.Result[0].MetaData[ClickHousePublicCountryMetadataSource.AddressMetadata]);
            foreach(var row in result.Result){Assert.IsTrue(row.IsPublic);Assert.AreEqual("18446744073709551615",row.MetaData["ClickHousePolygonCount"]);}
        }
        else Assert.IsTrue(result.IsError,"No malformed/partial native output may escape.");
        if(scenario is "inactive" or "unknown" or "cancel") Assert.AreEqual(0,transport.Requests);
        if(scenario is "403" or "429" or "500") Assert.AreEqual(1,transport.Requests,"No retry.");
    }
    [TestMethod]
    public async Task ParametersPreserveQuotesAndLiteralBackslashesWithoutSqlInjection()
    {
        string[] selected={"Cote-d'Ivoire","x');DROP/**/TABLE/**/test;--","\\N"};
        var transport=new Transport();using var client=new HttpClient(transport);using var source=Source(client,selected);
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        var result=await source.ExecuteCosmicQueryAsync(Plan(source,selected));
        Assert.IsFalse(result.IsError,result.Exception?.ToString());CollectionAssert.AreEqual(selected,transport.LastParameters);
    }
    [TestMethod]
    public async Task SourceSchemaAndCancellationFailuresRemainExplicit()
    {
        var transport=new Transport{Fault="schema"};using var client=new HttpClient(transport);using var source=Source(client);
        Assert.IsTrue((await source.ActivateQuerySourceAsync(default)).IsError);
        transport.Fault="cancel-flight";using var cancellation=new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await source.ActivateQuerySourceAsync(cancellation.Token)).ErrorCode);
    }
    [TestMethod]
    public async Task MutationBoundaryRejectsWritesWithoutNetworkOrInputMutation()
    {
        var transport=new Transport();using var client=new HttpClient(transport);using var source=Source(client);
        var row=new Holon{Version=7,Name="Unchanged"};
        Assert.AreEqual("CQL_READ_ONLY_SOURCE",(await source.SaveHolonAsync(row)).ErrorCode);
        Assert.AreEqual("CQL_READ_ONLY_SOURCE",(await source.SaveHolonsAsync(new[]{row})).ErrorCode);
        Assert.AreEqual("CQL_READ_ONLY_SOURCE",(await source.DeleteHolonAsync(row.Id)).ErrorCode);
        Assert.AreEqual("CQL_READ_ONLY_SOURCE",(await source.DeleteHolonAsync("unknown")).ErrorCode);
        Assert.AreEqual(7,row.Version);Assert.AreEqual(0,transport.Requests);
    }
    [TestMethod]
    public void ConfigurationRejectsKeysTheEnrolledStrategyCannotQuery()
    {
        var transport=new Transport();using var client=new HttpClient(transport);
        foreach(var key in new[]{"Côte-dIvoire","United Kingdom","", "line\nfeed"})
            Assert.ThrowsException<ArgumentException>(()=>Source(client,new[]{key}));
        Assert.AreEqual(0,transport.Requests);
    }
}
