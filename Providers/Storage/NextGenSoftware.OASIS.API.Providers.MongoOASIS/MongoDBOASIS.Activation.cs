using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    private async Task<OASISResult<bool>> ActivateMongoAsync(bool synchronous)
    {
        var result=new OASISResult<bool>();
        try
        {
            // MongoDbContext reuses its existing connection-string keyed MongoClient cache.
            // Keep candidate state local: an index error must not publish half an activation.
            var database=Database ?? new MongoDbContext(ConnectionString,DBName);
            SerializerRegister.GetInstance().RegisterGuidBsonSerializer();
            SerializerRegister.GetInstance().RegisterMetaDataDictionarySerializer();
            SerializerRegister.GetInstance().RegisterSTARNETDNADiscriminator();
            var avatars=new AvatarRepository(database,()=>IsVersionControlEnabled);
            var holons=new HolonRepository(database);
            var search=new SearchRepository(database);
            if(synchronous)
            {
                holons.EnsurePublicIdentityIndex();
                holons.EnsurePrimaryOutboxIndexes();
                avatars.EnsurePublicIdentityIndexes();
            }
            else
            {
                await holons.EnsurePublicIdentityIndexAsync().ConfigureAwait(false);
                await holons.EnsurePrimaryOutboxIndexesAsync().ConfigureAwait(false);
                await avatars.EnsurePublicIdentityIndexesAsync().ConfigureAwait(false);
            }
            await holons.UpgradeCqlMembershipAsync().ConfigureAwait(false);
            Database=database;
            _avatarRepository=avatars;
            _holonRepository=holons;
            _searchRepository=search;
            IsProviderActivated=true;
            result.Result=true;
        }
        catch(Exception cause)
        {
            IsProviderActivated=false;
            OASISErrorHandling.HandleError(ref result,"Mongo activation failed: "+cause.Message,cause);
        }
        return result;
    }
}
