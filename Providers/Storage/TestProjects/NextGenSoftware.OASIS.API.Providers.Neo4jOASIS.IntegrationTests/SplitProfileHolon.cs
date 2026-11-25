using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

/// <summary>Entity used by the M22 journey: Bio and Age are stored in a separate split part record.</summary>
public sealed class SplitProfileHolon : Holon
{
    public string Bio { get; set; }
    public int Age { get; set; }
}
