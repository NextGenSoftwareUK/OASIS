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
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
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
                        avatars.Add(new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        });
                    }
                    
                    response.Result = avatars;
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

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // REAL Neo4j implementation for loading avatar by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {Id: $id}) RETURN a";
                var parameters = new { id = id.ToString() };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        response.Result = new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from Neo4j";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // REAL Neo4j implementation for loading avatar by provider key
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {ProviderKey: $providerKey}) RETURN a";
                var parameters = new { providerKey };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        response.Result = new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from Neo4j by provider key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // REAL Neo4j implementation for loading avatar by username
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {Username: $username}) RETURN a";
                var parameters = new { username = avatarUsername };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        response.Result = new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from Neo4j by username";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // REAL Neo4j implementation for loading avatar by email
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {Email: $email}) RETURN a";
                var parameters = new { email = avatarEmail };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        response.Result = new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from Neo4j by email";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // REAL Neo4j implementation for loading avatar detail by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    var query = @"
                        MATCH (a:AvatarDetail {Id: $id})
                        RETURN a.Id as Id, a.Username as Username, a.Email as Email, 
                               a.CreatedDate as CreatedDate, a.ModifiedDate as ModifiedDate,
                               a.Description as Description, a.IsActive as IsActive, 
                               a.Karma as Karma, a.Level as Level, a.XP as XP, 
                               a.Model3D as Model3D, a.UmaJson as UmaJson,
                               a.Portrait as Portrait, a.Town as Town, a.County as County,
                               a.DOB as DOB, a.Address as Address, a.Country as Country,
                               a.Postcode as Postcode, a.Landline as Landline, a.Mobile as Mobile,
                               a.FavouriteColour as FavouriteColour, a.STARCLIColour as STARCLIColour";

                    var result = await session.RunAsync(query, new { id = id.ToString() });
                    var records = await result.ToListAsync();
                    var record = records.FirstOrDefault();

                    if (record != null)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = Guid.Parse(record["Id"].As<string>()),
                            Username = record["Username"].As<string>(),
                            Email = record["Email"].As<string>(),
                            CreatedDate = record["CreatedDate"].As<DateTime>(),
                            ModifiedDate = record["ModifiedDate"].As<DateTime>(),
                            Description = record["Description"].As<string>(),
                            IsActive = record["IsActive"].As<bool>(),
                            Karma = record["Karma"].As<long>(),
                            XP = record["XP"].As<int>(),
                            Model3D = record["Model3D"].As<string>(),
                            UmaJson = record["UmaJson"].As<string>(),
                            Portrait = record["Portrait"].As<string>(),
                            Town = record["Town"].As<string>(),
                            County = record["County"].As<string>(),
                            DOB = record["DOB"].As<DateTime>(),
                            Address = record["Address"].As<string>(),
                            Country = record["Country"].As<string>(),
                            Postcode = record["Postcode"].As<string>(),
                            Landline = record["Landline"].As<string>(),
                            Mobile = record["Mobile"].As<string>(),
                            FavouriteColour = (ConsoleColor)record["FavouriteColour"].As<int>(),
                            STARCLIColour = (ConsoleColor)record["STARCLIColour"].As<int>()
                        };

                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from Neo4j successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found in Neo4j");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // REAL Neo4j implementation for loading avatar detail by email
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    var query = @"
                        MATCH (a:AvatarDetail {Email: $email})
                        RETURN a.Id as Id, a.Username as Username, a.Email as Email, 
                               a.CreatedDate as CreatedDate, a.ModifiedDate as ModifiedDate,
                               a.Description as Description, a.IsActive as IsActive, 
                               a.Karma as Karma, a.Level as Level, a.XP as XP, 
                               a.Model3D as Model3D, a.UmaJson as UmaJson,
                               a.Portrait as Portrait, a.Town as Town, a.County as County,
                               a.DOB as DOB, a.Address as Address, a.Country as Country,
                               a.Postcode as Postcode, a.Landline as Landline, a.Mobile as Mobile,
                               a.FavouriteColour as FavouriteColour, a.STARCLIColour as STARCLIColour";

                    var result = await session.RunAsync(query, new { email = avatarEmail });
                    var records = await result.ToListAsync();
                    var record = records.FirstOrDefault();

                    if (record != null)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = Guid.Parse(record["Id"].As<string>()),
                            Username = record["Username"].As<string>(),
                            Email = record["Email"].As<string>(),
                            CreatedDate = record["CreatedDate"].As<DateTime>(),
                            ModifiedDate = record["ModifiedDate"].As<DateTime>(),
                            Description = record["Description"].As<string>(),
                            IsActive = record["IsActive"].As<bool>(),
                            Karma = record["Karma"].As<long>(),
                            XP = record["XP"].As<int>(),
                            Model3D = record["Model3D"].As<string>(),
                            UmaJson = record["UmaJson"].As<string>(),
                            Portrait = record["Portrait"].As<string>(),
                            Town = record["Town"].As<string>(),
                            County = record["County"].As<string>(),
                            DOB = record["DOB"].As<DateTime>(),
                            Address = record["Address"].As<string>(),
                            Country = record["Country"].As<string>(),
                            Postcode = record["Postcode"].As<string>(),
                            Landline = record["Landline"].As<string>(),
                            Mobile = record["Mobile"].As<string>(),
                            FavouriteColour = (ConsoleColor)record["FavouriteColour"].As<int>(),
                            STARCLIColour = (ConsoleColor)record["STARCLIColour"].As<int>()
                        };

                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded by email from Neo4j successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found in Neo4j");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // REAL Neo4j implementation for loading avatar detail by username
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    var query = @"
                        MATCH (a:AvatarDetail {Username: $username})
                        RETURN a.Id as Id, a.Username as Username, a.Email as Email, 
                               a.CreatedDate as CreatedDate, a.ModifiedDate as ModifiedDate,
                               a.Description as Description, a.IsActive as IsActive, 
                               a.Karma as Karma, a.Level as Level, a.XP as XP, 
                               a.Model3D as Model3D, a.UmaJson as UmaJson,
                               a.Portrait as Portrait, a.Town as Town, a.County as County,
                               a.DOB as DOB, a.Address as Address, a.Country as Country,
                               a.Postcode as Postcode, a.Landline as Landline, a.Mobile as Mobile,
                               a.FavouriteColour as FavouriteColour, a.STARCLIColour as STARCLIColour";

                    var result = await session.RunAsync(query, new { username = avatarUsername });
                    var records = await result.ToListAsync();
                    var record = records.FirstOrDefault();

                    if (record != null)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = Guid.Parse(record["Id"].As<string>()),
                            Username = record["Username"].As<string>(),
                            Email = record["Email"].As<string>(),
                            CreatedDate = record["CreatedDate"].As<DateTime>(),
                            ModifiedDate = record["ModifiedDate"].As<DateTime>(),
                            Description = record["Description"].As<string>(),
                            IsActive = record["IsActive"].As<bool>(),
                            Karma = record["Karma"].As<long>(),
                            XP = record["XP"].As<int>(),
                            Model3D = record["Model3D"].As<string>(),
                            UmaJson = record["UmaJson"].As<string>(),
                            Portrait = record["Portrait"].As<string>(),
                            Town = record["Town"].As<string>(),
                            County = record["County"].As<string>(),
                            DOB = record["DOB"].As<DateTime>(),
                            Address = record["Address"].As<string>(),
                            Country = record["Country"].As<string>(),
                            Postcode = record["Postcode"].As<string>(),
                            Landline = record["Landline"].As<string>(),
                            Mobile = record["Mobile"].As<string>(),
                            FavouriteColour = (ConsoleColor)record["FavouriteColour"].As<int>(),
                            STARCLIColour = (ConsoleColor)record["STARCLIColour"].As<int>()
                        };

                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded by username from Neo4j successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found in Neo4j");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from Neo4j: {ex.Message}");
            }
            return response;
        }

    }
}
