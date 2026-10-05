namespace RadixBridgeTest;

/// <summary>
/// Main program class that initializes and runs tests for the Radix Bridge API.
/// </summary>
public static class Program
{
    /// <summary>
    /// Configuration options for Radix Technical Account Bridge.
    /// </summary>
    private static readonly RadixTechnicalAccountBridgeOptions Options = new()
    {
        NetworkId = (byte)NetworkType.Test,
        PrivateKey = TechAccountData.PrivateKey,
        PublicKey = TechAccountData.PublicKey,
        HostUri = TestData.Url,
        AccountAddress = TechAccountData.AccountAddress
    };

    static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });

    static readonly ILogger<RadixBridge.RadixBridge> Logger = LoggerFactory.CreateLogger<RadixBridge.RadixBridge>();


    /// <summary>
    /// Service instance for running tests on the Radix Bridge API.
    /// </summary>
    private static readonly ITestService TestService = new TestService(
        new OptionsWrapper<RadixTechnicalAccountBridgeOptions>(Options),
        new HttpClient(), Logger
    );

    private static IRadixBridge _bridge = new RadixBridge.RadixBridge(Options, new(), Logger);

    /// <summary>
    /// Main entry point for the program, initiating tests.
    /// </summary>
    public static async Task Main()
    {
        Console.WriteLine("Starting Radix Bridge Tests...");

        await RunTestsAsync();

        Console.WriteLine("Tests Completed.");
    }

    /// <summary>
    /// Runs a set of predefined tests for the Radix Bridge API.
    /// </summary>
    private static async Task RunTestsAsync()
    {
        try
        {
            //ok// await TestService.CreateAccountTestAsync();

            //ok// PrivateKey privateKey = new(Encoders.Hex.DecodeData("d5a3e1f65e36c70ee8d1712f25adafbfd0730e19c22442334ee4b177ea8943df"), Curve.ED25519);
            //ok// await TestService.GetAddressTestAsync(privateKey.PublicKey(), AddressType.Account, NetworkType.Test);

            //ok// await TestService.RestoreAccountTestAsync("bacon pool door document cushion goat mammal design sheriff doll chicken butter fuel swift urge fury lobster agree jewel stock fine crawl beauty farm");

            //ok// await TestService.WithdrawTestAsync(10000, TestData.AccountAddress, TestData.PrivateKey);

            //ok// await TestService.DepositTestAsync(1000, TestData.AccountAddress);

            //ok// await TestService.GetAccountBalanceTestAsync(TechAccountData.AccountAddress);

            //ok// await TestService.GetTransactionStatusTestAsync("txid_tdx_2_1d479uqfc58m340uve6avnlvprd4fw7ua8e5c8f8yu5phtqtwwx5sdr9e23");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during tests: {ex.Message}");
        }
    }
}

/// <summary>
/// Contains technical account data for testing.
/// </summary>
file static class TechAccountData
{
    /// <summary>
    /// Seed phrase used for restoring the account.
    /// </summary>
    public const string SeedPhrase =
        "allow warfare celery man doctor put zoo vibrant dove kitten evil student property language reform castle culture help family observe pretty tag angle father";

    /// <summary>
    /// Public key associated with the technical account.
    /// </summary>
    public const string PublicKey = "5cd9650ea8271a4fcf328a4bcf1dcc6727d3fab1c404ff64982e7ee1e8b65fad";

    /// <summary>
    /// Private key associated with the technical account.
    /// </summary>
    public const string PrivateKey = "580d848fcb51369d392a58a40b34a73ddb322e7139d0da2542584323edf62be1";

    /// <summary>
    /// Address of the technical account.
    /// </summary>
    public const string AccountAddress = "account_tdx_2_12952p9zm5ech2x3fc65xujk04c8lewncjwvn4lj6ylsjnl3rmm55gm";
}

/// <summary>
/// Contains test data used for validating API functions.
/// </summary>
file static class TestData
{
    /// <summary>
    /// Transaction address used for testing transaction status retrieval.
    /// </summary>
    public const string TransactionAddress = "txid_tdx_2_12spde0frhxh3yqxxjkjymhp2mhxzuruk8hznqkah4zn85kje6g8s77n62w";

    /// <summary>
    /// URL of the Radix test network.
    /// </summary>
    public const string Url = "https://stokenet-core.radix.live";

    /// <summary>
    /// Public key used for test transactions.
    /// </summary>
    public const string PublicKey = "3f37990316fa1a0862b44c59e4645a3ab5d53e0018a8929ec07dd6b970837091";

    /// <summary>
    /// Private key used for test transactions.
    /// </summary>
    public const string PrivateKey = "e1b6d57310b22767097717ecc4758f9bd5129f473e1dba99898702f279d5ba7c";

    /// <summary>
    /// Primary test account address.
    /// </summary>
    public const string AccountAddress = "account_tdx_2_1283sedrjsv05ltuvsylegh8aqnl98mm4ycmxvxgq0qe8j47cakpahl";

    /// <summary>
    /// Secondary test account address.
    /// </summary>
    public const string AccountAddress1 = "account_tdx_2_128flnh8mpmlkwu90cthml28zzd6wprfql33u979zggpzqt2t65apjf";

    /// <summary>
    /// Additional test account address.
    /// </summary>
    public const string AccountAddress2 = "account_tdx_2_12y5ff8lafrhj4h7x874qhmyunv8mwxqurj6uactl7xjwvkscewq89w";

    /// <summary>
    /// Additional test account address.
    /// </summary>
    public const string AccountAddress3 = "account_tdx_2_129s2hahgsm7gyyfnpan0l4v0ve3yuuv4gkj9uur5nj9k0rxy0mczfk";

    /// <summary>
    /// Additional test account address.
    /// </summary>
    public const string AccountAddress4 = "account_tdx_2_129xflqtvkazcz57vaq0nymj93awh734896lejple3s0xun9qxrpzha";
}