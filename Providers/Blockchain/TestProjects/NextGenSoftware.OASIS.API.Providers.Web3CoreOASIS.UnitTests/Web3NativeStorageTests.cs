using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
[DoNotParallelize]
public partial class Web3NativeStorageTests
{
    public sealed class Deployment : ContractDeploymentMessage
    {
        public Deployment():base((string)JObject.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("WEB3CORE_NATIVE_FIXTURE")!))["bytecode"]!) {}
    }

    [TestMethod]
    public async Task ActualPolygonProviderRoundtripsEncryptedRecordsAndPagedCollections()
    {
        Assert.AreEqual("http://127.0.0.1:18545",Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_RPC"));
        var key=Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!;
        var web3=new Web3(new Account(key,31337),"http://127.0.0.1:18545");
        Assert.AreEqual(31337L,(long)(await web3.Eth.ChainId.SendRequestAsync()).Value);
        var receipt=await web3.Eth.GetContractDeploymentHandler<Deployment>().SendRequestAndWaitForReceiptAsync(new Deployment {Gas=new HexBigInteger(8000000)});
        Assert.IsTrue(receipt.Succeeded());
        var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545",key,receipt.ContractAddress,31337);
        try
        {
            var activation=await provider.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Message);
            await VerifyNativeCredentialSelectorsAsync(provider);
            var empty=await provider.LoadAllAvatarsAsync();Assert.IsFalse(empty.IsError,empty.Message);Assert.IsTrue(empty.IsLoaded);Assert.IsFalse(empty.Result.Any());
            var avatar=new Avatar {Id=Guid.NewGuid(),IsActive=true,Username="owned-private",Email="native@example.invalid"};
            var saved=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Message);Assert.IsTrue(saved.IsSaved);
            var read=await provider.LoadAvatarAsync(avatar.Id);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(avatar.Email,read.Result.Email);
            var detail=new AvatarDetail {Id=avatar.Id,IsActive=true,Username=avatar.Username,Email=avatar.Email,XP=321,Karma=29};detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="owned seed"});
            var detailSaved=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(detailSaved.IsError,detailSaved.Message);
            var detailRead=await provider.LoadAvatarDetailAsync(detail.Id);Assert.IsFalse(detailRead.IsError,detailRead.Message);
            Assert.AreEqual(321L,detailRead.Result.XP);Assert.AreEqual(29L,detailRead.Result.Karma);Assert.AreEqual("owned seed",detailRead.Result.Inventory.Single().Name);
            avatar.FirstName="updated native profile";
            Assert.IsFalse(avatar.IsNewHolon);Assert.AreEqual(avatar.Id.ToString(),avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.PolygonOASIS]);
            Assert.IsFalse(detail.IsNewHolon);Assert.AreEqual(detail.Id.ToString(),detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.PolygonOASIS]);
            Assert.IsInstanceOfType((await provider.SaveAvatarAsync(null!)).Exception,typeof(ArgumentNullException));
            Assert.IsInstanceOfType(provider.SaveAvatarDetail(null!).Exception,typeof(ArgumentNullException));
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            Assert.AreEqual("updated native profile",(await provider.LoadAvatarAsync(avatar.Id)).Result.FirstName);
            detail.XP=444;
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);
            Assert.AreEqual(444L,(await provider.LoadAvatarDetailAsync(detail.Id)).Result.XP);
            var details=provider.LoadAllAvatarDetails();Assert.IsFalse(details.IsError,details.Message);Assert.AreEqual(detail.Id,details.Result.Single().Id);
            Assert.AreEqual(avatar.Id,(await provider.LoadAvatarByUsernameAsync(avatar.Username.ToUpperInvariant())).Result.Id);
            Assert.AreEqual(avatar.Id,provider.LoadAvatarByEmail(avatar.Email.ToUpperInvariant()).Result.Id);
            Assert.AreEqual(avatar.Id,(await provider.LoadAvatarDetailByEmailAsync(avatar.Email.ToUpperInvariant())).Result.Id);
            Assert.AreEqual(avatar.Id,provider.LoadAvatarDetailByUsername(avatar.Username.ToUpperInvariant()).Result.Id);
            Assert.AreEqual(avatar.Id,(await provider.LoadAvatarByProviderKeyAsync(avatar.Id.ToString().ToUpperInvariant())).Result.Id);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,provider.LoadAvatarByEmail("absent@example.invalid").ErrorCode);
            var root=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="owned root",CreatedByAvatarId=avatar.Id,IsNewHolon=true,MetaData=new Dictionary<string,object> {{"biome","park"}}};
            root.MetaData["Latitude"]="21.1619";root.MetaData["Longitude"]="-86.8515";
            var child=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="owned child",ParentHolonId=root.Id,IsNewHolon=true};
            foreach(var row in new[] {root,child}) {var result=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(result.IsError,result.Message);}
            var nearby=provider.GetHolonsNearMe(21161900,-86851500,0,Core.Enums.HolonType.All);
            Assert.IsFalse(nearby.IsError,nearby.Message);Assert.IsTrue(nearby.IsLoaded);Assert.AreEqual(root.Id,nearby.Result.Single().Id);
            Assert.IsFalse(provider.GetHolonsNearMe(0,0,1,Core.Enums.HolonType.All).Result.Any());
            Assert.IsInstanceOfType(provider.GetAvatarsNearMe(90000001,0,1).Exception,typeof(ArgumentOutOfRangeException));
            Assert.IsInstanceOfType(provider.GetHolonsNearMe(0,0,-1,Core.Enums.HolonType.All).Exception,typeof(ArgumentOutOfRangeException));
            Assert.IsInstanceOfType(provider.GetHolonsNearMe(Core.Enums.HolonType.All).Exception,typeof(NotSupportedException));
            child.Name="updated child";
            Assert.IsFalse((await provider.SaveHolonAsync(child,saveChildren:false)).IsError);
            Assert.AreEqual("updated child",(await provider.LoadHolonAsync(child.Id,loadChildren:false)).Result.Name);
            Assert.AreEqual(child.Id,(await provider.LoadHolonAsync(child.Id.ToString().ToUpperInvariant(),loadChildren:false)).Result.Id);
            Assert.IsInstanceOfType((await provider.LoadHolonAsync("invalid",loadChildren:false)).Exception,typeof(ArgumentException));
            var graph=await provider.LoadAllHolonsAsync(loadChildrenFromProvider:true);Assert.IsFalse(graph.IsError,graph.Message);
            Assert.AreEqual(child.Id,graph.Result.Single(row=>row.Id==root.Id).Children.Single().Id);
            var single=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);
            Assert.IsFalse(single.IsError,single.Message);Assert.AreEqual(child.Id,single.Result.Children.Single().Id);
            Assert.AreEqual(child.Id,provider.LoadHolon(root.Id.ToString().ToUpperInvariant(),loadChildrenFromProvider:true).Result.Children.Single().Id);
            Assert.IsFalse((await provider.LoadHolonAsync(root.Id,loadChildren:false,loadChildrenFromProvider:true)).Result.Children.Any());
            Assert.IsFalse((await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:false)).Result.Children.Any());
            Assert.IsInstanceOfType((await provider.LoadHolonAsync(root.Id,maxChildDepth:-1)).Exception,typeof(ArgumentOutOfRangeException));
            var grandchild=new Holon {Id=Guid.NewGuid(),IsActive=true,ParentHolonId=child.Id,Name="temporary graph depth fixture"};
            Assert.IsFalse((await provider.SaveHolonAsync(grandchild,saveChildren:false)).IsError);
            Assert.AreEqual(grandchild.Id,(await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true)).Result.Children.Single().Children.Single().Id);
            Assert.IsFalse((await provider.LoadHolonAsync(root.Id,recursive:false,loadChildrenFromProvider:true)).Result.Children.Single().Children.Any());
            Assert.IsFalse((await provider.LoadHolonAsync(root.Id,maxChildDepth:1,loadChildrenFromProvider:true)).Result.Children.Single().Children.Any());
            Assert.AreEqual(grandchild.Id,(await provider.LoadHolonAsync(root.Id,maxChildDepth:2,loadChildrenFromProvider:true)).Result.Children.Single().Children.Single().Id);
            Assert.IsFalse((await provider.DeleteHolonAsync(grandchild.Id)).IsError);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,(await provider.LoadHolonAsync(Guid.NewGuid(),loadChildrenFromProvider:true)).ErrorCode);
            var parent=provider.LoadHolonsForParent(root.Id.ToString());Assert.IsFalse(parent.IsError,parent.Message);Assert.IsTrue(parent.IsLoaded);Assert.AreEqual(child.Id,parent.Result.Single().Id);
            var missing=await provider.LoadHolonsForParentAsync(Guid.NewGuid());Assert.IsFalse(missing.IsError,missing.Message);Assert.IsFalse(missing.Result.Any());
            var pairs=new Dictionary<string,string> {{"biome","park"},{"missing","no"}};
            var any=await provider.LoadHolonsByMetaDataAsync(pairs,Core.Enums.MetaKeyValuePairMatchMode.Any,loadChildrenFromProvider:true);
            Assert.IsFalse(any.IsError,any.Message);Assert.AreEqual(root.Id,any.Result.Single().Id);Assert.AreEqual(child.Id,any.Result.Single().Children.Single().Id);
            var all=provider.LoadHolonsByMetaData(pairs,Core.Enums.MetaKeyValuePairMatchMode.All);Assert.IsFalse(all.IsError,all.Message);Assert.IsFalse(all.Result.Any());
            Assert.AreEqual(root.Id,provider.LoadHolonsByMetaData("biome","park").Result.Single().Id);
            Assert.IsInstanceOfType((await provider.LoadHolonsByMetaDataAsync(" ","no")).Exception,typeof(ArgumentException));
            var search=await provider.SearchAsync(new Core.Objects.Search.SearchParams {
                SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string> {{"biome","park"}},
                SearchGroups=new() {new Core.Objects.Search.SearchTextGroup {SearchQuery="owned root",SearchHolons=true,HolonType=Core.Enums.HolonType.All}}});
            Assert.IsFalse(search.IsError,search.Message);Assert.IsTrue(search.IsLoaded);Assert.AreEqual(1,search.Result.NumberOfResults);
            Assert.AreEqual(root.Id,search.Result.SearchResultHolons.Single().Id);Assert.AreEqual(0,search.Result.SearchResultAvatars.Count);
            Assert.AreEqual(child.Id,search.Result.SearchResultHolons.Single().Children.Single().Id);
            var avatarSearch=provider.Search(new Core.Objects.Search.SearchParams {SearchOnlyForCurrentAvatar=false,
                SearchGroups=new() {new Core.Objects.Search.SearchTextGroup {SearchQuery="owned-private",SearchAvatars=true,HolonType=Core.Enums.HolonType.All}}});
            Assert.IsFalse(avatarSearch.IsError,avatarSearch.Message);Assert.AreEqual(avatar.Id,avatarSearch.Result.SearchResultAvatars.Single().Id);
            Assert.AreEqual(0,avatarSearch.Result.SearchResultHolons.Count);
            Assert.IsInstanceOfType((await provider.SearchAsync(null!)).Exception,typeof(ArgumentNullException));
            var export=await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username.ToUpperInvariant());
            Assert.IsFalse(export.IsError,export.Message);Assert.IsTrue(export.IsLoaded);Assert.AreEqual(root.Id,export.Result.Single().Id);
            Assert.AreEqual(child.Id,export.Result.Single().Children.Single().Id);
            Assert.AreEqual(root.Id,provider.ExportAllDataForAvatarByEmail(avatar.Email.ToUpperInvariant()).Result.Single().Id);
            Assert.AreEqual(2,(await provider.ExportAllAsync()).Result.Count());
            var importedChild=new Holon {IsActive=true,Name="imported child"};
            var importedRoot=new Holon {IsActive=true,Name="imported root",Children=new List<Core.Interfaces.IHolon> {importedChild}};
            var imported=await provider.ImportAsync(new[] {importedRoot});Assert.IsFalse(imported.IsError,imported.Message);Assert.IsTrue(imported.Result);Assert.IsTrue(imported.IsSaved);
            Assert.AreNotEqual(Guid.Empty,importedRoot.Id);Assert.AreEqual(importedChild.Id,(await provider.LoadHolonsForParentAsync(importedRoot.Id)).Result.Single().Id);
            for(var index=0;index<128;index++)
            {
                var result=await provider.SaveAvatarAsync(new Avatar {Id=Guid.NewGuid(),IsActive=true,Username="owned-"+index});
                Assert.IsFalse(result.IsError,result.Message);
            }
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=Guid.NewGuid(),IsActive=false,Username=avatar.Username,Email=avatar.Email})).IsError);
            Assert.AreEqual(avatar.Id,provider.LoadAvatarByUsername(avatar.Username).Result.Id);
            var avatars=await provider.LoadAllAvatarsAsync();Assert.IsFalse(avatars.IsError,avatars.Message);Assert.AreEqual(129,avatars.Result.Count());
            Assert.AreEqual(129,avatars.Result.Select(row=>row.Id).Distinct().Count());
            var raw=await new Web3CoreOASIS(key,"http://127.0.0.1:18545",receipt.ContractAddress,Web3CoreOASISBaseProviderHelper.Abi,31337).ReadCollectionAsync("avatar");
            Assert.IsFalse(raw.Any(row=>System.Text.Encoding.UTF8.GetString(row.Info).Contains(avatar.Email)));
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=Guid.NewGuid(),IsActive=true,Username=avatar.Username.ToUpperInvariant(),Email=avatar.Email.ToUpperInvariant()})).IsError);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarByUsernameAsync(avatar.Username)).ErrorCode);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,provider.LoadAvatarByEmail(avatar.Email).ErrorCode);
            await VerifyV2Async(provider,avatar.Id);
            var generatedChild=new Holon {IsActive=true,Name="generated child"};
            var generatedRoot=new Holon {IsActive=true,Name="generated root",Children=new List<Core.Interfaces.IHolon> {generatedChild}};
            var graphSaved=await provider.SaveHolonAsync(generatedRoot);
            Assert.IsFalse(graphSaved.IsError,graphSaved.Message);Assert.IsTrue(graphSaved.IsSaved);
            Assert.AreNotEqual(Guid.Empty,generatedRoot.Id);Assert.AreNotEqual(Guid.Empty,generatedChild.Id);
            Assert.AreEqual(generatedRoot.Id,generatedChild.ParentHolonId);
            Assert.AreEqual(generatedChild.Id,(await provider.LoadHolonsForParentAsync(generatedRoot.Id)).Result.Single().Id);
            var native=new Web3CoreOASIS(key,"http://127.0.0.1:18545",receipt.ContractAddress,Web3CoreOASISBaseProviderHelper.Abi,31337);
            var pairId=(uint)Core.Utilities.HashUtility.GetNumericHash(avatar.Id.ToString());
            var initialPair=await native.ReadAvatarPairAsync(pairId);
            var staleRequest=new AvatarLifecycleRequest {EntityId=pairId,ExternalId=Web3CoreOASISBaseProvider.PrivateExternalId(avatar.Id),
                ExpectedAvatarHash=new byte[32],ExpectedDetailHash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(initialPair.Detail.Info),HasDetail=true,HardDelete=true};
            await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>native.ApplyAvatarLifecycleAsync(staleRequest));
            var unchangedPair=await native.ReadAvatarPairAsync(pairId);
            CollectionAssert.AreEqual(initialPair.Avatar.Info,unchangedPair.Avatar.Info);CollectionAssert.AreEqual(initialPair.Detail.Info,unchangedPair.Detail.Info);
            var softDeleted=await provider.DeleteAvatarByUsernameAsync("ROUTED PROFILE");
            Assert.IsFalse(softDeleted.IsError,softDeleted.Message);Assert.IsTrue(softDeleted.Result);Assert.IsTrue(softDeleted.IsSaved);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarAsync(avatar.Id)).ErrorCode);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,provider.LoadAvatarDetail(avatar.Id).ErrorCode);
            var softPair=await native.ReadAvatarPairAsync(pairId);
            var deletedAvatar=provider.DecodePrivateEntity<Avatar>("avatar",softPair.Avatar);
            var deletedDetail=provider.DecodePrivateEntity<AvatarDetail>("detail",softPair.Detail);
            Assert.IsFalse(deletedAvatar.IsActive);Assert.IsFalse(deletedDetail.IsActive);
            Assert.AreEqual(DateTimeKind.Utc,deletedAvatar.DeletedDate.Kind);Assert.AreEqual(deletedAvatar.DeletedDate,deletedDetail.DeletedDate);
            Assert.AreEqual(600L,deletedDetail.XP);Assert.AreEqual(504L,deletedDetail.Karma);Assert.AreEqual("routed seed",deletedDetail.Inventory.Single().Name);
            var resurrectAvatar=await provider.SaveAvatarAsync(new Avatar {Id=avatar.Id,IsActive=true,Username="must not resurrect"});
            var resurrectDetail=provider.SaveAvatarDetail(new AvatarDetail {Id=avatar.Id,IsActive=true,XP=99999});
            Assert.AreEqual(Core.Helpers.StorageLifecycleConflictException.ErrorCode,resurrectAvatar.ErrorCode);
            Assert.AreEqual(Core.Helpers.StorageLifecycleConflictException.ErrorCode,resurrectDetail.ErrorCode);
            Assert.IsFalse(resurrectAvatar.IsSaved);Assert.IsFalse(resurrectDetail.IsSaved);
            var afterResurrection=await native.ReadAvatarPairAsync(pairId);
            CollectionAssert.AreEqual(softPair.Avatar.Info,afterResurrection.Avatar.Info);CollectionAssert.AreEqual(softPair.Detail.Info,afterResurrection.Detail.Info);
            var repeated=provider.DeleteAvatar(avatar.Id);Assert.IsFalse(repeated.IsError,repeated.Message);Assert.IsTrue(repeated.Result);Assert.IsFalse(repeated.IsSaved);
            var repeatedPair=await native.ReadAvatarPairAsync(pairId);CollectionAssert.AreEqual(softPair.Avatar.Info,repeatedPair.Avatar.Info);CollectionAssert.AreEqual(softPair.Detail.Info,repeatedPair.Detail.Info);
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.AvatarTombstoneEvidence.TryGet(softDeleted,out var committed));
            var lifecycle=(NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarTombstoneStorageProvider)provider;
            var reapplied=await lifecycle.ApplyAvatarTombstoneAsync(committed);
            Assert.IsFalse(reapplied.IsError,reapplied.Message);Assert.IsTrue(reapplied.Result);Assert.IsFalse(reapplied.IsSaved);
            var conflict=lifecycle.ApplyAvatarTombstone(new AvatarTombstone(avatar.Id,committed.DeletedAtUtc.AddTicks(1),Guid.NewGuid()));
            Assert.IsTrue(conflict.IsError);Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode,conflict.ErrorCode);
            var afterConflict=await native.ReadAvatarPairAsync(pairId);
            CollectionAssert.AreEqual(softPair.Avatar.Info,afterConflict.Avatar.Info);CollectionAssert.AreEqual(softPair.Detail.Info,afterConflict.Detail.Info);
            var hardDeleted=await provider.DeleteAvatarAsync(avatar.Id.ToString().ToUpperInvariant(),softDelete:false);
            Assert.IsFalse(hardDeleted.IsError,hardDeleted.Message);Assert.IsTrue(hardDeleted.Result);
            var removed=await native.ReadAvatarPairAsync(pairId);Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(removed.Avatar));Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(removed.Detail));
            var replicaId=Guid.NewGuid();
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=replicaId,IsActive=true,Username="exact lifecycle replica"})).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=replicaId,IsActive=true,XP=731,Karma=193})).IsError);
            var exactPatch=new AvatarTombstone(replicaId,new DateTime(2026,10,1,12,30,0,DateTimeKind.Utc).AddTicks(1234),Guid.NewGuid());
            var applied=await lifecycle.ApplyAvatarTombstoneAsync(exactPatch);
            Assert.IsFalse(applied.IsError,applied.Message);Assert.IsTrue(applied.IsSaved);
            var replicaPair=await native.ReadAvatarPairAsync((uint)Core.Utilities.HashUtility.GetNumericHash(replicaId.ToString()));
            var replicaAvatar=provider.DecodePrivateEntity<Avatar>("avatar",replicaPair.Avatar);
            var replicaDetail=provider.DecodePrivateEntity<AvatarDetail>("detail",replicaPair.Detail);
            Assert.AreEqual(exactPatch.DeletedAtUtc,replicaAvatar.DeletedDate);Assert.AreEqual(exactPatch.DeletedAtUtc,replicaDetail.DeletedDate);
            Assert.AreEqual(exactPatch.DeletedByAvatarId,replicaAvatar.DeletedByAvatarId);Assert.AreEqual(exactPatch.DeletedByAvatarId,replicaDetail.DeletedByAvatarId);
            Assert.IsFalse(replicaAvatar.IsActive);Assert.IsFalse(replicaDetail.IsActive);Assert.AreEqual(731L,replicaDetail.XP);Assert.AreEqual(193L,replicaDetail.Karma);
            await VerifyRecordHistoryAsync(provider,native);
            await VerifyHistoryResponseLossAsync(provider);
            var beforeCycle=await native.GetHolonsCountAsync();
            await VerifyProfileAcknowledgementAsync(provider,native);
            await VerifyPreparationScopeAsync(provider,native);
            if(Environment.GetEnvironmentVariable("OASIS_WEB3_PREPARED_OUTBOX_REQUIRED")=="1") await VerifyRealOutboxAsync(provider,native);
            await VerifyCheckedUpsertsAsync(native);
            var deleteFixture=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="checked deletion"};
            Assert.IsFalse((await provider.SaveHolonAsync(deleteFixture,saveChildren:false)).IsError);
            var deleteKey=(uint)Core.Utilities.HashUtility.GetNumericHash(deleteFixture.Id.ToString());
            var beforeDelete=await native.GetHolonByIdAsync(deleteKey);
            await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>native.DeleteHolonCheckedAsync(deleteKey,new byte[32],Nethereum.Util.Sha3Keccack.Current.CalculateHash(beforeDelete.Info)));
            await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>native.DeleteHolonCheckedAsync(deleteKey,Web3CoreOASISBaseProvider.PrivateExternalId(deleteFixture.Id),new byte[32]));
            CollectionAssert.AreEqual(beforeDelete.Info,(await native.GetHolonByIdAsync(deleteKey)).Info);
            var deletedHolon=provider.DeleteHolon(deleteFixture.Id.ToString().ToUpperInvariant());
            Assert.IsFalse(deletedHolon.IsError,deletedHolon.Message);Assert.IsTrue(deletedHolon.IsDeleted);Assert.IsTrue(deletedHolon.IsSaved);Assert.AreEqual(deleteFixture.Id,deletedHolon.Result.Id);
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetHolonByIdAsync(deleteKey)));
            var missingDelete=await provider.DeleteHolonAsync(deleteFixture.Id.ToString());
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missingDelete.ErrorCode);Assert.IsNull(missingDelete.Exception);Assert.IsFalse(missingDelete.IsSaved);
            Assert.IsInstanceOfType((await provider.DeleteHolonAsync("invalid key")).Exception,typeof(ArgumentException));
            Assert.AreEqual(beforeCycle,await native.GetHolonsCountAsync());
            var cycle=new Holon {IsActive=true};cycle.Children=new List<Core.Interfaces.IHolon> {cycle};
            var invalid=await provider.SaveHolonAsync(cycle);
            Assert.IsTrue(invalid.IsError);Assert.IsInstanceOfType(invalid.Exception,typeof(ArgumentException));
            Assert.AreEqual(Guid.Empty,cycle.Id);Assert.AreEqual(beforeCycle,await native.GetHolonsCountAsync());
            var first=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="first acknowledged"};
            var second=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="collision rejected"};
            var third=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="must not run"};
            await native.CreateHolonAsync((uint)Core.Utilities.HashUtility.GetNumericHash(second.Id.ToString()),System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("owned conflicting identity")),new byte[] {1});
            var beforePartialNonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(native.SigningAddress)).Value;
            var partial=await provider.SaveHolonsAsync(new[] {first,second,third},saveChildren:false,continueOnError:true);
            Assert.IsTrue(partial.IsError);Assert.IsFalse(partial.IsSaved);
            var evidence=partial.Exception as Web3PartialMutationException;Assert.IsNotNull(evidence);
            CollectionAssert.AreEqual(new[] {first.Id},evidence.AcknowledgedRecordIds.ToArray());
            Assert.AreEqual(1,evidence.AcknowledgedTransactionIds.Count);Assert.AreEqual(66,evidence.AcknowledgedTransactionIds.Single().Length);
            Assert.IsInstanceOfType(evidence.InnerException,typeof(System.IO.InvalidDataException));
            Assert.IsFalse(Core.Helpers.NativeMutationReconciliation.IsRequired(evidence.InnerException));
            Assert.AreEqual(beforePartialNonce+1,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(native.SigningAddress)).Value,
                "Only the first acknowledged write may broadcast; corrupt prior state fails before the second submission.");
            Assert.AreEqual("first acknowledged",(await provider.LoadHolonAsync(first.Id,loadChildren:false)).Result.Name);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,(await provider.LoadHolonAsync(third.Id)).ErrorCode);
        }
        finally {await provider.DeActivateProviderAsync();}
    }
}
