namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness;

internal static class TestData
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
    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static async Task Run_LoadNftAsync()
    {
        const string address = "HfNqg9TqctGiwfqYeDwKk6yRi1rUqhyBH6iWpsepnSqy";

        SolanaOASIS solanaOasis = new(TestData.HostUri, TestData.PrivateKey.Key, TestData.PublicKey.Key);
        await solanaOasis.ActivateProviderAsync();

        WriteInfo($"Loading NFT metadata for address: {address} ...");
        OASISResult<IOASISNFT> response = await solanaOasis.LoadNftAsync(address);

        if (response.IsError)
        {
            WriteError($"Error loading NFT: {response.Message}");
            return;
        }

        IOASISNFT result = response.Result;

        WriteSuccess($"Title: {result.Title}");
        WriteSuccess($"Description: {result.Description}");
        WriteSuccess($"MintedByAddress: {result.MintedByAddress}");
        WriteSuccess($"Hash (NFT Address): {result.Hash}");
        WriteSuccess($"Price: {result.Price}");
        WriteSuccess($"ImageUrl: {result.ImageUrl}");
        WriteSuccess($"ThumbnailUrl: {result.ThumbnailUrl}");
        WriteSuccess($"MemoText: {result.MemoText}");
        WriteSuccess($"OnChainProvider: {result.OnChainProvider?.Value}");
        WriteSuccess($"OffChainProvider: {result.OffChainProvider?.Value}");

        await solanaOasis.DeActivateProviderAsync();
    }

    private static async Task Run_SaveAndLoadAvatar()
    {
        SolanaOASIS solanaOasis = new(TestData.HostUri, TestData.PrivateKey.Key, TestData.PublicKey.Key);
        WriteInfo("Activating Solana provider...");
        await solanaOasis.ActivateProviderAsync();

        WriteInfo("Saving Avatar...");
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
            WriteError($"SaveAvatarAsync Error: {saveAvatarResult.Message}");
            return;
        }

        WriteSuccess("Avatar saved successfully.");
        await Task.Delay(5000); 

        WriteInfo("Loading Avatar by provider key...");
        var transactionHashProviderKey = saveAvatarResult.Result.ProviderUniqueStorageKey[ProviderType.SolanaOASIS];
        var loadAvatarResult = await solanaOasis.LoadAvatarByProviderKeyAsync(transactionHashProviderKey);

        if (loadAvatarResult.IsError)
        {
            WriteError($"LoadAvatarByProviderKeyAsync Error: {loadAvatarResult.Message}");
            return;
        }

        WriteSuccess($"Avatar Username: {loadAvatarResult.Result.Username}");
        WriteSuccess($"Avatar Email: {loadAvatarResult.Result.Email}");
        WriteSuccess($"Avatar Id: {loadAvatarResult.Result.Id}");
        WriteSuccess($"Avatar AvatarId: {loadAvatarResult.Result.AvatarId}");

        WriteInfo("Deactivating Solana provider...");
        await solanaOasis.DeActivateProviderAsync();
    }

    private static async Task Run_MintNFTAsync()
    {
        SolanaOASIS solanaOasis = new(TestData.HostUri, TestData.PrivateKey.Key, TestData.PublicKey.Key);

        WriteInfo("Activating Solana provider...");
        await solanaOasis.ActivateProviderAsync();

        IMintNFTTransactionRequestForProvider mintNftRequest = new MintNFTTransactionRequestForProvider()
        {
            JSONUrl = "https://example.com/metadata.json-lallalaall",
            Title = "Test for new Bug",
            Symbol = "BUG!",
        };

        WriteInfo($"Minting NFT: {mintNftRequest.Title}...");
        OASISResult<INFTTransactionRespone> mintNftResult = await solanaOasis.MintNFTAsync(mintNftRequest);
        if (mintNftResult.IsError)
        {
            WriteError($"MintNFTAsync Error: {mintNftResult.Message}");
            return;
        }

        WriteSuccess($"Result:{mintNftResult.Result.TransactionResult}");
        WriteSuccess("MintNFTAsync completed successfully.");
        WriteInfo("Deactivating Solana provider...");
        await solanaOasis.DeActivateProviderAsync();
    }

    private static async Task Main()
    {
        WriteInfo("=== Starting SolanaOASIS Test Harness ===");


        await Run_MintNFTAsync();
        // await Run_LoadNftAsync();
        // await Run_SaveAndLoadAvatar();

        WriteInfo("=== Test Harness finished ===");
    }
}