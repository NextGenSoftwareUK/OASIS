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
        var historical=await Read(key,2);
        Assert.IsFalse(historical.IsError,historical.Message);Assert.AreEqual(2,historical.Result.Version);
        Assert.AreEqual(2,repository.LastVersion);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await Read(key,1)).ErrorCode);
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
            Assert.AreEqual("owned-signature-"+id,result.Result.Single().ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
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
        public Task<IReadOnlyList<(T Record,string Signature)>> GetAllAsync<T>(int version=0) where T:SolanaBaseDto,new()
        {
            Reads++;LastVersion=version;if(Cause!=null) throw Cause;
            if(typeof(T)==typeof(SolanaAvatarDetailDto))
                return Task.FromResult<IReadOnlyList<(T Record,string Signature)>>(Details.Where(row=>version==0 || row.Version==version)
                    .Select(row=>((T)(SolanaBaseDto)row,"owned-signature-"+row.Id)).ToList());
            return Task.FromResult<IReadOnlyList<(T Record,string Signature)>>(Records.Where(row=>version==0 || row.Version==version).Select(row=>
                ((T)(SolanaBaseDto)Newtonsoft.Json.JsonConvert.DeserializeObject<SolanaAvatarDto>(Newtonsoft.Json.JsonConvert.SerializeObject(row))!,"owned-signature-"+row.Id)).ToList());
        }
        public Task<T> GetAsync<T>(string reference) where T:SolanaBaseDto,new()
        {
            Reads++;if(Cause!=null) throw Cause;
            Assert.AreEqual("owned-avatar-signature",reference);
            return Task.FromResult((T)(SolanaBaseDto)Records.Single());
        }
        public Task<string> CreateAsync<T>(T entity) where T:SolanaBaseDto,new()=>throw new InvalidOperationException("Unexpected write");
        public Task<string> UpdateAsync<T>(T entity) where T:SolanaBaseDto,new()=>throw new InvalidOperationException("Unexpected write");
        public Task<bool> DeleteAsync(string reference)=>throw new InvalidOperationException("Unexpected write");
        public Task<string> CreateTombstoneAsync(string reference)=>throw new InvalidOperationException("Unexpected write");
    }
}
