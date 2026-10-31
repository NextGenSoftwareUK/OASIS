using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// Native element terminals. The same examples run on SQLite, MongoDB and Neo4j.
/// An empty sequence is different from an execution error, even for OrDefault.
/// </summary>
public static class V1ElementQueries
{
    public static async Task<int> RunAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        CosmicQuery<Holon> visible,
        TextWriter output)
    {
        var completed = 0;
        var ordered = Value(visible.OrderBy(holon => holon.Name));
        var absent = Value(visible.Where(holon => holon.Name == "no such menu item"));
        var one = Value(visible.Where(holon => holon.Name == "C-public"));

        // First asks the database for at most one matching row. Explicit order
        // makes the chosen row deterministic; it does not load the whole menu.
        await Show("47. First / ordered native input", ordered.First(), expectedName: "B-owned");

        // An earlier source window still applies. First does not reset Skip.
        await Show("48. First / source Skip is preserved", ordered.Skip(1).First(), expectedName: "C-public");

        // First requires an element. An empty source is an explicit error.
        await Show("49. First / empty source", absent.First(), error: "CQL_SEQUENCE_EMPTY");

        // FirstOrDefault returns a successful null only when the query actually
        // succeeded and found no visible element. Provider errors stay errors.
        await Show("50. FirstOrDefault / empty source", absent.FirstOrDefault());
        await Show("51. FirstOrDefault / nonempty source", ordered.FirstOrDefault(), expectedName: "B-owned");

        // Single asks for at most TWO rows: one supplies the value, a second
        // proves ambiguity. It never substitutes First for a uniqueness check.
        await Show("52. Single / exactly one match", one.Single(), expectedName: "C-public");
        await Show("53. Single / ambiguous input", ordered.Single(), error: "CQL_SEQUENCE_MULTIPLE");
        await Show("54. Single / empty input", absent.Single(), error: "CQL_SEQUENCE_EMPTY");

        // SingleOrDefault permits zero matches, but still rejects two or more.
        await Show("55. SingleOrDefault / empty source", absent.SingleOrDefault());
        await Show("56. SingleOrDefault / exactly one match", one.SingleOrDefault(), expectedName: "C-public");
        await Show("57. SingleOrDefault / ambiguous input", ordered.SingleOrDefault(), error: "CQL_SEQUENCE_MULTIPLE");

        // Single operates on the composed source: Take(1) intentionally turns
        // this sequence into one element before the terminal checks cardinality.
        await Show("58. Single / explicit source Take(1)", ordered.Take(1).Single(), expectedName: "B-owned");

        return completed;

        async Task Show(
            string title,
            OASISResult<CosmicElementQuery<Holon>> query,
            string? expectedName = null,
            string? error = null)
        {
            // Eligibility/capability admission is shared Core code. The SDK
            // caller supplies the trusted read policy on 'visible' beforehand.
            var plan = Value(CosmicElementPlanner.Plan(
                Value(query),
                adapter.QueryProviderType,
                new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) }));

            var result = await manager.ExecuteElementQueryAsync(plan, new[] { adapter });

            if (error != null)
            {
                if (!result.IsError || result.ErrorCode != error || result.IsLoaded || result.Result != null)
                    throw new InvalidOperationException(title + ": expected " + error);
            }
            else if (result.IsError || !result.IsLoaded || result.Result?.Name != expectedName)
            {
                throw new InvalidOperationException(title + ": " + result.Message);
            }

            completed++;
            await output.WriteLineAsync($"\n{title}\n\n  {error ?? result.Result?.Name ?? "null (successful empty result)"}\n");
        }
    }

    private static T Value<T>(OASISResult<T> result)
    {
        if (result.IsError)
            throw new InvalidOperationException(result.Message, result.Exception);

        return result.Result;
    }
}
