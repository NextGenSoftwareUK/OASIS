using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaHolonAdapterTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MetadataAdaptersUseAuthenticatedValuesAndAllAnySemantics(bool synchronous)
    {
        var repository=new RecordsRepository();
        var first=new SolanaHolonDto {Id=Guid.NewGuid(),HolonType=HolonType.Holon,MetaData=new Dictionary<string,object> {{"parkType","Woodland"},{"count",2.5m}}};
        var second=new SolanaHolonDto {Id=Guid.NewGuid(),HolonType=HolonType.Holon,MetaData=new Dictionary<string,object> {{"parkType","Meadow"},{"count",9}}};
        repository.Records=new[] {(first,"first-signature"),(second,"second-signature")};
        var provider=new ActiveProvider(repository);
        var original=System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            var allPairs=new Dictionary<string,string> {{"parkType","Woodland"},{"count","2.5"}};
            var all=synchronous ? provider.LoadHolonsByMetaData(allPairs,MetaKeyValuePairMatchMode.All,loadChildren:false)
                : await provider.LoadHolonsByMetaDataAsync(allPairs,MetaKeyValuePairMatchMode.All,loadChildren:false);
            Assert.IsFalse(all.IsError,all.Message); Assert.AreEqual(first.Id,all.Result.Single().Id);
            Assert.AreEqual("first-signature",all.Result.Single().ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
            var anyPairs=new Dictionary<string,string> {{"parkType","Woodland"},{"count","9"}};
            var any=synchronous ? provider.LoadHolonsByMetaData(anyPairs,MetaKeyValuePairMatchMode.Any,loadChildren:false)
                : await provider.LoadHolonsByMetaDataAsync(anyPairs,MetaKeyValuePairMatchMode.Any,loadChildren:false);
            Assert.IsFalse(any.IsError,any.Message); Assert.AreEqual(2,any.Result.Count());
            var exact=synchronous ? provider.LoadHolonsByMetaData("parkType","woodland",loadChildren:false)
                : await provider.LoadHolonsByMetaDataAsync("parkType","woodland",loadChildren:false);
            Assert.IsFalse(exact.IsError,exact.Message); Assert.AreEqual(0,exact.Result.Count()); Assert.AreEqual(3,repository.Reads);
        }
        finally {System.Globalization.CultureInfo.CurrentCulture=original;}
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task HolonAdaptersSelectActualParentTypeAndIdentityWithGraphOptions(bool synchronous)
    {
        var repository=new RecordsRepository();
        var root=new SolanaHolonDto {Id=Guid.NewGuid(),HolonType=HolonType.Holon,Name="actual root"};
        var child=new SolanaHolonDto {Id=Guid.NewGuid(),ParentHolonId=root.Id,HolonType=HolonType.Holon,Name="actual child"};
        var leaf=new SolanaHolonDto {Id=Guid.NewGuid(),ParentHolonId=child.Id,HolonType=HolonType.Avatar,Name="actual leaf"};
        repository.Records=new[] {(root,"root-signature"),(child,"child-signature"),(leaf,"leaf-signature")};
        var provider=new ActiveProvider(repository);
        var all=synchronous ? provider.LoadAllHolons(HolonType.Holon,loadChildren:true,recursive:true,loadChildrenFromProvider:true)
            : await provider.LoadAllHolonsAsync(HolonType.Holon,loadChildren:true,recursive:true,loadChildrenFromProvider:true);
        Assert.IsFalse(all.IsError,all.Message); Assert.IsTrue(all.IsLoaded); Assert.AreEqual(2,all.Result.Count());
        var actualRoot=all.Result.Single(row=>row.Id==root.Id);
        Assert.AreEqual(child.Id,actualRoot.Children.Single().Id);
        Assert.AreEqual(leaf.Id,actualRoot.Children.Single().Children.Single().Id);
        Assert.AreEqual("root-signature",actualRoot.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
        var parents=synchronous ? provider.LoadHolonsForParent(root.Id,loadChildren:false,loadChildrenFromProvider:true)
            : await provider.LoadHolonsForParentAsync(root.Id,loadChildren:false,loadChildrenFromProvider:true);
        Assert.IsFalse(parents.IsError,parents.Message); Assert.AreEqual(child.Id,parents.Result.Single().Id); Assert.AreEqual(0,parents.Result.Single().Children.Count);
        var selected=synchronous ? provider.LoadHolon(leaf.Id,loadChildren:false) : await provider.LoadHolonAsync(leaf.Id,loadChildren:false);
        Assert.IsFalse(selected.IsError,selected.Message); Assert.AreEqual("actual leaf",selected.Result.Name);
        Assert.AreEqual("leaf-signature",selected.Result.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
        var bySignature=synchronous ? provider.LoadHolon("root-signature",loadChildren:true,recursive:true,maxChildDepth:1,loadChildrenFromProvider:true)
            : await provider.LoadHolonAsync("root-signature",loadChildren:true,recursive:true,maxChildDepth:1,loadChildrenFromProvider:true);
        Assert.IsFalse(bySignature.IsError,bySignature.Message);
        Assert.AreEqual(child.Id,bySignature.Result.Children.Single().Id);
        Assert.AreEqual(0,bySignature.Result.Children.Single().Children.Count);
        Assert.AreEqual("root-signature",bySignature.Result.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
        Assert.AreEqual(5,repository.Reads);
    }

    [TestMethod]
    public async Task InvalidDepthOrVersionFailsBeforeRepositoryRead()
    {
        var repository=new RecordsRepository(); var provider=new ActiveProvider(repository);
        Assert.IsTrue((await provider.LoadAllHolonsAsync(version:1)).IsError);
        Assert.IsTrue((await provider.LoadAllHolonsAsync(maxChildDepth:-1)).IsError);
        Assert.IsTrue((await provider.LoadAllHolonsAsync(curentChildDepth:-1)).IsError);
        Assert.IsTrue((await provider.LoadHolonAsync("signature",maxChildDepth:-1)).IsError);
        Assert.IsTrue((await provider.LoadHolonAsync("signature",version:1,loadChildrenFromProvider:true)).IsError);
        Assert.IsTrue((await provider.LoadHolonsByMetaDataAsync(" ","value")).IsError);
        Assert.IsTrue((await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>(),MetaKeyValuePairMatchMode.All)).IsError);
        Assert.IsTrue((await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string> {{"key","value"}},(MetaKeyValuePairMatchMode)999)).IsError);
        Assert.AreEqual(0,repository.Reads);
    }

    private sealed class ActiveProvider:SolanaOASIS
    {
        public ActiveProvider(ISolanaRepository repository):this(new Account(),repository) {}
        private ActiveProvider(Account account,ISolanaRepository repository):base("http://127.0.0.1:8899",account.PrivateKey.Key,account.PublicKey.Key)
        {
            IsProviderActivated=true;
            typeof(SolanaOASIS).GetField("_solanaRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,repository);
        }
    }

    private sealed class RecordsRepository:ISolanaRepository
    {
        public (SolanaHolonDto Record,string Signature)[] Records=Array.Empty<(SolanaHolonDto,string)>();
        public int Reads;
        public Task<IReadOnlyList<(T Record,string Signature)>> GetAllAsync<T>() where T:SolanaBaseDto,new()
        {
            Reads++;
            return Task.FromResult<IReadOnlyList<(T Record,string Signature)>>(Records.Select(entry=>((T)(SolanaBaseDto)entry.Record,entry.Signature)).ToList());
        }
        public Task<T> GetAsync<T>(string reference) where T:SolanaBaseDto,new()
        {
            Reads++; return Task.FromResult((T)(SolanaBaseDto)Records.Single(entry=>entry.Signature==reference).Record);
        }
        public Task<string> CreateAsync<T>(T entity) where T:SolanaBaseDto,new() => throw new InvalidOperationException("Unexpected write");
        public Task<string> UpdateAsync<T>(T entity) where T:SolanaBaseDto,new() => throw new InvalidOperationException("Unexpected write");
        public Task<bool> DeleteAsync(string reference) => throw new InvalidOperationException("Unexpected write");
        public Task<string> CreateTombstoneAsync(string reference) => throw new InvalidOperationException("Unexpected write");
    }
}
