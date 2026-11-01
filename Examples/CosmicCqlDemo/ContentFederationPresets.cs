using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>Readable content examples. Provider capability admission still decides
/// whether each query can run; a preset never grants access to source records.</summary>
public static class ContentFederationPresets
{
    public static IReadOnlyDictionary<string, CosmicQuery<Holon>> Create(ProviderType secondary)
    {
        // 1. Choose both native sources explicitly. Authorization is supplied by
        // the trusted host separately for each source, never by browser input.
        var scope = Require(CosmicQueryScope.Named(ProviderType.MongoDBOASIS, secondary));
        var source = CosmicQuery<Holon>.Create(scope);

        // 2. These expressions remain CQL syntax trees. Both providers translate
        // them natively before paging; this is not a managed collection scan.
        return new Dictionary<string, CosmicQuery<Holon>>
        {
            ["name"] = Require(source.Where(holon => holon.Name == "Espresso")),

            ["description"] = Require(source.Where(holon =>
                holon.Description.Contains("coffee"))),

            ["metadata"] = Require(source.Where(holon =>
                CosmicQueryMetadata.Value<string>(holon, "category") == "coffee")),

            ["combined"] = Require(source.Where(holon =>
                (holon.Name == "Espresso" || holon.Name == "Tea") &&
                CosmicQueryMetadata.Value<string>(holon, "category") == "coffee"))
        };
    }

    private static T Require<T>(OASISResult<T> result)
    {
        if (result.IsError || result.Result == null)
            throw new InvalidOperationException(result.ErrorCode + ": " + result.Message);
        return result.Result;
    }
}
