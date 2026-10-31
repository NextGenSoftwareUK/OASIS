using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.Common;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaCredentialSelectorTests
{
    [DataTestMethod]
    [DataRow(false,0)] [DataRow(true,0)] [DataRow(false,1)] [DataRow(true,1)]
    [DataRow(false,2)] [DataRow(true,2)] [DataRow(false,3)] [DataRow(true,3)] [DataRow(false,4)] [DataRow(true,4)]
    public async Task CredentialSelectorsUseSerializedDtoAndRejectAmbiguousRecords(bool sync,int kind)
    {
        var repository=new RecordsRepository();var provider=new ActiveProvider(repository);var storage=(IOASISStorageProvider)provider;
        const string key="owned-synthetic-credential";
        Avatar Create(string name) => new() {Id=Guid.NewGuid(),Username=name,IsActive=true,VerificationToken=key,ResetToken=key,
            ResetTokenExpires=new DateTime(2026,10,17,1,2,3,DateTimeKind.Utc),RefreshToken="owned-legacy-token",RefreshTokens=new() {new RefreshToken {Token=key}},
            ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=Guid.NewGuid(),PublicKey=key,PrivateKey=key}}}}};
        async Task<OASISResult<IAvatar>> Read(string value,int version=0) => kind switch {
            0=>sync ? storage.LoadAvatarByVerificationToken(value,version):await storage.LoadAvatarByVerificationTokenAsync(value,version),
            1=>sync ? storage.LoadAvatarByResetToken(value,version):await storage.LoadAvatarByResetTokenAsync(value,version),
            2=>sync ? storage.LoadAvatarByRefreshToken(value,version):await storage.LoadAvatarByRefreshTokenAsync(value,version),
            3=>sync ? storage.LoadAvatarByPublicKey(value,version):await storage.LoadAvatarByPublicKeyAsync(value,version),
            _=>sync ? storage.LoadAvatarByPrivateKey(value,version):await storage.LoadAvatarByPrivateKeyAsync(value,version)};
        foreach(var input in new[] {(" ",0),(key,-1)})
        {
            var invalid=await Read(input.Item1,input.Item2);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",invalid.ErrorCode);Assert.IsNull(invalid.Result);
            Assert.IsInstanceOfType(invalid.Exception,typeof(ArgumentException));
        }
        Assert.AreEqual(0,repository.Reads);
        var first=Create("first");repository.Records.Add(first.GetSolanaAvatarDto());
        var loaded=await Read(key);Assert.IsFalse(loaded.IsError,loaded.Message);Assert.IsTrue(loaded.IsLoaded);Assert.AreEqual(first.Id,loaded.Result.Id);
        Assert.AreEqual(first.ResetTokenExpires,loaded.Result.ResetTokenExpires);Assert.AreEqual(first.RefreshToken,loaded.Result.RefreshToken);
        Assert.AreEqual(first.ProviderWallets[ProviderType.SolanaOASIS][0].Id,loaded.Result.ProviderWallets[ProviderType.SolanaOASIS][0].Id);
        repository.Records[0].Version=2;
        var readsBeforeHistorical=repository.Reads;
        foreach(var version in new[]{1,2}){
            var historical=await Read(key,version);
            Assert.IsTrue(historical.IsError);Assert.IsNull(historical.Result);
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",historical.ErrorCode);
        }
        Assert.AreEqual(readsBeforeHistorical,repository.Reads,"Archived credentials must reject before provider I/O.");
        var latest=await Read(key);Assert.IsFalse(latest.IsError,latest.Message);
        Assert.AreEqual(2,latest.Result.Version);Assert.AreEqual(0,repository.LastVersion);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await Read(key+"missing")).ErrorCode);
        repository.Records.Add(Create("duplicate").GetSolanaAvatarDto());
        var duplicate=await Read(key);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,duplicate.ErrorCode);Assert.IsNull(duplicate.Result);
        repository.Records.RemoveAt(1);
        var cause=new System.IO.IOException("owned finalized collection failure");repository.Cause=cause;
        var failed=await Read(key);Assert.IsTrue(failed.IsError);Assert.AreSame(cause,failed.Exception);Assert.IsNull(failed.Result);
        repository.Cause=null;repository.Records[0].ProviderWalletsJson="{invalid";
        var corrupt=await Read(key);Assert.IsTrue(corrupt.IsError);Assert.IsInstanceOfType(corrupt.Exception,typeof(System.IO.InvalidDataException));Assert.IsNull(corrupt.Result);
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)]
    [DataRow(false,true)] [DataRow(true,true)]
    public async Task HistoricalCollectionsForwardVersionAndKeepTypedSignatures(bool sync,bool detail)
    {
        var id=Guid.NewGuid();
        var repository=new RecordsRepository();
        repository.Records.Add(new SolanaAvatarDto {Id=id,Version=3,UserName="historical-avatar"});
        repository.Details.Add(new SolanaAvatarDetailDto {Id=id,Version=3,Username="historical-detail",XP=89});
        var provider=new ActiveProvider(repository);
        if(detail)
        {
            var result=sync ? provider.LoadAllAvatarDetails(3):await provider.LoadAllAvatarDetailsAsync(3);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);
            Assert.AreEqual(89L,result.Result.Single().XP);Assert.AreEqual(3,result.Result.Single().Version);
            Assert.AreEqual("owned-detail-signature-"+id,result.Result.Single().ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
        }
        else
        {
            var result=sync ? provider.LoadAllAvatars(3):await provider.LoadAllAvatarsAsync(3);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);
            Assert.AreEqual("historical-avatar",result.Result.Single().Username);Assert.AreEqual(3,result.Result.Single().Version);
            Assert.AreEqual("owned-signature-"+id,result.Result.Single().ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
        }
        Assert.AreEqual(3,repository.LastVersion);Assert.AreEqual(1,repository.Reads);
    }

    [DataTestMethod]
    [DataRow(false,"latest")] [DataRow(true,"latest")]
    [DataRow(false,"exact")] [DataRow(true,"exact")]
    [DataRow(false,"mismatch")] [DataRow(true,"mismatch")]
    [DataRow(false,"blank")] [DataRow(true,"blank")]
    [DataRow(false,"negative")] [DataRow(true,"negative")]
    [DataRow(false,"failure")] [DataRow(true,"failure")]
    public async Task ProviderKeyAvatarReadValidatesVersionAndPreservesNativeIdentityAndCause(bool sync,string scenario)
    {
        var repository=new RecordsRepository();
        repository.Records.Add(new SolanaAvatarDto {Id=Guid.NewGuid(),Version=4,UserName="signature avatar"});
        var cause=new System.IO.IOException("owned signature read failure");
        if(scenario=="failure") repository.Cause=cause;
        var provider=new ActiveProvider(repository);
        var signature=scenario=="blank" ? " ":"owned-avatar-signature";
        var version=scenario=="negative" ? -1:scenario=="mismatch" ? 3:scenario=="exact" ? 4:0;
        var result=sync ? provider.LoadAvatarByProviderKey(signature,version):await provider.LoadAvatarByProviderKeyAsync(signature,version);
        if(scenario is "latest" or "exact")
        {
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);
            Assert.AreEqual(4,result.Result.Version);Assert.AreEqual("signature avatar",result.Result.Username);
            Assert.AreEqual(signature,result.Result.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
        }
        else
        {
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
            if(scenario=="failure") Assert.AreSame(cause,result.Exception);
            else Assert.IsInstanceOfType(result.Exception,scenario=="mismatch" ? typeof(KeyNotFoundException):typeof(ArgumentException));
        }
        Assert.AreEqual(scenario is "blank" or "negative" ? 0:1,repository.Reads);
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)]
    [DataRow(false,true)] [DataRow(true,true)]
    public async Task HistoricalSelectorExportResolvesCurrentOwnerAndVersionsOnlyRoots(bool sync,bool email)
    {
        var owner=Guid.NewGuid();var rootId=Guid.NewGuid();var childId=Guid.NewGuid();
        var repository=new RecordsRepository();
        repository.Records.Add(new SolanaAvatarDto {Id=owner,AvatarId=owner,Version=9,UserName="current-owner",Email="owner@example.invalid"});
        repository.Holons.Add(new SolanaHolonDto {Id=rootId,Version=2,CreatedByAvatarId=owner,Name="historical owned root"});
        repository.Holons.Add(new SolanaHolonDto {Id=childId,Version=11,ParentHolonId=rootId,Name="current child"});
        repository.Holons.Add(new SolanaHolonDto {Id=Guid.NewGuid(),Version=2,CreatedByAvatarId=Guid.NewGuid(),Name="unrelated"});
        var provider=new ActiveProvider(repository);
        async Task<OASISResult<IEnumerable<IHolon>>> Read(string selector,int version) => email
            ? sync ? provider.ExportAllDataForAvatarByEmail(selector,version):await provider.ExportAllDataForAvatarByEmailAsync(selector,version)
            : sync ? provider.ExportAllDataForAvatarByUsername(selector,version):await provider.ExportAllDataForAvatarByUsernameAsync(selector,version);
        var selector=email ? "owner@example.invalid":"current-owner";
        Assert.IsTrue((await Read(selector,-1)).IsError);Assert.AreEqual(0,repository.Reads);
        var result=await Read(selector,2);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);
        var root=result.Result.Single();Assert.AreEqual(rootId,root.Id);Assert.AreEqual(2,root.Version);
        Assert.AreEqual(childId,root.Children.Single().Id);Assert.AreEqual(11,root.Children.Single().Version);
        CollectionAssert.AreEqual(new[] {0,2,0},repository.Versions);
        var missing=await Read("missing",2);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);Assert.IsNull(missing.Result);
        repository.Records.Add(new SolanaAvatarDto {Id=Guid.NewGuid(),Version=4,UserName="current-owner",Email="owner@example.invalid"});
        var ambiguous=await Read(selector,2);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,ambiguous.ErrorCode);Assert.IsNull(ambiguous.Result);
        var cause=new System.IO.IOException("owned selector history failure");repository.Cause=cause;
        var failed=await Read(selector,2);Assert.AreSame(cause,failed.Exception);Assert.IsNull(failed.Result);
    }

    [DataTestMethod]
    [DataRow(false,"valid")] [DataRow(true,"valid")]
    [DataRow(false,"blank")] [DataRow(true,"blank")]
    [DataRow(false,"wrong-kind")] [DataRow(true,"wrong-kind")]
    [DataRow(false,"unknown")] [DataRow(true,"unknown")]
    [DataRow(false,"rejected")] [DataRow(true,"rejected")]
    public async Task AvatarSignatureDeleteRequiresTypedReadBeforeMutation(bool sync,string scenario)
    {
        var id=Guid.NewGuid();
        var repository=new RecordsRepository {AllowDelete=true};
        repository.Records.Add(new SolanaAvatarDto {Id=id,AvatarId=id,Version=2});
        var wrongKind=new InvalidOperationException("Solana storage record kind does not match the requested type.");
        var unknown=new SolanaTransactionReconciliationException("owned-delete-signature",new IOException("owned lost reply"));
        if(scenario=="wrong-kind") repository.Cause=wrongKind;
        if(scenario=="unknown") repository.DeleteCause=unknown;
        repository.DeleteAccepted=scenario!="rejected";
        var provider=new ActiveProvider(repository);
        var key=scenario=="blank" ? " ":"owned-avatar-signature";
        var result=sync ? provider.DeleteAvatar(key):await provider.DeleteAvatarAsync(key);
        Assert.AreEqual(scenario!="valid",result.IsError);
        Assert.AreEqual(scenario=="valid",result.Result);
        if(scenario=="wrong-kind") Assert.AreSame(wrongKind,result.Exception);
        if(scenario=="unknown") Assert.AreSame(unknown,result.Exception);
        Assert.AreEqual(scenario=="blank" ? 0:scenario=="wrong-kind" ? 1:scenario=="valid" ? 3:2,repository.Reads);
        Assert.AreEqual(scenario is "blank" or "wrong-kind" ? 0:1,repository.Deletes);
    }

    public static IEnumerable<object[]> DeleteSelectorCases()
    {
        foreach(var sync in new[] {false,true})
        foreach(var kind in new[] {"id","email","username"})
        foreach(var scenario in new[] {"valid","missing","lookup-failure","unknown"})
            yield return new object[] {sync,kind,scenario};
    }

    [DataTestMethod]
    [DynamicData(nameof(DeleteSelectorCases),DynamicDataSourceType.Method)]
    public async Task DeleteSelectorsShareTypedMutationAndPreserveFailures(bool sync,string kind,string scenario)
    {
        var id=Guid.NewGuid();var repository=new RecordsRepository {AllowDelete=true};
        if(scenario!="missing") repository.Records.Add(new SolanaAvatarDto {Id=id,AvatarId=id,UserName="owner",Email="owner@example.invalid"});
        var cause=new IOException("owned lookup failure");
        var unknown=new SolanaTransactionReconciliationException("owned-delete-signature",cause);
        if(scenario=="lookup-failure") repository.Cause=cause;
        if(scenario=="unknown") repository.DeleteCause=unknown;
        var provider=new ActiveProvider(repository);
        var result=kind switch
        {
            "id"=>sync ? provider.DeleteAvatar(id):await provider.DeleteAvatarAsync(id),
            "email"=>sync ? provider.DeleteAvatarByEmail("owner@example.invalid"):await provider.DeleteAvatarByEmailAsync("owner@example.invalid"),
            _=>sync ? provider.DeleteAvatarByUsername("owner"):await provider.DeleteAvatarByUsernameAsync("owner")
        };
        Assert.AreEqual(scenario!="valid",result.IsError);Assert.AreEqual(scenario=="valid",result.Result);
        if(scenario=="missing") Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
        if(scenario=="lookup-failure") Assert.AreSame(cause,result.Exception);
        if(scenario=="unknown") Assert.AreSame(unknown,result.Exception);
        Assert.AreEqual(scenario is "valid" or "unknown" ? 1:0,repository.Deletes);
        Assert.AreEqual(scenario=="valid" ? 4:scenario=="unknown" ? 3:1,repository.Reads);
    }

    [DataTestMethod]
    [DataRow(false,"failure")] [DataRow(true,"failure")]
    [DataRow(false,"date")] [DataRow(true,"date")]
    public async Task AvatarDeletionReadbackFailureRetainsNativeReceiptWithoutSuccess(bool sync,string scenario)
    {
        var id=Guid.NewGuid();var repository=new RecordsRepository {AllowDelete=true,DeleteReadScenario=scenario};
        var source=new SolanaAvatarDto {Id=id,AvatarId=id,Version=4};repository.Records.Add(source);
        var provider=new ActiveProvider(repository);
        var result=sync ? provider.DeleteAvatar("owned-avatar-signature"):await provider.DeleteAvatarAsync("owned-avatar-signature");
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);
        Assert.IsInstanceOfType(result.Exception,typeof(SolanaTransactionReconciliationException));
        Assert.AreEqual("owned-avatar-tombstone",((SolanaTransactionReconciliationException)result.Exception).OriginalTransactionId);
        Assert.AreEqual(1,repository.Deletes);Assert.AreEqual(3,repository.Reads);
        Assert.IsFalse(source.IsDeleted);Assert.AreEqual(4,source.Version);Assert.AreEqual(DateTime.MinValue,source.DeletedDate);
    }

    [DataTestMethod]
    [DataRow(false,"valid")] [DataRow(true,"valid")]
    [DataRow(false,"avatar-failure")] [DataRow(true,"avatar-failure")]
    [DataRow(false,"detail-readback")] [DataRow(true,"detail-readback")]
    [DataRow(false,"preflight")] [DataRow(true,"preflight")]
    public async Task AvatarPairDeletionPreflightsBothAndRetainsPartialReceipts(bool sync,string scenario)
    {
        var id=Guid.NewGuid();var repository=new RecordsRepository {AllowDelete=true};
        var avatar=new SolanaAvatarDto {Id=id,AvatarId=id,Version=3};repository.Records.Add(avatar);
        var detail=new SolanaAvatarDetailDto {Id=id,Version=8,XP=91};repository.Details.Add(detail);
        var cause=new IOException("owned pair failure");
        if(scenario=="preflight") repository.DetailCause=cause;
        var unknown=new SolanaTransactionReconciliationException("owned-uncertain-avatar",cause);
        if(scenario=="avatar-failure") repository.DeleteCause=unknown;
        if(scenario=="detail-readback") repository.DeleteReadScenario="detail-failure";
        var provider=new ActiveProvider(repository);
        var result=sync ? provider.DeleteAvatar("owned-avatar-signature"):await provider.DeleteAvatarAsync("owned-avatar-signature");
        Assert.AreEqual(scenario!="valid",result.IsError);Assert.AreEqual(scenario=="valid",result.Result);
        if(scenario=="preflight"){Assert.AreSame(cause,result.Exception);Assert.AreEqual(0,repository.Deletes);}
        else if(scenario=="avatar-failure")
        {
            Assert.IsInstanceOfType(result.Exception,typeof(SolanaPartialMutationException));
            CollectionAssert.AreEqual(new[] {"owned-detail-tombstone"},((SolanaPartialMutationException)result.Exception).CommittedSignatures.ToArray());
            Assert.AreSame(unknown,result.Exception.InnerException);Assert.AreEqual(2,repository.Deletes);
        }
        else if(scenario=="detail-readback")
        {
            Assert.AreEqual("owned-detail-tombstone",((SolanaTransactionReconciliationException)result.Exception).OriginalTransactionId);
            Assert.AreEqual(1,repository.Deletes);
        }
        else
        {
            Assert.IsTrue(result.IsDeleted);Assert.AreEqual(2,repository.Deletes);
            CollectionAssert.AreEqual(new[] {"owned-detail-signature-"+id,"owned-avatar-signature"},repository.TombstoneSources);
        }
        Assert.AreEqual(3,avatar.Version);Assert.AreEqual(8,detail.Version);Assert.AreEqual(91L,detail.XP);
        Assert.IsFalse(avatar.IsDeleted);Assert.IsFalse(detail.IsDeleted);
    }

    private sealed class ActiveProvider:SolanaOASIS
    {
        public ActiveProvider(ISolanaRepository repository):this(new Account(),repository) {}
        private ActiveProvider(Account account,ISolanaRepository repository):base("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key)
        {
            IsProviderActivated=true;
            typeof(SolanaOASIS).GetField("_solanaRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,repository);
        }
    }
    private sealed class RecordsRepository:ISolanaRepository
    {
        public List<SolanaAvatarDto> Records=new();public int Reads;public int LastVersion;public Exception? Cause;
        public List<SolanaAvatarDetailDto> Details=new();
        public List<SolanaHolonDto> Holons=new();public List<int> Versions=new();
        public bool AllowDelete,DeleteAccepted=true;public int Deletes;public Exception? DeleteCause;
        public string DeleteReadScenario="valid";
        public Exception? DetailCause;public List<string> TombstoneSources=new();
        public Task<IReadOnlyList<(T Record,string Signature)>> GetAllAsync<T>(int version=0) where T:SolanaBaseDto,new()
        {
            Reads++;LastVersion=version;Versions.Add(version);if(Cause!=null) throw Cause;
            if(typeof(T)==typeof(SolanaHolonDto))
                return Task.FromResult<IReadOnlyList<(T Record,string Signature)>>(Holons.Where(row=>version==0 || row.Version==version)
                    .Select(row=>((T)(SolanaBaseDto)row,"owned-signature-"+row.Id)).ToList());
            if(typeof(T)==typeof(SolanaAvatarDetailDto))
            {
                if(DetailCause!=null) throw DetailCause;
                return Task.FromResult<IReadOnlyList<(T Record,string Signature)>>(Details.Where(row=>version==0 || row.Version==version)
                    .Select(row=>((T)(SolanaBaseDto)row,"owned-detail-signature-"+row.Id)).ToList());
            }
            return Task.FromResult<IReadOnlyList<(T Record,string Signature)>>(Records.Where(row=>version==0 || row.Version==version).Select(row=>
                ((T)(SolanaBaseDto)Newtonsoft.Json.JsonConvert.DeserializeObject<SolanaAvatarDto>(Newtonsoft.Json.JsonConvert.SerializeObject(row))!,"owned-signature-"+row.Id)).ToList());
        }
        public Task<T> GetAsync<T>(string reference) where T:SolanaBaseDto,new()
        {
            Reads++;if(Cause!=null) throw Cause;
            if(reference=="owned-detail-tombstone")
            {
                if(DeleteReadScenario=="detail-failure") throw new IOException("owned detail readback failure");
                var source=Details.Single();
                return Task.FromResult((T)(SolanaBaseDto)new SolanaAvatarDetailDto {Id=source.Id,Version=source.Version+1,
                    PreviousVersionId=source.Id,IsDeleted=true,DeletedDate=new DateTime(2026,10,18,20,0,0,DateTimeKind.Utc),XP=source.XP});
            }
            if(reference=="owned-avatar-tombstone")
            {
                if(DeleteReadScenario=="failure") throw new IOException("owned avatar tombstone read failure");
                var source=Records.Single();
                return Task.FromResult((T)(SolanaBaseDto)new SolanaAvatarDto
                {
                    Id=source.Id,AvatarId=source.AvatarId,Version=source.Version+1,PreviousVersionId=source.Id,IsDeleted=true,
                    DeletedDate=DeleteReadScenario=="date" ? DateTime.MinValue:new DateTime(2026,10,18,20,0,0,DateTimeKind.Utc)
                });
            }
            Assert.IsTrue(reference=="owned-avatar-signature" || reference=="owned-signature-"+Records.Single().Id);
            return Task.FromResult((T)(SolanaBaseDto)Records.Single());
        }
        public Task<string> CreateAsync<T>(T entity) where T:SolanaBaseDto,new()=>throw new InvalidOperationException("Unexpected write");
        public Task<string> UpdateAsync<T>(T entity) where T:SolanaBaseDto,new()=>throw new InvalidOperationException("Unexpected write");
        public Task<bool> DeleteAsync(string reference)
        {
            if(!AllowDelete) throw new InvalidOperationException("Unexpected write");
            Deletes++;if(DeleteCause!=null) throw DeleteCause;
            return Task.FromResult(DeleteAccepted);
        }
        public Task<string> CreateTombstoneAsync(string reference)
        {
            if(!AllowDelete) throw new InvalidOperationException("Unexpected write");
            Deletes++;TombstoneSources.Add(reference);
            if(reference.StartsWith("owned-detail-signature-",StringComparison.Ordinal)) return Task.FromResult("owned-detail-tombstone");
            if(DeleteCause!=null) throw DeleteCause;
            if(!DeleteAccepted) throw new InvalidOperationException("Owned tombstone rejection");
            return Task.FromResult("owned-avatar-tombstone");
        }
    }
}
