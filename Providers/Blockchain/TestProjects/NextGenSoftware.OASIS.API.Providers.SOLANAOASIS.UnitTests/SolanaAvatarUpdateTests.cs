using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

// Boundary tests only: captures the DTO passed to storage, not native-chain execution.
[TestClass]
public class SolanaAvatarUpdateTests
{
    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task AcknowledgedProfileRevisionIsPublishedToCaller(bool detail,bool synchronous)
    {
        var account=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key);
        IHolonBase source=detail?new AvatarDetail():new Avatar();source.Id=Guid.NewGuid();source.Version=1;
        source.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]="old-signature";
        SolanaBaseDto persisted=detail?new SolanaAvatarDetailDto():new SolanaAvatarDto();persisted.Id=source.Id;persisted.Version=5;
        var repository=new CapturingRepository{Existing=persisted,AdvanceVersion=true};
        typeof(SolanaOASIS).GetField("_solanaRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,repository);
        typeof(SolanaOASIS).GetProperty("IsProviderActivated")!.SetValue(provider,true);
        Assert.IsFalse(await SaveOriginCase(provider,source,detail?"detail":"avatar",synchronous));
        Assert.AreEqual(6,source.Version);Assert.AreEqual(source.Id,source.PreviousVersionId);
        Assert.AreEqual("new-signature",source.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
    }

    [DataTestMethod]
    [DataRow("avatar",false)][DataRow("avatar",true)][DataRow("detail",false)][DataRow("detail",true)]
    [DataRow("holon",false)][DataRow("holon",true)][DataRow("bulk",false)][DataRow("bulk",true)]
    public async Task SuccessfulUpdateAcknowledgesExactlyOneLogicalRecord(string kind,bool synchronous)
    {
        var account=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key);
        IHolonBase source=kind switch {"avatar"=>new Avatar(),"detail"=>new AvatarDetail(),_=>new Holon()};
        source.Id=Guid.NewGuid();source.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]="old-signature";
        SolanaBaseDto existing=kind switch {"avatar"=>new SolanaAvatarDto(),"detail"=>new SolanaAvatarDetailDto(),_=>new SolanaHolonDto()};
        existing.Id=source.Id;existing.Version=5;
        var repository=new CapturingRepository{Existing=existing};
        typeof(SolanaOASIS).GetField("_solanaRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,repository);
        typeof(SolanaOASIS).GetProperty("IsProviderActivated")!.SetValue(provider,true);
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.AreEqual(1,result.SavedCount);}
        if(kind=="avatar")Check(synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source));
        else if(kind=="detail")Check(synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source));
        else if(kind=="bulk")Check(synchronous?provider.SaveHolons(new[]{(IHolon)source},saveChildren:false):await provider.SaveHolonsAsync(new[]{(IHolon)source},saveChildren:false));
        else Check(synchronous?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false));
    }

    private static async Task<bool> SaveOriginCase(SolanaOASIS provider,IHolonBase source,string kind,bool synchronous) => kind switch {
        "avatar"=>synchronous ? provider.SaveAvatar((IAvatar)source).IsError:(await provider.SaveAvatarAsync((IAvatar)source)).IsError,
        "detail"=>synchronous ? provider.SaveAvatarDetail((IAvatarDetail)source).IsError:(await provider.SaveAvatarDetailAsync((IAvatarDetail)source)).IsError,
        "bulk"=>synchronous ? provider.SaveHolons(new[] {(IHolon)source},saveChildren:false).IsError:(await provider.SaveHolonsAsync(new[] {(IHolon)source},saveChildren:false)).IsError,
        _=>synchronous ? provider.SaveHolon((IHolon)source,saveChildren:false).IsError:(await provider.SaveHolonAsync((IHolon)source,saveChildren:false)).IsError};

    [DataTestMethod]
    [DataRow("avatar",true)] [DataRow("avatar",false)]
    [DataRow("detail",true)] [DataRow("detail",false)]
    [DataRow("holon",true)] [DataRow("holon",false)]
    [DataRow("bulk",true)] [DataRow("bulk",false)]
    public async Task FailedUpdateDoesNotPublishStagedOrigin(string kind,bool synchronous)
    {
        var account=new Account();var provider=new SolanaOASIS("http://127.0.0.1:8899",account.PrivateKey.Key,account.PublicKey.Key);
        IHolonBase source=kind switch {"avatar"=>new Avatar(),"detail"=>new AvatarDetail(),_=>new Holon()};
        source.Id=Guid.NewGuid();source.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]="old-signature";
        source.CreatedProviderType=new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);source.CreatedOASISType=new EnumValue<OASISType>(OASISType.STARCLI);
        SolanaBaseDto existing=kind switch {"avatar"=>new SolanaAvatarDto(),"detail"=>new SolanaAvatarDetailDto(),_=>new SolanaHolonDto()};
        existing.Id=source.Id;existing.Version=5;existing.CreatedProviderType=ProviderType.EthereumOASIS;existing.CreatedOASISType=OASISType.OurWorld;
        var repository=new CapturingRepository {Existing=existing,UpdateFailure=new IOException("controlled storage failure")};
        typeof(SolanaOASIS).GetField("_solanaRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,repository);
        typeof(SolanaOASIS).GetProperty("IsProviderActivated")!.SetValue(provider,true);
        Assert.IsTrue(await SaveOriginCase(provider,source,kind,synchronous));Assert.IsNotNull(repository.Updated);
        Assert.AreEqual(ProviderType.EthereumOASIS,repository.Updated.CreatedProviderType);
        Assert.AreEqual(OASISType.OurWorld,repository.Updated.CreatedOASISType);
        Assert.AreEqual(ProviderType.MongoDBOASIS,source.CreatedProviderType.Value);Assert.AreEqual(OASISType.STARCLI,source.CreatedOASISType.Value);
        Assert.AreEqual("old-signature",source.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
    }

    public static IEnumerable<object[]> CreationOriginCases()
    {
        foreach(var kind in new[] {"avatar","detail","holon","bulk"})
        foreach(var synchronous in new[] {true,false})
        foreach(var conflict in new[] {true,false})
        foreach(var known in new[] {true,false}) yield return new object[] {kind,synchronous,conflict,known};
    }

    [DataTestMethod]
    [DynamicData(nameof(CreationOriginCases),DynamicDataSourceType.Method)]
    public async Task UpdatesPreserveStoredCreationOrigin(string kind,bool synchronous,bool conflict,bool known)
    {
        var account=new Account();var provider=new SolanaOASIS("http://127.0.0.1:8899",account.PrivateKey.Key,account.PublicKey.Key);
        IHolonBase source=kind switch {"avatar"=>new Avatar(),"detail"=>new AvatarDetail(),_=>new Holon()};
        source.Id=Guid.NewGuid();source.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]="old-signature";
        if(conflict)
        {
            source.CreatedProviderType=new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
            source.CreatedOASISType=new EnumValue<OASISType>(OASISType.STARCLI);
        }
        SolanaBaseDto existing=kind switch {"avatar"=>new SolanaAvatarDto(),"detail"=>new SolanaAvatarDetailDto(),_=>new SolanaHolonDto()};
        existing.Id=source.Id;existing.Version=5;
        existing.CreatedProviderType=known ? ProviderType.EthereumOASIS:null;existing.CreatedOASISType=known ? OASISType.OurWorld:null;
        var repository=new CapturingRepository {Existing=existing};
        typeof(SolanaOASIS).GetField("_solanaRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,repository);
        typeof(SolanaOASIS).GetProperty("IsProviderActivated")!.SetValue(provider,true);
        var error=await SaveOriginCase(provider,source,kind,synchronous);
        Assert.IsFalse(error);Assert.IsNotNull(repository.Updated);
        Assert.AreEqual(existing.CreatedProviderType,repository.Updated.CreatedProviderType);
        Assert.AreEqual(existing.CreatedOASISType,repository.Updated.CreatedOASISType);
        Assert.AreEqual(existing.CreatedProviderType,source.CreatedProviderType?.Value);
        Assert.AreEqual(existing.CreatedOASISType,source.CreatedOASISType?.Value);
        Assert.AreEqual("new-signature",source.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
    }

    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public async Task UpdatesUseCurrentPayloadAndRejectMismatchedReferences(bool detail, bool mismatch)
    {
        var account = new Account();
        var provider = new SolanaOASIS("http://127.0.0.1:8899", account.PrivateKey.Key, account.PublicKey.Key);
        var identity = Guid.NewGuid();
        var existingId = mismatch ? Guid.NewGuid() : identity;
        var repository = new CapturingRepository { Existing = detail
            ? new SolanaAvatarDetailDto { Id = existingId, Version = 5, Karma = 1 }
            : new SolanaAvatarDto { Id = existingId, Version = 5, Email = "old@example.invalid" } };
        typeof(SolanaOASIS).GetField("_solanaRepository", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, repository);
        typeof(SolanaOASIS).GetProperty("IsProviderActivated")!.SetValue(provider,true);
        if (detail)
        {
            var avatar = new AvatarDetail { Id = identity, Karma = 999, XP = 321, Version = 1 };
            avatar.ProviderUniqueStorageKey[ProviderType.SolanaOASIS] = "old-signature";
            var result = await provider.SaveAvatarDetailAsync(avatar);
            Assert.AreEqual(mismatch, result.IsError);
            if (!mismatch)
            {
                var submitted = (SolanaAvatarDetailDto)repository.Updated!;
                Assert.AreEqual(avatar.Karma, submitted.Karma);
                Assert.AreEqual(avatar.XP, submitted.XP);
                Assert.AreEqual("new-signature", avatar.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
            }
        }
        else
        {
            var avatar = new Avatar { Id = identity, Email = "new@example.invalid", Version = 1 };
            avatar.ProviderUniqueStorageKey[ProviderType.SolanaOASIS] = "old-signature";
            var result = await provider.SaveAvatarAsync(avatar);
            Assert.AreEqual(mismatch, result.IsError);
            if (!mismatch)
                Assert.AreEqual(avatar.Email, ((SolanaAvatarDto)repository.Updated!).Email);
        }
        if (mismatch) Assert.IsNull(repository.Updated);
        else
        {
            Assert.AreEqual(identity, repository.Updated!.Id);
            Assert.AreEqual(5, repository.Updated.Version, "repository owns incrementing persisted version");
        }
    }

    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public async Task HolonUpdatesUseHolonDtoAndCurrentFields(bool bulk, bool mismatch)
    {
        var account = new Account();
        var provider = new SolanaOASIS("http://127.0.0.1:8899", account.PrivateKey.Key, account.PublicKey.Key);
        var holon = new Holon { Id = Guid.NewGuid(), Name = "updated synthetic holon", Description = "changed", Version = 1 };
        holon.ProviderUniqueStorageKey[ProviderType.SolanaOASIS] = "old-signature";
        var repository = new CapturingRepository { Existing = new SolanaHolonDto {
            Id = mismatch ? Guid.NewGuid() : holon.Id, Name = "old", Version = 8 } };
        typeof(SolanaOASIS).GetField("_solanaRepository", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, repository);
        typeof(SolanaOASIS).GetProperty("IsProviderActivated")!.SetValue(provider,true);
        var isError = bulk
            ? (await provider.SaveHolonsAsync(new[] { holon }, saveChildren:false)).IsError
            : (await provider.SaveHolonAsync(holon, saveChildren:false)).IsError;
        Assert.AreEqual(mismatch, isError);
        if (mismatch) Assert.IsNull(repository.Updated);
        else
        {
            var submitted = (SolanaHolonDto)repository.Updated!;
            Assert.AreEqual(holon.Id, submitted.Id);
            Assert.AreEqual(holon.Name, submitted.Name);
            Assert.AreEqual(holon.Description, submitted.Description);
            Assert.AreEqual(8, submitted.Version);
        }
    }

    private sealed class CapturingRepository : ISolanaRepository
    {
        public SolanaBaseDto Existing = null!;
        public SolanaBaseDto? Updated;
        public Exception? UpdateFailure;
        public bool AdvanceVersion;
        public Task<T> GetAsync<T>(string reference) where T : SolanaBaseDto, new() => Task.FromResult((T)Existing);
        public Task<IReadOnlyList<(T Record,string Signature)>> GetAllAsync<T>(int version=0) where T:SolanaBaseDto,new() => throw new InvalidOperationException("Unexpected collection read");
        public Task<string> UpdateAsync<T>(T entity) where T : SolanaBaseDto, new() { Updated = entity; if(UpdateFailure!=null) throw UpdateFailure; if(AdvanceVersion){entity.Version=checked(entity.Version+1);entity.PreviousVersionId=entity.Id;} return Task.FromResult("new-signature"); }
        public Task<string> CreateAsync<T>(T entity) where T : SolanaBaseDto, new() => throw new InvalidOperationException("Unexpected create");
        public Task<bool> DeleteAsync(string reference) => throw new InvalidOperationException("Unexpected delete");
        public Task<string> CreateTombstoneAsync(string reference) => throw new InvalidOperationException("Unexpected tombstone");
    }
}
