using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(true)][DataRow(false)]
    public async Task DurableAvatarFieldsSurviveNativeCreateUpdateAndFreshReads(bool synchronous)
    {
        var (provider,contract)=await CreateActivatedProviderAsync();
        IAvatar source=new Avatar{Id=Guid.NewGuid()};
        var fixedId=source.Id;
        // Scalar RefreshToken is explicitly JsonIgnore/HTTP-cookie state; the authoritative
        // persisted RefreshTokens list and public refresh-token selector are checked below.
        var fields=typeof(IAvatar).GetProperties().Where(property=>property.CanWrite && property.Name!="JwtToken" && property.Name!="RefreshToken"
            && new[]{typeof(string),typeof(bool),typeof(int),typeof(Guid),typeof(DateTime?)}.Contains(property.PropertyType)).ToArray();
        Assert.IsTrue(fields.Any(property=>property.Name=="Verified") && fields.Any(property=>property.Name=="HerzVoiceprintId")
            && fields.Any(property=>property.Name=="DID"),"Do not silently shrink durable avatar acceptance.");
        for(var revision=1;revision<=2;revision++){
            foreach(var property in fields){
                object? value=property.PropertyType==typeof(string)?$"fixture-{source.Id:N}-{revision}-{property.Name}":
                    property.PropertyType==typeof(bool)?revision==1:property.PropertyType==typeof(int)?revision+10:
                    property.PropertyType==typeof(Guid)?fixedId:
                    revision==1?new DateTime(2024,2,3,4,5,6,DateTimeKind.Utc):null;
                if(property.Name=="Email")value=$"{source.Id:N}-{revision}@example.invalid";
                property.SetValue(source,value);
            }
            Assert.AreEqual(fixedId,source.Id,"AvatarId aliases Id; updates must retain the same root identity.");
            source.RefreshTokens=new(){new NextGenSoftware.OASIS.API.Core.Objects.RefreshToken{
                Id=revision,Token=$"fixture-refresh-{fixedId:N}-{revision}",
                Created=new DateTime(2024,2,3,4,5,6,DateTimeKind.Utc),Expires=new DateTime(2099,2,3,4,5,6,DateTimeKind.Utc),CreatedByIp="192.0.2.1"}};
            var saved=synchronous?provider.SaveAvatar(source):await provider.SaveAvatarAsync(source);
            Assert.IsFalse(saved.IsError,saved.Message+" "+saved.Exception);Assert.IsNotNull(saved.Result);
            var reader=new EthereumOASIS(Host,PrivateKey,ChainId,contract);
            try{
                var activated=await reader.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);
                foreach(var loaded in new[]{reader.LoadAvatar(source.Id),await reader.LoadAvatarAsync(source.Id),
                    reader.LoadAvatarByUsername(source.Username),await reader.LoadAvatarByUsernameAsync(source.Username),
                    reader.LoadAvatarByEmail(source.Email),await reader.LoadAvatarByEmailAsync(source.Email),
                    reader.LoadAvatarByRefreshToken(source.RefreshTokens[0].Token),await reader.LoadAvatarByRefreshTokenAsync(source.RefreshTokens[0].Token)}){
                    Assert.IsFalse(loaded.IsError,loaded.Message);Assert.IsNotNull(loaded.Result);
                    foreach(var property in fields)Assert.AreEqual(property.GetValue(source),property.GetValue(loaded.Result),property.Name+" revision "+revision);
                    Assert.AreEqual(source.IsVerified,loaded.Result.IsVerified);
                    Assert.AreEqual(1,loaded.Result.RefreshTokens.Count);
                    Assert.AreEqual(source.RefreshTokens[0].Token,loaded.Result.RefreshTokens[0].Token);
                    Assert.AreEqual(source.RefreshTokens[0].Expires,loaded.Result.RefreshTokens[0].Expires);
                    Assert.AreEqual(source.RefreshTokens[0].Created,loaded.Result.RefreshTokens[0].Created);
                }
            }finally{await reader.DeActivateProviderAsync();}
        }
    }
}
