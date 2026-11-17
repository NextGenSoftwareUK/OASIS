using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class HyperDriveWalletTokenBindingTests
{
    [Theory]
    [InlineData("ImportWeb4NFTAsync")]
    [InlineData("ExportWeb4NFTAsync")]
    public void Web4NftObjectRoutesUseExistingConcreteBodyDto(string method)
    {
        var parameter = typeof(NftController).GetMethod(method)!.GetParameters().Single(value => value.Name == "oasisNFT");
        Assert.Equal(typeof(NextGenSoftware.OASIS.API.Core.Objects.NFT.Web4NFT), parameter.ParameterType);
        Assert.NotNull(Attribute.GetCustomAttribute(parameter, typeof(FromBodyAttribute)));
        var json = "{\"id\":\"5f675630-2622-43b5-a940-2897af23d5a7\",\"title\":\"Binding fixture\",\"web3NFTs\":[{\"NFTTokenAddress\":\"fixture-object\"}]}";
        var restored = (NextGenSoftware.OASIS.API.Core.Objects.NFT.Web4NFT)JsonSerializer.Deserialize(json, parameter.ParameterType, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(Guid.Parse("5f675630-2622-43b5-a940-2897af23d5a7"), restored.Id);
        Assert.Equal("Binding fixture", restored.Title);
        Assert.Equal("fixture-object", Assert.Single(restored.Web3NFTs).NFTTokenAddress);
    }

    [Fact]
    public void NftImportRouteUsesExistingConcreteBodyDto()
    {
        var parameter=typeof(NftController).GetMethod("ImportWeb3NFTAsync")!.GetParameters().Single();
        Assert.Equal(typeof(NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.ImportWeb3NFTRequest),parameter.ParameterType);
        Assert.NotNull(Attribute.GetCustomAttribute(parameter,typeof(FromBodyAttribute)));
    }

    [Fact]
    public void SendRouteUsesInstantiableBodyDto()
    {
        var parameter=typeof(WalletController).GetMethod("SendTokenAsync")!.GetParameters().Single();
        Assert.Equal(typeof(SendWeb4TokenRequest),parameter.ParameterType);
        Assert.NotNull(Attribute.GetCustomAttribute(parameter,typeof(FromBodyAttribute)));
    }

    [Fact]
    public async Task TransferWithoutExplicitProvidersReturnsErrorWithoutReceipt()
    {
        var response = await new WalletController().TransferBetweenWalletsAsync(new SendWeb4TokenRequest
        {
            FromWalletAddress = "fixture-sender",
            ToWalletAddress = "fixture-recipient",
            Amount = 1m
        });

        Assert.True(response.IsError);
        Assert.Null(response.Result);
        Assert.False(response.IsSaved);
    }

    [Fact]
    public async Task WalletKeyReadsRejectEveryForeignAvatarSelector()
    {
        var controller = CreateWalletOwnerController();

        var byId = await controller.LoadProviderWalletsForAvatarByIdAsync(Guid.NewGuid(), decryptPrivateKeys: true);
        var byUsername = await controller.LoadProviderWalletsForAvatarByUsernameAsync("other", decryptPrivateKeys: true);
        var byEmail = await controller.LoadProviderWalletsForAvatarByEmailAsync("other@example.test", decryptPrivateKeys: true);
        var defaultWallet = await controller.GetAvatarDefaultWalletByUsernameAsync("other", decryptPrivateKeys: true);

        Assert.All(new[] { byId, byUsername, byEmail }, result =>
        {
            Assert.Equal("AVATAR_OPERATION_FORBIDDEN", result.ErrorCode);
            Assert.True(result.IsError);
            Assert.Null(result.Result);
        });
        Assert.Equal("AVATAR_OPERATION_FORBIDDEN", defaultWallet.ErrorCode);
        Assert.True(defaultWallet.IsError);
        Assert.Null(defaultWallet.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, controller.Response.StatusCode);
    }

    [Fact]
    public async Task WalletWritesRejectEveryForeignAvatarSelectorBeforeStorage()
    {
        var controller = CreateWalletOwnerController();
        var id = Guid.NewGuid();
        var wallet = Guid.NewGuid();
        const string username = "other";
        const string email = "other@example.test";
        const string key = "not-signing-material";

        Assert.All(new[]
        {
            await controller.SaveProviderWalletsForAvatarByIdAsync(id, new()),
            await controller.SaveProviderWalletsForAvatarByUsernameAsync(username, new()),
            await controller.SaveProviderWalletsForAvatarByEmailAsync(email, new())
        }, result => Assert.Equal("AVATAR_OPERATION_FORBIDDEN", result.ErrorCode));
        Assert.All(new[]
        {
            await controller.SetAvatarDefaultWalletByIdAsync(id, wallet, default),
            await controller.SetAvatarDefaultWalletByUsernameAsync(username, wallet, default),
            await controller.SetAvatarDefaultWalletByEmailAsync(email, wallet, default),
            controller.ImportWalletUsingPrivateKeyById(id, key, default),
            controller.ImportWalletUsingPrivateKeyByUsername(username, key, default),
            controller.ImportWalletUsingPrivateKeyByEmail(email, key, default),
            controller.ImportWalletUsingPublicKeyById(id, key, "other-wallet", default),
            controller.ImportWalletUsingPublicKeyByUsername(username, key, "other-wallet", default),
            controller.ImportWalletUsingPublicKeyByEmail(email, key, "other-wallet", default)
        }, result =>
        {
            Assert.Equal("AVATAR_OPERATION_FORBIDDEN", result.ErrorCode);
            Assert.True(result.IsError);
            Assert.Null(result.Result);
        });
        Assert.Equal(StatusCodes.Status403Forbidden, controller.Response.StatusCode);
    }

    [Fact]
    public async Task WalletsByChainRejectsAnotherAvatarBeforeReadingStorage()
    {
        var controller = CreateWalletOwnerController();
        var result = await controller.GetWalletsByChainAsync(Guid.NewGuid(), "Solana");

        Assert.Equal(StatusCodes.Status403Forbidden, controller.Response.StatusCode);
        Assert.Equal("AVATAR_OPERATION_FORBIDDEN", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task WalletTokensRejectAnotherAvatarBeforeReadingStorageOrChain()
    {
        var controller = CreateWalletOwnerController();
        var result = await controller.GetWalletTokensAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(StatusCodes.Status403Forbidden, controller.Response.StatusCode);
        Assert.Equal("AVATAR_OPERATION_FORBIDDEN", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task WalletActivityRejectsAnotherAvatarBeforeReadingStorageOrChain()
    {
        var controller = CreateWalletOwnerController();
        var result = await controller.GetWalletAnalyticsAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(StatusCodes.Status403Forbidden, controller.Response.StatusCode);
        Assert.Equal("AVATAR_OPERATION_FORBIDDEN", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData("All")]
    [InlineData("1")]
    [InlineData("UnrecognizedNetwork")]
    public async Task WalletsByChainRejectsInvalidNetworksBeforeReadingStorage(string chain)
    {
        var controller = CreateWalletOwnerController();
        var result = await controller.GetWalletsByChainAsync(controller.Avatar.Id, chain);

        Assert.Equal(StatusCodes.Status400BadRequest, controller.Response.StatusCode);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
    }

    private static WalletController CreateWalletOwnerController()
    {
        var controller = new WalletController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar
        {
            Id = Guid.NewGuid(), Username = "wallet-owner", Email = "wallet-owner@example.test"
        };
        return controller;
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
