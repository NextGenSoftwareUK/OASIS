using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static async Task<bool> HasProfileIdentitySchemaAsync(IAsyncSession session)
    {
        var row=await(await session.RunAsync("SHOW CONSTRAINTS YIELD type,labelsOrTypes,properties "
            +"WHERE type IN ['UNIQUENESS','NODE_KEY'] AND "
            +"((labelsOrTypes=['Avatar'] AND size(properties)=1 AND properties[0] IN ['Id','Username','Email']) "
            +"OR (labelsOrTypes=['AvatarDetail'] AND properties=['Id'])) "
            +"RETURN count(DISTINCT labelsOrTypes[0]+'.'+properties[0]) AS count").ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
        return row["count"].As<long>()==4;
    }

    private static async Task EnsureProfileIdentitySchemaAsync(IAsyncSession session)
    {
        if(await HasProfileIdentitySchemaAsync(session).ConfigureAwait(false))return;
        // These same native constraints also govern ordinary account creates and
        // saves. A read-before-write uniqueness check cannot prevent write skew.
        foreach(var item in new[]{("Avatar","Id"),("AvatarDetail","Id"),("Avatar","Username"),("Avatar","Email")})
            await(await session.RunAsync("CREATE CONSTRAINT oasis_profile_"+item.Item1.ToLowerInvariant()+"_"+item.Item2.ToLowerInvariant()
                +" IF NOT EXISTS FOR (a:"+item.Item1+") REQUIRE a."+item.Item2+" IS UNIQUE").ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
        if(!await HasProfileIdentitySchemaAsync(session).ConfigureAwait(false))
            throw new InvalidDataException("Neo4j profile writes require native account/detail and login identity uniqueness constraints.");
    }
}
