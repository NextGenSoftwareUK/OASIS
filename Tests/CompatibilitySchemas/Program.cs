using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Path = System.IO.Path;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL;

// Build the registered GraphQL types without starting hosts or invoking any
// business resolver. Read registrations from production source to avoid a
// second hand-maintained list of schema types.
string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
string evidence = Path.GetFullPath(args.Length > 1 ? args[1] : "artifacts/compatibility-schemas");
Directory.CreateDirectory(evidence);
var services = new[] {
    ("WEB4", typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL.Query).Assembly, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs"),
    ("WEB5", typeof(NextGenSoftware.OASIS.STAR.WebAPI.GraphQL.Query).Assembly, "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Program.cs"),
    ("WEB6", typeof(NextGenSoftware.OASIS.Web6.WebAPI.GraphQL.Query).Assembly, "WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Program.cs"),
    ("WEB7", typeof(NextGenSoftware.OASIS.Web7.WebAPI.GraphQL.Query).Assembly, "WEB7/NextGenSoftware.OASIS.Web7.WebAPI/Program.cs"),
    ("WEB8", typeof(NextGenSoftware.OASIS.Web8.WebAPI.GraphQL.Query).Assembly, "WEB8/NextGenSoftware.OASIS.Web8.WebAPI/Program.cs"),
    ("WEB9", typeof(NextGenSoftware.OASIS.Web9.WebAPI.GraphQL.Query).Assembly, "WEB9/NextGenSoftware.OASIS.Web9.WebAPI/Program.cs"),
    ("WEB10", typeof(NextGenSoftware.OASIS.Web10.WebAPI.GraphQL.Query).Assembly, "WEB10/NextGenSoftware.OASIS.Web10.WebAPI/Program.cs")
};
var results = new List<object>();
int failures = 0;
foreach (var (service, assembly, sourcePath) in services)
{
    var collection = new ServiceCollection();
    collection.AddLogging();
    collection.AddHttpContextAccessor();
    var builder = collection.AddGraphQLServer();
    builder.DisableIntrospection(false);
    if (service == "WEB4") builder.AddType(new HotChocolate.Types.AnyType("JSON")).BindRuntimeType<Dictionary<string, object>>("JSON");
    if (service == "WEB4") builder.AddOasisDataTypes();
    var source = await File.ReadAllTextAsync(Path.Combine(root, sourcePath));
    Type Resolve(string name) => assembly.GetType(name) ?? assembly.GetTypes().Single(t => (t.Name == name || t.FullName?.EndsWith("." + name, StringComparison.Ordinal) == true) && t.Namespace?.Contains("GraphQL") == true);
    foreach (Match match in Regex.Matches(source, @"\.Add(Query|Mutation)Type<([\w.]+)>\(\)"))
    {
        if (match.Groups[1].Value == "Query") builder.AddQueryType(Resolve(match.Groups[2].Value));
        else builder.AddMutationType(Resolve(match.Groups[2].Value));
    }
    foreach (Match match in Regex.Matches(source, @"\.AddType<([\w.]+)>\(\)")) builder.AddType(Resolve(match.Groups[1].Value));
    if (service == "WEB4") builder.TryAddTypeInterceptor<NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL.OASISMethodFieldTypeInterceptor>();
    try
    {
        await using var provider = collection.BuildServiceProvider();
        var executor = await provider.GetRequestExecutorAsync();
        var schema = executor.Schema;
        var probe = await executor.ExecuteAsync("{ __typename }");
        try
        {
            if (probe is OperationResult operation && operation.Errors?.Count > 0)
                throw new InvalidOperationException($"{service}: root typename execution failed.");
        }
        finally { await probe.DisposeAsync(); }
        var introspection = await executor.ExecuteAsync("query AuditSchema { __schema { queryType { fields { name } } mutationType { fields { name } } } }");
        try
        {
            if (introspection is OperationResult metadata && metadata.Errors?.Count > 0)
                throw new InvalidOperationException($"{service}: schema introspection failed: {string.Join("; ", metadata.Errors.Select(e => e.Message))}");
        }
        finally { await introspection.DisposeAsync(); }
        if (service == "WEB4")
        {
            var settings = schema.MutationType!.Fields["updateHyperDriveSettings"].Arguments["settings"];
            if (settings.Type.NamedType() is not AnyType) throw new InvalidOperationException("Settings input must use a JSON scalar, not KeyValuePair input objects.");
            var dataType = schema.Types.GetType<HotChocolate.Types.ObjectType>("IAvatarDetail");
            if (dataType.Fields.Any(f => f.Name is "load" or "save" or "addHolon" or "globalHolonData"))
                throw new InvalidOperationException("Model data must not expose persistence methods or infrastructure services.");
        }
        await File.WriteAllTextAsync(Path.Combine(evidence, service.ToLowerInvariant() + ".local.graphql"), schema.ToString());
        var fields = new[] { ("query", schema.QueryType), ("mutation", schema.MutationType), ("subscription", schema.SubscriptionType) }
            .Where(x => x.Item2 != null).SelectMany(x => x.Item2!.Fields.Where(f => !f.Name.StartsWith("__"))
                .Select(f => new { Kind = x.Item1, Name = f.Name })).ToArray();
        var result = new { Service = service, Result = "PASS", Fields = fields };
        results.Add(result);
        Console.WriteLine($"{service}: schema PASS, {fields.Length} root fields");
    }
    catch (SchemaException ex)
    {
        failures++;
        var errors = ex.Errors.Select(e => new { e.Code, e.Message, Detail = e.ToString() }).ToArray();
        results.Add(new { Service = service, Result = "FAIL", Errors = errors });
        Console.WriteLine(JsonSerializer.Serialize(new { Service = service, Result = "FAIL", Errors = errors }));
    }
}
await File.WriteAllTextAsync(Path.Combine(evidence, "local-schema-results.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
return failures == 0 ? 0 : 1;
