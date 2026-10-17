using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredWalletSearchTests
{
    private static Avatar Record() => new() {Id=Guid.NewGuid(),Username="unselected-avatar-name",Version=987,
        ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {
            WalletId=Guid.Parse("b51f1188-7743-4dc3-8d68-75987bbdd211"),PublicKey="selected-public-key",WalletAddress="selected-address",
            PrivateKey="hidden-private",SecretRecoveryPhrase="hidden-recovery",ProviderType=ProviderType.SolanaOASIS,
            Balance=123,IsDefaultWallet=true,Transactions=new() {new NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response.WalletTransaction {
                FromWalletAddress="selected-from",ToWalletAddress="selected-to",Amount=77,Description="unselected-description",
                Token="selected-token",MemoText="selected-memo",ProviderType=ProviderType.SuiOASIS}}
        }}}}};

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task PersistedWalletMasksRespectGroupCombinationAndOwnerScope(bool synchronous)
    {
        var provider=new TestProvider(new InMemoryBackend());var row=Record();row.Email="owned-wallet@example.test";
        var owner=Guid.NewGuid();row.CreatedByAvatarId=owner;
        Assert.IsFalse((await provider.SaveAvatarAsync(row)).IsError);
        var group=new SearchTextGroup {SearchAvatars=true,HolonType=HolonType.All,SearchQuery="selected-to",
            AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=new SearchAvatarWalletParams {
                SearchAvatarWalletTransactionParams=new SearchAvatarWalletTransactionParams {ToWalletAddress=true}}}};
        var query=new SearchParams {AvatarId=owner,SearchOnlyForCurrentAvatar=true,SearchGroups=new() {group,
            new SearchNumberGroup {SearchAvatars=true,HolonType=HolonType.All,Number=123,NumberOperator=SearchOperatorType.EqualTo,
                PreviousSearchGroupOperator=SearchParamGroupOperator.And,AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=new SearchAvatarWalletParams {Balance=true}}}}};
        var found=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(found.IsError,found.Message);Assert.AreEqual(1,found.Result.NumberOfResults);Assert.AreEqual(row.Id,found.Result.SearchResultAvatars[0].Id);
        foreach(var field in new[]{"Token","MemoText"}){
            var mask=new SearchAvatarWalletTransactionParams();mask.GetType().GetProperty(field)!.SetValue(mask,true);
            group.AvatarSearchParams.SearchAvatarWalletParams=new SearchAvatarWalletParams {SearchAvatarWalletTransactionParams=mask};
            group.SearchQuery=field=="Token"?"selected-token":"selected-memo";
            var metadata=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
            Assert.IsFalse(metadata.IsError,metadata.Message);Assert.AreEqual(1,metadata.Result.NumberOfResults);
        }
        query.AvatarId=Guid.NewGuid();
        var foreign=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(foreign.IsError,foreign.Message);Assert.AreEqual(0,foreign.Result.NumberOfResults);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task EmptyWalletMaskRejectsBeforeBackendAccess(bool synchronous)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {new SearchTextGroup {
            SearchAvatars=true,SearchQuery="anything",AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=new SearchAvatarWalletParams {
                SearchAvatarWalletTransactionParams=new SearchAvatarWalletTransactionParams()}}}}};
        var rejected=synchronous?provider.Search(query):await provider.SearchAsync(query);
        Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType<ArgumentException>(rejected.Exception);Assert.AreEqual(0,backend.ListCalls);
    }

    [TestMethod]
    public void ProviderTypeUsesStoredEnumValueAndFractionalBalancesAreNotRounded()
    {
        var row=Record();
        var group=new SearchNumberGroup {SearchAvatars=true,HolonType=HolonType.All,Number=(int)ProviderType.SolanaOASIS,
            NumberOperator=SearchOperatorType.EqualTo,AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=new SearchAvatarWalletParams {ProviderType=true}}};
        Assert.IsTrue(StoredRecordSearch.MatchesSearchFields(row,true,group));
        group.AvatarSearchParams.SearchAvatarWalletParams=new SearchAvatarWalletParams {Balance=true};
        row.ProviderWallets[ProviderType.SolanaOASIS][0].Balance=123.5;group.Number=123;group.NumberOperator=SearchOperatorType.GreaterThan;
        Assert.IsTrue(StoredRecordSearch.MatchesSearchFields(row,true,group));
        group.NumberOperator=SearchOperatorType.EqualTo;Assert.IsFalse(StoredRecordSearch.MatchesSearchFields(row,true,group));
    }

    [TestMethod]
    public void AbsentTransactionFieldsDoNotAliasDescriptionOrWalletProvider()
    {
        var group=new SearchTextGroup {SearchAvatars=true,SearchQuery="unselected-description",HolonType=HolonType.All,
            AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=new SearchAvatarWalletParams {
                SearchAvatarWalletTransactionParams=new SearchAvatarWalletTransactionParams {MemoText=true,Token=true,ProviderType=true}}}};
        Assert.IsFalse(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
    }

    [DataTestMethod]
    [DataRow("Token","selected-token")] [DataRow("MemoText","selected-memo")]
    public void TransactionMetadataMasksUseCanonicalFields(string field,string text)
    {
        var mask=new SearchAvatarWalletTransactionParams();mask.GetType().GetProperty(field)!.SetValue(mask,true);
        var group=new SearchTextGroup {SearchAvatars=true,HolonType=HolonType.All,SearchQuery=text,
            AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=new SearchAvatarWalletParams {SearchAvatarWalletTransactionParams=mask}}};
        Assert.IsTrue(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
        group.SearchQuery="unselected-description";Assert.IsFalse(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
    }

    [TestMethod]
    public void TransactionProviderIsNotInferredFromTheContainingWallet()
    {
        var group=new SearchNumberGroup {SearchAvatars=true,HolonType=HolonType.All,Number=(int)ProviderType.SuiOASIS,
            NumberOperator=SearchOperatorType.EqualTo,AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=new SearchAvatarWalletParams {
                SearchAvatarWalletTransactionParams=new SearchAvatarWalletTransactionParams {ProviderType=true}}}};
        Assert.IsTrue(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
        group.Number=(int)ProviderType.SolanaOASIS;Assert.IsFalse(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
    }

    [DataTestMethod]
    [DataRow("WalletId","b51f1188-7743-4dc3-8d68-75987bbdd211")]
    [DataRow("PublicKey","selected-public-key")]
    [DataRow("WalletAddress","selected-address")]
    [DataRow("IsDefaultWallet","True")]
    public void ExplicitWalletTextMaskSelectsOnlyRequestedPublicField(string field,string query)
    {
        var mask=new SearchAvatarWalletParams();mask.GetType().GetProperty(field)!.SetValue(mask,true);
        var group=new SearchTextGroup {SearchAvatars=true,HolonType=HolonType.All,SearchQuery=query,
            AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=mask}};
        Assert.IsTrue(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
        foreach(var excluded in new[]{"unselected-avatar-name","hidden-private","hidden-recovery","unselected-description"}){
            group.SearchQuery=excluded;Assert.IsFalse(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
        }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void WalletAndTransactionNumberMasksDoNotFallBackToAvatarVersion(bool transaction)
    {
        var mask=transaction?new SearchAvatarWalletParams {SearchAvatarWalletTransactionParams=new SearchAvatarWalletTransactionParams {Amount=true}}
            :new SearchAvatarWalletParams {Balance=true};
        var group=new SearchNumberGroup {SearchAvatars=true,HolonType=HolonType.All,Number=transaction?77:123,
            NumberOperator=SearchOperatorType.EqualTo,AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=mask}};
        Assert.IsTrue(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
        group.Number=987;Assert.IsFalse(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void TransactionAddressMaskDoesNotSearchWalletOrDescription(bool from)
    {
        var group=new SearchTextGroup {SearchAvatars=true,HolonType=HolonType.All,SearchQuery=from?"selected-from":"selected-to",
            AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=new SearchAvatarWalletParams {
                SearchAvatarWalletTransactionParams=new SearchAvatarWalletTransactionParams {FromWalletAddress=from,ToWalletAddress=!from}}}};
        Assert.IsTrue(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
        group.SearchQuery="selected-address";Assert.IsFalse(StoredRecordSearch.MatchesSearchFields(Record(),true,group));
    }
}
