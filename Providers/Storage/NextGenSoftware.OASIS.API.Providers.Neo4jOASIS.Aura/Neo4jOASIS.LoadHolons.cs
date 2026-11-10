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
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) =>
            LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var session = Driver.AsyncSession(WithDatabase);
                await using var sessionLifetime = session.ConfigureAwait(false);
                result.Result = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                        MATCH (detail:AvatarDetail)
                        RETURN detail.GUId AS guid, detail.Username AS username,
                               detail.Email AS email, detail.Address AS address, detail.Country AS country")
                        .ConfigureAwait(false);
                    return await cursor.ToListAsync(ReadAvatarDetail).ConfigureAwait(false);
                }).ConfigureAwait(false);
                result.IsLoaded = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error listing Neo4j Aura avatar details.", ex);
            }

            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) =>
            SaveAvatarAsync(avatar).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();

            try
            {
                var id = avatar.AvatarId;
                if (id == Guid.Empty)
                {
                    var providerKey = avatar.ProviderUniqueStorageKey?.GetValueOrDefault(Core.Enums.ProviderType.Neo4jOASIS)
                        ?? avatar.ProviderUniqueStorageKey?.Values?.FirstOrDefault()
                        ?? avatar.Username
                        ?? $"Neo4jOASIS:{avatar.CreatedDate.Ticks}";
                    id = CreateDeterministicGuid($"{Core.Enums.ProviderType.Neo4jOASIS}:{providerKey}");
                }

                var requestedKey = avatar.ProviderUniqueStorageKey?.GetValueOrDefault(Core.Enums.ProviderType.Neo4jOASIS);
                var candidateKey = string.IsNullOrWhiteSpace(requestedKey) ? id.ToString() : requestedKey;
                string persistedKey;
                var session = Driver.AsyncSession(WithDatabase);
                try
                {
                    persistedKey = await session.WriteTransactionAsync(async transaction =>
                    {
                        var cursor = await transaction.RunAsync(@"
                            MERGE (avatar:Avatar {GUId: $guid})
                            SET avatar.name = $name, avatar.FirstName = $firstName,
                                avatar.LastName = $lastName, avatar.EMail = $email,
                                avatar.username = $username, avatar.password = $password
                            SET avatar.ProviderKey = coalesce(avatar.ProviderKey, $providerKey)
                            RETURN avatar.ProviderKey AS providerKey",
                            new
                            {
                                guid = id.ToString(),
                                providerKey = candidateKey,
                                name = avatar.Title,
                                firstName = avatar.FirstName,
                                lastName = avatar.LastName,
                                email = avatar.Email,
                                username = avatar.Username,
                                password = avatar.Password
                            }).ConfigureAwait(false);
                        return await cursor.SingleAsync(row => row["providerKey"].As<string>()).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                finally
                {
                    await session.CloseAsync().ConfigureAwait(false);
                }

                avatar.AvatarId = id;
                avatar.CreatedProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
                avatar.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = persistedKey;
                result.Result = avatar;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error saving Neo4j Aura avatar.", ex);
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail AvatarDetail) =>
            SaveAvatarDetailAsync(AvatarDetail).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail AvatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();

            try
            {
                var id = AvatarDetail.Id;
                if (id == Guid.Empty)
                {
                    var providerKey = AvatarDetail.ProviderUniqueStorageKey?.GetValueOrDefault(Core.Enums.ProviderType.Neo4jOASIS)
                        ?? AvatarDetail.ProviderUniqueStorageKey?.Values?.FirstOrDefault()
                        ?? AvatarDetail.Username
                        ?? $"Neo4jOASIS:{AvatarDetail.CreatedDate.Ticks}";
                    id = CreateDeterministicGuid($"{Core.Enums.ProviderType.Neo4jOASIS}:{providerKey}");
                }

                var session = Driver.AsyncSession(WithDatabase);
                try
                {
                    await session.WriteTransactionAsync(async transaction =>
                    {
                        var cursor = await transaction.RunAsync(@"
                            MERGE (detail:AvatarDetail {GUId: $guid})
                            SET detail.Username = $username, detail.Email = $email,
                                detail.Address = $address, detail.Country = $country
                            RETURN detail.GUId",
                            new
                            {
                                guid = id.ToString(),
                                username = AvatarDetail.Username,
                                email = AvatarDetail.Email,
                                address = AvatarDetail.Address,
                                country = AvatarDetail.Country
                            }).ConfigureAwait(false);
                        await cursor.SingleAsync().ConfigureAwait(false);
                        return true;
                    }).ConfigureAwait(false);
                }
                finally
                {
                    await session.CloseAsync().ConfigureAwait(false);
                }

                AvatarDetail.Id = id;
                AvatarDetail.CreatedProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
                result.Result = AvatarDetail;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error saving Neo4j Aura avatar detail.", ex);
            }

            return result;
        }

    }
}
