using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;
using VDS.RDF;
using VDS.RDF.Parsing;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests")]
[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests")]

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

/// <summary>
/// Reads resources other applications keep in the user's Pod so NativeDataImportManager can holonize them. Each container
/// listed in <see cref="NativeImportContainers"/> is a source; each resource directly inside it is a record keyed by its
/// path from the Pod root, paged in path order. RDF resources contribute their literal properties as fields; any other
/// resource contributes its content type and size. OASIS's own <c>oasis/</c> tree is never read.
/// </summary>
public partial class SOLIDOASIS : INativeDataImportProvider
{
    private const string LdpContains = "http://www.w3.org/ns/ldp#contains";
    private const int MaximumResourceBytes = 1024 * 1024;

    /// <summary>Comma-separated container paths relative to the Pod root, for example "public/people/" (DNA SOLIDOASIS.NativeImportContainers).</summary>
    public string NativeImportContainers { get; set; }

    public API.Core.Enums.ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.SOLIDOASIS;

    public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
        try
        {
            var sources = new List<NativeDataSource>();
            foreach (var path in AllowedNativeContainers())
            {
                using var graph = await ReadPrivateContainerPagesAsync(ContainerUri(path));
                sources.Add(new NativeDataSource(path, "container", ContainedResources(graph, ContainerUri(path)).Count(resource => !resource.EndsWith("/", StringComparison.Ordinal))));
            }
            result.Result = sources;
            result.Message = $"{sources.Count} allowed SOLID containers.";
        }
        catch (Exception cause) when (cause is InvalidOperationException or HttpRequestException or InvalidDataException or ArgumentException)
        {
            OASISErrorHandling.HandleError(ref result, $"Listing native SOLID containers failed: {cause.Message}", cause);
            result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_LIST_FAILED";
        }
        return result;
    }

    public async Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<NativeRecordPage>();
        try
        {
            if (pageSize < 1) throw new ArgumentException("Page size must be at least 1.");
            if (!AllowedNativeContainers().Contains(sourceName, StringComparer.Ordinal))
                throw new ArgumentException($"'{sourceName}' is not a container listed in NativeImportContainers.");

            var container = ContainerUri(sourceName);
            List<string> resources;
            using (var graph = await ReadPrivateContainerPagesAsync(container))
                resources = ContainedResources(graph, container).Where(resource => !resource.EndsWith("/", StringComparison.Ordinal))
                    .Where(resource => continuationToken == null || string.CompareOrdinal(PodPath(resource), continuationToken) > 0)
                    .OrderBy(PodPath, StringComparer.Ordinal).Take(pageSize + 1).ToList();

            var records = new List<NativeRecord>();
            foreach (var resource in resources.Take(pageSize))
                records.Add(await ReadNativeResourceAsync(sourceName, new Uri(resource), cancellationToken));
            result.Result = new NativeRecordPage(records, resources.Count > pageSize ? records[^1].NativeKey : null);
        }
        catch (Exception cause) when (cause is InvalidOperationException or HttpRequestException or InvalidDataException or ArgumentException or RdfParseException)
        {
            OASISErrorHandling.HandleError(ref result, $"Reading native SOLID container '{sourceName}' failed: {cause.Message}", cause);
            result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_READ_FAILED";
        }
        return result;
    }

    private async Task<NativeRecord> ReadNativeResourceAsync(string sourceName, Uri resource, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, resource);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var key = PodPath(resource.AbsoluteUri);
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var length = response.Content.Headers.ContentLength;
        if (length > MaximumResourceBytes)
            throw new InvalidDataException($"{key} is {length} bytes; native import reads resources up to {MaximumResourceBytes} bytes.");

        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (body.Length > MaximumResourceBytes)
            throw new InvalidDataException($"{key} exceeds {MaximumResourceBytes} bytes; native import will not read it.");

        if (!string.Equals(mediaType, "text/turtle", StringComparison.OrdinalIgnoreCase))
            return new NativeRecord(sourceName, key, new Dictionary<string, object>
            { ["contentType"] = mediaType, ["size"] = (long)body.Length });

        return new NativeRecord(sourceName, key, TurtleFields(System.Text.Encoding.UTF8.GetString(body), resource));
    }

    /// <summary>The literal and link properties of the resource's own subject; a predicate seen twice keeps its full URI the second time.</summary>
    internal static Dictionary<string, object> TurtleFields(string turtle, Uri resource)
    {
        using var graph = new Graph { BaseUri = resource };
        using var input = new StringReader(turtle);
        new TurtleParser().Load(graph, input);

        var fields = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var triple in graph.Triples.Where(triple => triple.Subject is IUriNode subject && subject.Uri == resource && triple.Predicate is IUriNode)
                     .OrderBy(triple => ((IUriNode)triple.Predicate).Uri.AbsoluteUri, StringComparer.Ordinal))
        {
            var predicate = ((IUriNode)triple.Predicate).Uri;
            var name = LocalName(predicate);
            if (fields.ContainsKey(name)) name = predicate.AbsoluteUri;
            if (fields.ContainsKey(name)) continue;
            fields[name] = triple.Object switch
            {
                ILiteralNode literal => LiteralValue(literal),
                IUriNode link => link.Uri.AbsoluteUri,
                _ => null
            };
        }
        return fields.Where(field => field.Value != null).ToDictionary(field => field.Key, field => field.Value, StringComparer.Ordinal);
    }

    private static object LiteralValue(ILiteralNode literal)
    {
        var type = literal.DataType?.AbsoluteUri;
        var xsd = "http://www.w3.org/2001/XMLSchema#";
        if (type == xsd + "integer" || type == xsd + "int" || type == xsd + "long")
            return long.TryParse(literal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole) ? whole : literal.Value;
        if (type == xsd + "decimal" || type == xsd + "double" || type == xsd + "float")
            return double.TryParse(literal.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var real) ? real : literal.Value;
        if (type == xsd + "boolean")
            return bool.TryParse(literal.Value, out var flag) ? flag : literal.Value;
        return literal.Value;
    }

    private static string LocalName(Uri predicate)
    {
        var text = predicate.AbsoluteUri;
        var cut = text.LastIndexOfAny(new[] { '#', '/' });
        return cut >= 0 && cut < text.Length - 1 ? text[(cut + 1)..] : text;
    }

    private static IEnumerable<string> ContainedResources(IGraph graph, Uri container) =>
        graph.Triples.Where(triple => triple.Subject is IUriNode subject && subject.Uri == container
                                      && triple.Predicate is IUriNode predicate && predicate.Uri.AbsoluteUri == LdpContains
                                      && triple.Object is IUriNode)
            .Select(triple => ((IUriNode)triple.Object).Uri.AbsoluteUri).Distinct(StringComparer.Ordinal);

    private Uri ContainerUri(string path) => new Uri(_podServerUrl + "/" + path);

    private string PodPath(string absoluteUri) => absoluteUri.Substring(_podServerUrl.Length + 1);

    private IReadOnlyList<string> AllowedNativeContainers()
    {
        var containers = (NativeImportContainers ?? "").Split(',').Select(path => path.Trim().TrimStart('/')).Where(path => path.Length > 0)
            .Select(path => path.EndsWith("/", StringComparison.Ordinal) ? path : path + "/").Distinct(StringComparer.Ordinal).ToList();
        if (containers.Count == 0)
            throw new InvalidOperationException("Set NativeImportContainers to the Pod containers other applications own before importing native data.");
        foreach (var path in containers)
        {
            if (path.Split('/').Any(segment => segment == ".." || segment == "."))
                throw new InvalidOperationException($"'{path}' is not a valid Pod container path.");
            if (path.StartsWith("oasis/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"'{path}' is OASIS's own tree; native import will not read it.");
        }
        return containers;
    }
}
