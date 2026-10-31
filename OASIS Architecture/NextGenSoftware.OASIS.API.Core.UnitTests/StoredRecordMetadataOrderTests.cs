using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class StoredRecordMetadataOrderTests
{
    private static JObject Reordered(bool last,bool hostile)
    {
        var source=new Avatar{Id=Guid.NewGuid(),ProviderWallets=new(){[ProviderType.AzureCosmosDBOASIS]=new List<IProviderWallet>{new ProviderWallet{Id=Guid.NewGuid(),PublicKey="owned-public",PrivateKey="owned-private"}}}};
        var document=JObject.Parse(StoredRecordJson.Serialize(source));
        var wallet=(JObject)document["ProviderWallets"]!["AzureCosmosDBOASIS"]![0]!;
        var annotation=wallet.Property("$type")!;var value=hostile?typeof(Version).AssemblyQualifiedName:annotation.Value.Value<string>();annotation.Remove();
        if(last)wallet.Add("$type",value);else wallet.Property("Id")!.AddAfterSelf(new JProperty("$type",value));
        return document;
    }

    [Theory][InlineData(false)][InlineData(true)]
    public void NativePropertyReorderingMustNotLoseWalletConcreteType(bool last)
    {
        var document=Reordered(last,false);var result=StoredRecordJson.Deserialize<Avatar>(document.ToString());
        var wallet=Assert.Single(result.ProviderWallets[ProviderType.AzureCosmosDBOASIS]);
        Assert.IsType<ProviderWallet>(wallet);Assert.Equal("owned-public",wallet.PublicKey);Assert.Equal("owned-private",wallet.PrivateKey);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public void ReorderedMetadataCannotBypassRestrictedTypeBinder(bool last)
        =>Assert.Throws<JsonSerializationException>(()=>StoredRecordJson.Deserialize<Avatar>(Reordered(last,true).ToString()));
}
