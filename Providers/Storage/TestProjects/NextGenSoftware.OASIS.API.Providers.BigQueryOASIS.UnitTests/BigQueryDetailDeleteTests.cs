using Google.Cloud.BigQuery.V2;
using Google.Apis.Bigquery.v2.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQueryDetailDeleteTests
{
    [TestMethod]
    public void AllSixDetailDeleteEntriesResolveToProviderOwner()
    {
        var expected=new HashSet<string>{"DeleteAvatarDetail","DeleteAvatarDetailAsync","DeleteAvatarDetailByUsername","DeleteAvatarDetailByUsernameAsync","DeleteAvatarDetailByEmail","DeleteAvatarDetailByEmailAsync"};var map=typeof(BigQueryOASIS).GetInterfaceMap(typeof(IOASISStorageProvider));var checkedCount=0;
        for(var index=0;index<map.InterfaceMethods.Length;index++)if(expected.Contains(map.InterfaceMethods[index].Name)){Assert.AreEqual(typeof(BigQueryOASIS),map.TargetMethods[index].DeclaringType);checkedCount++;}
        Assert.AreEqual(6,checkedCount);
    }
    private sealed class Client(AvatarDetail source):BigQueryClient
    {
        public readonly Dictionary<Guid,string> Details=new(){{source.Id,StoredRecordJson.Serialize(source)}};
        public readonly string AvatarShadow=StoredRecordJson.Serialize(new Avatar{Id=source.Id,Username="protected-avatar",Email="protected@example.invalid"});
        public int Reads,Writes;public Action? AfterRead;public Exception? LostAcknowledgement;public string? RequestedJob;
        public override Task<BigQueryResults> ExecuteQueryAsync(string sql,IEnumerable<BigQueryParameter> parameters,QueryOptions? queryOptions=null,GetQueryResultsOptions? resultsOptions=null,CancellationToken token=default)
        {
            StringAssert.Contains(sql,".oasis_avatar_details`");var args=parameters.ToDictionary(row=>row.Name,row=>row.Value);var job=new JobReference{ProjectId="my-project",JobId=queryOptions?.JobId??"owned-detail-read"};
            if(sql.TrimStart().StartsWith("SELECT",StringComparison.Ordinal)){
                Reads++;var rows=Details.Where(row=>args.TryGetValue("id",out var id)?row.Key.ToString()==id.ToString():OasisJson.ParseStoredObject(row.Value)[sql.Contains("WHERE username =",StringComparison.Ordinal)?"Username":"Email"]?.ToString()==args["value"].ToString()).Select(row=>row.Value).Take(2).ToArray();AfterRead?.Invoke();
                return Task.FromResult(new BigQueryResults(this,new GetQueryResultsResponse{JobComplete=true,JobReference=job,TotalRows=(ulong)rows.Length,Schema=new(){Fields=new List<TableFieldSchema>{new(){Name="doc",Type="STRING"}}},Rows=rows.Select(value=>new TableRow{F=new List<TableCell>{new(){V=value}}}).ToList()},null,new GetQueryResultsOptions()));
            }
            Writes++;RequestedJob=queryOptions?.JobId;var key=Guid.Parse((string)args["id"]);var exists=Details.TryGetValue(key,out var original);var admitted=exists&&args.TryGetValue("expected_doc",out var expected)&&original==(string?)expected;
            if(sql.TrimStart().StartsWith("DELETE",StringComparison.Ordinal)){StringAssert.Contains(sql,"doc = @expected_doc");if(admitted)Details.Remove(key);}
            else{StringAssert.Contains(sql,"t.doc = @expected_doc");if(admitted)Details[key]=(string)args["doc"];}
            if(LostAcknowledgement!=null)throw LostAcknowledgement;
            return Task.FromResult(new BigQueryResults(this,new GetQueryResultsResponse{JobComplete=true,JobReference=job,NumDmlAffectedRows=admitted?1:0},null,new GetQueryResultsOptions()));
        }
    }
    private static BigQueryOASIS Provider(Client client){var p=new BigQueryOASIS("my-project"){IsVersionControlEnabled=true};typeof(BigQueryOASIS).GetField("_client",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(p,client);return p;}
    private static AvatarDetail Detail()=>new(){Id=Guid.NewGuid(),Username="owned-detail",Email="detail@example.invalid",Version=1,IsActive=true,MetaData=new(){{"large",9007199254740993L}}};
    private static Task<OASISResult<bool>> Delete(IOASISStorageProvider provider,AvatarDetail detail,bool sync,bool soft,int route)
        =>route==0?(sync?Task.FromResult(provider.DeleteAvatarDetail(detail.Id,soft)):provider.DeleteAvatarDetailAsync(detail.Id,soft)):route==1?(sync?Task.FromResult(provider.DeleteAvatarDetailByUsername(detail.Username,soft)):provider.DeleteAvatarDetailByUsernameAsync(detail.Username,soft)):(sync?Task.FromResult(provider.DeleteAvatarDetailByEmail(detail.Email,soft)):provider.DeleteAvatarDetailByEmailAsync(detail.Email,soft));
    public static IEnumerable<object[]> Cases(){foreach(var sync in new[]{false,true})foreach(var soft in new[]{false,true})foreach(var route in new[]{0,1,2})yield return new object[]{sync,soft,route};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task DetailDeleteUsesNativeFamilyAndRetainsHistoryOnSoftDelete(bool sync,bool soft,int route)
    {
        var detail=Detail();var client=new Client(detail);var shadow=client.AvatarShadow;var provider=Provider(client);var result=await Delete(provider,detail,sync,soft,route);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.Result);Assert.IsTrue(result.IsDeleted);Assert.AreEqual(1,result.DeletedCount);Assert.AreEqual(1,client.Reads);Assert.AreEqual(1,client.Writes);Assert.AreEqual(shadow,client.AvatarShadow);
        if(soft){var current=OasisJson.ParseStoredObject(client.Details[detail.Id]);Assert.AreEqual("2",current["Version"]!.ToString());Assert.AreEqual("False",current["IsActive"]!.ToString());var historical=await provider.LoadAvatarDetailAsync(detail.Id,1);Assert.IsFalse(historical.IsError,historical.Exception?.ToString());Assert.IsFalse(historical.Result.IsDeleted);Assert.IsTrue(historical.Result.IsActive);Assert.AreEqual("9007199254740993",historical.Result.MetaData["large"].ToString());}
        else Assert.IsFalse(client.Details.ContainsKey(detail.Id));
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task DetailDeleteRejectsNativeChangeAfterOriginalObservation(bool sync,bool soft)
    {
        var detail=Detail();var client=new Client(detail);string? concurrent=null;client.AfterRead=()=>{detail.Description="concurrent";concurrent=StoredRecordJson.Serialize(detail);client.Details[detail.Id]=concurrent;};var result=await Delete(Provider(client),detail,sync,soft,0);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreEqual(1,client.Reads);Assert.AreEqual(1,client.Writes);Assert.IsNotNull(concurrent);Assert.AreEqual(concurrent,client.Details[detail.Id]);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task DetailDeleteKeepsOriginalJobOnCommittedLostAcknowledgement(bool sync,bool soft)
    {
        var detail=Detail();var lost=new IOException("Controlled detail deletion acknowledgement loss.");var client=new Client(detail){LostAcknowledgement=lost};var result=await Delete(Provider(client),detail,sync,soft,0);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));var native=result.Exception as BigQueryRecordReconciliationException;Assert.IsNotNull(native);Assert.AreEqual(client.RequestedJob,native.OriginalTransactionId);Assert.AreEqual(detail.Id,native.RecordId);Assert.AreSame(lost,native.InnerException);Assert.AreEqual(1,client.Writes);
        if(soft)Assert.AreEqual("2",OasisJson.ParseStoredObject(client.Details[detail.Id])["Version"]!.ToString());else Assert.IsFalse(client.Details.ContainsKey(detail.Id));
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task RepeatedSoftDeleteDoesNotCreateAnotherArchivedVersion(bool sync,int route)
    {
        var detail=Detail();var client=new Client(detail);var provider=Provider(client);var first=await Delete(provider,detail,sync,true,route);Assert.IsFalse(first.IsError,first.Exception?.ToString());var original=client.Details[detail.Id];var second=await Delete(provider,detail,sync,true,route);
        Assert.IsFalse(second.IsError,second.Exception?.ToString());Assert.IsTrue(second.Result);Assert.IsTrue(second.IsDeleted);Assert.AreEqual(1,second.DeletedCount);Assert.AreEqual(0,second.SavedCount);Assert.IsFalse(second.IsSaved);Assert.AreEqual(1,client.Writes);Assert.AreEqual(original,client.Details[detail.Id]);
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task HardDeleteAfterSoftDeleteRemovesWholeDetailArchive(bool sync,int route)
    {
        var detail=Detail();var client=new Client(detail);var provider=Provider(client);var first=await Delete(provider,detail,sync,true,route);Assert.IsFalse(first.IsError,first.Exception?.ToString());var original=client.AvatarShadow;var second=await Delete(provider,detail,sync,false,route);
        Assert.IsFalse(second.IsError,second.Exception?.ToString());Assert.IsTrue(second.Result);Assert.IsTrue(second.IsDeleted);Assert.AreEqual(1,second.DeletedCount);Assert.AreEqual(2,client.Writes);Assert.AreEqual(0,client.Details.Count);Assert.AreEqual(original,client.AvatarShadow);
    }
    [DataTestMethod][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task AmbiguousDetailIdentityCannotDeleteEitherRecord(bool sync,int route)
    {
        var detail=Detail();var other=Detail();var client=new Client(detail);client.Details[other.Id]=StoredRecordJson.Serialize(other);var originals=client.Details.ToArray();var result=await Delete(Provider(client),detail,sync,false,route);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);Assert.AreEqual(0,client.Writes);CollectionAssert.AreEqual(originals,client.Details.ToArray());
    }
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task MissingDetailIsNotReportedAsDeleted(bool sync,bool soft,int route)
    {
        var detail=Detail();var client=new Client(detail);client.Details.Clear();var result=await Delete(Provider(client),detail,sync,soft,route);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);Assert.AreEqual(0,client.Writes);
    }
    public static IEnumerable<object[]> InvalidCases(){foreach(var sync in new[]{false,true})foreach(var soft in new[]{false,true})foreach(var route in new[]{0,1,2})foreach(var blank in route==0?new[]{false}:new[]{false,true})yield return new object[]{sync,soft,route,blank};}
    [DataTestMethod][DynamicData(nameof(InvalidCases),DynamicDataSourceType.Method)]
    public async Task InvalidDetailDeleteSelectorRejectsBeforeNativeIo(bool sync,bool soft,int route,bool blank)
    {
        var detail=Detail();var client=new Client(detail);if(route==0)detail.Id=Guid.Empty;else if(route==1)detail.Username=blank?" \t":null!;else detail.Email=blank?" \t":null!;
        var result=await Delete(Provider(client),detail,sync,soft,route);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);Assert.IsInstanceOfType<ArgumentException>(result.Exception);Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Writes);
    }
}
