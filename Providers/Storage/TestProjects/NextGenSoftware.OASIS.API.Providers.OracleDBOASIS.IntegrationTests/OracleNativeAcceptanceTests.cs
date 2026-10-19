using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public sealed class OracleNativeAcceptanceTests
{
    private sealed class CreateRaceProvider(string connection):OracleDBOASIS(connection)
    {
        public Func<Task>? BeforeWrite;public int WriteCalls;
        protected override async Task<int> ExecuteCanonicalWriteAsync(OracleCommand command)
        {
            WriteCalls++;if(BeforeWrite!=null){var callback=BeforeWrite;BeforeWrite=null;await callback();}
            return await base.ExecuteCanonicalWriteAsync(command);
        }
    }
    private sealed class LostCommitProvider(string connection):OracleDBOASIS(connection)
    {
        public bool LoseNextCommit;public int CommitCalls;
        public readonly IOException Lost=new("owned response withheld after actual Oracle SDK commit");
        protected override async Task CommitLifecycleTransactionAsync(OracleTransaction transaction)
        {
            CommitCalls++;await base.CommitLifecycleTransactionAsync(transaction);
            if(LoseNextCommit){LoseNextCommit=false;throw Lost;}
        }
    }
    [TestMethod]
    public async Task RealSdkCanonicalRecordsSelectorsAndMetadataPaths()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")
            ?? throw new InvalidOperationException("Explicit owned test connection required.");
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")
            ?? throw new InvalidOperationException("Use the retained Oracle environment.");
        var journal=Path.Combine(work,"canonical-records.pending.json");
        var resume=Environment.GetEnvironmentVariable("OASIS_ORACLE_RESUME_CANONICAL")=="1";
        Assert.AreEqual(resume,File.Exists(journal),"Resume requires the original journal; a fresh run requires absence.");
        var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true};var id=Guid.NewGuid();var childId=Guid.NewGuid();var name="owned-oracle-"+id.ToString("N");
        if(resume){
            using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.AreEqual("before-native-canonical-records",intent.RootElement.GetProperty("state").GetString());
            id=intent.RootElement.GetProperty("id").GetGuid();name=intent.RootElement.GetProperty("name").GetString()!;
            childId=intent.RootElement.GetProperty("childId").GetGuid();
            Assert.AreEqual("owned-oracle-"+id.ToString("N"),name);
        }
        var avatar=new Avatar{Id=id,Username=name,Email=name+"@example.invalid",VerificationToken=name+"-original-token"};
        var detail=new AvatarDetail{Id=id,Username=name,Email=avatar.Email,Karma=321,XP=456};
        detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name="owned oak seed",Quantity=7,GameSource="Our World"});
        var holon=new Holon{Id=id,Name=name,ParentHolonId=Guid.NewGuid(),CreatedByAvatarId=id,
            MetaData=new(){["owned.fixture"]="literal-dot",["owner's key"]="literal-apostrophe",["quote\"key"]="literal-quote"}};
        holon.Children.Add(new Holon{Id=Guid.NewGuid(),Name="must-not-be-embedded"});
        try{
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Exception?.ToString());Assert.IsTrue(provider.IsProviderActivated);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            if(!resume){
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS"})Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,childId,name,state="before-native-canonical-records"}));file.Flush(true);
            }
            var savedAvatar=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(savedAvatar.IsError,savedAvatar.Exception?.ToString());
            var savedDetail=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(savedDetail.IsError,savedDetail.Exception?.ToString());
            var savedHolon=await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(savedHolon.IsError,savedHolon.Exception?.ToString());
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",childId));
            var childSaved=await provider.SaveHolonAsync(new Holon{Id=childId,ParentHolonId=id,Name="owned independently current child",Version=5},saveChildren:false);Assert.IsFalse(childSaved.IsError,childSaved.Exception?.ToString());
            }else{
                // Reconcile originals; never submit these writes again.
                var originalHolon=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
                Assert.AreEqual(id,originalHolon.Id);Assert.AreEqual(name,originalHolon.Name);
                holon=originalHolon;
            }
            Assert.AreEqual(id,(await provider.LoadAvatarAsync(id)).Result.Id);
            Assert.AreEqual(id,(await provider.LoadAvatarByUsernameAsync(name)).Result.Id);
            var byEmail=await provider.LoadAvatarByEmailAsync(avatar.Email);Assert.IsFalse(byEmail.IsError,byEmail.Exception?.ToString());Assert.AreEqual(id,byEmail.Result.Id);
            foreach(var loaded in new[]{await provider.LoadAvatarDetailAsync(id),await provider.LoadAvatarDetailByUsernameAsync(name),await provider.LoadAvatarDetailByEmailAsync(avatar.Email)}){
                Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(321L,loaded.Result.Karma);Assert.AreEqual(detail.XP,loaded.Result.XP);
                Assert.AreEqual(7,loaded.Result.Inventory.Single().Quantity);
            }
            var readHolon=await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsFalse(readHolon.IsError,readHolon.Exception?.ToString());
            Assert.AreEqual(holon.ParentHolonId,readHolon.Result.ParentHolonId);Assert.AreEqual(0,readHolon.Result.Children.Count);
            foreach(var pair in holon.MetaData){
                var found=await provider.LoadHolonsByMetaDataAsync(pair.Key,(string)pair.Value,holon.HolonType,loadChildren:false);
                Assert.IsFalse(found.IsError,found.Exception?.ToString());Assert.IsTrue(found.Result.Any(row=>row.Id==id));
            }
            var exported=await provider.ExportAllDataForAvatarByIdAsync(holon.CreatedByAvatarId);Assert.IsFalse(exported.IsError,exported.Exception?.ToString());Assert.IsTrue(exported.Result.Any(row=>row.Id==id));
            if(!resume){
                foreach(var synchronous in new[]{false,true}){
                    if(synchronous)provider.IsVersionControlEnabled=false;
                    var oldName=avatar.Username;var oldEmail=avatar.Email;
                    avatar.Username=name+(synchronous?"-sync":"-async");avatar.Email=avatar.Username+"@example.invalid";
                    avatar.VerificationToken=avatar.Username+"-current-token";
                    detail.Username=avatar.Username;detail.Email=avatar.Email;detail.Karma+=11;detail.XP+=17;detail.Inventory[0].Quantity+=2;
                    holon.Name=avatar.Username;holon.ParentHolonId=Guid.NewGuid();holon.CreatedByAvatarId=Guid.NewGuid();holon.MetaData["owned.fixture"]=avatar.Username;
                    var updateAvatar=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);
                    var updateDetail=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
                    var updateHolon=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);
                    Assert.IsFalse(updateAvatar.IsError,updateAvatar.Exception?.ToString());Assert.IsTrue(updateAvatar.IsSaved);Assert.AreSame(avatar,updateAvatar.Result);
                    Assert.IsFalse(updateDetail.IsError,updateDetail.Exception?.ToString());Assert.IsTrue(updateDetail.IsSaved);Assert.AreSame(detail,updateDetail.Result);
                    Assert.IsFalse(updateHolon.IsError,updateHolon.Exception?.ToString());Assert.IsTrue(updateHolon.IsSaved);Assert.AreSame(holon,updateHolon.Result);
                    Assert.AreEqual(synchronous?3:2,avatar.Version);Assert.AreEqual(avatar.Version,detail.Version);Assert.AreEqual(avatar.Version,holon.Version);
                    foreach(var loaded in new[]{await provider.LoadAvatarAsync(id),await provider.LoadAvatarByUsernameAsync(avatar.Username),await provider.LoadAvatarByEmailAsync(avatar.Email)}){
                        Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(avatar.Username,loaded.Result.Username);Assert.AreEqual(avatar.Email,loaded.Result.Email);
                    }
                    Assert.IsNull((await provider.LoadAvatarByUsernameAsync(oldName)).Result);Assert.IsNull((await provider.LoadAvatarByEmailAsync(oldEmail)).Result);
                    foreach(var loaded in new[]{await provider.LoadAvatarDetailAsync(id),await provider.LoadAvatarDetailByUsernameAsync(detail.Username),await provider.LoadAvatarDetailByEmailAsync(detail.Email)}){
                        Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(detail.Karma,loaded.Result.Karma);Assert.AreEqual(detail.XP,loaded.Result.XP);Assert.AreEqual(detail.Inventory[0].Quantity,loaded.Result.Inventory.Single().Quantity);
                    }
                    Assert.IsNull((await provider.LoadAvatarDetailByUsernameAsync(oldName)).Result);Assert.IsNull((await provider.LoadAvatarDetailByEmailAsync(oldEmail)).Result);
                    var loadedHolon=await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsFalse(loadedHolon.IsError,loadedHolon.Exception?.ToString());
                    Assert.AreEqual(holon.Name,loadedHolon.Result.Name);Assert.AreEqual(holon.ParentHolonId,loadedHolon.Result.ParentHolonId);
                    var found=await provider.LoadHolonsByMetaDataAsync("owned.fixture",avatar.Username,holon.HolonType,loadChildren:false);Assert.IsFalse(found.IsError,found.Exception?.ToString());Assert.IsTrue(found.Result.Any(row=>row.Id==id));
                    var raw=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>((await Read(verifier,"OASIS_AVATAR_DETAILS",id))!);
                    Assert.AreEqual(detail.Karma,raw.Karma);Assert.AreEqual(detail.XP,raw.XP);Assert.AreEqual(detail.Inventory[0].Quantity,raw.Inventory.Single().Quantity);
                }
                foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS"}){
                    using var history=JsonDocument.Parse((await Read(verifier,table,id))!);
                    Assert.AreEqual(3,history.RootElement.GetProperty("Version").GetInt32());
                    var archive=history.RootElement.GetProperty("_oasis_record_history").GetProperty("records");
                    Assert.AreEqual(2,archive.GetArrayLength());Assert.AreEqual(1,archive[0].GetProperty("Version").GetInt32());Assert.AreEqual(2,archive[1].GetProperty("Version").GetInt32());
                    if(table=="OASIS_AVATAR_DETAILS"){Assert.AreEqual(456,archive[0].GetProperty("XP").GetInt64());Assert.AreEqual(321,archive[0].GetProperty("Karma").GetInt64());}
                }
            }
            var invalidKeyDelete=await provider.DeleteAvatarAsync(name,false);
            if(!resume){
                foreach(var old in new[]{await provider.LoadAvatarAsync(id,1),await provider.LoadAvatarByProviderKeyAsync(id.ToString(),1),await provider.LoadAvatarByUsernameAsync(name,1),await provider.LoadAvatarByEmailAsync(name+"@example.invalid",1)}){
                    Assert.IsFalse(old.IsError,old.Exception?.ToString());Assert.AreEqual(1,old.Result.Version);Assert.AreEqual(name,old.Result.Username);
                }
                foreach(var old in new[]{await provider.LoadAvatarDetailAsync(id,1),await provider.LoadAvatarDetailByUsernameAsync(name,1),await provider.LoadAvatarDetailByEmailAsync(name+"@example.invalid",1)}){
                    Assert.IsFalse(old.IsError,old.Exception?.ToString());Assert.AreEqual(1,old.Result.Version);Assert.AreEqual(321L,old.Result.Karma);Assert.AreEqual(456L,old.Result.XP);Assert.AreEqual(7,old.Result.Inventory.Single().Quantity);
                }
                var oldHolon=await provider.LoadHolonAsync(id,loadChildren:false,version:1);Assert.IsFalse(oldHolon.IsError,oldHolon.Exception?.ToString());Assert.AreEqual(name,oldHolon.Result.Name);
                foreach(var root in new[]{await provider.LoadHolonAsync(id,loadChildrenFromProvider:true,version:1),await provider.LoadHolonAsync(id,loadChildrenFromProvider:true,maxChildDepth:1,version:1)}){
                    Assert.IsFalse(root.IsError,root.Exception?.ToString());Assert.AreEqual(1,root.Result.Version);Assert.AreEqual(5,root.Result.Children.Single().Version);Assert.AreEqual(childId,root.Result.Children.Single().Id);
                }
                var noChildren=await provider.LoadHolonAsync(id,loadChildren:false,loadChildrenFromProvider:true,version:1);Assert.AreEqual(0,noChildren.Result.Children.Count);
                var historicalParents=await provider.LoadHolonsForParentAsync(oldHolon.Result.ParentHolonId,loadChildrenFromProvider:true,version:1);Assert.IsFalse(historicalParents.IsError,historicalParents.Exception?.ToString());Assert.AreEqual(5,historicalParents.Result.Single(row=>row.Id==id).Children.Single().Version);
                foreach(var currentRoots in new[]{await provider.LoadAllHolonsAsync(loadChildrenFromProvider:true),await provider.LoadHolonsForParentAsync(holon.ParentHolonId,loadChildrenFromProvider:true),await provider.LoadHolonsByMetaDataAsync("owned.fixture",avatar.Username,loadChildrenFromProvider:true),await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>{{"owned.fixture",avatar.Username}},NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,loadChildrenFromProvider:true)}){
                    Assert.IsFalse(currentRoots.IsError,currentRoots.Exception?.ToString());Assert.AreEqual(3,currentRoots.Result.Single(row=>row.Id==id).Version);Assert.AreEqual(5,currentRoots.Result.Single(row=>row.Id==id).Children.Single().Version);
                }
                var oldAvatars=await provider.LoadAllAvatarsAsync(1);Assert.IsFalse(oldAvatars.IsError,oldAvatars.Exception?.ToString());Assert.AreEqual(name,oldAvatars.Result.Single(row=>row.Id==id).Username);
                var oldDetails=await provider.LoadAllAvatarDetailsAsync(1);Assert.IsFalse(oldDetails.IsError,oldDetails.Exception?.ToString());Assert.AreEqual(456L,oldDetails.Result.Single(row=>row.Id==id).XP);
                foreach(var historical in new[]{
                    await provider.LoadAllHolonsAsync(version:1),
                    await provider.LoadHolonsForParentAsync(oldHolon.Result.ParentHolonId,version:1),
                    await provider.LoadHolonsByMetaDataAsync("owned.fixture","literal-dot",version:1),
                    await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>{{"owned.fixture","literal-dot"}},NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,version:1),
                    await provider.ExportAllAsync(1),await provider.ExportAllDataForAvatarByIdAsync(id,1),
                    await provider.ExportAllDataForAvatarByUsernameAsync(name,1),await provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",1)}){
                    Assert.IsFalse(historical.IsError,historical.Exception?.ToString());Assert.AreEqual(1,historical.Result.Single(row=>row.Id==id).Version);
                }
                Assert.IsFalse((await provider.LoadHolonsForParentAsync(oldHolon.Result.ParentHolonId)).Result.Any(row=>row.Id==id));
                Assert.IsFalse((await provider.ExportAllDataForAvatarByIdAsync(id)).Result.Any(row=>row.Id==id));
                Assert.AreEqual(0,(await provider.LoadAllHolonsAsync(version:99)).Result.Count());
                var search=await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){["owned.fixture"]="literal-dot"}},version:1);
                Assert.IsFalse(search.IsError,search.Exception?.ToString());Assert.AreEqual(1,search.Result.SearchResultHolons.Single(row=>row.Id==id).Version);Assert.AreEqual(5,search.Result.SearchResultHolons.Single(row=>row.Id==id).Children.Single().Version);
                foreach(var exportedRoot in new[]{await provider.ExportAllAsync(1),await provider.ExportAllDataForAvatarByIdAsync(id,1),await provider.ExportAllDataForAvatarByUsernameAsync(name,1),await provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",1)}){
                    Assert.IsFalse(exportedRoot.IsError,exportedRoot.Exception?.ToString());Assert.AreEqual(5,exportedRoot.Result.Single(row=>row.Id==id).Children.Single().Version);
                }
                var historicalCredential=await provider.LoadAvatarByVerificationTokenAsync(name+"-original-token",1);Assert.IsTrue(historicalCredential.IsError);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(historicalCredential.Exception);
                Assert.IsNull((await provider.LoadAvatarByVerificationTokenAsync(name+"-original-token")).Result);
                var currentCredential=await provider.LoadAvatarByVerificationTokenAsync(avatar.VerificationToken);Assert.IsFalse(currentCredential.IsError,currentCredential.Exception?.ToString());Assert.AreEqual(id,currentCredential.Result.Id);Assert.AreEqual(3,currentCredential.Result.Version);
                Assert.IsNull((await provider.LoadAvatarAsync(id,99)).Result);Assert.IsNull((await provider.LoadAvatarDetailAsync(id,99)).Result);Assert.IsNull((await provider.LoadHolonAsync(id,version:99)).Result);
            }
            if(!resume){
                var caller=holon;var callerVersion=caller.Version;var callerVersionId=caller.VersionId;var commits=provider.CommitCalls;
                caller.Name+="-lost-save-ack";provider.LoseNextCommit=true;
                var lostSave=await provider.SaveHolonAsync(caller,saveChildren:false);Assert.IsTrue(lostSave.IsError);Assert.IsFalse(lostSave.IsSaved);Assert.IsNull(lostSave.Result);
                Assert.AreEqual(commits+1,provider.CommitCalls);Assert.AreEqual(callerVersion,caller.Version);Assert.AreEqual(callerVersionId,caller.VersionId);
                Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(lostSave.Exception);Assert.AreSame(provider.Lost,lostSave.Exception.InnerException);
                var committed=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
                Assert.AreEqual(callerVersion+1,committed.Version);Assert.AreEqual(caller.Name,committed.Name);Assert.AreEqual(callerVersionId,committed.PreviousVersionId);
                holon=committed; // Original committed outcome reconciled by independent native read; no resubmission.
            }
            Assert.IsTrue(invalidKeyDelete.IsError);Assert.IsInstanceOfType<FormatException>(invalidKeyDelete.Exception);
            Assert.IsNotNull(await Read(verifier,"OASIS_AVATARS",id));
            provider.IsVersionControlEnabled=true;
            if(!resume){
                var beforeAvatar=await Read(verifier,"OASIS_AVATARS",id);var beforeDetail=await Read(verifier,"OASIS_AVATAR_DETAILS",id);
                var trigger="OASIS_TEST_"+id.ToString("N")[..20];
                await using(var create=new OracleCommand($"CREATE TRIGGER {trigger} BEFORE UPDATE ON OASIS_AVATAR_DETAILS FOR EACH ROW BEGIN IF :NEW.ID=HEXTORAW('{Convert.ToHexString(id.ToByteArray())}') THEN RAISE_APPLICATION_ERROR(-20001,'owned lifecycle rollback proof'); END IF; END;",verifier))await create.ExecuteNonQueryAsync();
                var rejected=await provider.DeleteAvatarAsync(id,true);Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.Result);
                Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(rejected.Exception);
                Assert.IsInstanceOfType<OracleException>(rejected.Exception.InnerException);Assert.AreEqual(20001,((OracleException)rejected.Exception.InnerException!).Number);
                Assert.AreEqual(beforeAvatar,await Read(verifier,"OASIS_AVATARS",id),"First row update must roll back when second-row native update fails.");
                Assert.AreEqual(beforeDetail,await Read(verifier,"OASIS_AVATAR_DETAILS",id));
                await using(var drop=new OracleCommand($"DROP TRIGGER {trigger}",verifier))await drop.ExecuteNonQueryAsync();
            }
            if(!resume){
                var commits=provider.CommitCalls;provider.LoseNextCommit=true;
                var lost=await provider.DeleteAvatarAsync(id,true);Assert.IsTrue(lost.IsError);Assert.IsFalse(lost.Result);
                Assert.AreEqual(commits+1,provider.CommitCalls);Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(lost.Exception);Assert.AreSame(provider.Lost,lost.Exception.InnerException);
                Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(lost.Exception));
                var committedAvatar=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Avatar>((await Read(verifier,"OASIS_AVATARS",id))!);
                var committedDetail=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>((await Read(verifier,"OASIS_AVATAR_DETAILS",id))!);
                Assert.IsTrue(committedAvatar.IsDeleted);Assert.IsTrue(committedDetail.IsDeleted);Assert.AreEqual(committedAvatar.DeletedDate,committedDetail.DeletedDate);
                // Original native outcome is now independently reconciled; only then exercise repeat semantics below.
            }
            var soft=await provider.DeleteAvatarAsync(id,true);Assert.IsFalse(soft.IsError,soft.Exception?.ToString());Assert.IsTrue(soft.Result);
            var deletedAvatar=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Avatar>((await Read(verifier,"OASIS_AVATARS",id))!);
            var deletedDetail=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>((await Read(verifier,"OASIS_AVATAR_DETAILS",id))!);
            Assert.IsTrue(deletedAvatar.IsDeleted);Assert.IsFalse(deletedAvatar.IsActive);Assert.IsTrue(deletedDetail.IsDeleted);Assert.IsFalse(deletedDetail.IsActive);
            Assert.AreEqual(deletedAvatar.DeletedDate,deletedDetail.DeletedDate);var deletedAt=deletedAvatar.DeletedDate;
            Assert.AreEqual(avatar.Version+1,deletedAvatar.Version);Assert.AreEqual(detail.Version+1,deletedDetail.Version);
            var retainedDetail=await Read(verifier,"OASIS_AVATAR_DETAILS",id);
            using(var history=JsonDocument.Parse(retainedDetail!))Assert.AreEqual(1,history.RootElement.GetProperty("_oasis_record_history").GetProperty("records")[0].GetProperty("Version").GetInt32());
            Assert.IsNull((await provider.LoadAvatarAsync(id)).Result);Assert.IsNull((await provider.LoadAvatarDetailAsync(id)).Result);
            if(!resume){Assert.AreEqual(1,(await provider.LoadAvatarAsync(id,1)).Result.Version);Assert.AreEqual(1,(await provider.LoadAvatarDetailAsync(id,1)).Result.Version);}
            Assert.IsNull((await provider.LoadAvatarDetailByUsernameAsync(deletedDetail.Username)).Result);Assert.IsNull((await provider.LoadAvatarDetailByEmailAsync(deletedDetail.Email)).Result);
            var details=await provider.LoadAllAvatarDetailsAsync();Assert.IsFalse(details.IsError,details.Exception?.ToString());Assert.IsFalse(details.Result.Any(row=>row.Id==id));
            provider.IsVersionControlEnabled=false;
            var repeated=await provider.DeleteAvatarAsync(id,true);Assert.IsFalse(repeated.IsError,repeated.Exception?.ToString());Assert.AreEqual(retainedDetail,await Read(verifier,"OASIS_AVATAR_DETAILS",id));
            var repeatedDetail=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>((await Read(verifier,"OASIS_AVATAR_DETAILS",id))!);
            Assert.AreEqual(deletedAt,repeatedDetail.DeletedDate);
            var keyDelete=await provider.DeleteAvatarAsync(id.ToString(),false);Assert.IsFalse(keyDelete.IsError,keyDelete.Exception?.ToString());Assert.IsTrue(keyDelete.Result);
            Assert.IsNull(await Read(verifier,"OASIS_AVATARS",id));
            Assert.IsNull(await Read(verifier,"OASIS_AVATAR_DETAILS",id));
            provider.IsVersionControlEnabled=true;
            if(!resume){
                var commits=provider.CommitCalls;provider.LoseNextCommit=true;
                var lost=await provider.DeleteHolonAsync(id);Assert.IsTrue(lost.IsError);Assert.IsNull(lost.Result);Assert.AreEqual(commits+1,provider.CommitCalls);
                Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(lost.Exception);Assert.AreSame(provider.Lost,lost.Exception.InnerException);
                Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(lost.Exception));
                var committed=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
                Assert.IsTrue(committed.IsDeleted);Assert.IsFalse(committed.IsActive);Assert.AreEqual(holon.ParentHolonId,committed.ParentHolonId);
            }
            var holonDelete=await provider.DeleteHolonAsync(id);Assert.IsFalse(holonDelete.IsError,holonDelete.Exception?.ToString());
            Assert.IsTrue(holonDelete.Result.IsDeleted);Assert.IsFalse(holonDelete.Result.IsActive);
            var tombstone=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
            Assert.IsTrue(tombstone.IsDeleted);Assert.IsFalse(tombstone.IsActive);Assert.AreEqual(holonDelete.Result.DeletedDate,tombstone.DeletedDate);
            Assert.AreEqual(holon.ParentHolonId,tombstone.ParentHolonId);Assert.IsNull((await provider.LoadHolonAsync(id)).Result);
            var repeatHolon=provider.DeleteHolonWithSoftDelete(id.ToString(),true);Assert.IsFalse(repeatHolon.IsError,repeatHolon.Exception?.ToString());Assert.IsTrue(repeatHolon.Result);
            var repeatedHolon=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
            Assert.AreEqual(tombstone.DeletedDate,repeatedHolon.DeletedDate);
            var hardHolon=await provider.DeleteHolonWithSoftDeleteAsync(id.ToString(),false);Assert.IsFalse(hardHolon.IsError,hardHolon.Exception?.ToString());Assert.IsTrue(hardHolon.Result);
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            var childDelete=await provider.DeleteHolonWithSoftDeleteAsync(childId,false);Assert.IsFalse(childDelete.IsError,childDelete.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",childId));
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();Assert.IsFalse(provider.IsProviderActivated);}
    }

    [TestMethod]
    public async Task DuplicateIdentityIsRejectedInsteadOfSelectingArbitraryAvatar()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"identity-ambiguity.pending.json");
        var resume=Environment.GetEnvironmentVariable("OASIS_ORACLE_RECONCILE_AMBIGUITY")=="1";
        Assert.AreEqual(resume,File.Exists(journal));
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var name="owned-duplicate-"+ids[0].ToString("N");
        if(resume){
            using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.AreEqual("before-native-duplicate-identities",intent.RootElement.GetProperty("state").GetString());
            ids=intent.RootElement.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray();
            Assert.AreEqual(2,ids.Length);Assert.AreNotEqual(ids[0],ids[1]);name="owned-duplicate-"+ids[0].ToString("N");
            Assert.AreEqual(name,intent.RootElement.GetProperty("name").GetString());
        }
        var email=name+"@example.invalid";var provider=new OracleDBOASIS(connection);
        try{
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Exception?.ToString());
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            if(!resume){
                foreach(var id in ids)foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"})Assert.IsNull(await Read(verifier,table,id));
                using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                    await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,name,state="before-native-duplicate-identities"}));file.Flush(true);
                }
                foreach(var id in ids){
                    var avatar=await provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=email});Assert.IsFalse(avatar.IsError,avatar.Exception?.ToString());
                    var detail=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=email});Assert.IsFalse(detail.IsError,detail.Exception?.ToString());
                }
            }
            foreach(var id in ids){
                var avatar=await provider.LoadAvatarAsync(id);Assert.IsFalse(avatar.IsError,avatar.Exception?.ToString());Assert.AreEqual(name,avatar.Result.Username);Assert.AreEqual(email,avatar.Result.Email);
                var detail=await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(detail.IsError,detail.Exception?.ToString());Assert.AreEqual(name,detail.Result.Username);Assert.AreEqual(email,detail.Result.Email);
            }
            Check(await provider.LoadAvatarByUsernameAsync(name));Check(provider.LoadAvatarByUsername(name));
            Check(await provider.LoadAvatarByEmailAsync(email));Check(provider.LoadAvatarByEmail(email));
            Check(await provider.LoadAvatarDetailByUsernameAsync(name));Check(provider.LoadAvatarDetailByUsername(name));
            Check(await provider.LoadAvatarDetailByEmailAsync(email));Check(provider.LoadAvatarDetailByEmail(email));
            foreach(var id in ids)foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"}){
                var actual=await Read(verifier,table,id);Assert.IsNotNull(actual);
                await using var delete=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                delete.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
                Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,id));
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
        static void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError,"Duplicate identity must not silently pick the first record.");Assert.IsNull(result.Result);
            Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
        }
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task ActualConcurrentCreatorWinsWithoutOverwriteOrCallerPublication(int kind)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"create-race-{kind}.pending.json");Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var name="owned-race-"+id.ToString("N");var table=kind==0?"OASIS_AVATARS":kind==1?"OASIS_AVATAR_DETAILS":"OASIS_HOLONS";
        var provider=new CreateRaceProvider(connection){IsVersionControlEnabled=true};var rival=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase Row(string marker){
            NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row=kind==0?new Avatar{Id=id,Username=name+marker,Email=name+marker+"@example.invalid"}:kind==1?new AvatarDetail{Id=id,Username=name+marker,Email=name+marker+"@example.invalid"}:new Holon{Id=id,Name=name+marker};
            row.MetaData=new(){["owned_race"]=marker};return row;
        }
        var loser=Row("loser");var originalKeys=loser.ProviderUniqueStorageKey;
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.IsFalse((await rival.ActivateProviderAsync()).IsError);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,table,kind,state="before-native-create-race"}));file.Flush(true);
            }
            string? winnerJson=null;
            provider.BeforeWrite=async()=>{
                var winner=Row("winner");
                if(winner is Avatar avatar)Success(await rival.SaveAvatarAsync(avatar));else if(winner is AvatarDetail detail)Success(await rival.SaveAvatarDetailAsync(detail));else Success(await rival.SaveHolonAsync((Holon)winner,saveChildren:false));
                winnerJson=await Read(verifier,table,id);Assert.IsNotNull(winnerJson);
            };
            if(loser is Avatar avatar)Conflict(await provider.SaveAvatarAsync(avatar));else if(loser is AvatarDetail detail)Conflict(await provider.SaveAvatarDetailAsync(detail));else Conflict(await provider.SaveHolonAsync((Holon)loser,saveChildren:false));
            Assert.AreEqual(1,provider.WriteCalls);Assert.AreEqual(0,loser.Version);Assert.AreEqual(Guid.Empty,loser.VersionId);Assert.AreSame(originalKeys,loser.ProviderUniqueStorageKey);Assert.AreEqual(0,originalKeys.Count);
            var actual=await Read(verifier,table,id);Assert.AreEqual(winnerJson,actual);
            using(var document=JsonDocument.Parse(actual!)){Assert.AreEqual(1,document.RootElement.GetProperty("Version").GetInt32());Assert.AreEqual("winner",document.RootElement.GetProperty("MetaData").GetProperty("owned_race").GetString());}
            var first=Row("update-a");var second=Row("update-b");
            async Task Save(OracleDBOASIS writer,NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row){
                if(row is Avatar a)Success(await writer.SaveAvatarAsync(a));else if(row is AvatarDetail d)Success(await writer.SaveAvatarDetailAsync(d));else Success(await writer.SaveHolonAsync((Holon)row,saveChildren:false));
            }
            await Task.WhenAll(Save(provider,first),Save(rival,second));
            CollectionAssert.AreEquivalent(new[]{2,3},new[]{first.Version,second.Version});
            actual=await Read(verifier,table,id);
            using(var document=JsonDocument.Parse(actual!)){
                Assert.AreEqual(3,document.RootElement.GetProperty("Version").GetInt32());
                var history=document.RootElement.GetProperty("_oasis_record_history").GetProperty("records");Assert.AreEqual(2,history.GetArrayLength());
                Assert.AreEqual("winner",history[0].GetProperty("MetaData").GetProperty("owned_race").GetString());
                CollectionAssert.AreEquivalent(new[]{"update-a","update-b"},new[]{history[1].GetProperty("MetaData").GetProperty("owned_race").GetString(),document.RootElement.GetProperty("MetaData").GetProperty("owned_race").GetString()});
            }
            var corrupt=Newtonsoft.Json.Linq.JObject.Parse(actual!);
            if(kind==0)corrupt["_oasis_record_history"]!["records"]![0]!["Version"]=3;
            else if(kind==1)corrupt["_oasis_record_history"]!["schema"]=999;
            else corrupt["id"]=Guid.NewGuid().ToString();
            var corruptJson=corrupt.ToString(Newtonsoft.Json.Formatting.None);
            await using(var inject=new OracleCommand($"UPDATE {table} SET DATA_JSON=:Corrupt WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){
                inject.Parameters.Add("Corrupt",OracleDbType.Clob).Value=corruptJson;inject.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();inject.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
                Assert.AreEqual(1,await inject.ExecuteNonQueryAsync());
            }
            Assert.AreEqual(corruptJson,await Read(verifier,table,id));provider.IsVersionControlEnabled=false;
            var rejected=Row("must-reject-corruption");var writes=provider.WriteCalls;
            if(rejected is Avatar a)Corruption(await provider.SaveAvatarAsync(a));else if(rejected is AvatarDetail d)Corruption(await provider.SaveAvatarDetailAsync(d));else Corruption(await provider.SaveHolonAsync((Holon)rejected,saveChildren:false));
            Assert.AreEqual(writes,provider.WriteCalls,"Corrupt original must be rejected BEFORE mutation dispatch.");Assert.AreEqual(0,rejected.Version);Assert.AreEqual(Guid.Empty,rejected.VersionId);Assert.AreEqual(0,rejected.ProviderUniqueStorageKey.Count);
            if(kind==0)Corruption(await provider.LoadAvatarAsync(id));else if(kind==1)Corruption(await provider.LoadAvatarDetailAsync(id));else Corruption(await provider.LoadHolonAsync(id,loadChildren:false));
            if(kind==0)Corruption(await provider.DeleteAvatarAsync(id));else if(kind==1)Corruption(await provider.LoadAllAvatarDetailsAsync());else {Corruption(await provider.DeleteHolonAsync(id));Corruption(await provider.LoadAllHolonsAsync(loadChildren:false));}
            actual=await Read(verifier,table,id);Assert.AreEqual(corruptJson,actual,"Do not trim, rewrite or silently repair the corrupt original.");
            await using var delete=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
            delete.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
            Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();await rival.DeActivateProviderAsync();}
        static void Success<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);}
        static void Conflict<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
            Assert.IsInstanceOfType<OracleException>(result.Exception.InnerException);Assert.AreEqual(1,((OracleException)result.Exception.InnerException!).Number);
            Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
        }
        static void Corruption<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);if(result.Result is bool deleted)Assert.IsFalse(deleted);else Assert.IsNull(result.Result);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);}
    }

    [TestMethod][TestCategory("Reconciliation")]
    public async Task ReconcileKnownHistoricalExportFixture()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var originalReport=Path.Combine(work,"oracle-historical-export-before.trx");
        Assert.AreEqual("49A7E27CC064176F400C197DBBF3FBC9B644738EB6B52C3D09E6DD371A860E66",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(originalReport))));
        var journal=Path.Combine(work,"canonical-records.pending.json");using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
        var id=Guid.Parse("7dcc1028-b6ec-4b72-8646-551212209f11");var name="owned-oracle-"+id.ToString("N");
        Assert.AreEqual(id,intent.RootElement.GetProperty("id").GetGuid());Assert.AreEqual(name,intent.RootElement.GetProperty("name").GetString());
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var provider=new OracleDBOASIS(connection);
        try{
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Exception?.ToString());
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            var originals=new Dictionary<string,string>();
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS"}){
                var json=await Read(verifier,table,id);Assert.IsNotNull(json);using var document=JsonDocument.Parse(json);
                Assert.AreEqual(id,document.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(3,document.RootElement.GetProperty("Version").GetInt32());
                Assert.AreEqual(name+"-sync",document.RootElement.GetProperty(table=="OASIS_HOLONS"?"Name":"Username").GetString());
                Assert.AreEqual(2,document.RootElement.GetProperty("_oasis_record_history").GetProperty("records").GetArrayLength());originals.Add(table,json);
            }
            var old=await provider.LoadHolonAsync(id,version:1);Assert.IsFalse(old.IsError,old.Exception?.ToString());Assert.AreEqual(id,old.Result.CreatedByAvatarId);
            foreach(var loaded in new[]{await provider.ExportAllDataForAvatarByUsernameAsync(name,1),await provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",1),await provider.ExportAllDataForAvatarByIdAsync(id,1),await provider.LoadHolonsForParentAsync(old.Result.ParentHolonId,version:1),await provider.LoadHolonsByMetaDataAsync("owned.fixture","literal-dot",version:1)}){
                Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(name,loaded.Result.Single(row=>row.Id==id).Name);
            }
            var searched=await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){["owned.fixture"]="literal-dot"}},version:1);
            Assert.IsFalse(searched.IsError,searched.Exception?.ToString());Assert.AreEqual(name,searched.Result.SearchResultHolons.Single(row=>row.Id==id).Name);
            foreach(var original in originals){
                await using var delete=new OracleCommand($"DELETE FROM {original.Key} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                delete.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=original.Value;
                Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,original.Key,id));
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [TestMethod][TestCategory("Reconciliation")]
    public async Task ReconcileKnownCorruptAvatarFixture()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        Assert.AreEqual("ECFC83772408C854DFD31A808212D1A7DCA3D502EBC2C502362765B2100CE8BD",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"oracle-corrupt-avatar-before.trx")))));
        var id=Guid.Parse("792f3ede-f423-4e00-b297-6f6c1f0947ae");var journal=Path.Combine(work,"create-race-0.pending.json");
        using(var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal))){Assert.AreEqual(id,intent.RootElement.GetProperty("id").GetGuid());Assert.AreEqual("OASIS_AVATARS",intent.RootElement.GetProperty("table").GetString());Assert.AreEqual(0,intent.RootElement.GetProperty("kind").GetInt32());}
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var provider=new OracleDBOASIS(connection);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            var original=await Read(verifier,"OASIS_AVATARS",id);Assert.IsNotNull(original);
            using(var document=JsonDocument.Parse(original)){
                Assert.AreEqual(id,document.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(3,document.RootElement.GetProperty("Version").GetInt32());
                Assert.AreEqual(3,document.RootElement.GetProperty("_oasis_record_history").GetProperty("records")[0].GetProperty("Version").GetInt32());
                Assert.AreEqual("winner",document.RootElement.GetProperty("_oasis_record_history").GetProperty("records")[0].GetProperty("MetaData").GetProperty("owned_race").GetString());
            }
            var read=await provider.LoadAvatarAsync(id);Assert.IsTrue(read.IsError);Assert.IsNull(read.Result);Assert.IsInstanceOfType<InvalidDataException>(read.Exception);
            var deleted=await provider.DeleteAvatarAsync(id);Assert.IsTrue(deleted.IsError);Assert.IsFalse(deleted.Result);Assert.IsInstanceOfType<InvalidDataException>(deleted.Exception);
            Assert.AreEqual(original,await Read(verifier,"OASIS_AVATARS",id));
            await using var cleanup=new OracleCommand("DELETE FROM OASIS_AVATARS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
            cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=original;
            Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_AVATARS",id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    private static async Task<string?> Read(OracleConnection connection,string table,Guid id)
    {
        await using var command=new OracleCommand($"SELECT DATA_JSON FROM {table} WHERE ID=:Id",connection){BindByName=true};
        command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
        var value=await command.ExecuteScalarAsync();return value==null||value==DBNull.Value?null:value.ToString();
    }
}
