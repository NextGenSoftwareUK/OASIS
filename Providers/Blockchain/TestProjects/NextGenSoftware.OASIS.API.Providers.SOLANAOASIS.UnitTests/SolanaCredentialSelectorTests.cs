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
        foreach(var input in new[] {(" ",0),(key,-1),(key,1)})
        {
            var invalid=await Read(input.Item1,input.Item2);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",invalid.ErrorCode);Assert.IsNull(invalid.Result);
            if(input.Item2==1) Assert.IsInstanceOfType(invalid.Exception,typeof(NotSupportedException));
            else Assert.IsInstanceOfType(invalid.Exception,typeof(ArgumentException));
        }
        Assert.AreEqual(0,repository.Reads);
        var first=Create("first");repository.Records.Add(first.GetSolanaAvatarDto());
        var loaded=await Read(key);Assert.IsFalse(loaded.IsError,loaded.Message);Assert.IsTrue(loaded.IsLoaded);Assert.AreEqual(first.Id,loaded.Result.Id);
        Assert.AreEqual(first.ResetTokenExpires,loaded.Result.ResetTokenExpires);Assert.AreEqual(first.RefreshToken,loaded.Result.RefreshToken);
        Assert.AreEqual(first.ProviderWallets[ProviderType.SolanaOASIS][0].Id,loaded.Result.ProviderWallets[ProviderType.SolanaOASIS][0].Id);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await Read(key+"missing")).ErrorCode);
        repository.Records.Add(Create("duplicate").GetSolanaAvatarDto());
        var duplicate=await Read(key);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,duplicate.ErrorCode);Assert.IsNull(duplicate.Result);
        repository.Records.RemoveAt(1);
        var cause=new System.IO.IOException("owned finalized collection failure");repository.Cause=cause;
        var failed=await Read(key);Assert.IsTrue(failed.IsError);Assert.AreSame(cause,failed.Exception);Assert.IsNull(failed.Result);
        repository.Cause=null;repository.Records[0].ProviderWalletsJson="{invalid";
        var corrupt=await Read(key);Assert.IsTrue(corrupt.IsError);Assert.IsInstanceOfType(corrupt.Exception,typeof(System.IO.InvalidDataException));Assert.IsNull(corrupt.Result);
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
        public List<SolanaAvatarDto> Records=new();public int Reads;public Exception? Cause;
        public Task<IReadOnlyList<(T Record,string Signature)>> GetAllAsync<T>(int version=0) where T:SolanaBaseDto,new()
        {
            Reads++;if(Cause!=null) throw Cause;
            return Task.FromResult<IReadOnlyList<(T Record,string Signature)>>(Records.Select(row=>
                ((T)(SolanaBaseDto)Newtonsoft.Json.JsonConvert.DeserializeObject<SolanaAvatarDto>(Newtonsoft.Json.JsonConvert.SerializeObject(row))!,"owned-signature-"+row.Id)).ToList());
        }
        public Task<T> GetAsync<T>(string reference) where T:SolanaBaseDto,new()=>throw new InvalidOperationException("Unexpected point read");
        public Task<string> CreateAsync<T>(T entity) where T:SolanaBaseDto,new()=>throw new InvalidOperationException("Unexpected write");
        public Task<string> UpdateAsync<T>(T entity) where T:SolanaBaseDto,new()=>throw new InvalidOperationException("Unexpected write");
        public Task<bool> DeleteAsync(string reference)=>throw new InvalidOperationException("Unexpected write");
        public Task<string> CreateTombstoneAsync(string reference)=>throw new InvalidOperationException("Unexpected write");
    }
}
