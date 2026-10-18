using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS
{
    public partial class Neo4jOASIS
    {
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            if(version>0)return await ReadHistoricalAvatarsAsync(version);
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                ValidateCurrentAvatarVersion(version);
                // REAL Neo4j implementation for loading all avatars
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar) RETURN a";
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query);
                    var records = await result.ToListAsync();
                    
                    var avatars = new List<IAvatar>();
                    foreach (var record in records)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        avatars.Add(ReadAvatarNode(node));
                    }
                    
                    response.Result = avatars;
                    response.IsLoaded = true;
                    response.IsError = false;
                    response.Message = "All avatars loaded successfully from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
            => ReadOneAvatarAsync("Id",id.ToString(),version);

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
            => ReadOneAvatarAsync("ProviderKey",providerKey,version);

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => ReadOneAvatarAsync("Username",avatarUsername,version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => ReadOneAvatarAsync("Email",avatarEmail,version);

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }


    }
}
