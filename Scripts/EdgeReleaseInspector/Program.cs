using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using System.IO.Compression;
using System.Xml.Linq;

if (args.Length is < 5 or > 6)
    throw new ArgumentException("Usage: EdgeReleaseInspector <full.dll> <edge.dll> <artifact-dir> <commit> <configuration> [SqliteMvp|HoloEnabled]");

string fullAssembly = Path.GetFullPath(args[0]);
string edgeAssembly = Path.GetFullPath(args[1]);
string output = Path.GetFullPath(args[2]);
string profile = args.Length == 6 ? args[5] : "HoloEnabled";
if (profile is not ("SqliteMvp" or "HoloEnabled"))
    throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown Edge release profile.");
Directory.CreateDirectory(output);
if (!File.Exists(fullAssembly) || !File.Exists(edgeAssembly))
    throw new FileNotFoundException("Both Full and Edge Native Endpoint assemblies must exist before report generation.");

var fullApi = ReadApi(fullAssembly);
var edgeApi = ReadApi(edgeAssembly);
WriteJson(Path.Combine(output, "api-compatibility.json"), new
{
    schemaVersion = 1,
    fullAssembly = Path.GetFileName(fullAssembly),
    edgeAssembly = Path.GetFileName(edgeAssembly),
    commonPublicMembers = fullApi.Intersect(edgeApi, StringComparer.Ordinal).Order().ToArray(),
    fullOnlyPublicMembers = fullApi.Except(edgeApi, StringComparer.Ordinal).Order().ToArray(),
    edgeOnlyPublicMembers = edgeApi.Except(fullApi, StringComparer.Ordinal).Order().ToArray(),
    fullPublicMemberCount = fullApi.Count,
    edgePublicMemberCount = edgeApi.Count
});

var dependencies = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var releasePackages = new List<PackageEvidence>();
foreach (string depsPath in Directory.EnumerateFiles(Path.GetDirectoryName(edgeAssembly)!, "*.deps.json"))
{
    using var document = JsonDocument.Parse(File.ReadAllText(depsPath));
    if (!document.RootElement.TryGetProperty("libraries", out var libraries)) continue;
    foreach (var library in libraries.EnumerateObject())
    {
        int separator = library.Name.LastIndexOf('/');
        string name = separator < 0 ? library.Name : library.Name[..separator];
        string version = separator < 0 ? "unknown" : library.Name[(separator + 1)..];
        if (library.Value.TryGetProperty("type", out var type) && type.GetString() == "package")
            dependencies[name] = version;
    }
}
string[] forbiddenEdgeRuntimeLibraries =
{
    "NextGenSoftware.OASIS.API.Core",
    "NextGenSoftware.OASIS.Common",
    "Microsoft.Data.SqlClient",
    "MailKit",
    "NBitcoin",
    "AutoMapper"
};
string[] forbiddenPresent = forbiddenEdgeRuntimeLibraries
    .Where(name => dependencies.ContainsKey(name))
    .OrderBy(name => name, StringComparer.Ordinal)
    .ToArray();
if (forbiddenPresent.Length != 0)
    throw new InvalidDataException(
        "The Edge runtime dependency closure contains full/server-only libraries: " +
        string.Join(", ", forbiddenPresent) + ". HyperDrive portable contracts must remain isolated from Full Core.");
foreach (string packagePath in Directory.EnumerateFiles(output, "*.nupkg"))
{
    using var archive = ZipFile.OpenRead(packagePath);
    var nuspec = archive.Entries.SingleOrDefault(x => x.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
    if (nuspec == null) throw new InvalidDataException($"NuGet package '{packagePath}' has no nuspec manifest.");
    using var stream = nuspec.Open();
    XDocument manifest = XDocument.Load(stream);
    XNamespace ns = manifest.Root?.Name.Namespace ?? XNamespace.None;
    XElement metadata = manifest.Root?.Element(ns + "metadata") ?? throw new InvalidDataException($"NuGet package '{packagePath}' has no metadata.");
    string packageName = metadata.Element(ns + "id")?.Value ?? throw new InvalidDataException("NuGet package id is missing.");
    string packageVersion = metadata.Element(ns + "version")?.Value ?? "unknown";
    dependencies[packageName] = packageVersion;
    releasePackages.Add(new PackageEvidence(packageName, packageVersion, Path.GetFileName(packagePath), Hash(packagePath)));
    foreach (var dependency in metadata.Descendants(ns + "dependency"))
    {
        string? id = dependency.Attribute("id")?.Value;
        if (!string.IsNullOrWhiteSpace(id)) dependencies[id] = dependency.Attribute("version")?.Value ?? "unknown";
    }
}

WriteJson(Path.Combine(output, "sbom.spdx.json"), new
{
    spdxVersion = "SPDX-2.3",
    dataLicense = "CC0-1.0",
    SPDXID = "SPDXRef-DOCUMENT",
    name = "OASIS-Edge-Runtime",
    documentNamespace = $"https://oasisomniverse.one/spdx/edge/{args[3]}",
    creationInfo = new { created = DateTime.UtcNow.ToString("O"), creators = new[] { "Tool: OASIS-EdgeReleaseInspector-1" } },
    packages = dependencies.Select(x => new
    {
        name = x.Key, SPDXID = "SPDXRef-Package-" + SafeId(x.Key), versionInfo = x.Value,
        downloadLocation = "NOASSERTION", filesAnalyzed = false,
        licenseConcluded = "NOASSERTION", licenseDeclared = "NOASSERTION"
    }).ToArray()
});

string[] requiredTestReports =
{
    "dna.trx", "core-hyperdrive.trx", "edge-store.trx", "edge-runtime.trx", "onet-sync.trx",
    "offline-session-grants.trx", "hosted-mongo-sync.trx", "hosted-mongo-process-kill.trx"
};
var minimumExecutedTests = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    ["dna.trx"] = 11,
    ["core-hyperdrive.trx"] = 167,
    ["edge-store.trx"] = 26,
    ["edge-runtime.trx"] = 49,
    ["onet-sync.trx"] = 9,
    ["offline-session-grants.trx"] = 12,
    ["hosted-mongo-sync.trx"] = 24,
    ["hosted-mongo-process-kill.trx"] = 1
};
var testEvidence = requiredTestReports.Select(name => ReadTestEvidence(Path.Combine(output, name))).ToArray();
if (testEvidence.Any(test => test.Executed <= 0 || test.Failed != 0 || test.Errors != 0 ||
                             test.Timeouts != 0 || test.Aborted != 0))
    throw new InvalidDataException("Every required release test report must contain executed tests with no failures, errors, timeouts or aborted tests.");
