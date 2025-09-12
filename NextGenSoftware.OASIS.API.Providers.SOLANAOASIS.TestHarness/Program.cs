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
    private static void WriteWithTime(ConsoleColor color, string message)
    {
        Console.ForegroundColor = color;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    private static void WriteSuccess(string message) => WriteWithTime(ConsoleColor.Green, message);
    private static void WriteError(string message) => WriteWithTime(ConsoleColor.Red, message);
    private static void WriteInfo(string message) => WriteWithTime(ConsoleColor.Cyan, message);

    private static void WriteColored(string label, string value, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] {label}: ");
        Console.ResetColor();
        Console.WriteLine(string.IsNullOrWhiteSpace(value) ? "<empty>" : value);
    }


    private static async Task Run_LoadNftAsync()
    {
        const string address = "46SPSK3KbLUVmwPbqbx1PiC6hpqSpBqe5GUeUzsfmZVN";

        SolanaOASIS solanaOasis = new(TestData.HostUri, TestData.PrivateKey.Key, TestData.PublicKey.Key);
        WriteInfo("Activating Solana provider...");
        await solanaOasis.ActivateProviderAsync();

        WriteInfo($"Loading NFT metadata for address: {address} ...");
        OASISResult<IOASISNFT> response = await solanaOasis.LoadNftAsync(address);

        if (response.IsError)
        {
            WriteError($"Error loading NFT: {response.Message}");
            return;
        }

        IOASISNFT result = response.Result;

        WriteColored("Title", result.Title, ConsoleColor.Green);
        WriteColored("Symbol", result.Symbol, ConsoleColor.Cyan);
        WriteColored("SellerFeeBasisPoints", result.SellerFeeBasisPoints.ToString(), ConsoleColor.Magenta);
        WriteColored("URL", result.JSONMetaDataURL, ConsoleColor.Blue);
        WriteColored("OnChainProvider", result.OnChainProvider?.Value.ToString(), ConsoleColor.DarkGreen);
        WriteColored("OffChainProvider", result.OffChainProvider?.Value.ToString(), ConsoleColor.DarkYellow);

        WriteSuccess("LoadNftAsync completed successfully.");
        WriteInfo("Deactivating Solana provider...");
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

        IMintNFTTransactionRequest mintNftRequest = new MintNFTTransactionRequest()
        {
            JSONMetaDataURL = "https://example.com/metadata.json-#1",
            Title = "Test data for LoadNft #1",
            Symbol = "LOADNFT!#1",
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

    /// <summary>
    /// Sends an NFT from one wallet to another on the Solana network.
    ///
    /// Input (NFTWalletTransactionRequest):
    /// - FromWalletAddress: public key of the sender's wallet. This wallet must own the NFT being sent and must match the active Oasis account signing the transaction.
    /// - ToWalletAddress: public key of the recipient's wallet.
    /// - TokenAddress: the token (mint) address of the NFT to be transferred.
    /// - Amount: number of NFTs to transfer (usually 1 for unique tokens).
    ///
    /// The method checks if the recipient’s associated token account exists and creates it if necessary.
    /// It handles transaction errors and returns clear error messages when something goes wrong.
    ///
    /// See the example usage in Run_SendNFTAsync where FromWalletAddress matches the Oasis account.
    /// </summary>
    private static async Task Run_SendNFTAsync()
    {
        SolanaOASIS solanaOasis = new(TestData.HostUri, TestData.PrivateKey.Key, TestData.PublicKey.Key);

        WriteInfo("Activating Solana provider...");
        await solanaOasis.ActivateProviderAsync();


        // Before using, update with new data!
        INFTWalletTransactionRequest request = new NFTWalletTransactionRequest()
        {
            FromWalletAddress = TestData.PublicKey.Key,
            TokenAddress = "46SPSK3KbLUVmwPbqbx1PiC6hpqSpBqe5GUeUzsfmZVN",
            Amount = 1,
            ToWalletAddress = "2Gtzh4ywuvxNWmtLkS8zqJ3CJpbguquuqRWJCdeZF1Jm"
        };

        OASISResult<INFTTransactionRespone> response = await solanaOasis.SendNFTAsync(request);

        if (response.IsError)
        {
            WriteError($"SendNFTAsync Error: {response.Message}");
            return;
        }

        WriteSuccess($"Result:{response.Result.TransactionResult}");
        WriteSuccess("SendNFTAsync completed successfully.");
        WriteInfo("Deactivating Solana provider...");
        await solanaOasis.DeActivateProviderAsync();
    }

    private static async Task Main()
    {
        WriteInfo("=== Starting SolanaOASIS Test Harness ===");


        //await Run_MintNFTAsync();
        await Run_LoadNftAsync();
        //ok// await Run_SendNFTAsync();

        // await Run_SaveAndLoadAvatar();

        WriteInfo("=== Test Harness finished ===");
    }
}