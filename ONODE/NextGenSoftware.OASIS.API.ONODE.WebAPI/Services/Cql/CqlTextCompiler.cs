using System;
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
    .filter(provider => sources.some(source => source.provider === provider && source.available));
  if (!ready.length) return null;
  const keys = Object.entries(profiles).map(([provider, { key, maximumKeys }]) => provider + ':metadata.' + key + '(' + maximumKeys + ')');
  return CqlAsk.queryInstructions(ready) + '\n' + 'Use only these currently available WEB4 providers: ' + ready.join(', ') + '.' +
    (keys.length ? '\nAdditional ready sources (provider:metadata key, maximum IN keys). Use SELECT * and one equality on a supplied quoted key. IN is allowed only when the bound is positive. No ORDER BY or aggregation. ' + keys.join(', ') : '') +
    '\nNo virtual mapping is selected. Do not use WITH VIRTUAL MAPPING.';
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
    public string SystemPrompt(string sourcesJson) => Run(engine =>
    {
        engine.SetValue("sourcesJson", sourcesJson);
        var value = engine.Evaluate(SystemPromptScript);
        return value.IsNull() ? null : value.AsString();
    });

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
