using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredRecordSearchIdTests
{
    [TestMethod]
    public void JsonElementMetadataUsesActualValuesWithoutInferringDatesOrSearchingCredentials()
    {
        var linked=Guid.NewGuid();var secret=Guid.NewGuid();
        using var document=System.Text.Json.JsonDocument.Parse("{\"score\":8181,\"when\":\"2020-01-02T00:00:00Z\",\"linked\":\""+linked+"\",\"PrivateKey\":\""+secret+"\"}");
        var record=new Holon {Id=Guid.NewGuid(),MetaData=new() {{"nested",document.RootElement.Clone()}}};
        var group=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchNumberGroup {Number=8181,
            NumberOperator=NextGenSoftware.OASIS.API.Core.Enums.SearchOperatorType.EqualTo,
            HolonSearchParams=new NextGenSoftware.OASIS.API.Core.Interfaces.SearchHolonParams {MetaData=true}};
        Assert.IsTrue(StoredRecordSearch.MatchesSelectedFields(record,false,group));
        Assert.IsTrue(StoredRecordSearch.MatchesTextIds(record,linked.ToString()));
        Assert.IsFalse(StoredRecordSearch.MatchesTextIds(record,secret.ToString()));
        var date=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchDateGroup {Date=new DateTime(2020,1,2,0,0,0,DateTimeKind.Utc),
            DateOperator=NextGenSoftware.OASIS.API.Core.Enums.SearchOperatorType.EqualTo,
            HolonSearchParams=new NextGenSoftware.OASIS.API.Core.Interfaces.SearchHolonParams {MetaData=true}};
        Assert.IsFalse(StoredRecordSearch.MatchesSelectedFields(record,false,date));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void MetadataPredicateKeepsAllAnyEmptyNullAndInvariantScalarSemantics(bool any)
    {
        var mode=any ? NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.Any:NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All;
        var metadata=new System.Collections.Generic.Dictionary<string,object> {{"number",1.5m},{"flag",true},{"null",null}};
        var prior=System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            Assert.IsTrue(StoredRecordSearch.MatchesMetadata(metadata,new System.Collections.Generic.Dictionary<string,string> {{"number","1.5"}},mode));
            Assert.IsTrue(StoredRecordSearch.MatchesMetadata(metadata,new System.Collections.Generic.Dictionary<string,string> {{"flag","True"}},mode));
            Assert.AreEqual(any,StoredRecordSearch.MatchesMetadata(metadata,new System.Collections.Generic.Dictionary<string,string> {{"number","1.5"},{"missing","never"}},mode));
            Assert.IsFalse(StoredRecordSearch.MatchesMetadata(metadata,new System.Collections.Generic.Dictionary<string,string> {{"null",""}},mode));
            Assert.IsTrue(StoredRecordSearch.MatchesMetadata(metadata,new System.Collections.Generic.Dictionary<string,string>(),mode));
            Assert.IsTrue(StoredRecordSearch.MatchesMetadata(null,null,mode));
        }
        finally {System.Globalization.CultureInfo.CurrentCulture=prior;}
    }

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
