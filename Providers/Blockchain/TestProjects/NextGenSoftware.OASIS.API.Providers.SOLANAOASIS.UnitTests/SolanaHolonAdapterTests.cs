using System.Reflection;
using NextGenSoftware.OASIS.API.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaHolonAdapterTests
{
    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(false,true)]
    [DataRow(true,false)]
    [DataRow(true,true)]
    public async Task ImportPreservesCommittedReceiptsAndStopsAfterFailure(bool synchronous,bool uncertain)
    {
        var cause=uncertain ? (Exception)new SolanaTransactionReconciliationException("owned-unknown-import-signature",new System.IO.IOException("owned lost reply"))
            :new System.IO.IOException("owned rejected import");
        var repository=new RecordsRepository {AllowPartialWrite=true,WriteCause=cause};
        var provider=new ActiveProvider(repository);
        var first=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
        var second=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
        var third=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
        var result=synchronous ? provider.Import(new[] {first,second,third}):await provider.ImportAsync(new[] {first,second,third});
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.Result); Assert.IsFalse(result.IsSaved);
        var failure=(SolanaPartialMutationException)result.Exception!;
        Assert.AreSame(cause,failure.InnerException); Assert.AreEqual("owned-first-committed-signature",failure.CommittedSignatures.Single());
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
        Assert.AreEqual(2,repository.WriteCalls); Assert.IsFalse(third.ProviderUniqueStorageKey.ContainsKey(ProviderType.SolanaOASIS));
    }

    [TestMethod]
    public async Task ImportPreflightsAllNodesAndIncludesSelectedChildren()
    {
        var repository=new RecordsRepository {AllowAllWrites=true}; var provider=new ActiveProvider(repository);
        Assert.IsTrue((await provider.ImportAsync(Array.Empty<IHolon>())).IsError);
        Assert.IsTrue((await provider.ImportAsync(new IHolon[] {new NextGenSoftware.OASIS.API.Core.Holons.Holon(),null!})).IsError);
        Assert.AreEqual(0,repository.WriteCalls);
        var root=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
        root.Children.Add(new NextGenSoftware.OASIS.API.Core.Holons.Holon());
        var imported=await provider.ImportAsync(new[] {root});
        Assert.IsFalse(imported.IsError,imported.Message); Assert.IsTrue(imported.Result); Assert.IsTrue(imported.IsSaved);
        Assert.AreEqual(2,repository.WriteCalls); Assert.AreEqual(root.Id,root.Children.Single().ParentHolonId);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ExportUsesAuthenticStoredIdentitiesAndOwnerScope(bool synchronous)
    {
        var owner=Guid.NewGuid(); var root=new SolanaHolonDto {Id=Guid.NewGuid(),Name="stored export root",CreatedByAvatarId=owner,HolonType=HolonType.Holon};
        var child=new SolanaHolonDto {Id=Guid.NewGuid(),Name="stored export child",ParentHolonId=root.Id,HolonType=HolonType.Holon};
        var linked=new SolanaHolonDto {Id=Guid.NewGuid(),Name="direct owner link",ParentHolonId=owner,HolonType=HolonType.Holon};
        var unrelated=new SolanaHolonDto {Id=Guid.NewGuid(),Name="unrelated",HolonType=HolonType.Holon};
        var repository=new RecordsRepository {Records=new[] {(root,"root-receipt"),(child,"child-receipt"),(linked,"linked-receipt"),(unrelated,"unrelated-receipt")}};
        var provider=new ActiveProvider(repository);
        Assert.IsTrue(provider.ExportAllDataForAvatarById(Guid.Empty).IsError);
        Assert.IsTrue(provider.ExportAll(version:-1).IsError);
        Assert.IsTrue((await provider.ExportAllDataForAvatarByUsernameAsync(" ")).IsError);
        Assert.IsTrue((await provider.ExportAllDataForAvatarByEmailAsync("email",version:-1)).IsError);
        Assert.AreEqual(0,repository.Reads);
        var owned=synchronous ? provider.ExportAllDataForAvatarById(owner):await provider.ExportAllDataForAvatarByIdAsync(owner);
        Assert.IsFalse(owned.IsError,owned.Message); Assert.IsTrue(owned.IsLoaded); Assert.AreEqual(2,owned.Result.Count());
        var loaded=owned.Result.Single(row=>row.Id==root.Id);
        Assert.AreEqual(child.Id,loaded.Children.Single().Id); Assert.AreEqual("root-receipt",loaded.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
        var all=synchronous ? provider.ExportAll():await provider.ExportAllAsync();
        Assert.IsFalse(all.IsError,all.Message); Assert.AreEqual(4,all.Result.Count()); Assert.AreEqual(2,repository.Reads);
    }

    [TestMethod]
    public async Task ExportRetainsRepositoryFailureInsteadOfReturningPartialSuccess()
    {
        var cause=new System.IO.IOException("owned authentic history failure");
        var repository=new RecordsRepository {ReadCause=cause}; var provider=new ActiveProvider(repository);
        var result=await provider.ExportAllAsync();
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.AreSame(cause,result.Exception);
    }

    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(false,true)]
    [DataRow(true,false)]
    [DataRow(true,true)]
    public async Task ContinuationSkipsDependentNodesAndStopsOnNativeUncertainty(bool continueOnError,bool uncertain)
    {
        var repository=new RecordsRepository {ContinueAfterOneFailure=true};
        if(uncertain) repository.WriteCause=new SolanaTransactionReconciliationException("owned-uncertain-signature",new System.IO.IOException("Owned lost reply"));
        var provider=new ActiveProvider(repository);
        var first=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
        var failed=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
        var dependent=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
        failed.Children=new List<IHolon> {dependent};
        var last=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
        var result=await provider.SaveHolonsAsync(new[] {first,failed,last},continueOnError:continueOnError);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved);
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
        var continues=continueOnError && !uncertain;
        Assert.AreEqual(continues ? 3:2,repository.WriteCalls);
        Assert.AreEqual(continues,last.ProviderUniqueStorageKey.ContainsKey(ProviderType.SolanaOASIS));
        Assert.IsFalse(dependent.ProviderUniqueStorageKey.ContainsKey(ProviderType.SolanaOASIS));
        Assert.IsFalse(failed.ProviderUniqueStorageKey.ContainsKey(ProviderType.SolanaOASIS));
        Assert.AreEqual(continues ? 2:1,((SolanaPartialMutationException)result.Exception!).CommittedSignatures.Count);
    }

    [DataTestMethod]
    [DataRow(false,0,3)]
    [DataRow(true,1,3)]
    [DataRow(true,2,5)]
    public async Task SelectedGraphUsesStableSiblingDepthAndAssignsParentIdentities(bool recursive,int depth,int expected)
    {
        var repository=new RecordsRepository {AllowAllWrites=true}; var provider=new ActiveProvider(repository);
        var root=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
        var first=new NextGenSoftware.OASIS.API.Core.Holons.Holon(); var second=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
        first.Children=new List<IHolon> {new NextGenSoftware.OASIS.API.Core.Holons.Holon()};
        second.Children=new List<IHolon> {new NextGenSoftware.OASIS.API.Core.Holons.Holon()};
        root.Children=new List<IHolon> {first,second};
        var result=await provider.SaveHolonAsync(root,recursive:recursive,maxChildDepth:depth);
        Assert.IsFalse(result.IsError,result.Message); Assert.IsTrue(result.IsSaved);
        Assert.AreEqual(expected,repository.WriteCalls); Assert.AreNotEqual(Guid.Empty,root.Id);
        Assert.AreEqual(root.Id,first.ParentHolonId); Assert.AreEqual(root.Id,second.ParentHolonId);
        Assert.AreEqual(expected,repository.Written.Select(record=>record.Id).Distinct().Count());
        if(expected==5) {Assert.AreEqual(first.Id,first.Children.Single().ParentHolonId); Assert.AreEqual(second.Id,second.Children.Single().ParentHolonId);}
    }

    [DataTestMethod]
    [DataRow("cycle")]
    [DataRow("duplicate")]
    [DataRow("parent")]
    [DataRow("enumeration")]
    [DataRow("reference")]
    public async Task EntireSelectedGraphIsValidatedBeforeAnyWrite(string scenario)
    {
        var repository=new RecordsRepository {AllowAllWrites=true}; var provider=new ActiveProvider(repository);
        var root=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
        var child=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=scenario=="duplicate" ? root.Id:Guid.NewGuid()};
        root.Children=new List<IHolon> {scenario=="cycle" ? root:child};
        if(scenario=="parent") child.ParentHolonId=Guid.NewGuid();
        if(scenario=="reference")
        {
            child.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]="old-signature";
            repository.Records=new[] {(new SolanaHolonDto {Id=Guid.NewGuid()},"old-signature")};
        }
        IEnumerable<IHolon> Source()
        {
            yield return root;
            if(scenario=="enumeration") throw new InvalidOperationException("Owned enumeration failure");
        }
        var result=await provider.SaveHolonsAsync(Source());
        Assert.IsTrue(result.IsError); Assert.AreEqual(0,repository.WriteCalls); Assert.AreEqual(scenario=="reference" ? 1:0,repository.Reads);
        Assert.IsFalse(root.ProviderUniqueStorageKey.ContainsKey(ProviderType.SolanaOASIS));
    }

    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(false,true)]
    [DataRow(true,false)]
    [DataRow(true,true)]
    public async Task PartialBatchRetainsCommittedSignatureAndRequiresReconciliation(bool synchronous,bool continueOnError)
    {
        var repository=new RecordsRepository {AllowPartialWrite=true};
        var provider=new ActiveProvider(repository);
        var first=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
        var second=new NextGenSoftware.OASIS.API.Core.Holons.Holon {Id=Guid.NewGuid()};
        var input=new[] {first,second};
        var result=synchronous ? provider.SaveHolons(input,saveChildren:false,continueOnError:continueOnError)
            : await provider.SaveHolonsAsync(input,saveChildren:false,continueOnError:continueOnError);
        Assert.IsTrue(result.IsError); Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
        var partial=result.Exception as SolanaPartialMutationException;
        Assert.IsNotNull(partial); Assert.AreEqual("owned-first-committed-signature",partial.CommittedSignatures.Single());
        Assert.AreSame(repository.WriteCause,partial.InnerException);
        Assert.AreEqual("owned-first-committed-signature",first.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
        Assert.IsFalse(second.ProviderUniqueStorageKey.ContainsKey(ProviderType.SolanaOASIS));
        Assert.AreEqual(2,repository.WriteCalls);
        var shouldFailOver=typeof(OASISHyperDrive).GetMethod("ShouldFailOver",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(IEnumerable<IHolon>));
        Assert.IsFalse((bool)shouldFailOver.Invoke(null,new object[] {new StorageOperationRequest {Operation="SaveHolons",Payload=input},result})!);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SearchUsesStoredHolonsAndSharedTextNumberDateGroups(bool synchronous)
    {
        var repository=new RecordsRepository();
        var first=new SolanaHolonDto {Id=Guid.NewGuid(),HolonType=HolonType.Holon,Name="forest guardian",Version=7,CreatedDate=new DateTime(2026,10,14,0,0,0,DateTimeKind.Utc)};
        var second=new SolanaHolonDto {Id=Guid.NewGuid(),HolonType=HolonType.Holon,Name="forest seed",Version=2,CreatedDate=new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc)};
        repository.Records=new[] {(first,"first-signature"),(second,"second-signature")};
        var provider=new ActiveProvider(repository);
        async Task<NextGenSoftware.OASIS.Common.OASISResult<ISearchResults>> Run(params ISearchGroupBase[] groups)
        {
            var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=groups.ToList()};
            return synchronous ? provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        }
        SearchTextGroup Text()=>new() {SearchQuery="forest",SearchHolons=true,HolonType=HolonType.All};
        var text=await Run(Text()); Assert.IsFalse(text.IsError,text.Message); Assert.AreEqual(2,text.Result.NumberOfResults);
        var combined=await Run(Text(),new SearchNumberGroup {Number=5,NumberOperator=SearchOperatorType.GreaterThan,PreviousSearchGroupOperator=SearchParamGroupOperator.And,SearchHolons=true,HolonType=HolonType.All});
        Assert.IsFalse(combined.IsError,combined.Message); Assert.AreEqual(first.Id,combined.Result.SearchResultHolons.Single().Id);
        var dates=await Run(new SearchDateGroup {Date=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc),DateOperator=SearchOperatorType.GreaterThan,SearchHolons=true,HolonType=HolonType.All});
        Assert.IsFalse(dates.IsError,dates.Message); Assert.AreEqual(first.Id,dates.Result.SearchResultHolons.Single().Id);
        Assert.AreEqual(3,repository.Reads);
    }

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
        Assert.IsTrue((await provider.LoadAllHolonsAsync(version:-1)).IsError);
        Assert.IsTrue((await provider.LoadAllHolonsAsync(maxChildDepth:-1)).IsError);
        Assert.IsTrue((await provider.LoadAllHolonsAsync(curentChildDepth:-1)).IsError);
        Assert.IsTrue((await provider.LoadHolonAsync("signature",maxChildDepth:-1)).IsError);
        Assert.IsTrue((await provider.LoadHolonAsync("signature",version:-1,loadChildrenFromProvider:true)).IsError);
        Assert.IsTrue((await provider.LoadHolonsByMetaDataAsync(" ","value")).IsError);
        Assert.IsTrue((await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>(),MetaKeyValuePairMatchMode.All)).IsError);
        Assert.IsTrue((await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string> {{"key","value"}},(MetaKeyValuePairMatchMode)999)).IsError);
        Assert.IsTrue((await provider.SearchAsync(new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase> {new SearchTextGroup {SearchQuery=" "}}})).IsError);
        Assert.AreEqual(0,repository.Reads);
    }

    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(true,false)]
    [DataRow(false,true)]
    [DataRow(true,true)]
    public async Task HistoricalRootsUseIndependentCurrentProviderChildren(bool synchronous,bool flat)
    {
        var rootId=Guid.NewGuid();
        var childId=Guid.NewGuid();
        var oldRoot=new SolanaHolonDto {Id=rootId,Version=2,Name="historical root"};
        var currentRoot=new SolanaHolonDto {Id=rootId,Version=7,Name="current root"};
        var child=new SolanaHolonDto {Id=childId,ParentHolonId=rootId,Version=11,Name="current child"};
        var repository=new RecordsRepository
        {
            HistoricalRecords=new[] {(oldRoot,"old-root-signature")},
            Records=new[] {(currentRoot,"new-root-signature"),(child,"child-signature")}
        };
        var provider=new ActiveProvider(repository);
        var result=synchronous
            ? provider.LoadAllHolons(loadChildren:!flat,loadChildrenFromProvider:true,version:2)
            : await provider.LoadAllHolonsAsync(loadChildren:!flat,loadChildrenFromProvider:true,version:2);
        Assert.IsFalse(result.IsError,result.Message);
        var root=result.Result.Single();
        Assert.AreEqual(2,root.Version);
        Assert.AreEqual("historical root",root.Name);
        Assert.AreEqual("old-root-signature",root.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
        Assert.AreEqual(flat ? 0:1,root.Children.Count);
        if(!flat){Assert.AreEqual(childId,root.Children.Single().Id);Assert.AreEqual(11,root.Children.Single().Version);}
        CollectionAssert.AreEqual(flat ? new[] {2}:new[] {2,0},repository.RequestedVersions);
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)]
    [DataRow(false,true)] [DataRow(true,true)]
    public async Task HistoricalSignatureGraphsValidateRootVersionBeforeLoadingCurrentChildren(bool sync,bool mismatch)
    {
        var root=new SolanaHolonDto {Id=Guid.NewGuid(),Version=2,Name="historical signature root"};
        var child=new SolanaHolonDto {Id=Guid.NewGuid(),ParentHolonId=root.Id,Version=19,Name="independent current child"};
        var repository=new RecordsRepository {Records=new[] {(root,"historical-root"),(child,"current-child")}};
        var provider=new ActiveProvider(repository);
        var result=sync ? provider.LoadHolon("historical-root",loadChildrenFromProvider:true,version:mismatch ? 3:2)
            : await provider.LoadHolonAsync("historical-root",loadChildrenFromProvider:true,version:mismatch ? 3:2);
        Assert.AreEqual(mismatch,result.IsError,result.Message);
        if(mismatch)
        {
            Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(KeyNotFoundException));
            Assert.AreEqual(1,repository.Reads);Assert.AreEqual(0,repository.RequestedVersions.Count);
        }
        else
        {
            Assert.IsTrue(result.IsLoaded);Assert.AreEqual(2,result.Result.Version);
            Assert.AreEqual("historical-root",result.Result.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
            Assert.AreEqual(19,result.Result.Children.Single().Version);
            Assert.AreEqual("current-child",result.Result.Children.Single().ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
            CollectionAssert.AreEqual(new[] {0},repository.RequestedVersions);Assert.AreEqual(2,repository.Reads);
        }
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)]
    [DataRow(false,true)] [DataRow(true,true)]
    public async Task DeleteHolonDoesNotMutateRepositoryOwnedRecordBeforeConfirmedTombstone(bool sync,bool fail)
    {
        var stored=new SolanaHolonDto {Id=Guid.NewGuid(),Version=5,PreviousVersionId=Guid.NewGuid(),Name="owned source"};
        var previous=stored.PreviousVersionId;
        var repository=new RecordsRepository {Records=new[] {(stored,"owned-source")},AllowTombstone=true};
        var cause=new SolanaTransactionReconciliationException("owned-pending-tombstone",new System.IO.IOException("owned lost reply"));
        if(fail) repository.TombstoneCause=cause;
        var provider=new ActiveProvider(repository);
        var result=sync ? provider.DeleteHolon("owned-source"):await provider.DeleteHolonAsync("owned-source");
        Assert.AreEqual(5,stored.Version);Assert.IsFalse(stored.IsDeleted);Assert.AreEqual(previous,stored.PreviousVersionId);
        Assert.AreEqual(fail,result.IsError);
        if(fail){Assert.IsNull(result.Result);Assert.IsFalse(result.IsDeleted);Assert.AreSame(cause,result.Exception);}
        else
        {
            Assert.IsTrue(result.IsDeleted);Assert.AreEqual(1,result.DeletedCount);
            Assert.AreEqual(6,result.Result.Version);Assert.AreEqual(stored.Id,result.Result.PreviousVersionId);
            Assert.IsTrue(result.Result.IsDeleted);Assert.AreEqual(repository.TombstoneDate,result.Result.DeletedDate);
            Assert.AreEqual("owned-tombstone",result.Result.ProviderUniqueStorageKey[ProviderType.SolanaOASIS]);
        }
        Assert.AreEqual(1,repository.TombstoneCalls);
    }

    [DataTestMethod]
    [DataRow(false,"failure")] [DataRow(true,"failure")]
    [DataRow(false,"identity")] [DataRow(true,"identity")]
    [DataRow(false,"date")] [DataRow(true,"date")]
    public async Task TombstoneReadbackFailureRetainsReceiptAndNeverClaimsDeletion(bool sync,string scenario)
    {
        var source=new SolanaHolonDto {Id=Guid.NewGuid(),Version=3};
        var repository=new RecordsRepository {Records=new[] {(source,"owned-source")},AllowTombstone=true,TombstoneReadScenario=scenario};
        var provider=new ActiveProvider(repository);
        var result=sync ? provider.DeleteHolon("owned-source"):await provider.DeleteHolonAsync("owned-source");
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);Assert.IsNull(result.Result);
        Assert.IsInstanceOfType(result.Exception,typeof(SolanaTransactionReconciliationException));
        Assert.AreEqual("owned-tombstone",((SolanaTransactionReconciliationException)result.Exception).OriginalTransactionId);
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
        Assert.AreEqual(scenario=="failure" ? typeof(IOException):typeof(System.IO.InvalidDataException),result.Exception.InnerException!.GetType());
        Assert.AreEqual(1,repository.TombstoneCalls);Assert.AreEqual(2,repository.Reads);
        Assert.AreEqual(3,source.Version);Assert.IsFalse(source.IsDeleted);Assert.AreEqual(DateTime.MinValue,source.DeletedDate);
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
        public (SolanaHolonDto Record,string Signature)[] HistoricalRecords=Array.Empty<(SolanaHolonDto,string)>();
        public List<int> RequestedVersions=new();
        public int Reads;
        public Exception? ReadCause;
        public bool AllowPartialWrite;
        public bool AllowAllWrites;
        public bool AllowTombstone;public int TombstoneCalls;public Exception? TombstoneCause;
        public DateTime TombstoneDate=new(2026,10,18,19,30,0,DateTimeKind.Utc);
        public string TombstoneReadScenario="valid";
        public bool ContinueAfterOneFailure;
        public List<SolanaBaseDto> Written=new();
        public int WriteCalls;
        public Exception WriteCause=new System.IO.IOException("Owned second write rejection");
        public Task<IReadOnlyList<(T Record,string Signature)>> GetAllAsync<T>(int version=0) where T:SolanaBaseDto,new()
        {
            Reads++;
            RequestedVersions.Add(version);
            if(ReadCause!=null) throw ReadCause;
            return Task.FromResult<IReadOnlyList<(T Record,string Signature)>>((version==0 ? Records:HistoricalRecords).Select(entry=>((T)(SolanaBaseDto)entry.Record,entry.Signature)).ToList());
        }
        public Task<T> GetAsync<T>(string reference) where T:SolanaBaseDto,new()
        {
            Reads++;
            if(reference=="owned-tombstone" && AllowTombstone)
            {
                if(TombstoneReadScenario=="failure") throw new IOException("owned tombstone readback failure");
                var original=Records.Single().Record;
                return Task.FromResult((T)(SolanaBaseDto)new SolanaHolonDto
                {
                    Id=TombstoneReadScenario=="identity" ? Guid.NewGuid():original.Id,
                    Version=original.Version+1,PreviousVersionId=original.Id,IsDeleted=true,
                    DeletedDate=TombstoneReadScenario=="date" ? DateTime.MinValue:TombstoneDate
                });
            }
            return Task.FromResult((T)(SolanaBaseDto)Records.Single(entry=>entry.Signature==reference).Record);
        }
        public Task<string> CreateAsync<T>(T entity) where T:SolanaBaseDto,new()
        {
            if(!AllowPartialWrite && !AllowAllWrites && !ContinueAfterOneFailure) throw new InvalidOperationException("Unexpected write");
            WriteCalls++;
            Written.Add(entity);
            if(ContinueAfterOneFailure)
            {
                if(WriteCalls==2) throw WriteCause;
                return Task.FromResult("owned-committed-"+WriteCalls);
            }
            if(AllowAllWrites) return Task.FromResult("owned-committed-"+WriteCalls);
            if(WriteCalls==1) return Task.FromResult("owned-first-committed-signature");
            throw WriteCause;
        }
        public Task<string> UpdateAsync<T>(T entity) where T:SolanaBaseDto,new() => throw new InvalidOperationException("Unexpected write");
        public Task<bool> DeleteAsync(string reference) => throw new InvalidOperationException("Unexpected write");
        public Task<string> CreateTombstoneAsync(string reference)
        {
            if(!AllowTombstone) throw new InvalidOperationException("Unexpected write");
            TombstoneCalls++;Assert.AreEqual("owned-source",reference);
            if(TombstoneCause!=null) throw TombstoneCause;
            return Task.FromResult("owned-tombstone");
        }
    }
}
