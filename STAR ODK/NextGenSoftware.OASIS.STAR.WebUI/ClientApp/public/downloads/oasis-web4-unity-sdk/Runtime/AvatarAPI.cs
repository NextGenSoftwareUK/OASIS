using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Unity
{
    /// <summary>
    /// Avatar management API for OASIS
    /// </summary>
    public class AvatarAPI
    {
        private readonly OASISClient _client;

        public AvatarAPI(OASISClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Authenticate an avatar
        /// </summary>
        public async Task<OASISResult<AuthResponse>> AuthenticateAsync(
            string email,
            string password,
            string deviceInfo = null)
        {
            var data = new
            {
                email,
                password,
                deviceInfo = deviceInfo ?? UnityEngine.SystemInfo.deviceModel
            };

            return await _client.PostAsync<AuthResponse>("/avatar/authenticate", data);
        }

        /// <summary>
        /// Register a new avatar
        /// </summary>
        public async Task<OASISResult<Avatar>> RegisterAsync(RegisterRequest request)
        {
            return await _client.PostAsync<Avatar>("/avatar/register", request);
        }

        /// <summary>
        /// Get avatar by ID
        /// </summary>
        public async Task<OASISResult<Avatar>> GetAvatarByIdAsync(Guid avatarId)
        {
            return await _client.GetAsync<Avatar>($"/avatar/{avatarId}");
        }

        /// <summary>
        /// Get avatar by username
        /// </summary>
        public async Task<OASISResult<Avatar>> GetAvatarByUsernameAsync(string username)
        {
            return await _client.GetAsync<Avatar>($"/avatar/username/{username}");
        }

        /// <summary>
        /// Update avatar
        /// </summary>
        public async Task<OASISResult<Avatar>> UpdateAvatarAsync(Guid avatarId, UpdateAvatarRequest request)
        {
            return await _client.PutAsync<Avatar>($"/avatar/{avatarId}", request);
        }

        /// <summary>
        /// Add karma to avatar
        /// </summary>
        public async Task<OASISResult<KarmaResponse>> AddKarmaAsync(Guid avatarId, AddKarmaRequest request)
        {
            return await _client.PostAsync<KarmaResponse>($"/avatar/{avatarId}/karma/add", request);
        }

        /// <summary>
        /// Remove karma from avatar
        /// </summary>
        public async Task<OASISResult<KarmaResponse>> RemoveKarmaAsync(Guid avatarId, RemoveKarmaRequest request)
        {
            return await _client.PostAsync<KarmaResponse>($"/avatar/{avatarId}/karma/remove", request);
        }

        /// <summary>
        /// Get avatar karma history
        /// </summary>
        public async Task<OASISResult<List<KarmaRecord>>> GetKarmaHistoryAsync(Guid avatarId)
        {
            return await _client.GetAsync<List<KarmaRecord>>($"/avatar/{avatarId}/karma/history");
        }
    }

    #region Data Models

    [Serializable]
    public class AuthResponse
    {
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
        public Avatar Avatar { get; set; }
    }

    [Serializable]
    public class Avatar
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Karma { get; set; }
        public int Level { get; set; }
        public string AvatarType { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
    }

    [Serializable]
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AvatarType { get; set; } = "User";
    }

    [Serializable]
    public class UpdateAvatarRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
    }

    [Serializable]
    public class AddKarmaRequest
    {
        public int Amount { get; set; }
        public string KarmaType { get; set; }
        public string KarmaSourceType { get; set; }
        public string KarmaSourceTitle { get; set; }
        public string KarmaSourceDesc { get; set; }
    }

    [Serializable]
    public class RemoveKarmaRequest
    {
        public int Amount { get; set; }
        public string KarmaType { get; set; }
        public string KarmaSourceType { get; set; }
        public string KarmaSourceTitle { get; set; }
        public string KarmaSourceDesc { get; set; }
    }

    [Serializable]
    public class KarmaResponse
    {
        public int TotalKarma { get; set; }
        public int CurrentLevel { get; set; }
        public KarmaRecord LastKarmaUpdate { get; set; }
    }

    [Serializable]
    public class KarmaRecord
    {
        public Guid Id { get; set; }
        public string KarmaType { get; set; }
        public int Amount { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}

