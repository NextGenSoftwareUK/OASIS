using System.Text.Json;
using CosmicCqlDemo;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

// HOST SETUP - console mode runs once; --serve also opens the local Swagger API.
var serve = args.Length == 1 && args[0] == "--serve";
if (args.Length != 0 && !serve)
{
    Console.Error.WriteLine("Use no arguments for console mode, or --serve for the loopback Swagger demo.");
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
