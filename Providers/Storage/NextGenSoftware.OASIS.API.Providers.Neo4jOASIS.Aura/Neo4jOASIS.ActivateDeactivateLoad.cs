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

        public override OASISResult<bool> ActivateProvider() =>
            ActivateProviderAsync().GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();

            if (IsProviderActivated)
            {
                result.Result = true;
                return result;
            }

            try
            {
                var driver = GraphDatabase.Driver(Host, AuthTokens.Basic(Username, Password));
                try
                {
                    await driver.VerifyConnectivityAsync().ConfigureAwait(false);
                }
                catch
                {
                    await driver.CloseAsync().ConfigureAwait(false);
                    throw;
                }

                Driver = driver;
                IsProviderActivated = true;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error activating Neo4j Aura provider.", ex);
            }

            return result;
        }

        public override OASISResult<bool> DeActivateProvider() =>
            DeActivateProviderAsync().GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                if (Driver != null)
                    await Driver.CloseAsync().ConfigureAwait(false);

                Driver = null;
                IsProviderActivated = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error deactivating Neo4j Aura provider.", ex);
            }

            return result;
        }

        private static void WithDatabase(SessionConfigBuilder sessionConfigBuilder)
        {
            //var neo4jVersion = System.Environment.GetEnvironmentVariable("NEO4J_VERSION") ?? "";
            //if (!neo4jVersion.StartsWith("4"))
            //{
            //    return;
            //}
            sessionConfigBuilder.WithDatabase("neo4j");
        }
        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Neo4j: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Neo4j: {ex.Message}", ex);
            }
            return result;
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            // Neo4j provider does not generate native code from STAR metadata.
            return true;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) =>
            LoadAllAvatarsAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var session = Driver.AsyncSession(WithDatabase);
                await using var sessionLifetime = session.ConfigureAwait(false);
                result.Result = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                        MATCH (av:Avatar) WHERE NOT coalesce(av.IsDeleted, false)
                        RETURN av.GUId AS guid, av.ProviderKey AS providerKey, av.name AS title, av.FirstName AS firstname,
                               av.LastName AS lastname, av.username AS username, av.EMail AS email").ConfigureAwait(false);
                    return await cursor.ToListAsync(ReadAvatar).ConfigureAwait(false);
                }).ConfigureAwait(false);
                result.IsLoaded = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error listing Neo4j Aura avatars.", ex);
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) =>
            LoadAvatarBySelectorAsync("toLower(av.username) = toLower($value)", avatarUsername);

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0) =>
            LoadAvatarBySelectorAsync("toLower(av.EMail) = toLower($value)", avatarEmail);

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0) =>
            LoadAvatarBySelectorAsync("av.GUId = $value", Id.ToString());

        private async Task<OASISResult<IAvatar>> LoadAvatarBySelectorAsync(string predicate, string value)
        {
            var result = new OASISResult<IAvatar>();

            try
            {
                var session = Driver.AsyncSession(WithDatabase);
                await using var sessionLifetime = session.ConfigureAwait(false);
                result.Result = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync($@"
                        MATCH (av:Avatar) WHERE {predicate} AND NOT coalesce(av.IsDeleted, false)
                        RETURN av.GUId AS guid, av.ProviderKey AS providerKey, av.name AS title, av.FirstName AS firstname,
                               av.LastName AS lastname, av.username AS username, av.EMail AS email
                        LIMIT 2",
                        new { value }).ConfigureAwait(false);
                    if (!await cursor.FetchAsync().ConfigureAwait(false))
                        return null;

                    var avatar = ReadAvatar(cursor.Current);
                    if (await cursor.FetchAsync().ConfigureAwait(false))
                        throw new StorageIdentityAmbiguousException();

                    return avatar;
                }).ConfigureAwait(false);
                if (result.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura avatar was not found.");
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    return result;
                }

                result.IsLoaded = true;
                result.LoadedCount = 1;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error loading Neo4j Aura avatar.", ex);
                if (ex is StorageIdentityAmbiguousException)
                    result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity;
            }

            return result;
        }

        private static IAvatar ReadAvatar(IRecord row)
        {
            var avatar = new Avatar
            {
                AvatarId = Guid.Parse(row["guid"].As<string>()),
                Title = row["title"].As<string>(),
                FirstName = row["firstname"].As<string>(),
                LastName = row["lastname"].As<string>(),
                Username = row["username"].As<string>(),
                Email = row["email"].As<string>()
            };
            if (row["providerKey"] is string providerKey)
            {
                avatar.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = providerKey;
            }
            return avatar;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0) =>
            LoadAvatarAsync(Id, version).GetAwaiter().GetResult();
    }
}
