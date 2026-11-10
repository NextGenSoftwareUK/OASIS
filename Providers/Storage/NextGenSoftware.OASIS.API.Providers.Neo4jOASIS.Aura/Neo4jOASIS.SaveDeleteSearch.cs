using System;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Neo4jNode = Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura
{
    public partial class Neo4jOASIS
    {
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) =>
            DeleteAvatarAsync(id, softDelete).GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) =>
            DeleteAvatarBySelectorAsync("av.GUId = $value", id.ToString(), softDelete);

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) =>
            DeleteAvatarByEmailAsync(avatarEmail, softDelete).GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) =>
            DeleteAvatarBySelectorAsync("toLower(av.EMail) = toLower($value)", avatarEmail, softDelete);

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) =>
            DeleteAvatarByUsernameAsync(avatarUsername, softDelete).GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true) =>
            DeleteAvatarBySelectorAsync("toLower(av.username) = toLower($value)", avatarUsername, softDelete);

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) =>
            DeleteAvatarAsync(providerKey, softDelete).GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) =>
            DeleteAvatarBySelectorAsync("av.ProviderKey = $value", providerKey, softDelete);

        private async Task<OASISResult<bool>> DeleteAvatarBySelectorAsync(string predicate, string value, bool softDelete)
        {
            var result = new OASISResult<bool>();

            try
            {
                var session = Driver.AsyncSession(WithDatabase);
                await using var sessionLifetime = session.ConfigureAwait(false);
                var mutation = softDelete
                    ? "SET target.IsDeleted = true"
                    : "DETACH DELETE target";
                var matches = await session.WriteTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync($@"
                        MATCH (av:Avatar)
                        WHERE {predicate} AND (NOT $softDelete OR NOT coalesce(av.IsDeleted, false))
                        WITH av LIMIT 2
                        WITH collect(av) AS matches
                        FOREACH (target IN CASE WHEN size(matches) = 1 THEN matches ELSE [] END |
                            {mutation})
                        RETURN size(matches) AS matched",
                        new { value, softDelete }).ConfigureAwait(false);
                    return await cursor.SingleAsync(row => row["matched"].As<long>()).ConfigureAwait(false);
                }).ConfigureAwait(false);

                if (matches == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura avatar was not found.");
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    return result;
                }

                if (matches > 1)
                    throw new StorageIdentityAmbiguousException();

                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error deleting Neo4j Aura avatar.", ex);
                if (ex is StorageIdentityAmbiguousException)
                    result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity;
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version)
                .GetAwaiter().GetResult();

    }
}
