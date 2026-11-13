using System.Net;
using System.Text.Json;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using Source=NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.GoogleCloudPublicMetadataSource;
using NativeObject=Google.Apis.Storage.v1.Data.Object;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GooglePublicMetadataTests
{
    private sealed class Native:StorageClient
    {
        public List<string> Reads=new();
        public string? Missing,Failed,Malformed;
        public int Disposals;
        public override Task<NativeObject> GetObjectAsync(string bucket,string objectName,GetObjectOptions options=null!,CancellationToken token=default)
        {
            token.ThrowIfCancellationRequested();Assert.AreEqual("public-test-bucket",bucket);
            Assert.IsNull(options.UserProject,"No billing project is supplied.");
            Assert.AreEqual(Projection.NoAcl,options.Projection!.Value);
            Assert.AreSame(RetryOptions.Never,options.RetryOptions,"No hidden SDK retry fan-out.");
            Reads.Add(objectName);
            if(objectName==Missing)throw new GoogleApiException("storage","Controlled missing metadata"){HttpStatusCode=HttpStatusCode.NotFound};
            if(objectName==Failed)throw new GoogleApiException("storage","Controlled denied metadata"){HttpStatusCode=HttpStatusCode.Forbidden};
            return Task.FromResult(new NativeObject {Bucket=bucket,Name=objectName==Malformed?"different-object":objectName,
                Size=ulong.MaxValue,Generation=long.MaxValue,Metageneration=1,ETag="native-etag",ContentType="image/tiff",
                UpdatedRaw="2024-01-01T00:00:00.123456789Z",Crc32c="native-crc",Md5Hash="native-md5"});
        }
        public override void Dispose(){Disposals++;}
    }

    [DataTestMethod]
    [DataRow("selection")][DataRow("point")][DataRow("window")][DataRow("missing")]
    [DataRow("failed")][DataRow("empty-window-failure")][DataRow("budget")][DataRow("cancelled")]
    [DataRow("unknown")][DataRow("inactive")][DataRow("malformed")]
    public async Task BoundedMetadataQueriesValidateCompleteSelectionAndNeverDownloadBodies(string scenario)
    {
        var native=new Native();var keys=new[]{"imagery/one.tif","imagery/two.tif","imagery/three.tif"};var owner=Guid.NewGuid();
        using var source=new Source(native,"public-test-bucket",keys,owner);
        keys[2]="changed-after-construction";
        if(scenario!="inactive")Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Reads.Clear();
        var selected=new[]{keys[0],scenario=="unknown"?"outside-catalogue":keys[1]};
        native.Missing=scenario=="missing"?selected[1]:null;
        native.Failed=scenario is "failed" or "empty-window-failure"?selected[1]:null;
        native.Malformed=scenario=="malformed"?selected[1]:null;
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.GoogleCloudOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var pointKey=selected[0];
        query=scenario=="point"?query.Where(row=>CosmicQueryMetadata.Value<string>(row,Source.ObjectKeyMetadata)==pointKey).Result
            :query.Where(row=>selected.Contains(CosmicQueryMetadata.Value<string>(row,Source.ObjectKeyMetadata))).Result;
        if(scenario=="window")query=query.Skip(1).Result.Take(1).Result;
        if(scenario=="empty-window-failure")query=query.Take(0).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.GoogleCloudOASIS,new[]{new CosmicQueryTarget(ProviderType.GoogleCloudOASIS,source.QueryCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);
        using var cancellation=new CancellationTokenSource();if(scenario=="cancelled")cancellation.Cancel();
        var result=await source.ExecuteCosmicQueryAsync(plan.Result,scenario=="budget"?1:8,cancellation.Token);
        if(scenario is "failed" or "empty-window-failure" or "budget" or "cancelled" or "unknown" or "inactive" or "malformed")
        {
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
            Assert.AreEqual(scenario switch {"budget"=>"CQL_QUERY_LIMIT","cancelled"=>"CQL_QUERY_CANCELLED",
                "unknown"=>"CQL_INVALID_SOURCE_KEY","inactive"=>"CQL_PROVIDER_INACTIVE",_=>"CQL_QUERY_FAILED"},result.ErrorCode);
        }
        else
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(scenario is "point" or "window" or "missing"?1:2,result.Result.Count);
            foreach(var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic);Assert.AreEqual(owner,row.CreatedByAvatarId);
                Assert.AreEqual("18446744073709551615",row.MetaData["GCSContentLength"]);
                Assert.AreEqual("9223372036854775807",row.MetaData["GCSGeneration"]);
                Assert.AreEqual("2024-01-01T00:00:00.123456789Z",row.MetaData["GCSUpdated"]);
                StringAssert.Contains(row.Description,"Object body not downloaded");
            }
        }
        Assert.AreEqual(scenario is "cancelled" or "unknown" or "inactive"?0:scenario=="point"?1:2,native.Reads.Count);
        Assert.AreEqual(0,native.Disposals);
    }

    [DataTestMethod]
    [DataRow("bucket")][DataRow("owner")][DataRow("duplicate")][DataRow("too-many")][DataRow("oversize-key")]
    public void InvalidCatalogueRefusesBeforeAnyNativeRequest(string invalid)
    {
        var native=new Native();var keys=invalid switch {"duplicate"=>new[]{"one","one"},"too-many"=>Enumerable.Range(0,9).Select(i=>i.ToString()).ToArray(),
            "oversize-key"=>new[]{new string('x',129)},_=>new[]{"one"}};
        Assert.ThrowsException<ArgumentException>(()=>new Source(native,invalid=="bucket"?"https://unexpected.example":"public-test-bucket",keys,
            invalid=="owner"?Guid.Empty:Guid.NewGuid()));Assert.AreEqual(0,native.Reads.Count);
    }

    [TestMethod]
    public async Task UnsupportedContentQueriesAndDisposedSourcesDoNotReachSdk()
    {
        var native=new Native();var owner=Guid.NewGuid();var source=new Source(native,"public-test-bucket",new[]{"one"},owner);
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.GoogleCloudOASIS).Result).Where(row=>row.Name.Contains("one")).Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query,ProviderType.GoogleCloudOASIS,new[]{new CosmicQueryTarget(ProviderType.GoogleCloudOASIS,source.QueryCapabilities)}).IsError);
        Assert.AreEqual(0,native.Reads.Count);
        source.Dispose();Assert.AreEqual(0,native.Disposals);
        Assert.IsTrue((await source.ActivateQuerySourceAsync(default)).IsError);Assert.AreEqual(0,native.Reads.Count);
    }

    [TestMethod,TestCategory("GcsPublicNative")]
    public async Task AnonymousSdkCqlReturnsThreeRealObjectsAndTwoRecordSubset()
    {
        var path=Environment.GetEnvironmentVariable("OASIS_GCS_PUBLIC_SOURCE")??throw new InvalidOperationException("Explicit owned source config required.");
        using var document=JsonDocument.Parse(await File.ReadAllBytesAsync(path));var config=document.RootElement;
        var keys=config.GetProperty("keys").EnumerateArray().Select(row=>row.GetString()!).ToArray();
        Assert.AreEqual(3,keys.Length);
        using var source=new Source(config.GetProperty("bucket").GetString()!,keys,config.GetProperty("owner").GetGuid());
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var activation=await source.ActivateQuerySourceAsync(deadline.Token);Assert.IsFalse(activation.IsError,activation.Exception?.ToString());
        foreach(var count in new[]{3,2})
        {
            var selected=keys.Take(count).ToArray();
            var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.GoogleCloudOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
                .Where(row=>selected.Contains(CosmicQueryMetadata.Value<string>(row,Source.ObjectKeyMetadata))).Result;
            var plan=CosmicQueryPlanner.Plan(query,ProviderType.GoogleCloudOASIS,new[]{new CosmicQueryTarget(ProviderType.GoogleCloudOASIS,source.QueryCapabilities)});
            Assert.IsFalse(plan.IsError,plan.Message);
            var result=await source.ExecuteCosmicQueryAsync(plan.Result,cancellationToken:deadline.Token);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(count,result.Result.Count);
            CollectionAssert.AreEqual(selected,result.Result.Select(row=>(string)row.MetaData[Source.ObjectKeyMetadata]).ToArray());
            Assert.IsTrue(result.Result.All(row=>row.IsPublic&&ulong.Parse((string)row.MetaData["GCSContentLength"])>0));
        }
    }
}
