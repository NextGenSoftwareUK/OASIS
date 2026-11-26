using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jint;
using Jint.Runtime;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>
/// Runs the SQL-like CQL parser and the question prompt (Services/Cql/Text/*.js), which are the single source of
/// truth for both. Every call gets a fresh, bounded JavaScript engine; no .NET objects are exposed to it.
/// </summary>
public sealed class CqlTextCompiler
{
    private static readonly Prepared<Acornima.Ast.Script> Parser = Load("cql-text.js");
    private static readonly Prepared<Acornima.Ast.Script> Ask = Load("cql-ask.js");

    // The system prompt the sandbox sends: only ready providers, plus the key sources the host discovered.
    private const string SystemPromptScript = @"(() => {
  const sources = JSON.parse(sourcesJson);
  const profiles = CqlText.discoverKeyProfiles(sources);
  const ready = [...CqlText.providers, ...Object.keys(profiles)]
    .filter(provider => sources.some(source => source.provider === provider && source.available))
    .filter(provider => !only || provider === only);
  if (!ready.length) return null;
  const keys = Object.entries(profiles).filter(([provider]) => !example || provider === only).map(([provider, { key, maximumKeys }]) => provider + ':metadata.' + key + '(' + maximumKeys + ')');
  return CqlAsk.queryInstructions(ready) + '\n' + 'Use only these currently available WEB4 providers: ' + ready.join(', ') + '.' +
    (keys.length ? '\nAdditional ready sources (provider:metadata key, maximum IN keys). Use SELECT * and one equality on a supplied quoted key. IN is allowed only when the bound is positive. No ORDER BY or aggregation. ' + keys.join(', ') : '') +
    (example ? '\nFor this public example, select all fields and use exactly one WHERE comparison on the supplied field. Preserve every supplied key verbatim, using IN for multiple keys. Include the requested LIMIT and, if requested, append TIMEOUT followed by the duration in milliseconds after LIMIT.' : '') +
    (mappingId ? '\nSelected source: the visitor chose saved virtual mapping ' + mappingId + ' and target provider ' + only + '. Preserve both exactly in the statement. Use only logical metadata paths and types supplied in the question; otherwise return UNSUPPORTED.'
      : '\nNo virtual mapping is selected. Do not use WITH VIRTUAL MAPPING.');
})()";

    /// <summary>Compiles SQL-like text into the typed query-cql request it stands for.</summary>
    public CqlTextQuery Compile(string text, string parametersJson = null, string sourcesJson = "[]")
    {
        var json = Run(engine =>
        {
            engine.SetValue("text", text);
            engine.SetValue("parametersJson", string.IsNullOrWhiteSpace(parametersJson) ? "{}" : parametersJson);
            engine.SetValue("sourcesJson", sourcesJson);
            return engine.Evaluate("JSON.stringify(CqlText.compile(text, CqlText.discoverKeyProfiles(JSON.parse(sourcesJson)), JSON.parse(parametersJson)))").AsString();
        });
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new CqlTextQuery(root.GetProperty("path").GetString(), root.GetProperty("body").Clone(),
            root.TryGetProperty("scalarAlias", out var alias) ? alias.GetString() : null);
    }

    /// <summary>The CQL drafting rules for the given providers.</summary>
    public string AskPrompt(params string[] providers) => Run(engine =>
    {
        engine.SetValue("providersJson", JsonSerializer.Serialize(providers));
        return engine.Evaluate("CqlAsk.queryInstructions(JSON.parse(providersJson))").AsString();
    });

    /// <summary>The full system prompt for a host's source catalogue, or null when no source is ready.</summary>
    public string SystemPrompt(string sourcesJson, string provider = null, string virtualMappingId = null, bool publicExample = false) => Run(engine =>
    {
        engine.SetValue("sourcesJson", sourcesJson);
        engine.SetValue("only", provider ?? "");
        engine.SetValue("mappingId", virtualMappingId ?? "");
        engine.SetValue("example", publicExample);
        var value = engine.Evaluate(SystemPromptScript);
        return value.IsNull() ? null : value.AsString();
    });

    /// <summary>SQL-like text for the sources' public row examples, keyed "provider/exampleId".</summary>
    public IReadOnlyDictionary<string, string> ExampleTexts(string sourcesJson) => JsonSerializer.Deserialize<Dictionary<string, string>>(Run(engine =>
    {
        engine.SetValue("sourcesJson", sourcesJson);
        return engine.Evaluate(ExampleTextScript).AsString();
    }));

    private const string ExampleTextScript = @"(() => {
  const sources = JSON.parse(sourcesJson);
  const profiles = CqlText.discoverKeyProfiles(sources);
  const texts = {};
  for (const source of sources) for (const row of source.examples ?? []) {
    if (!['public-records', 'public-posts', 'public-subset'].includes(row.id) || row.operation !== 'Rows') continue;
    try {
      const text = CqlText.publicExample(source.provider, row.request, profiles);
      if (text) texts[source.provider + '/' + row.id] = text;
    } catch (error) { /* an example with no SQL-like form simply has no text */ }
  }
  return JSON.stringify(texts);
})()";

    private static T Run<T>(Func<Engine, T> evaluate)
    {
        try
        {
            var engine = new Engine(options => options.TimeoutInterval(TimeSpan.FromSeconds(2))
                .LimitRecursion(256).MaxStatements(2_000_000));
            engine.Execute(Parser);
            engine.Execute(Ask);
            return evaluate(engine);
        }
        catch (JavaScriptException error) { throw new CqlTextException(error.Message); }
        catch (Exception error) when (error is TimeoutException or StatementsCountOverflowException or RecursionDepthOverflowException)
        {
            throw new CqlTextException("The query is too complex to compile.");
        }
    }

    private static Prepared<Acornima.Ast.Script> Load(string name)
    {
        var assembly = typeof(CqlTextCompiler).Assembly;
        using var stream = assembly.GetManifestResourceStream("OASIS.Cql.Text." + name)
            ?? throw new InvalidOperationException("Missing embedded CQL script " + name);
        using var reader = new StreamReader(stream);
        return Engine.PrepareScript(reader.ReadToEnd());
    }
}

public sealed record CqlTextQuery(string Path, JsonElement Body, string ScalarAlias);

public sealed class CqlTextException : Exception
{
    public CqlTextException(string message) : base(message) { }
}
