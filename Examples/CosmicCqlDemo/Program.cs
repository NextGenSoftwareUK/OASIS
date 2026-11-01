using System.Text.Json;
using CosmicCqlDemo;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

// Explicit read-only Base source; reuse its retained stable catalogue.
// Explicit anonymous Optimism read profile; no wallet or fixture creation.
if (args.Length == 2 && args[0] == "--linea-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await LineaPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

if (args.Length == 2 && args[0] == "--scroll-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await ScrollPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

if (args.Length == 2 && args[0] == "--zksync-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await ZkSyncPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

if (args.Length == 2 && args[0] == "--abstract-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await AbstractPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

if (args.Length == 2 && args[0] == "--sei-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await SeiPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Query explicit TON descriptors through shared HolonManager/LINQ.
// No wallet or source write; preserve the reusable catalogue and owner.
if (args.Length == 2 && args[0] == "--ton-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await TonPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Read explicit historical Arweave keys through shared HolonManager/LINQ.
// This anonymous profile never signs transactions or scans the chain.
if (args.Length == 2 && args[0] == "--arweave-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await ArweavePublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Read historical Cardano headers through the shared manager and LINQ planner.
// No wallet or source transaction is involved; reuse your stable catalogue.
if (args.Length == 2 && args[0] == "--cardano-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await CardanoPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Query explicitly selected accepted Starknet headers through shared manager/LINQ.
// This anonymous read profile cannot become a wallet or replication authority.
if (args.Length == 2 && args[0] == "--starknet-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await StarknetPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Query public Radix resources with one acknowledged ledger state per query.
// No wallet, transaction signing or write-provider enrollment is involved.
if (args.Length == 2 && args[0] == "--radix-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await RadixPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Select explicitly enrolled public MultiversX accounts through native filters.
// The existing signed Elrond wallet provider is not invoked by this read profile.
if (args.Length == 2 && args[0] == "--elrond-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await ElrondPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Query public Sui objects under one GraphQL checkpoint per source query.
// The existing signed-storage gRPC bridge remains separate and is not invoked.
if (args.Length == 2 && args[0] == "--sui-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await SuiPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Read bounded Hedera Mainnet mirror account observations through CQL.
// Native timestamps are retained; no SDK payment or transaction is submitted.
if (args.Length == 2 && args[0] == "--hedera-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await HederaPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Read public Aptos accounts at one explicit version per source query.
// No key, transaction or account creation is required.
if (args.Length == 2 && args[0] == "--aptos-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await AptosPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Query current public Algorand accounts through HolonManager and LINQ.
// Each row carries its own observed round; this is not a historical snapshot.
if (args.Length == 2 && args[0] == "--algorand-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await AlgorandPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Read the retained Stellar ledger catalogue through the shared manager/LINQ API.
// The source verifies complete native XDR; this demo does not sign transactions.
if (args.Length == 2 && args[0] == "--stellar-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await StellarPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Query the retained public XRPL ledger hashes through HolonManager and LINQ.
// This read-only example needs no wallet and submits no transaction.
if (args.Length == 2 && args[0] == "--xrpl-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await XrplPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Read a small explicit Bitcoin header catalogue through the shared CQL manager.
// This read profile never signs or broadcasts a Bitcoin transaction.
if (args.Length == 2 && args[0] == "--bitcoin-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    await BitcoinPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

if (args.Length == 2 && args[0] == "--berachain-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await BerachainPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

if (args.Length == 2 && args[0] == "--monad-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await MonadPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

if (args.Length == 2 && args[0] == "--optimism-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await OptimismPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

if (args.Length == 2 && args[0] == "--base-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await BasePublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Explicit read-only Avalanche source; reuse its retained stable catalogue.
if (args.Length == 2 && args[0] == "--avalanche-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await AvalanchePublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Explicit read-only BNBChain source; reuse its retained stable catalogue.
if (args.Length == 2 && args[0] == "--bnb-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await BnbPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// NEAR uses an explicitly configured source and a bounded demo lifetime.
// No retry or source substitution occurs if that endpoint is unavailable.
if (args.Length == 2 && args[0] == "--near-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    await NearPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Polkadot reads only the retained hash catalogue and verifies header contents.
// Keep this read-only path ahead of the SQLite database fixture setup.
if (args.Length == 2 && args[0] == "--polkadot-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await PolkadotPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Cosmos reads configured validators at an explicitly pinned state height.
// Native3/2 is verified; hosted/federation acceptance is tracked in WIP.
if (args.Length == 2 && args[0] == "--cosmos-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await CosmosPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Arbitrum uses the official finalized-state RPC with its fixed /rpc path.
// The bounded console walkthrough needs no wallet, funding or local chain.
if (args.Length == 2 && args[0] == "--arbitrum-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await ArbitrumPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Polygon uses its fixed mainnet descriptor, not the local signed-storage demo.
// This console command reads the existing catalogue and creates no fixture.
if (args.Length == 2 && args[0] == "--polygon-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await PolygonPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// The Ethereum read profile is separate from wallet/transaction storage.
// Reuse an existing public catalogue, query once, and exit without local fixtures.
if (args.Length == 2 && args[0] == "--ethereum-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await EthereumPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// The public Google console path performs no database setup or writes.
// Keep its bounded network lifetime independent of the SQLite fixture below.
if (args.Length == 2 && args[0] == "--google-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await GooglePublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Query Azure's real public object properties without downloading dataset bodies.
// This source is independent of the pending hosted Cosmos DB demonstration.
if (args.Length == 2 && args[0] == "--azure-blob-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await AzureBlobPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// Fantom Opera uses an explicit canonical-at-read profile, not finalized state.
// Reuse the stable public catalogue; no wallet or source writes are involved.
if (args.Length == 2 && args[0] == "--fantom-public")
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await FantomPublicWalkthrough.RunAsync(args[1], Console.Out, deadline.Token);
    return 0;
}

// HOST SETUP - console mode runs once; --serve also opens the local Swagger API.
var serve = args.Length == 1 && args[0] == "--serve";
if (args.Length != 0 && !serve)
{
    Console.Error.WriteLine("Use no arguments, --serve, --fantom-public <existing-source.json>, --azure-blob-public <existing-source.json>, --cardano-public <existing-source.json>, --starknet-public <existing-source.json>, --radix-public <existing-source.json>, --google-public <existing-source.json>, --ethereum-public <existing-source.json>, --polygon-public <existing-source.json>, --arbitrum-public <existing-source.json>, --cosmos-public <existing-source.json>, --polkadot-public <existing-source.json>, --near-public <existing-source.json>, or --base-public / --avalanche-public / --bnb-public <existing-source.json>.");
    return 2;
}

// Find the existing checkout rather than creating another environment.
var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root != null && !File.Exists(Path.Combine(root.FullName,
    "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")))
    root = root.Parent;
if (root == null)
{
    Console.Error.WriteLine("Run this sample from its existing OASIS checkout.");
    return 2;
}

// FIXTURE OWNERSHIP - a fixed synthetic database is protected by an exclusive lock.
// Refuse an existing fixture so a failed run can be inspected before cleanup.
var work = Path.GetFullPath(Path.Combine(root.FullName, "TestResults/CosmicCqlDemo"));
Directory.CreateDirectory(work);
foreach (var directory in new[] { new DirectoryInfo(work), new DirectoryInfo(work).Parent! })
    if (directory.LinkTarget != null)
        throw new InvalidOperationException("The owned demo work directories must not be links.");
var database = Path.Combine(work, "demo.db");
var manifest = Path.Combine(work, "demo-manifest.json");
var files = new[] { database, database + "-wal", database + "-shm" };
var ownsFixture = false;
var resultPath = Path.Combine(work, serve ? "api-demo-result.json" : "demo-result.json");

try
{
    using var lease = new FileStream(Path.Combine(work,
        "environment.lock"),
        FileMode.OpenOrCreate,
        FileAccess.ReadWrite,
        FileShare.None);
    if (files.Any(File.Exists) || File.Exists(manifest))
        throw new InvalidOperationException("An existing fixture requires review. Refusing to overwrite or replay it.");

    // Record ownership before the walkthrough can create any database records.
    var runId = Guid.NewGuid();
    var startedUtc = DateTime.UtcNow;
    void RecordManifest(Guid? actor = null,
        IReadOnlyList<Guid>? ids = null) => File.WriteAllText(manifest,
        JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            runId,
            database,
            startedUtc,
            purpose = "Synthetic COSMIC CQL coffee walkthrough",
            actor,
            ids
        }));
    RecordManifest();
    ownsFixture = true;

    // Two separate provider/manager instances demonstrate a real stale-client save.
    DemoReport report;
    using (var provider = new SQLLiteDBOASIS($"Data Source={database};Pooling=False") { IsVersionControlEnabled = true })
    using (var secondProvider = new SQLLiteDBOASIS($"Data Source={database};Pooling=False") { IsVersionControlEnabled = true })
    {
        var activated = await provider.ActivateProviderAsync();
        if (activated.IsError)
            throw new InvalidOperationException(activated.Message, activated.Exception);
        activated = await secondProvider.ActivateProviderAsync();
        if (activated.IsError)
            throw new InvalidOperationException(activated.Message, activated.Exception);

        // Use Hyperdrive V2 with a fixed local provider for reproducible examples.
        // Replication and routing policies are outside this focused query demo.
        HolonManager Manager(SQLLiteDBOASIS storage)
        {
            var dna = new OASISDNA
            {
                OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                {
                    HyperDriveMode = HyperDriveModes.V2,
                    StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
                }
            };
            var registry = new ProviderManager(null,
                dna)
            {
                IsAutoFailOverEnabled = false,
                IsAutoReplicationEnabled = false,
                IsAutoLoadBalanceEnabled = false
            };
            return new HolonManager(storage, dna, registry);
        }

        // Run the annotated C# examples before optionally serving HTTP requests.
        var manager = Manager(provider);
        report = await CoffeeWalkthrough.RunAsync(manager,
            Manager(secondProvider),
            provider,
            secondProvider,
            Console.Out,
            seeding: (actor, ids) => RecordManifest(actor, ids));
        if (serve)
        {
            File.WriteAllText(resultPath,
                JsonSerializer.Serialize(report,
                new JsonSerializerOptions { WriteIndented = true }));
            await CoffeeApiHost.RunAsync(manager, provider, root.FullName);
        }
    }

    // SUCCESSFUL TEARDOWN - providers are disposed before exact owned files are removed.
    // Reports remain available as verification evidence.
    foreach (var path in files)
    {
        if (Path.GetDirectoryName(Path.GetFullPath(path)) != work)
            throw new InvalidOperationException("Cleanup escaped the exact owned directory.");
        if (File.Exists(path))
            File.Delete(path);
    }
    File.Delete(manifest);
    File.WriteAllText(resultPath,
        JsonSerializer.Serialize(report,
        new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("\nPASS: queries, clean tracking, no-op saves, committed deltas and cross-client conflicts verified.");
    Console.WriteLine("Owned demo database removed. Evidence: " + resultPath);
    return 0;
}
// Keep failed fixtures and the original exception for diagnosis.
catch (Exception error)
{
    Console.Error.WriteLine("Demo failed: " + error);
    if (ownsFixture)
        Console.Error.WriteLine("Preserved owned fixture for inspection: " + work);
    return 1;
}
