using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;
[TestClass]
public sealed class MongoChildRoleMappingTests
{
    [TestMethod]
    [DataRow(false)] [DataRow(true)]
    public void NativeMappingRetainsObservedRolesWithoutHydratingUnselectedChildren(bool observed)
    {
        var source=new Holon {Id=Guid.NewGuid()};
        if(observed) source.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,source.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Components",new[]{Guid.NewGuid()})}));
        var native=DataHelper.ConvertOASISHolonToMongoEntity(source,false);
        Assert.AreEqual(source.ChildRoleManifestJson,native.ChildRoleManifestJson);
        var loaded=(Holon)DataHelper.ConvertMongoEntityToOASISHolon(native,false);
        Assert.AreEqual(source.ChildRoleManifestJson,loaded.ChildRoleManifestJson);
        Assert.AreEqual(0,loaded.Children.Count);
    }
}
