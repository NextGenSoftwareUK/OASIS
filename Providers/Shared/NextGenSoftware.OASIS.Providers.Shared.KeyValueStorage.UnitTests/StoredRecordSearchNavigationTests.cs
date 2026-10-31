using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredRecordSearchNavigationTests
{
    private sealed class NamedOwner:Holon {public List<IHolon> Components {get;set;}=new();}

    [DataTestMethod]
    [DataRow("children")] [DataRow("parent")] [DataRow("original")] [DataRow("named")]
    public void RootAllFieldsDoesNotSearchNavigationOrTrackingSnapshots(string source)
    {
        const string needle="owned navigation-only needle";
        var record=new NamedOwner {Id=Guid.NewGuid(),Name="unrelated owner"};
        var linked=new Holon {Id=Guid.NewGuid(),Name=needle};
        switch(source)
        {
            case "children":record.Children.Add(linked);break;
            case "parent":record.ParentHolon=linked;break;
            case "original":record.Original=linked;break;
            case "named":record.Components.Add(linked);break;
        }
        var group=new SearchTextGroup {SearchHolons=true,SearchQuery=needle,
            HolonSearchParams=new SearchHolonParams {SearchAllFields=true}};
        Assert.IsFalse(StoredRecordSearch.MatchesSelectedFields(record,false,group));
        record.MetaData["owned-value"]=needle;
        Assert.IsTrue(StoredRecordSearch.MatchesSelectedFields(record,false,group),"Scalar metadata remains searchable.");
        record.MetaData.Clear();record.Name=needle;
        Assert.IsTrue(StoredRecordSearch.MatchesSelectedFields(record,false,group),"The root name remains searchable.");
    }
}