foreach (var test in testEvidence)
    if (test.Executed < minimumExecutedTests[test.File])
        throw new InvalidDataException($"Release test report '{test.File}' executed {test.Executed} tests; at least {minimumExecutedTests[test.File]} are required by this release baseline.");
string hAppManifestPath = Path.Combine(output, "holooasis-happ-build-manifest.json");
object? holoOasisEvidence = null;
if (profile == "HoloEnabled")
{
    if (!File.Exists(hAppManifestPath))
        throw new FileNotFoundException("The verified HoloOASIS hApp build manifest is missing.", hAppManifestPath);
    using var hAppManifestDocument = JsonDocument.Parse(File.ReadAllText(hAppManifestPath));
    string hAppSourceCommit = hAppManifestDocument.RootElement.GetProperty("sourceCommit").GetString() ??
        throw new InvalidDataException("The HoloOASIS hApp build manifest has no source commit.");
    holoOasisEvidence = new
    {
        buildManifest = Path.GetFileName(hAppManifestPath),
        buildManifestSha256 = Hash(hAppManifestPath),
        sourceCommit = hAppSourceCommit
    };
}
string unityPackagePath = Path.Combine(output, "com.nextgensoftware.oasis.edge.tgz");
string unityManifestPath = Path.Combine(output, "unity-package-build-manifest.json");
string unityValidationLogPath = Path.Combine(output, "unity-edge-validation.log");
string unityAndroidValidationLogPath = Path.Combine(output, "unity-edge-android-validation.log");
string ourWorldValidationLogPath = Path.Combine(output, "our-world-edge-integration.log");
if (!File.Exists(unityPackagePath) || !File.Exists(unityManifestPath) || !File.Exists(unityValidationLogPath) ||
    !File.Exists(unityAndroidValidationLogPath) || !File.Exists(ourWorldValidationLogPath))
    throw new FileNotFoundException("The generated Unity Edge package, build manifest, Editor validation, Android player-build and Our World integration logs are required.");
string unityManifestHash = Hash(unityManifestPath);
if (!File.ReadAllText(unityValidationLogPath).Contains(
        $"OASIS_EDGE_UNITY_PACKAGE_VALIDATION_PASSED manifest={unityManifestHash}", StringComparison.Ordinal) ||
    !File.ReadAllText(unityAndroidValidationLogPath).Contains(
        $"OASIS_EDGE_ANDROID_BUILD_VALIDATION_PASSED manifest={unityManifestHash}", StringComparison.Ordinal))
    throw new InvalidDataException("Unity release evidence does not certify the exact submitted package manifest for both Editor and Android validation.");
if (!File.ReadAllText(ourWorldValidationLogPath).Contains(
        "OASIS_OUR_WORLD_EDGE_INTEGRATION_VALIDATION_PASSED", StringComparison.Ordinal))
    throw new InvalidDataException("Our World did not compile with the submitted Edge integration.");
using var unityManifestDocument = JsonDocument.Parse(File.ReadAllText(unityManifestPath));
var unityFiles = unityManifestDocument.RootElement.GetProperty("files").EnumerateArray()
    .Select(x => x.GetProperty("path").GetString() ?? string.Empty).ToHashSet(StringComparer.Ordinal);
