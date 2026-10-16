using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredRecordSearchIdTests
{
    [TestMethod]
    public void ExtendedComparisonsUseSelectedTypedMetadataAndAllFields()
    {
        var when=new DateTime(2020,1,2,0,0,0,DateTimeKind.Utc);
        var record=new Holon {Id=Guid.NewGuid(),Version=7,MetaData=new() {{"score",8181},{"when",when}}};
        var number=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchNumberGroup {SearchHolons=true,
            HolonType=NextGenSoftware.OASIS.API.Core.Enums.HolonType.All,Number=8181,NumberOperator=NextGenSoftware.OASIS.API.Core.Enums.SearchOperatorType.EqualTo,
            HolonSearchParams=new NextGenSoftware.OASIS.API.Core.Interfaces.SearchHolonParams {MetaData=true,MetaDataKey="score"}};
        Assert.IsTrue(StoredRecordSearch.MatchesSearchFields(record,false,number));
        number.HolonSearchParams.MetaDataKey="when";Assert.IsFalse(StoredRecordSearch.MatchesSearchFields(record,false,number));
        number.HolonSearchParams=new NextGenSoftware.OASIS.API.Core.Interfaces.SearchHolonParams {SearchAllFields=true};
        number.Number=7;Assert.IsTrue(StoredRecordSearch.MatchesSearchFields(record,false,number));
        var date=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchDateGroup {SearchHolons=true,
            HolonType=NextGenSoftware.OASIS.API.Core.Enums.HolonType.All,Date=when,DateOperator=NextGenSoftware.OASIS.API.Core.Enums.SearchOperatorType.EqualTo,
            HolonSearchParams=new NextGenSoftware.OASIS.API.Core.Interfaces.SearchHolonParams {MetaData=true,MetaDataKey="when"}};
        Assert.IsTrue(StoredRecordSearch.MatchesSearchFields(record,false,date));
        date.Date=when.AddDays(1);Assert.IsFalse(StoredRecordSearch.MatchesSearchFields(record,false,date));
    }

    [TestMethod]
    public void ExtendedTextMasksRespectAvatarIdMetadataScopeAndCredentialExclusion()
    {
        var record=new Avatar {Id=Guid.NewGuid(),Title="selected-title",VerificationToken="excluded-secret",MetaData=new() {{"avatar-only","selected-metadata"}}};
        var group=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup {SearchAvatars=true,SearchHolons=true,
            HolonType=NextGenSoftware.OASIS.API.Core.Enums.HolonType.All,SearchQuery=record.Id.ToString(),
            AvatarSearchParams=new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams {AvatarId=true}};
        Assert.IsTrue(StoredRecordSearch.MatchesTextFields(record,true,group));
        group.SearchQuery="selected-metadata";
        group.AvatarSearchParams=new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams {MetaData=true};
        group.HolonSearchParams=new NextGenSoftware.OASIS.API.Core.Interfaces.SearchHolonParams {MetaData=true,MetaDataKey="not-an-avatar-key"};
        Assert.IsTrue(StoredRecordSearch.MatchesTextFields(record,true,group));
        group.SearchQuery="excluded-secret";
        group.AvatarSearchParams=new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams {SearchAllFields=true};
        Assert.IsFalse(StoredRecordSearch.MatchesTextFields(record,true,group));
    }

    [TestMethod]
    public void ProviderKeySearchIsLiteralAndDoesNotSearchOtherFieldsOrProviderNames()
    {
        var record=new Holon {Id=Guid.NewGuid(),Name="unrelated",ProviderUniqueStorageKey=new() {{NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SolanaOASIS,"owned.[key]+value"}}};
        Assert.IsTrue(StoredRecordSearch.MatchesTextIdentifiers(record,".[KEY]+",false,true));
        Assert.IsFalse(StoredRecordSearch.MatchesTextIdentifiers(record,"SolanaOASIS",false,true));
        Assert.IsFalse(StoredRecordSearch.MatchesTextIdentifiers(record,"unrelated",false,true));
        Assert.IsFalse(StoredRecordSearch.MatchesTextIdentifiers(record,"owned",true,false));
        Assert.IsFalse(StoredRecordSearch.MatchesTextIdentifiers(record,"owned",false,false));
    }

    [TestMethod]
    public void IdSearchMatchesGuidSubstringsButNotArbitraryText()
    {
        var id=Guid.NewGuid();var linked=Guid.NewGuid();
        var record=new Holon {Id=id,Name="not-an-id",MetaData=new() {{"linked",linked.ToString()}}};
        Assert.IsTrue(StoredRecordSearch.MatchesTextIds(record,id.ToString().ToUpperInvariant()));
        Assert.IsTrue(StoredRecordSearch.MatchesTextIds(record,linked.ToString()[..12]));
        Assert.IsFalse(StoredRecordSearch.MatchesTextIds(record,"not-an-id"));
        Assert.ThrowsException<ArgumentException>(()=>StoredRecordSearch.MatchesTextIds(record," "));
    }

    [TestMethod]
    public void GuidLookingCredentialsAreNotSearchableIds()
    {
        var secret=Guid.NewGuid().ToString();
        var record=new Avatar {Id=Guid.NewGuid(),Password=secret,VerificationToken=secret,ResetToken=secret,RefreshToken=secret,
            RefreshTokens=new() {new NextGenSoftware.OASIS.API.Core.Objects.RefreshToken {Token=secret}},
            ProviderWallets=new() {{NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SolanaOASIS,new() {new NextGenSoftware.OASIS.API.Core.Objects.ProviderWallet {PrivateKey=secret}}}}};
        Assert.IsFalse(StoredRecordSearch.MatchesTextIds(record,secret));
    }
}
