using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Rpc;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness;

internal static class Program
{
    private static string hostUri = "https://api.devnet.solana.com";
    private static PublicKey publicKey = new("8pBKg1JCoa2cFAB9Bu5nLStaHzRADJujLVyjKEfDgbKT");

    private static PrivateKey privateKey =
        new("5VCRWersYAwwhzGoFB7ye82AXrjnY2Jn9v4FTr1wC2eGSfhSPRygBW6FLCAnQzXxZZBgDeb1roQ2uV74772eRV8R");

    private static async Task Run_LoadNftAsync()
    {
        const string address = "HfNqg9TqctGiwfqYeDwKk6yRi1rUqhyBH6iWpsepnSqy";

        SolanaOASIS solanaOasis = new(hostUri, privateKey.Key, publicKey.Key);
        await solanaOasis.ActivateProviderAsync();

        OASISResult<IOASISNFT> response = await solanaOasis.LoadNftAsync(address);

        if (response.IsError)
        {
            Console.WriteLine(response.Message);
            return;
        }

        IOASISNFT result = response.Result;

        Console.WriteLine($"Title: {result.Title}");
        Console.WriteLine($"Description: {result.Description}");
        Console.WriteLine($"MintedByAddress: {result.MintedByAddress}");
        Console.WriteLine($"Hash (NFT Address): {result.Hash}");
        Console.WriteLine($"Price: {result.Price}");
        Console.WriteLine($"ImageUrl: {result.ImageUrl}");
        Console.WriteLine($"ThumbnailUrl: {result.ThumbnailUrl}");
        Console.WriteLine($"MemoText: {result.MemoText}");
        Console.WriteLine($"OnChainProvider: {result.OnChainProvider?.Value}");
        Console.WriteLine($"OffChainProvider: {result.OffChainProvider?.Value}");
        
        await solanaOasis.ActivateProviderAsync();
    }

    private static async Task Run_SaveAndLoadAvatar()
    {
        SolanaOASIS solanaOasis = new(hostUri, privateKey.Key, publicKey.Key);
        Console.WriteLine("Run_SaveAndLoadAvatar()->ActivateProvider()");
        await solanaOasis.ActivateProviderAsync();

        Console.WriteLine("Run_SaveAndLoadAvatar()->SaveAvatarAsync()");
        var saveAvatarResult = await solanaOasis.SaveAvatarAsync(new Avatar()
        {
            Username = "@bob",
            Password = "P@ssw0rd!",
            Email = "bob@mail.ru",
            Id = Guid.NewGuid(),
            AvatarId = Guid.NewGuid()
        });

        if (saveAvatarResult.IsError)
        {
            Console.WriteLine(saveAvatarResult.Message);
            return;
        }

        await Task.Delay(5000);

        Console.WriteLine("Run_SaveAndLoadAvatar()->LoadAvatarAsync()");
        var transactionHashProviderKey = saveAvatarResult.Result.ProviderUniqueStorageKey[ProviderType.SolanaOASIS];
        var loadAvatarResult = await solanaOasis.LoadAvatarByProviderKeyAsync(transactionHashProviderKey);

        if (loadAvatarResult.IsError)
        {
            Console.WriteLine(loadAvatarResult.Message);
            return;
        }

        Console.WriteLine("Avatar UserName: " + loadAvatarResult.Result.Username);
        Console.WriteLine("Avatar Password: " + loadAvatarResult.Result.Password);
        Console.WriteLine("Avatar Email: " + loadAvatarResult.Result.Email);
        Console.WriteLine("Avatar Id: " + loadAvatarResult.Result.Id);
        Console.WriteLine("Avatar AvatarId: " + loadAvatarResult.Result.AvatarId);

        Console.WriteLine("Run_SaveAndLoadAvatar()->DeActivateProvider()");
        await solanaOasis.DeActivateProviderAsync();
    }

    private static async Task Run_MintNFTAsync()
    {
        SolanaOASIS solanaOasis = new(hostUri, privateKey.Key, publicKey.Key);

        Console.WriteLine("Run_MintNFTAsync()->ActivateProvider()");
        await solanaOasis.ActivateProviderAsync();

        IMintNFTTransactionRequestForProvider mintNFTRequest = new MintNFTTransactionRequestForProvider()
        {
            JSONUrl = "https://example.com/metadata.json-lallalaall",
            Title = "Test NFT One more time!!!",
            Price = 2000,
            Symbol = "MUNFT",
            MintWalletAddress = publicKey.Key
        };

        Console.WriteLine("Run_MintNFTAsync-->MintNFTAsync()-->Sending...");
        var mintNftResult = await solanaOasis.MintNFTAsync(mintNFTRequest);
        if (mintNftResult.IsError)
        {
            Console.WriteLine("Run_MintNFTAsync-->MintNFTAsync()-->Failed...");
            Console.WriteLine(mintNftResult.Message);
            return;
        }

        Console.WriteLine("Run_MintNFTAsync-->MintNFTAsync()-->Completed...");

        Console.WriteLine("Run_MintNFTAsync()->DeActivateProvider()");
        await solanaOasis.DeActivateProviderAsync();
    }

    private static async Task Main()
    {
        // Transferring Examples
        //await Run_MintNFTAsync();

        await Run_LoadNftAsync();

        // Solana Provider Examples
        // await Run_SaveAndLoadAvatar();
    }
}