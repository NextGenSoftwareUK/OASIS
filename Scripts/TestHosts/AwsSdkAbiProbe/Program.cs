using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

if (args.Length != 1) throw new ArgumentException("Supply exactly one compiled API output directory.");
var directory = Path.GetFullPath(args[0]);
var dependencies = Path.Combine(directory, "NextGenSoftware.OASIS.Web6.WebAPI.deps.json");
using var graph = JsonDocument.Parse(File.ReadAllText(dependencies));
var packages = graph.RootElement.GetProperty("libraries").EnumerateObject()
    .Where(item => item.Name.StartsWith("AWSSDK.", StringComparison.Ordinal))
    .Select(item => item.Name.Split('/')).ToArray();
if (packages.Length == 0 || packages.Any(item => item.Length != 2))
    throw new InvalidOperationException("The compiled WEB6 graph has no valid AWS SDK packages.");
var majors = packages.Select(item => Version.Parse(item[1]).Major).Distinct().ToArray();
if (majors.Length != 1)
    throw new InvalidOperationException("The compiled WEB6 graph mixes incompatible AWS SDK major versions.");

AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(directory, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
foreach (var name in new[] { "AWSSDK.Core", "AWSSDK.DynamoDBv2", "AWSSDK.S3", "AWSSDK.BedrockRuntime" })
{
    var path = Path.Combine(directory, name + ".dll");
    if (!File.Exists(path)) throw new FileNotFoundException("Required shipping AWS SDK assembly is missing.", name);
    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    // Loading every real type reproduces the runtime ABI failure without constructing
    // clients, loading OASIS DNA, starting providers or making network requests.
    var count = assembly.GetTypes().Length;
    if (count == 0) throw new InvalidOperationException("An AWS SDK assembly contains no loadable types.");
    Console.WriteLine($"AWS_SDK_ABI: assembly={name} types={count} major={majors[0]}");
}
Console.WriteLine("AWS_SDK_ABI_ACCEPTED: four shipping assemblies loaded; no clients constructed or network operations.");
