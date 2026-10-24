using System.Reflection;
using Google.Cloud.BigQuery.V2;
using Google.Apis.Bigquery.v2.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQuerySaveAcknowledgementTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ImportSuccessRetainsActualAcknowledgedCount(bool sync,bool empty)
    {
        var client=new Sequence(int.MaxValue);var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        var roots=empty?Array.Empty<Holon>():new[]{new Holon(),new Holon()};var result=sync?provider.Import(roots):await provider.ImportAsync(roots);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.Result);Assert.IsTrue(result.IsSaved);Assert.AreEqual(roots.Length,result.SavedCount);Assert.AreEqual(roots.Length,client.Calls);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ImportCannotLoseAcknowledgedPartialMutationFence(bool sync)
    {
        var client=new Sequence(2);var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        var roots=new[]{new Holon(),new Holon(),new Holon()};var result=sync?provider.Import(roots):await provider.ImportAsync(roots);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);Assert.AreEqual(1,result.SavedCount);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreEqual(2,client.Calls);
    }
    [DataTestMethod][DataRow(false,false,0)][DataRow(false,true,0)][DataRow(true,false,0)][DataRow(true,true,0)]
    [DataRow(false,false,5)][DataRow(false,true,5)][DataRow(true,false,5)][DataRow(true,true,5)]
    public async Task PublicHolonDeletePreservesMutationOutcome(bool key,bool sync,int outcome)
    {
        var id=Guid.NewGuid();var client=new Client(outcome,()=>{}){ReadRecord=Core.Serialization.StoredRecordJson.Serialize(new Holon{Id=id})};
        var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        var result=key?(sync?provider.DeleteHolon(id.ToString()):await provider.DeleteHolonAsync(id.ToString())):(sync?provider.DeleteHolon(id):await provider.DeleteHolonAsync(id));
        Assert.AreEqual(outcome!=5,result.IsError);Assert.AreEqual(outcome==5,result.IsDeleted);Assert.AreEqual(outcome==5?1:0,result.DeletedCount);
        if(outcome==0){Assert.IsNull(result.Result);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));}
        else Assert.AreEqual(id,result.Result.Id);
    }

    public static IEnumerable<object[]> DeleteCases(){foreach(var holon in new[]{false,true})foreach(var sync in new[]{false,true})foreach(var outcome in new[]{0,1,2,3,4,5,7,8,9})yield return new object[]{holon,sync,outcome};}
    [DataTestMethod][DynamicData(nameof(DeleteCases),DynamicDataSourceType.Method)]
    public async Task HardDeleteRequiresBoundOneRowNativeAcknowledgement(bool holon,bool sync,int outcome)
    {
        var id=Guid.NewGuid();var client=new Client(outcome,()=>{});var provider=new BigQueryOASIS("my-project");
        typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        var result=holon?(sync?provider.DeleteHolonSoft(id,false):await provider.DeleteHolonSoftAsync(id,false)):(sync?provider.DeleteAvatar(id,false):await provider.DeleteAvatarAsync(id,false));
        Assert.AreEqual(1,client.Calls);Assert.AreEqual(outcome!=5,result.IsError);Assert.AreEqual(outcome==5,result.Result);
        Assert.AreEqual(outcome==5,result.IsDeleted);Assert.AreEqual(outcome==5?1:0,result.DeletedCount);
        if(outcome!=5){
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));var cause=result.Exception as BigQueryRecordReconciliationException;Assert.IsNotNull(cause);
            Assert.AreEqual(id,cause.RecordId);Assert.AreEqual("my-project",cause.ProjectId);Assert.AreEqual(client.ProposedJobId,cause.OriginalTransactionId);
            StringAssert.StartsWith(cause.OriginalTransactionId,"oasis-record-");Assert.IsFalse(cause.WriteAcknowledged);
            if(outcome==0)Assert.AreSame(client.Failure,cause.InnerException);
        }
    }

    private sealed class Sequence(int failAt):BigQueryClient
    {
        public int Calls;
        public override Task<BigQueryResults> ExecuteQueryAsync(string sql,IEnumerable<BigQueryParameter> parameters,QueryOptions? queryOptions=null,GetQueryResultsOptions? resultsOptions=null,CancellationToken token=default)
        {
            if(++Calls==failAt)throw new IOException("Controlled uncertain batch member.");
            return Task.FromResult(new BigQueryResults(this,new GetQueryResultsResponse{JobComplete=true,TotalRows=0,NumDmlAffectedRows=1,JobReference=new(){ProjectId="my-project",JobId=queryOptions!.JobId}},null,new GetQueryResultsOptions()));
        }
    }
    [DataTestMethod][DataRow(false,false,1)][DataRow(false,true,1)][DataRow(true,false,1)][DataRow(true,true,1)]
    [DataRow(false,false,2)][DataRow(false,true,2)][DataRow(true,false,2)][DataRow(true,true,2)]
    public async Task BulkFailureCannotDiscardUncertaintyOrPublishSuccessfulSubset(bool sync,bool keepGoing,int failAt)
    {
        var client=new Sequence(failAt);var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        var rows=new[]{new Holon(),new Holon(),new Holon()};
        var result=sync?provider.SaveHolons(rows,saveChildren:false,continueOnError:keepGoing):await provider.SaveHolonsAsync(rows,saveChildren:false,continueOnError:keepGoing);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(failAt,client.Calls);Assert.AreEqual(failAt-1,result.SavedCount);
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreEqual(failAt-1,result.Result.Count());
        Assert.IsTrue(rows.Skip(failAt-1).All(row=>row.Id==Guid.Empty));
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task CompleteBulkCountsOnlyAcknowledgedRoots(bool sync,bool empty)
    {
        var client=new Sequence(int.MaxValue);var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        var rows=empty?Array.Empty<Holon>():new[]{new Holon(),new Holon(),new Holon()};
        var result=sync?provider.SaveHolons(rows,saveChildren:false):await provider.SaveHolonsAsync(rows,saveChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);Assert.AreEqual(rows.Length,result.SavedCount);Assert.AreEqual(rows.Length,result.Result.Count());Assert.AreEqual(rows.Length,client.Calls);
        Assert.IsTrue(rows.All(row=>row.Id!=Guid.Empty&&row.ProviderUniqueStorageKey[ProviderType.BigQueryOASIS]==row.Id.ToString()));
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task InvalidLaterRootCannotFollowAnEarlierWrite(bool sync)
    {
        var client=new Sequence(int.MaxValue);var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        var first=new Holon();var rows=new IHolon[]{first,null!};
        var result=sync?provider.SaveHolons(rows,saveChildren:false):await provider.SaveHolonsAsync(rows,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsInstanceOfType<ArgumentException>(result.Exception);Assert.AreEqual(0,result.SavedCount);Assert.AreEqual(0,client.Calls);Assert.AreEqual(Guid.Empty,first.Id);
    }
    private sealed class Client(int outcome,Action edit):BigQueryClient
    {
        public int Calls;public string? ProposedId,ProposedJobId,ReadRecord;
        public readonly IOException Failure=new("Controlled native result loss.");
        public override Task<BigQueryResults> ExecuteQueryAsync(string sql,IEnumerable<BigQueryParameter> parameters,QueryOptions? queryOptions=null,GetQueryResultsOptions? resultsOptions=null,CancellationToken token=default)
        {
            if(sql.TrimStart().StartsWith("SELECT",StringComparison.Ordinal)&&ReadRecord!=null)
                return Task.FromResult(new BigQueryResults(this,new GetQueryResultsResponse{JobComplete=true,JobReference=new(){ProjectId="my-project",JobId="owned-read"},TotalRows=1,Schema=new(){Fields=new List<TableFieldSchema>{new(){Name="doc",Type="STRING"}}},Rows=new List<TableRow>{new(){F=new List<TableCell>{new(){V=ReadRecord}}}}},null,new GetQueryResultsOptions()));
            Calls++;ProposedId=parameters?.SingleOrDefault(parameter=>parameter.Name=="id")?.Value?.ToString();ProposedJobId=queryOptions?.JobId;
            if(outcome==0)throw Failure;
            if(outcome==1)return Task.FromResult<BigQueryResults>(null!);
            if(outcome==6)edit();
            return Task.FromResult(new BigQueryResults(this,new GetQueryResultsResponse{JobComplete=true,TotalRows=0,
                JobReference=new(){ProjectId=outcome==8?"other-project":"my-project",JobId=outcome==7?"":outcome==9?"unrelated-job":ProposedJobId,Location="us-central1"},NumDmlAffectedRows=outcome==2?null:outcome==3?0:outcome==4?2:1},null,new GetQueryResultsOptions()));
        }
    }
    public static IEnumerable<object[]> Cases(){foreach(var kind in Enumerable.Range(0,3))foreach(var sync in new[]{false,true})foreach(var outcome in Enumerable.Range(0,10))yield return new object[]{kind,sync,outcome};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task CallerIdentityAndSaveFlagsRequireNativeAcknowledgement(int kind,bool sync,int outcome)
    {
        IHolonBase source=kind==0?new Avatar():kind==1?new AvatarDetail():new Holon();source.Description="original";source.Version=7;
        source.ProviderUniqueStorageKey=new(){{ProviderType.SQLLiteDBOASIS,"keep-other-provider"}};
        var keys=source.ProviderUniqueStorageKey;var client=new Client(outcome,()=>source.Description="newer caller edit");var provider=new BigQueryOASIS("my-project");
        typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        bool error,saved;int count;Exception? cause;
        if(kind==0){var result=sync?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);error=result.IsError;saved=result.IsSaved;count=result.SavedCount;cause=result.Exception;}
        else if(kind==1){var result=sync?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);error=result.IsError;saved=result.IsSaved;count=result.SavedCount;cause=result.Exception;}
        else{var result=sync?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false);error=result.IsError;saved=result.IsSaved;count=result.SavedCount;cause=result.Exception;}
        Assert.AreEqual(1,client.Calls);Assert.AreEqual(outcome!=5,error);Assert.AreEqual(outcome==5,saved);Assert.AreEqual(outcome is 5 or 6?1:0,count);
        Assert.AreSame(keys,source.ProviderUniqueStorageKey);Assert.AreEqual("keep-other-provider",keys[ProviderType.SQLLiteDBOASIS]);Assert.AreEqual(7,source.Version);
        if(outcome==5){Assert.AreNotEqual(Guid.Empty,source.Id);Assert.AreEqual(source.Id.ToString(),keys[ProviderType.BigQueryOASIS]);Assert.IsNull(cause);}
        else{
            Assert.AreEqual(Guid.Empty,source.Id);Assert.AreEqual(1,keys.Count);Assert.IsNotNull(cause);Assert.IsTrue(NativeMutationReconciliation.IsRequired(cause));
            var native=cause as BigQueryRecordReconciliationException;Assert.IsNotNull(native);Assert.AreEqual(Guid.Parse(client.ProposedId!),native.RecordId);
            Assert.AreEqual(client.ProposedJobId,native.OriginalTransactionId);StringAssert.StartsWith(native.OriginalTransactionId,"oasis-record-");Assert.AreEqual(outcome==6,native.WriteAcknowledged);
            if(outcome==6)Assert.AreEqual("us-central1",native.JobLocation);if(outcome==0)Assert.AreSame(client.Failure,native.InnerException);
        }
        Assert.AreEqual(outcome==6?"newer caller edit":"original",source.Description);
    }
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task NullSourceFailsBeforeSdkDispatch(int kind,bool sync)
    {
        var provider=new BigQueryOASIS("my-project");var client=new Client(5,()=>{});typeof(BigQueryOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,client);
        Exception? cause;
        if(kind==0){var result=sync?provider.SaveAvatar(null!):await provider.SaveAvatarAsync(null!);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);cause=result.Exception;}
        else if(kind==1){var result=sync?provider.SaveAvatarDetail(null!):await provider.SaveAvatarDetailAsync(null!);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);cause=result.Exception;}
        else{var result=sync?provider.SaveHolon(null!):await provider.SaveHolonAsync(null!);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);cause=result.Exception;}
        Assert.IsInstanceOfType<ArgumentNullException>(cause);Assert.AreEqual(0,client.Calls);Assert.IsFalse(NativeMutationReconciliation.IsRequired(cause));
    }
}
