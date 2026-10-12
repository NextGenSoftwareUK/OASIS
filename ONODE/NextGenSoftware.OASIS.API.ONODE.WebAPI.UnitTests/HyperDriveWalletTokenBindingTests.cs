using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class HyperDriveWalletTokenBindingTests
{
    [Fact]
    public void SendRouteUsesInstantiableBodyDto()
    {
        var parameter=typeof(WalletController).GetMethod("SendTokenAsync")!.GetParameters().Single();
        Assert.Equal(typeof(SendWeb4TokenRequest),parameter.ParameterType);
        Assert.NotNull(Attribute.GetCustomAttribute(parameter,typeof(FromBodyAttribute)));
    }

    [Fact]
    public void SendJsonPreservesSignerAssetAndAvatarFields()
    {
        var source=new SendWeb4TokenRequest {FromAvatarId=Guid.NewGuid(),ToAvatarId=Guid.NewGuid(),Amount=0.001m,
            OwnerPrivateKey="public-test-only",OwnerPublicKey="public-test-public-key",OwnerSeedPhrase="public-test-seed",
            FromTokenAddress="fixture-token",FromWalletAddress="fixture-owner",ToWalletAddress="fixture-recipient"};
        var restored=JsonSerializer.Deserialize<SendWeb4TokenRequest>(JsonSerializer.Serialize(source))!;
        Assert.Equal(source.FromAvatarId,restored.FromAvatarId);Assert.Equal(source.ToAvatarId,restored.ToAvatarId);
        Assert.Equal(source.Amount,restored.Amount);Assert.Equal(source.FromTokenAddress,restored.FromTokenAddress);
        Assert.Equal(source.OwnerPrivateKey,restored.OwnerPrivateKey);Assert.Equal(source.OwnerPublicKey,restored.OwnerPublicKey);
        Assert.Equal(source.OwnerSeedPhrase,restored.OwnerSeedPhrase);
        Assert.Equal(source.FromWalletAddress,restored.FromWalletAddress);Assert.Equal(source.ToWalletAddress,restored.ToWalletAddress);
    }
}
