using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredRecordSearchHistoryTests
{
    private static SearchParams Query() => new() {SearchOnlyForCurrentAvatar=false,MetaKeyValuePairMatchMode=MetaKeyValuePairMatchMode.All};

    [TestMethod]
    public void ExistingThreeArgumentContractStillRejectsUnsupportedHistory()
        => Assert.ThrowsException<NotSupportedException>(()=>StoredRecordSearch.Validate(Query(),1,0));

    [TestMethod]
    public void ExplicitHistoryCapabilityAllowsPositiveVersion()
        => StoredRecordSearch.Validate(Query(),1,0,supportsHistory:true);

    [TestMethod]
    public void HistoryCapabilityStillValidatesRequestAndNegativeVersion()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(()=>StoredRecordSearch.Validate(Query(),-1,0,supportsHistory:true));
        Assert.ThrowsException<ArgumentOutOfRangeException>(()=>StoredRecordSearch.Validate(Query(),1,-1,supportsHistory:true));
        Assert.ThrowsException<ArgumentNullException>(()=>StoredRecordSearch.Validate(null,1,0,supportsHistory:true));
    }
}