string[] requiredUnityAssets =
{
    "Runtime/Plugins/Managed/NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge.dll",
    "Runtime/Plugins/Managed/NextGenSoftware.OASIS.HyperDrive.Synchronization.dll",
    "Runtime/Plugins/Managed/NextGenSoftware.OASIS.Contracts.dll",
    "Runtime/Plugins/Android/libs/armeabi-v7a/libe_sqlite3.so",
    "Runtime/Plugins/Android/libs/arm64-v8a/libe_sqlite3.so",
    "Runtime/Plugins/Android/libs/x86/libe_sqlite3.so",
    "Runtime/Plugins/Android/libs/x86_64/libe_sqlite3.so",
    "Runtime/Plugins/Android/SecureSessionStore.java",
    "Runtime/Plugins/iOS/device/libe_sqlite3.a",
    "Runtime/Plugins/iOS/simulator/libe_sqlite3.a",
    "Runtime/Plugins/iOS/OASISEdgeKeychain.mm",
    "Runtime/Plugins/x86_64/e_sqlite3.dll",
    "Runtime/Plugins/Linux/x86_64/libe_sqlite3.so",
    "Runtime/Plugins/macOS/arm64/libe_sqlite3.dylib"
};
string[] missingUnityAssets = requiredUnityAssets.Where(x => !unityFiles.Contains(x)).ToArray();
if (missingUnityAssets.Length != 0)
    throw new InvalidDataException("The Unity Edge package manifest is missing required assets: " +
        string.Join(", ", missingUnityAssets));

WriteJson(Path.Combine(output, "acceptance-report.json"), new
{
    schemaVersion = 1,
    commit = args[3],
    configuration = args[4],
    profile,
    generatedUtc = DateTime.UtcNow.ToString("O"),
    fullNativeEndpoint = Hash(fullAssembly),
    edgeNativeEndpoint = Hash(edgeAssembly),
    unityPackage = new
    {
        file = Path.GetFileName(unityPackagePath),
        sha256 = Hash(unityPackagePath),
        buildManifest = Path.GetFileName(unityManifestPath),
        buildManifestSha256 = unityManifestHash,
        unityCompilationLog = Path.GetFileName(unityValidationLogPath),
        unityCompilationLogSha256 = Hash(unityValidationLogPath),
        androidPlayerBuildLog = Path.GetFileName(unityAndroidValidationLogPath),
        androidPlayerBuildLogSha256 = Hash(unityAndroidValidationLogPath),
        ourWorldIntegrationLog = Path.GetFileName(ourWorldValidationLogPath),
        ourWorldIntegrationLogSha256 = Hash(ourWorldValidationLogPath)
    },
    releasePackages = releasePackages.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray(),
    testResults = testEvidence,
    holoOasisHApp = holoOasisEvidence,
    apiCompatibilityReport = "api-compatibility.json",
    sbom = "sbom.spdx.json"
});

var checksumFiles = Directory.EnumerateFiles(output)
    .Where(x => Path.GetExtension(x).Equals(".nupkg", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(x).Equals(".trx", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(x).Equals(".tgz", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(x) is "api-compatibility.json" or "sbom.spdx.json" or
                    "acceptance-report.json" or "holooasis-happ-build-manifest.json" or
                    "unity-package-build-manifest.json" or "unity-edge-validation.log" or
                    "unity-edge-android-validation.log" or "our-world-edge-integration.log")
    .OrderBy(Path.GetFileName, StringComparer.Ordinal).ToArray();
File.WriteAllLines(Path.Combine(output, "SHA256SUMS.txt"), checksumFiles.Select(x => $"{Hash(x)}  {Path.GetFileName(x)}"));

static SortedSet<string> ReadApi(string assemblyPath)
{
    string directory = Path.GetDirectoryName(assemblyPath)!;
    var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(assemblyPath), isCollectible: true);
    context.Resolving += (_, name) =>
    {
        string candidate = Path.Combine(directory, name.Name + ".dll");
        return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
    };
    try
    {
        Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);
        var members = new SortedSet<string>(StringComparer.Ordinal);
        foreach (Type type in assembly.GetExportedTypes().OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            members.Add("T:" + type.FullName);
            foreach (MemberInfo member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (member.MemberType is MemberTypes.Method or MemberTypes.Constructor or MemberTypes.Property or MemberTypes.Event)
                    members.Add($"{member.MemberType}:{type.FullName}.{member}");
            }
        }
        return members;
    }
    finally { context.Unload(); }
}

static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
static string SafeId(string value) => string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' ? c : '-'));
static void WriteJson(string path, object value) => File.WriteAllText(path,
    JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));

static TestEvidence ReadTestEvidence(string path)
{
    if (!File.Exists(path)) throw new FileNotFoundException("A required release test report is missing.", path);
    XDocument document = XDocument.Load(path);
    XElement counters = document.Descendants().SingleOrDefault(x => x.Name.LocalName == "Counters") ??
        throw new InvalidDataException($"Test report '{path}' has no counters element.");
    int Read(string name) => int.TryParse(counters.Attribute(name)?.Value, out int value) ? value : 0;
    return new TestEvidence(Path.GetFileName(path), Hash(path), Read("executed"), Read("passed"),
        Read("failed"), Read("error"), Read("timeout"), Read("aborted"));
}

internal sealed record TestEvidence(string File, string Sha256, int Executed, int Passed, int Failed,
    int Errors, int Timeouts, int Aborted);
internal sealed record PackageEvidence(string Id, string Version, string File, string Sha256);
