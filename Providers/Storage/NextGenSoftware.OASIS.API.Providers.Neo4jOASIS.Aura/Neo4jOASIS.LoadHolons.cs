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
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (avd:AvatarDetail)                        
                            RETURN avd.Username AS username,avd.Email AS email"
                    );

                    IEnumerable<IAvatarDetail> objList = (from d in cursor
                                                          select new AvatarDetail
                                                          {
                                                              Username = d["username"].As<string>(),
                                                              Email = d["email"].As<string>()
                                                          }).ToList();

                    return new OASISResult<IEnumerable<IAvatarDetail>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail(s) Loaded successfully",
                        Result = objList,
                    };

                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (avd:AvatarDetail)                        
                            RETURN avd.Username AS username,avd.Email AS email"
                    );

                    IEnumerable<IAvatarDetail> objList = await cursor.ToListAsync(record => new AvatarDetail
                    {
                        Username = record["username"].As<string>(),
                        Email = record["email"].As<string>()
                    });

                    return new OASISResult<IEnumerable<IAvatarDetail>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail(s) Loaded successfully",
                        Result = objList,
                    };

                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
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

                var session = Driver.AsyncSession(WithDatabase);
                try
                {
                    await session.WriteTransactionAsync(async transaction =>
                    {
                        var cursor = await transaction.RunAsync(@"
                            MERGE (avatar:Avatar {GUId: $guid})
                            SET avatar.name = $name, avatar.FirstName = $firstName,
                                avatar.LastName = $lastName, avatar.EMail = $email,
                                avatar.username = $username, avatar.password = $password
                            RETURN avatar.GUId",
                            new
                            {
                                guid = id.ToString(),
                                name = avatar.Title,
                                firstName = avatar.FirstName,
                                lastName = avatar.LastName,
                                email = avatar.Email,
                                username = avatar.Username,
                                password = avatar.Password
                            }).ConfigureAwait(false);
                        await cursor.SingleAsync().ConfigureAwait(false);
                        return true;
                    }).ConfigureAwait(false);
                }
                finally
                {
                    await session.CloseAsync().ConfigureAwait(false);
                }

                avatar.AvatarId = id;
                avatar.CreatedProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
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
