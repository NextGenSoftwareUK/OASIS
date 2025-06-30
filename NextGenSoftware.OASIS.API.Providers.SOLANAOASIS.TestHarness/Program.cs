namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness;

file static class TestData
{
    public const string HostUri = "https://api.devnet.solana.com";

    private const string PrivateKeyBase64 =
        "z5mQD+vgwzrmzSmrXicfY2rVgS3FTSVYWDNNdmg1SoePquZBys9GbCn5tEl8GvbzrWCHE87qoGj5f+PrmaiLew==";

    static readonly byte[] _privateKeyBytes = Convert.FromBase64String(PrivateKeyBase64);
    static readonly string _privateKeyBase58 = Base58.Encode(_privateKeyBytes);

    public static readonly PublicKey PublicKey = new("AfpSpMjNyoHTZWMWkog6Znf57KV82MGzkpDUUjLtmHwG");
    public static readonly PrivateKey PrivateKey = new(_privateKeyBase58);
}

internal static class Program
{
    private static async Task Run_LoadNftAsync()
    {
        const string address = "HfNqg9TqctGiwfqYeDwKk6yRi1rUqhyBH6iWpsepnSqy";

        SolanaOASIS solanaOasis = new(TestData.HostUri, TestData.PrivateKey.Key, TestData.PublicKey.Key);
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
        SolanaOASIS solanaOasis = new(TestData.HostUri, TestData.PrivateKey.Key, TestData.PublicKey.Key);
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
        SolanaOASIS solanaOasis = new(TestData.HostUri, TestData.PrivateKey.Key, TestData.PublicKey.Key);

        await solanaOasis.ActivateProviderAsync();

        IMintNFTTransactionRequestForProvider mintNftRequest = new MintNFTTransactionRequestForProvider()
        {
            JSONUrl = "https://example.com/metadata.json-lallalaall",
            Title = "Fix bug NFT",
            Symbol = "BUG",
        };

        OASISResult<INFTTransactionRespone> mintNftResult = await solanaOasis.MintNFTAsync(mintNftRequest);
        if (mintNftResult.IsError)
        {
            Console.WriteLine("Error: " + mintNftResult.Message);
            return;
        }

        Console.WriteLine("Run_MintNFTAsync-->MintNFTAsync()-->Completed...");

        Console.WriteLine("Run_MintNFTAsync()->DeActivateProvider()");
        await solanaOasis.DeActivateProviderAsync();
    }

    private static async Task Main()
    {
        // Transferring Examples
        await Run_MintNFTAsync();

        //  await Run_LoadNftAsync();

        // Solana Provider Examples
        // await Run_SaveAndLoadAvatar();
    }
}