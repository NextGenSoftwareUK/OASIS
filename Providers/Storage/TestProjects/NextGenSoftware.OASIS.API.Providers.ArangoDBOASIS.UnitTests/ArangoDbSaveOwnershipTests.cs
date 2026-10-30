using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS.UnitTests;

[TestClass]
public class ArangoDbSaveOwnershipTests
{
    private sealed class Handler:HttpMessageHandler
    {
        public string Mode="valid";public int Writes;public Guid StagedId;
        public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            
            Assert.AreEqual(HttpMethod.Post,request.Method);Writes++;
            using var payload=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            StagedId=Guid.Parse(payload.RootElement.GetProperty("_key").GetString()!);
            Assert.AreEqual(StagedId,payload.RootElement.GetProperty("id").GetGuid());
            Assert.AreEqual(StagedId.ToString(),payload.RootElement.GetProperty("providerUniqueStorageKey").GetProperty("ArangoDBOASIS").GetString());
            Started.TrySetResult();if(Mode=="delayed")await Release.Task;
            if(Mode=="rejected" || Mode=="partial" && Writes==1)return new(HttpStatusCode.Conflict);
            var id=Mode=="wrongId"?Guid.NewGuid().ToString():StagedId.ToString();
            var body=Mode=="malformed"?"!":JsonSerializer.Serialize(new{error=Mode=="falseOk",_key=id,_id="holons/"+id,_rev=Mode=="emptyRev"?"":"1-a"});
            return new(HttpStatusCode.Created){Content=new StringContent(body,Encoding.UTF8,"application/json")};
        }
    }
    private static (ArangoDBOASIS P,Handler H,Holon Row) Setup()
    {
        var h=new Handler();var p=new ArangoDBOASIS("http://127.0.0.1:1");
        var f=typeof(ArangoDBOASIS).GetField("_http",BindingFlags.NonPublic|BindingFlags.Instance)!;
        ((HttpClient)f.GetValue(p)!).Dispose();f.SetValue(p,new HttpClient(h));
        var row=new Holon{Id=Guid.Empty,IsNewHolon=true};row.ProviderUniqueStorageKey[ProviderType.ArangoDBOASIS]="original";
        return(p,h,row);
    }
    [TestMethod]
    public async Task CallerIdentityAndReceiptPublishOnlyAfterAck()
    {
        var(p,h,row)=Setup();h.Mode="delayed";var pending=p.SaveHolonAsync(row,saveChildren:false);
        await h.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));var before=row.Id;var oldKey=row.ProviderUniqueStorageKey[ProviderType.ArangoDBOASIS];h.Release.SetResult();
        var saved=await pending;Assert.AreEqual(Guid.Empty,before);Assert.AreEqual("original",oldKey);
        Assert.IsFalse(saved.IsError,saved.Message);Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);Assert.AreEqual("1-a",saved.MetaData["ArangoDB.Revision"]);
        Assert.AreEqual(h.StagedId,row.Id);Assert.AreNotEqual(Guid.Empty,row.Id);
        Assert.AreEqual(row.Id.ToString(),row.ProviderUniqueStorageKey[ProviderType.ArangoDBOASIS]);
        Assert.IsTrue(row.IsNewHolon,"Manager owns clean-state publication");
    }
    [DataTestMethod][DataRow("rejected")][DataRow("wrongId")][DataRow("emptyRev")][DataRow("falseOk")][DataRow("malformed")]
    public async Task FailedOrInvalidAckPreservesCaller(string mode)
    {
        var(p,h,row)=Setup();h.Mode=mode;var result=await p.SaveHolonAsync(row,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);
        Assert.AreEqual(Guid.Empty,row.Id);Assert.AreEqual("original",row.ProviderUniqueStorageKey[ProviderType.ArangoDBOASIS]);
    }
    [DataTestMethod][DataRow(false,1,0)][DataRow(true,2,1)]
    public async Task BatchFailureAndContinuationAreExplicit(bool keepGoing,int puts,int saved)
    {
        var(p,h,row)=Setup();h.Mode="partial";
        var result=await p.SaveHolonsAsync(new IHolon[]{row,new Holon()},saveChildren:false,continueOnError:keepGoing);
        Assert.IsTrue(result.IsError);Assert.AreEqual(puts,h.Writes);Assert.AreEqual(saved,result.SavedCount);Assert.AreEqual(saved>0,result.IsSaved);
        Assert.AreEqual(Guid.Empty,row.Id);
    }
}
