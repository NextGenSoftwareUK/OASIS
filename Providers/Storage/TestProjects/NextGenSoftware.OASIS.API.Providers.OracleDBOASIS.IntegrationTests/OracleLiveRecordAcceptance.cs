using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

/// <summary>Bounded public-provider acceptance, not the DDL/trigger-bearing local fault suite.</summary>
public static class OracleLiveRecordAcceptance
{
    internal sealed record Row(string Table,Guid Id);
    internal static string IntentFingerprint(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));

    public static Task RunAsync(string connectionVariable,string database,string schema,string service,string work,bool approveCloudWrites)
    {
        if(!approveCloudWrites)throw new InvalidOperationException("Explicit approval for potentially billable Oracle cloud writes/reads is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionVariable);
        var connection=Environment.GetEnvironmentVariable(connectionVariable);
        if(string.IsNullOrWhiteSpace(connection))throw new InvalidOperationException("Configure the protected Oracle test connection outside chat/command-line values.");
        return RunAsync(connection,database,schema,service,work,true,null);
    }

    internal static Task RunLocalControlAsync(string connection,string database,string schema,string service,string work,
        Func<string,OracleLiveSchemaPreflight.ExistingSchemaOwner>? owner=null)
        =>RunAsync(connection,database,schema,service,work,false,owner);

    private static async Task RunAsync(string connection,string database,string schema,string service,string work,bool requireTcps,
        Func<string,OracleLiveSchemaPreflight.ExistingSchemaOwner>? owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);ArgumentException.ThrowIfNullOrWhiteSpace(schema);ArgumentException.ThrowIfNullOrWhiteSpace(service);
        if(!Directory.Exists(work))throw new DirectoryNotFoundException("Reuse the existing Oracle evidence directory.");
        var pending=Path.Combine(work,"oracle-live-records.pending.json");var report=Path.Combine(work,"oracle-live-records.current.json");
        void RefuseExisting(){if(new[]{pending,report,pending+".next",report+".next"}.Any(File.Exists))throw new InvalidOperationException("Preserve original/interrupted Oracle record acceptance evidence before another run; no replay.");}
        RefuseExisting();
        await using var lease=new FileStream(Path.Combine(work,"oracle-live-records.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        RefuseExisting();
        await using var native=new OracleConnection(connection);await native.OpenAsync();
        var target=await OracleLiveSchemaPreflight.ReadTargetAsync(native);
        OracleLiveSchemaPreflight.ValidateTarget(target,database,schema,service,requireTcps);
        var objects=await OracleLiveSchemaPreflight.ReadCanonicalObjectsAsync(native);
        var operation=Guid.NewGuid();var actor=Guid.NewGuid();var rootId=Guid.NewGuid();var childId=Guid.NewGuid();
        var name="owned-live-"+operation.ToString("N");
        var avatar=new Avatar{Id=actor,Username=name,Email=name+"@example.invalid",CreatedByAvatarId=actor,IsActive=true};
        var detail=new AvatarDetail{Id=actor,Username=name,Email=avatar.Email,CreatedByAvatarId=actor,Karma=11,XP=22,IsActive=true};
        var root=new Holon{Id=rootId,CreatedByAvatarId=actor,Name=name+"-root",MetaData=new(){{"owned.live",operation.ToString("D")}}};
        var child=new Holon{Id=childId,CreatedByAvatarId=actor,Name=name+"-child"};root.Children.Add(child);
        var records=new Dictionary<Row,IHolonBase>{{new("OASIS_AVATARS",actor),avatar},{new("OASIS_AVATAR_DETAILS",actor),detail},{new("OASIS_HOLONS",rootId),root},{new("OASIS_HOLONS",childId),child}};
        var retired=new HashSet<Row>();var originals=new Dictionary<Row,string?>();var mutations=0;var verifierReads=0;
        var deadline=DateTime.UtcNow.AddMinutes(5);
        static void Require(bool condition,string message,Exception? cause=null){if(!condition)throw new InvalidDataException(message,cause);}
        static string? Hash(string? value)=>value==null?null:Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task<string?> Read(Row row){
            await using var command=new OracleCommand($"SELECT DATA_JSON FROM {row.Table} WHERE ID=:Id",native){BindByName=true,CommandTimeout=10};
            command.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();verifierReads++;
            await using var reader=await command.ExecuteReaderAsync();if(!await reader.ReadAsync())return null;
            var json=reader.GetString(0);Require(!await reader.ReadAsync(),"Native acceptance identity is ambiguous.");return json;
        }
        async Task Observe(){
            foreach(var pair in records){
                var json=await Read(pair.Key);
                if(retired.Contains(pair.Key)||pair.Value.Version==0)Require(json==null,"Owned acceptance identity was unexpectedly occupied or retained.");
                else{
                    Require(json!=null,"Acknowledged acceptance record is absent.");
                    var decoded=pair.Value switch{Avatar _=>(IHolonBase)StoredRecordJson.DeserializeStored<Avatar>(json!),AvatarDetail _=>StoredRecordJson.DeserializeStored<AvatarDetail>(json!),_=>StoredRecordJson.DeserializeStored<Holon>(json!)};
                    Require(decoded!=null&&decoded.Id==pair.Key.Id&&decoded.Version==pair.Value.Version&&!decoded.IsDeleted,"Native acceptance identity/version/lifecycle differs from its acknowledgement.");
                    if(decoded is IAvatar found)Require(found.Username==avatar.Username&&found.Email==avatar.Email&&found.Description==avatar.Description,"Avatar native content differs.");
                    else if(decoded is IAvatarDetail foundDetail)Require(foundDetail.Username==detail.Username&&foundDetail.Email==detail.Email&&foundDetail.Karma==detail.Karma&&foundDetail.XP==detail.XP,"Detail native content differs.");
                    else Require(decoded.Name==pair.Value.Name&&decoded.CreatedByAvatarId==actor&&decoded.Description==pair.Value.Description,"Holon native content/owner differs.");
                }
                originals[pair.Key]=json;
            }
        }
        async Task Unchanged(){foreach(var row in records.Keys)Require(await Read(row)==originals[row],"Original acceptance content changed; preserve evidence and do not mutate/delete it.");}
        object[] Evidence()=>records.Select(pair=>(object)new{pair.Key.Table,pair.Key.Id,Version=pair.Value.Version,SHA256=Hash(originals[pair.Key])}).ToArray();
        async Task<string> Durable(string path,object value,bool replace){
            var bytes=JsonSerializer.SerializeToUtf8Bytes(value);
            await using(var file=new FileStream(path+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(bytes);await file.FlushAsync();file.Flush(true);
            }
            File.Move(path+".next",path,replace);
            return IntentFingerprint(bytes);
        }
        string? ownPendingHash=null;
        async Task Journal(string state){
            if(ownPendingHash!=null)Require(IntentFingerprint(await File.ReadAllBytesAsync(pending))==ownPendingHash,"Original acceptance intent changed before checkpoint; preserve it.");
            ownPendingHash=await Durable(pending,new{state,operation,actor,rootId,childId,name,target,admittedPublicMutationCalls=mutations,rows=Evidence()},ownPendingHash!=null);
        }
        async Task Before(string state){Require(DateTime.UtcNow<deadline,"Acceptance mutation admission deadline elapsed; preserve originals.");await Unchanged();mutations++;await Journal("before-"+state);}
        async Task Saved<T>(OASISResult<T> response,string state,int count=1){
            Require(response!=null&&!response.IsError&&response.IsSaved&&response.SavedCount==count,"Public acceptance mutation did not acknowledge the expected result; do not replay or clean up.",response?.Exception);
            await Observe();await Journal("acknowledged-"+state);
        }
        var provider=owner?.Invoke(connection)??new OracleLiveSchemaPreflight.ExistingSchemaOwner(connection);provider.IsVersionControlEnabled=true;
        try{
            var active=await provider.ActivateProviderAsync();Require(!active.IsError&&active.Result&&provider.ProvisionCalls==0,"Existing-schema validation failed; provisioning/repair is forbidden.",active.Exception);
            await Observe();Require(originals.Values.All(value=>value==null),"Acceptance identities already exist.");
            await Before("create-avatar");await Saved(await provider.SaveAvatarAsync(avatar),"create-avatar");
            avatar.Description="owned-live-updated";await Before("update-avatar");await Saved(await provider.SaveAvatarAsync(avatar),"update-avatar");
            await Before("create-detail");await Saved(await provider.SaveAvatarDetailAsync(detail),"create-detail");
            detail.Karma=33;detail.XP=44;await Before("update-detail");await Saved(await provider.SaveAvatarDetailAsync(detail),"update-detail");
            await Before("create-graph");await Saved(await provider.SaveHolonAsync(root),"create-graph",2);
            root.Description="owned-root-only-update";await Before("update-root");await Saved(await provider.SaveHolonAsync(root,saveChildren:false),"update-root");
            Require(avatar.Version==2&&detail.Version==2&&root.Version==2&&child.Version==1,"Independent versions did not advance as expected.");
            foreach(var selected in new[]{await provider.LoadAvatarAsync(actor),await provider.LoadAvatarByUsernameAsync(name),await provider.LoadAvatarByEmailAsync(avatar.Email)})Require(!selected.IsError&&selected.IsLoaded&&selected.LoadedCount==1&&selected.Result?.Id==actor&&selected.Result.Version==2,"Public avatar selector failed.",selected.Exception);
            var oldAvatar=await provider.LoadAvatarAsync(actor,1);Require(!oldAvatar.IsError&&oldAvatar.Result?.Version==1&&oldAvatar.Result.Description!=avatar.Description,"Original avatar history differs.",oldAvatar.Exception);
            var oldDetail=await provider.LoadAvatarDetailAsync(actor,1);Require(!oldDetail.IsError&&oldDetail.Result?.Karma==11&&oldDetail.Result.XP==22,"Original detail history differs.",oldDetail.Exception);
            foreach(var selected in new[]{await provider.LoadAvatarDetailAsync(actor),await provider.LoadAvatarDetailByUsernameAsync(name),await provider.LoadAvatarDetailByEmailAsync(avatar.Email)})Require(!selected.IsError&&selected.Result?.Version==2&&selected.Result.Karma==33&&selected.Result.XP==44,"Public detail selector failed.",selected.Exception);
            foreach(var version in new[]{0,1}){
                var selected=await provider.LoadHolonAsync(rootId,loadChildrenFromProvider:true,version:version);
                Require(!selected.IsError&&selected.Result?.Version==(version==0?2:1)&&selected.Result.Children.Count==1&&selected.Result.Children.Single().Id==childId&&selected.Result.Children.Single().Version==1,"Public independent root/child history failed.",selected.Exception);
            }
            var metadata=await provider.LoadHolonsByMetaDataAsync("owned.live",operation.ToString("D"),loadChildren:false);
            Require(!metadata.IsError&&metadata.Result?.Single().Id==rootId,"Owned metadata selector failed.",metadata.Exception);
            await Before("delete-child");var deletedChild=await provider.DeleteHolonAsync(childId);Require(!deletedChild.IsError&&deletedChild.IsDeleted&&deletedChild.DeletedCount==1,"Child deletion was not acknowledged.",deletedChild.Exception);retired.Add(new("OASIS_HOLONS",childId));await Observe();await Journal("acknowledged-delete-child");
            await Before("delete-root");var deletedRoot=await provider.DeleteHolonAsync(rootId);Require(!deletedRoot.IsError&&deletedRoot.IsDeleted&&deletedRoot.DeletedCount==1,"Root deletion was not acknowledged.",deletedRoot.Exception);retired.Add(new("OASIS_HOLONS",rootId));await Observe();await Journal("acknowledged-delete-root");
            await Before("delete-identity");var deletedIdentity=await provider.DeleteAvatarAsync(actor,false);Require(!deletedIdentity.IsError&&deletedIdentity.Result&&deletedIdentity.IsDeleted&&deletedIdentity.DeletedCount==1,"Identity deletion was not acknowledged.",deletedIdentity.Exception);retired.Add(new("OASIS_AVATARS",actor));retired.Add(new("OASIS_AVATAR_DETAILS",actor));await Observe();await Journal("acknowledged-delete-identity");
            Require(originals.Values.All(value=>value==null)&&mutations==9,"Original acceptance rows remain or the bounded operation count differs.");
            var afterObjects=await OracleLiveSchemaPreflight.ReadCanonicalObjectsAsync(native);
            Require(objects.SequenceEqual(afterObjects)&&provider.ProvisionCalls==0,"Canonical schema objects changed.");
            await Durable(report,new{state="verified-record-lifecycle",operation,actor,rootId,childId,target,canonicalObjects=objects,publicMutationCalls=mutations,verifierRecordReads=verifierReads,provisioningCalls=provider.ProvisionCalls,
                scope=requireTcps?"hosted bounded public-provider CRUD":"LOCAL bounded public-provider CRUD; NOT hosted/TLS proof",tcpsRequired=requireTcps,crudVerified=true,currentNativeRecordsRemaining=0,
                protocolObservedOnInspectionSessionOnly=true,providerSessionBindingVerified=false,walletPolicyVerified=false,certificatePolicyVerified=false,durabilityVerified=false,physicalErasureVerified=false,
                sdkRequestsMeasured=false,cloudCostMeasured=false,retainedLogsBackupsMayIncurCharges=true,fullProviderVerified=false,hyperDriveVerified=false,deadlineIsNextMutationAdmissionOnly=true,completedUtc=DateTime.UtcNow},false);
            Require(IntentFingerprint(await File.ReadAllBytesAsync(pending))==ownPendingHash,"Original acceptance intent changed; do not retire it.");File.Delete(pending);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
