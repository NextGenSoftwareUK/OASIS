using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [TestMethod]
    public async Task NativeQueryScalarsMatchIndependentPointReadWithoutDateCoercion()
    {
        const string date="2030-01-02T03:04:05.0000000+05:30",number="9007199254740993.125";
        var id=Guid.NewGuid();var name="owned-query-scalar-"+id.ToString("N");
        var work=Environment.GetEnvironmentVariable("OASIS_COSMOSDB_WORK")??throw new InvalidOperationException("Retained Cosmos runner work directory required.");
        var journal=Path.Combine(work,"cosmos-query-scalar.pending.json");
        Assert.IsFalse(File.Exists(journal));var completed=false;
        try{
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,name,container="holonItems",state="before-owned-query-scalar"}));file.Flush(true);
            }
            using var precise=JsonDocument.Parse(number);
            _provider.IsVersionControlEnabled=true;
            var saved=await _provider.SaveHolonAsync(new Holon{Id=id,Name=name,MetaData=new(){["date"]=date,["number"]=precise.RootElement.Clone()}},saveChildren:false);
            Assert.IsFalse(saved.IsError,saved.Message+"; cause="+saved.Exception);
            saved.Result.Description="owned-second-version";
            var updated=await _provider.SaveHolonAsync(saved.Result,saveChildren:false);
            Assert.IsFalse(updated.IsError,updated.Message+"; cause="+updated.Exception);
            var container=_client.GetContainer(DatabaseName,"holonItems");var key=new PartitionKey(id.ToString());
            using var point=await container.ReadItemStreamAsync(id.ToString(),key);Assert.AreEqual(HttpStatusCode.OK,point.StatusCode);
            var etag=point.Headers.ETag;Assert.IsFalse(string.IsNullOrWhiteSpace(etag));
            using var reader=new StreamReader(point.Content);var pointText=await reader.ReadToEndAsync();using var raw=JsonDocument.Parse(pointText);
            var metadata=raw.RootElement.GetProperty("MetaData");Assert.AreEqual(date,metadata.GetProperty("date").GetString());
            Assert.AreEqual(JsonValueKind.String,metadata.GetProperty("number").ValueKind);
            Assert.AreEqual(number,metadata.GetProperty("number").GetString(),"Native wire string must preserve the exact source literal.");
            Assert.AreEqual("binary64-v1",raw.RootElement.GetProperty(Binary64JsonNumberCodec.PropertyName).GetProperty("schema").GetString());
            using var canonical=JsonDocument.Parse(Binary64JsonNumberCodec.Decode(OasisJson.ParseStoredObject(pointText)).ToString(Newtonsoft.Json.Formatting.None));
            var nativeNumber=canonical.RootElement.GetProperty("MetaData").GetProperty("number").GetDecimal();
            Assert.AreEqual(decimal.Parse(number,System.Globalization.CultureInfo.InvariantCulture),nativeNumber);
            var archive=await _provider.LoadHolonAsync(id,loadChildren:false,version:1);Assert.IsFalse(archive.IsError,archive.Message);
            using var historical=JsonDocument.Parse(OasisJson.Serialize(archive.Result));
            Assert.AreEqual(nativeNumber,historical.RootElement.GetProperty("MetaData").GetProperty("number").GetDecimal());
            var selected=await _provider.LoadHolonsByMetaDataAsync("number",number,loadChildren:false);
            Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual(id,selected.Result.Single().Id);
            Console.WriteLine(JsonSerializer.Serialize(new{sourceNumber=number,nativeWireNumber=metadata.GetProperty("number").GetString(),restoredNumericValue=nativeNumber}));
            var client=new CosmosDbClient(container);
            foreach(var document in new[]{client.ReadDocumentByField("Name",name),client.ReadAllDocuments().Single(),(await client.ReadAllDocumentsAsync()).Single()}){
                using var query=JsonDocument.Parse(document);var values=query.RootElement.GetProperty("MetaData");
                Assert.AreEqual(date,values.GetProperty("date").GetString());Assert.AreEqual(nativeNumber,values.GetProperty("number").GetDecimal());
            }
            using var deleted=await container.DeleteItemStreamAsync(id.ToString(),key,new ItemRequestOptions{IfMatchEtag=etag});
            Assert.AreEqual(HttpStatusCode.NoContent,deleted.StatusCode);
            using var absent=await container.ReadItemStreamAsync(id.ToString(),key);Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);
            File.Delete(journal);completed=true;
        }finally{if(!completed)_ownsDatabase=false;}
    }
}

