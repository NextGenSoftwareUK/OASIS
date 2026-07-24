using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class AvatarGrpcService : AvatarService.AvatarServiceBase
    {
        private static AvatarManager AvatarManager => Program.AvatarManager;

        public override async Task<AvatarResponse> Register(RegisterAvatarRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                var avatarType = ParseEnum(request.AvatarType, AvatarType.User);

                var result = await AvatarManager.RegisterAsync(
                    request.Title,
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.Password,
                    request.Username,
                    avatarType,
                    OASISType.OASISAPIgRPC,
                    suppressVerificationEmail: request.SuppressVerificationEmail);

                return result.IsError
                    ? new AvatarResponse { IsError = true, Message = result.Message }
                    : new AvatarResponse { Avatar = MapAvatar(result.Result) };
            }
            catch (Exception ex)
            {
                return new AvatarResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<AuthenticateAvatarResponse> Authenticate(AuthenticateAvatarRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                var result = await AvatarManager.AuthenticateAsync(request.Username, request.Password, "gRPC");

                if (result.IsError)
                    return new AuthenticateAvatarResponse { IsError = true, Message = result.Message };

                return new AuthenticateAvatarResponse
                {
                    Avatar = MapAvatar(result.Result),
                    JwtToken = result.Result?.JwtToken ?? string.Empty,
                    RefreshToken = result.Result?.RefreshTokens?.LastOrDefault()?.Token ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                return new AuthenticateAvatarResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<AvatarResponse> GetById(GetAvatarByIdRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                if (!Guid.TryParse(request.Id, out var id))
                    return new AvatarResponse { IsError = true, Message = "Invalid avatar ID format." };

                var result = await AvatarManager.LoadAvatarAsync(id);

                return result.IsError
                    ? new AvatarResponse { IsError = true, Message = result.Message }
                    : new AvatarResponse { Avatar = MapAvatar(result.Result) };
            }
            catch (Exception ex)
            {
                return new AvatarResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<AvatarResponse> GetByEmail(GetAvatarByEmailRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                var result = await AvatarManager.LoadAvatarByEmailAsync(request.Email);

                return result.IsError
                    ? new AvatarResponse { IsError = true, Message = result.Message }
                    : new AvatarResponse { Avatar = MapAvatar(result.Result) };
            }
            catch (Exception ex)
            {
                return new AvatarResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<AvatarResponse> GetByUsername(GetAvatarByUsernameRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                // LoadAvatarAsync(string username, bool loadPrivateKeys, bool hideAuthDetails)
                var result = await AvatarManager.LoadAvatarAsync(request.Username, false, true);

                return result.IsError
                    ? new AvatarResponse { IsError = true, Message = result.Message }
                    : new AvatarResponse { Avatar = MapAvatar(result.Result) };
            }
            catch (Exception ex)
            {
                return new AvatarResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<GetAllAvatarsResponse> GetAll(GetAllAvatarsRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                var result = await AvatarManager.LoadAllAvatarsAsync();

                if (result.IsError)
                    return new GetAllAvatarsResponse { IsError = true, Message = result.Message };

                var response = new GetAllAvatarsResponse();

                if (result.Result != null)
                    response.Avatars.AddRange(result.Result.Select(MapAvatar));

                return response;
            }
            catch (Exception ex)
            {
                return new GetAllAvatarsResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<OASISGrpcResponse> Delete(DeleteAvatarRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                if (!Guid.TryParse(request.Id, out var id))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID format." };

                var result = await AvatarManager.DeleteAvatarAsync(id);

                return new OASISGrpcResponse { IsError = result.IsError, Message = result.Message };
            }
            catch (Exception ex)
            {
                return new OASISGrpcResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<AvatarResponse> AddKarmaToAvatar(AvatarKarmaRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new AvatarResponse { IsError = true, Message = "Invalid avatar ID format." };

                var karmaTypePositive = ParseEnum(request.KarmaTypePositive, KarmaTypePositive.None);
                var karmaSource = ParseEnum(request.KarmaSource, KarmaSourceType.API);

                // KarmaManager.Instance gives us OASISResult<KarmaAkashicRecord> with proper error handling
                var karmaResult = await KarmaManager.Instance.AddKarmaToAvatarAsync(
                    avatarId, karmaTypePositive, karmaSource,
                    request.KarmaSourceTitle, request.KarmaSourceDesc);

                if (karmaResult.IsError)
                    return new AvatarResponse { IsError = true, Message = karmaResult.Message };

                var avatarResult = await AvatarManager.LoadAvatarAsync(avatarId);

                return avatarResult.IsError
                    ? new AvatarResponse { IsError = true, Message = avatarResult.Message }
                    : new AvatarResponse { Avatar = MapAvatar(avatarResult.Result) };
            }
            catch (Exception ex)
            {
                return new AvatarResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<AvatarResponse> RemoveKarmaFromAvatar(AvatarKarmaRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new AvatarResponse { IsError = true, Message = "Invalid avatar ID format." };

                var karmaTypeNegative = ParseEnum(request.KarmaTypeNegative, KarmaTypeNegative.None);
                var karmaSource = ParseEnum(request.KarmaSource, KarmaSourceType.API);

                var karmaResult = await KarmaManager.Instance.RemoveKarmaFromAvatarAsync(
                    avatarId, karmaTypeNegative, karmaSource,
                    request.KarmaSourceTitle, request.KarmaSourceDesc);

                if (karmaResult.IsError)
                    return new AvatarResponse { IsError = true, Message = karmaResult.Message };

                var avatarResult = await AvatarManager.LoadAvatarAsync(avatarId);

                return avatarResult.IsError
                    ? new AvatarResponse { IsError = true, Message = avatarResult.Message }
                    : new AvatarResponse { Avatar = MapAvatar(avatarResult.Result) };
            }
            catch (Exception ex)
            {
                return new AvatarResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<AvatarResponse> AddXpToAvatar(AddXpToAvatarRequest request, ServerCallContext context)
        {
            try
            {
                await ActivateProviderIfSpecifiedAsync(request.ProviderType, request.SetGlobally);

                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new AvatarResponse { IsError = true, Message = "Invalid avatar ID format." };

                if (request.XpToAdd < 0)
                    return new AvatarResponse { IsError = true, Message = "XP amount must be non-negative." };

                var loadResult = await AvatarManager.LoadAvatarDetailAsync(avatarId);

                if (loadResult.IsError || loadResult.Result == null)
                    return new AvatarResponse { IsError = true, Message = loadResult.Message ?? "Failed to load avatar detail." };

                var detail = loadResult.Result;
                detail.XP += request.XpToAdd;

                var updateResult = await AvatarManager.UpdateAvatarDetailAsync(avatarId, detail);

                if (updateResult.IsError)
                    return new AvatarResponse { IsError = true, Message = updateResult.Message ?? "Failed to update avatar XP." };

                var avatarResult = await AvatarManager.LoadAvatarAsync(avatarId);

                return avatarResult.IsError
                    ? new AvatarResponse { IsError = true, Message = avatarResult.Message }
                    : new AvatarResponse { Avatar = MapAvatar(avatarResult.Result) };
            }
            catch (Exception ex)
            {
                return new AvatarResponse { IsError = true, Message = ex.Message };
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static AvatarMessage MapAvatar(IAvatar avatar)
        {
            if (avatar == null) return new AvatarMessage();

            return new AvatarMessage
            {
                Id = avatar.Id.ToString(),
                Username = avatar.Username ?? string.Empty,
                Email = avatar.Email ?? string.Empty,
                Title = avatar.Title ?? string.Empty,
                FirstName = avatar.FirstName ?? string.Empty,
                LastName = avatar.LastName ?? string.Empty,
                FullName = avatar.FullName ?? string.Empty,
                AvatarType = avatar.AvatarType?.Value.ToString() ?? string.Empty,
                IsActive = avatar.IsActive,
                IsVerified = avatar.IsVerified,
                CreatedDate = avatar.CreatedDate != default
                    ? Timestamp.FromDateTime(avatar.CreatedDate.ToUniversalTime()) : null,
                ModifiedDate = avatar.ModifiedDate != default
                    ? Timestamp.FromDateTime(avatar.ModifiedDate.ToUniversalTime()) : null
            };
        }

        private static async Task ActivateProviderIfSpecifiedAsync(string providerType, bool setGlobally)
        {
            if (string.IsNullOrWhiteSpace(providerType)) return;
            if (!System.Enum.TryParse<ProviderType>(providerType, true, out var pt)) return;
            if (pt == ProviderType.Default || pt == ProviderType.None) return;

            await OASISBootLoader.OASISBootLoader.GetAndActivateStorageProviderAsync(pt, null, false, setGlobally);
        }

        private static T ParseEnum<T>(string value, T defaultValue) where T : struct, System.Enum
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return System.Enum.TryParse<T>(value, true, out var result) ? result : defaultValue;
        }
    }
}