public partial class CosmosNativeStorageTests
{
    [TestMethod][TestCategory("ScalarReconciliation")]
    public async Task RemoveOnlyReviewedCommittedScalarFixtureByOriginalEtag()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_COSMOSDB_WORK")??throw new InvalidOperationException("Retained runner required.");
        string Hash(byte[] bytes)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        var journal=Path.Combine(work,"cosmos-query-scalar.pending.json");
        Assert.AreEqual("1E63DBEC39F0466F2CC2BC0487E8F93EF780F0D2A37D0CD5853BF51F85990BA8",Hash(await File.ReadAllBytesAsync(journal)));
        var inventory=Path.Combine(work,"cosmos-query-scalar-inspection.json");
        Assert.AreEqual("3DBE4BDF106385A1C9BC88983BA0E6903016DCBDE1D3903C219AC3770A7B1803",Hash(await File.ReadAllBytesAsync(inventory)));
        var id="b8de3c86-356f-4db8-a0f9-28df785c3daa";const string etag="47f5b6a9-aed5-45bb-971f-e3f908843a8b",bodyHash="8D09887823FB75466EEE89F29C6AEFEEAD13E23230B60736F962ADBADF89F8BC";
        using var client=new CosmosClient("https://127.0.0.1:8081",EmulatorKey,new CosmosClientOptions{ConnectionMode=ConnectionMode.Gateway,RequestTimeout=TimeSpan.FromSeconds(10),MaxRetryAttemptsOnRateLimitedRequests=0,
            HttpClientFactory=()=>new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=HttpClientHandler.DangerousAcceptAnyServerCertificateValidator})});
        var database=client.GetDatabase("oasis-cosmos-crud");
        async Task Counts(bool removed){
            foreach(var table in new[]{"avatarItems","avatarDetailItems","holonItems"}){
                using var query=database.GetContainer(table).GetItemQueryIterator<long>("SELECT VALUE COUNT(1) FROM c");long count=0;
                while(query.HasMoreResults)count+=(await query.ReadNextAsync()).Single();
                Assert.AreEqual(!removed&&table=="holonItems"?1L:0L,count,"Only the reviewed single fixture is disposable.");
            }
        }
        await Counts(false);var container=database.GetContainer("holonItems");var key=new PartitionKey(id);
        using(var point=await container.ReadItemStreamAsync(id,key)){
            Assert.AreEqual(HttpStatusCode.OK,point.StatusCode);Assert.AreEqual(etag,point.Headers.ETag);
            using var reader=new StreamReader(point.Content);var raw=await reader.ReadToEndAsync();Assert.AreEqual(bodyHash,Hash(System.Text.Encoding.UTF8.GetBytes(raw)));
            using var value=JsonDocument.Parse(raw);Assert.AreEqual(id,value.RootElement.GetProperty("Id").GetString());
            Assert.AreEqual("owned-query-scalar-b8de3c86356f4db8a0f928df785c3daa",value.RootElement.GetProperty("Name").GetString());
        }
        using(var deleted=await container.DeleteItemStreamAsync(id,key,new ItemRequestOptions{IfMatchEtag=etag}))Assert.AreEqual(HttpStatusCode.NoContent,deleted.StatusCode);
        using(var absent=await container.ReadItemStreamAsync(id,key))Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);
        await Counts(true);
        using(var file=new FileStream(Path.Combine(work,"cosmos-query-scalar-cleanup.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,etag,bodyHash,originalReplayed=false,conditionalDeleteAcknowledged=true,independentAbsenceVerified=true,allThreeFixtureContainersEmpty=true}));file.Flush(true);
        }
        File.Delete(journal);
    }

    [TestMethod][TestCategory("ScalarInspection")]
    public async Task InspectOriginalQueryScalarFixtureWithoutReplayingSave()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_COSMOSDB_WORK")??throw new InvalidOperationException("Retained runner required.");
        var journal=Path.Combine(work,"cosmos-query-scalar.pending.json");
        Assert.AreEqual("1E63DBEC39F0466F2CC2BC0487E8F93EF780F0D2A37D0CD5853BF51F85990BA8",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(journal))));
        var id=Guid.Parse("b8de3c86-356f-4db8-a0f9-28df785c3daa");var name="owned-query-scalar-b8de3c86356f4db8a0f928df785c3daa";
        using var client=new CosmosClient("https://127.0.0.1:8081",EmulatorKey,new CosmosClientOptions{ConnectionMode=ConnectionMode.Gateway,RequestTimeout=TimeSpan.FromSeconds(10),MaxRetryAttemptsOnRateLimitedRequests=0,
            HttpClientFactory=()=>new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=HttpClientHandler.DangerousAcceptAnyServerCertificateValidator})});
        using var response=await client.GetContainer("oasis-cosmos-crud","holonItems").ReadItemStreamAsync(id.ToString(),new PartitionKey(id.ToString()));
        Assert.AreEqual(HttpStatusCode.OK,response.StatusCode);using var reader=new StreamReader(response.Content);var raw=await reader.ReadToEndAsync();using var json=JsonDocument.Parse(raw);
        Assert.AreEqual(id,json.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(name,json.RootElement.GetProperty("Name").GetString());
        var metadata=json.RootElement.GetProperty("MetaData");var etag=response.Headers.ETag;Assert.IsFalse(string.IsNullOrWhiteSpace(etag));
        var evidence=new{readOnly=true,id,name,etag,bodySha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw))),
            nativeDate=metadata.GetProperty("date").GetString(),nativeNumber=metadata.GetProperty("number").GetRawText(),sourceNumber="9007199254740993.125"};
        var path=Path.Combine(work,"cosmos-query-scalar-inspection.json");
        using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(evidence));file.Flush(true);}
        Console.WriteLine(JsonSerializer.Serialize(evidence));
    }
}
