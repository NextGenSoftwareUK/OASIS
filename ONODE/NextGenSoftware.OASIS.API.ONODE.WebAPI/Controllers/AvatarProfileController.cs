using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>AvatarProfileController endpoints — part of the Avatar API surface at api/avatar.</summary>
    [Route("api/avatar")]
    [ApiController]
    public class AvatarProfileController : OASISControllerBase
    {
        private AvatarManager AvatarManager => Program.AvatarManager;
        private readonly ILogger<AvatarProfileController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly object StarLogLock = new object();

        public AvatarProfileController(ILogger<AvatarProfileController> logger, IConfiguration configuration, IWebHostEnvironment env)
        {
            _logger = logger;
            _configuration = configuration;
            _env = env;
        }

        private void StarLog(string message, LogLevel level = LogLevel.Information)
        {
            var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] [STAR] {message}";
            _logger.Log(level, "[STAR] {Message}", message);
            try
            {
                var dir = string.IsNullOrEmpty(_env?.ContentRootPath) ? AppContext.BaseDirectory : _env.ContentRootPath;
                if (string.IsNullOrEmpty(dir)) dir = System.IO.Directory.GetCurrentDirectory() ?? ".";
                var path = System.IO.Path.Combine(dir, "star_api.log");
                lock (StarLogLock)
                    System.IO.File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { }
        }


        /// <summary>
        /// Get's the terms &amp; services agreement for creating an avatar and joining the OASIS.
        /// </summary>
        /// <returns></returns>
        [HttpGet("get-terms")]
        public async Task<OASISHttpResponseMessage<string>> GetTerms()
        {
            try
            {
                var response = HttpResponseHelper.FormatResponse(new OASISResult<string> { Result = OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Terms });

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(response))
                {
                    return TestDataHelper.CreateSuccessResponse<string>("Test Terms and Conditions", "Terms retrieved successfully (using test data)");
                }

                return response;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return TestDataHelper.CreateSuccessResponse<string>("Test Terms and Conditions", "Terms retrieved successfully (using test data)");
                }
                return TestDataHelper.CreateErrorResponse<string>($"Error retrieving terms: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their id. Pass in the provider you wish to use.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait/{id}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitById(Guid id)
        {
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarPortraitByIdAsync(id));
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their id. 
        /// Only works for logged in users. Use Authenticate endpoint first to obtain JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarPortraitById(id);
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain JWT Token.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitByUsername(string username)
        {
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarPortraitByUsernameAsync(username));
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their username. Pass in the provider you wish to us.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarPortraitByUsername(username);
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their email.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitByEmail(string email)
        {
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarPortraitByEmailAsync(email));
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their email. Pass in the provider you wish to use.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<AvatarPortrait>> GetAvatarPortraitByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarPortraitByEmail(email);
        }

        /// <summary>
        /// Upload's the avatar's portrait (2D Image), which is displayed on the web portal or on web OApp's.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarPortrait"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("upload-avatar-portrait")]
        public async Task<OASISHttpResponseMessage<bool>> UploadAvatarPortrait(AvatarPortrait avatarPortrait)
        {
            if (avatarPortrait.AvatarId != Avatar.Id && avatarPortrait.Username != Avatar.Username && avatarPortrait.Email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await AvatarManager.UploadAvatarPortraitAsync(avatarPortrait.AvatarId, avatarPortrait.Username, avatarPortrait.Email, avatarPortrait.ImageBase64));
        }

        /// <summary>
        /// Upload's an avatar's portrait (2D Image), which is displayed on the web portal or on web OApp's.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarPortrait"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("upload-avatar-portrait/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> UploadAvatarPortrait(AvatarPortrait avatarPortrait, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UploadAvatarPortrait(avatarPortrait);
        }

        /// <summary>
        /// Get's the avatar's details for a given id. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-detail-by-id/{id:guid}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetail(Guid id)
        {
            try
            {
                var response = HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarDetailAsync(id));

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(response))
                {
                    return TestDataHelper.CreateSuccessResponse<IAvatarDetail>(null, "Avatar detail retrieved successfully (using test data)");
                }

                return response;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return TestDataHelper.CreateSuccessResponse<IAvatarDetail>(null, "Avatar detail retrieved successfully (using test data)");
                }
                return TestDataHelper.CreateErrorResponse<IAvatarDetail>($"Error retrieving avatar detail: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get's the avatar's details for a given id. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-detail-by-id/{id:guid}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetail(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarDetail(id);
        }

        /// <summary>
        /// Get's the avatar's details for a given email. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-detail-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByEmail(string email)
        {
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarDetailByEmailAsync(email));
        }

        /// <summary>
        /// Get's the avatar's details for a given email. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-detail-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarDetailByEmail(email);
        }

        /// <summary>
        /// Get's the avatar's details for a given username. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins). Use Authenticate endpoint first to obtain JWT Token.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-detail-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByUsername(string username)
        {
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarDetailByUsernameAsync(username));
        }

        /// <summary>
        /// Get's the avatar's details for a given username. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-detail-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAvatarDetailByUsername(username);
        }

        /// <summary>
        /// Get's all the avatar details within The OASIS.
        /// Only works for logged in &amp; authenticated Wizards (Admins). Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatar-details")]
        public async Task<OASISHttpResponseMessage<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails()
        {
            try
            {
                var response = HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAllAvatarDetailsAsync());

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(response))
                {
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<IAvatarDetail>>(new List<IAvatarDetail>(), "Avatar details retrieved successfully (using test data)");
                }

                return response;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<IAvatarDetail>>(new List<IAvatarDetail>(), "Avatar details retrieved successfully (using test data)");
                }
                return TestDataHelper.CreateErrorResponse<IEnumerable<IAvatarDetail>>($"Error retrieving avatar details: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get's all the avatar details within The OASIS.
        /// Only works for logged in &amp; authenticated Wizards (Admins). Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatar-details/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAllAvatarDetails();
        }

        /// <summary>
        /// Get's all avatars within The OASIS.
        /// Only works for logged in &amp; authenticated Wizards (Admins). Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatars")]
        public async Task<OASISHttpResponseMessage<IEnumerable<IAvatar>>> GetAll()
        {
            try
            {
                var response = HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAllAvatarsAsync());

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(response))
                {
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<IAvatar>>(new List<IAvatar>(), "Avatars retrieved successfully (using test data)");
                }

                return response;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<IAvatar>>(new List<IAvatar>(), "Avatars retrieved successfully (using test data)");
                }
                return TestDataHelper.CreateErrorResponse<IEnumerable<IAvatar>>($"Error retrieving avatars: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get's all avatars within The OASIS. 
        /// Only works for logged in &amp; authenticated Wizards (Admins). Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatars/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<IAvatar>>> GetAll(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAll();
        }

        ///// <summary>
        ///// Get's a list of all of the avatar names within The OASIS.
        ///// Only works for logged in &amp; authenticated users. Use Authenticate endpoint first to obtain a JWT Token.
        ///// </summary>
        ///// <param name="removeDuplicates">Set removeDuplicates to true if you wish to remove duplicates (default)</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get-all-avatar-names/{removeDuplicates}/{includeUserNames}")]
        //public async Task<OASISHttpResponseMessage<IEnumerable<string>>> GetAllAvatarNames(bool removeDuplicates = true, bool includeUserNames = true)
        //{
        //    return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAllAvatarNamesAsync(removeDuplicates, includeUserNames));
        //}

        ///// <summary>
        ///// Get's a list of all of the avatar names within The OASIS.
        ///// Only works for logged in &amp; authenticated users. Use Authenticate endpoint first to obtain a JWT Token.
        ///// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        ///// </summary>
        ///// <param name="removeDuplicates">Set removeDuplicates to true if you wish to remove duplicates (default)</param>
        ///// <param name="providerType"></param>
        ///// <param name="setGlobally"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get-all-avatar-names/{removeDuplicates}/{includeUserNames}/{providerType}/{setGlobally}")]
        //public async Task<OASISHttpResponseMessage<IEnumerable<string>>> GetAllAvatarNames(bool removeDuplicates, bool includeUserNames, ProviderType providerType, bool setGlobally = false)
        //{
        //    GetAndActivateProvider(providerType, setGlobally);
        //    return await GetAllAvatarNames(removeDuplicates, includeUserNames);
        //}

        /// <summary>
        /// Get's a list of all of the avatar names within The OASIS.
        /// Only works for logged in &amp; authenticated users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-avatar-names/{includeUsernames}/{includeIds}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<string>>> GetAllAvatarNames(bool includeUsernames = true, bool includeIds = true)
        {
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAllAvatarNamesAsync(includeUsernames, includeIds));
        }

        /// <summary>
        /// Get's a list of all of the avatar names within The OASIS.
        /// Only works for logged in &amp; authenticated users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-avatar-names/{includeUsernames}/{includeIds}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<string>>> GetAllAvatarNames(bool includeUsernames, bool includeIds, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAllAvatarNames(includeUsernames, includeIds);
        }

        /// <summary>
        /// Get's a list of all of the avatar names within The OASIS along with their respective id's.
        /// Only works for logged in &amp; authenticated users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-avatar-names-grouped-by-name/{includeUsernames}/{includeIds}")]
        public async Task<OASISHttpResponseMessage<Dictionary<string, List<string>>>> GetAllAvatarNamesGroupedByName(bool includeUsernames = true, bool includeIds = true)
        {
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAllAvatarNamesGroupedByNameAsync(includeUsernames, includeIds));
        }

        /// <summary>
        /// Get's a list of all of the avatar names within The OASIS along with their respective id's.
        /// Only works for logged in &amp; authenticated users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-avatar-names-grouped-by-name/{includeUsernames}/{includeIds}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<Dictionary<string, List<string>>>> GetAllAvatarNamesGroupedByName(bool includeUsernames, bool includeIds, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetAllAvatarNamesGroupedByName(includeUsernames, includeIds);
        }

        /// <summary>
        /// Get's the avatar for the given id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetById(Guid id)
        {
            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarAsync(id));
        }

        /// <summary>
        /// Get's the avatar for the given id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetById(id);
        }

        /// <summary>
        /// Get's the avatar for the given username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetByUsername(string username)
        {
            // users can get their own account and admins can get any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            try
            {
                return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarAsync(username));
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = ex.Message }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Get's the avatar for the given username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetByUsername(username);
        }

        /// <summary>
        /// Get's the avatar for the given email.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetByEmail(string email)
        {
            // users can get their own account and admins can get any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarByEmailAsync(email));
        }

        /// <summary>
        /// Get's the avatar for the given email.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetByUsername(email);
        }

        /// <summary>
        /// Search avatars for the given search term. Coming soon...
        /// </summary>
        /// <param name="searchParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("search")]
        public async Task<OASISHttpResponseMessage<ISearchResults>> SearchAvatar(SearchParams searchParams)
        {
            return HttpResponseHelper.FormatResponse(await SearchManager.Instance.SearchAsync(searchParams));
        }

        /// <summary>
        /// Search avatars for the given search term. Coming soon...
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("search/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<ISearchResults>> SearchAvatar(SearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await SearchAvatar(searchParams);
        }

        /// <summary>
        ///     Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma
        ///     was earnt (App, dApp, hApp, Website, Game, karmaSourceTitle/karamSourceDesc = The name/desc of the app/website/game
        ///     where the karma was earnt. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karmaSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("add-karma-to-avatar/{avatarId}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> AddKarmaToAvatar(Guid avatarId,
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            try
            {
                var result = await AvatarManager.AddKarmaToAvatarAsync(
                    avatarId, 
                    (KarmaTypePositive)Enum.Parse(typeof(KarmaTypePositive), addKarmaToAvatarRequest.KarmaType), 
                    (KarmaSourceType)Enum.Parse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType), 
                    addKarmaToAvatarRequest.KaramSourceTitle, 
                    addKarmaToAvatarRequest.KarmaSourceDesc, 
                    null); // KarmaSourceWebLink not available in request
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { Result = result });
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        /// <summary>
        ///     Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma
        ///     was earnt (App, dApp, hApp, Website, Game, karmaSourceTitle/karamSourceDesc = The name/desc of the app/website/game
        ///     where the karma was earnt.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karmaSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally">
        ///     Set this to false for this provider to be used only for this request or true for it to be
        ///     used for all future requests too.
        /// </param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> AddKarmaToAvatar(
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType,
            bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await AddKarmaToAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        ///     Remove karma from the given avatar. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game,
        ///     karmaSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karmaSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("remove-karma-from-avatar/{avatarId}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> RemoveKarmaFromAvatar(Guid avatarId,
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            try
            {
                var result = await AvatarManager.RemoveKarmaFromAvatarAsync(
                    avatarId, 
                    (KarmaTypeNegative)Enum.Parse(typeof(KarmaTypeNegative), addKarmaToAvatarRequest.KarmaType), 
                    (KarmaSourceType)Enum.Parse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType), 
                    addKarmaToAvatarRequest.KaramSourceTitle, 
                    addKarmaToAvatarRequest.KarmaSourceDesc, 
                    null); // KarmaSourceWebLink not available in request
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { Result = result });
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        /// <summary>
        ///     Remove karma from the given avatar. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game,
        ///     karmaSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or
        ///     true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karmaSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally">
        ///     Set this to false for this provider to be used only for this request or true for it to be
        ///     used for all future requests too.
        /// </param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> RemoveKarmaFromAvatar(
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType,
            bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await RemoveKarmaFromAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        ///     Update the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> Update(UpdateRequest avatar, Guid id)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            // Load existing avatar and update with new data
            var existingAvatarResult = await Program.AvatarManager.LoadAvatarAsync(id);
            if (existingAvatarResult.IsError || existingAvatarResult.Result == null)
                return HttpResponseHelper.FormatResponse(existingAvatarResult, HttpStatusCode.NotFound);

            var existingAvatar = existingAvatarResult.Result;
            
            // Enforce uniqueness before applying username/email changes
            if (!string.IsNullOrEmpty(avatar.Username) && !string.Equals(avatar.Username, existingAvatar.Username, StringComparison.OrdinalIgnoreCase))
            {
                var usernameCheck = Program.AvatarManager.CheckIfUsernameIsAlreadyInUse(avatar.Username);
                if (usernameCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Username '{avatar.Username}' is already taken." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Email) && !string.Equals(avatar.Email, existingAvatar.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailCheck = Program.AvatarManager.CheckIfEmailIsAlreadyInUse(avatar.Email, false);
                if (emailCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Email '{avatar.Email}' is already registered to another account." }, HttpStatusCode.Conflict);
            }

            // Update avatar properties from UpdateRequest
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.AvatarType) && Avatar.AvatarType.Value == AvatarType.Wizard)
            {
                if (Enum.TryParse<AvatarType>(avatar.AvatarType, out var avatarType))
                    existingAvatar.AvatarType = new EnumValue<AvatarType>(avatarType);
            }

            // Use AvatarManager for business logic
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.SaveAvatarAsync(existingAvatar));
        }

        /// <summary>
        ///     Update the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for
        ///     it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="avatar">The avatar to update.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-id/{id}/{providerType}/{setGlobally}")]
        //public ActionResult<IAvatar> Update(Guid id, Core.Avatar avatar, ProviderType providerType, bool setGlobally = false)
        public async Task<OASISHttpResponseMessage<IAvatar>> Update(Guid id, UpdateRequest avatar, ProviderType providerType,
            bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await Update(avatar, id);
        }

        /// <summary>
        /// Update the given avatar using their email address.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByEmail(UpdateRequest avatar, string email)
        {
            // users can update their own account and admins can update any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            // Load existing avatar by email and update with new data
            var existingAvatarResult = await Program.AvatarManager.LoadAvatarByEmailAsync(email);
            if (existingAvatarResult.IsError || existingAvatarResult.Result == null)
                return HttpResponseHelper.FormatResponse(existingAvatarResult, HttpStatusCode.NotFound);

            var existingAvatar = existingAvatarResult.Result;
            
            // Enforce uniqueness before applying username/email changes
            if (!string.IsNullOrEmpty(avatar.Username) && !string.Equals(avatar.Username, existingAvatar.Username, StringComparison.OrdinalIgnoreCase))
            {
                var usernameCheck = Program.AvatarManager.CheckIfUsernameIsAlreadyInUse(avatar.Username);
                if (usernameCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Username '{avatar.Username}' is already taken." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Email) && !string.Equals(avatar.Email, existingAvatar.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailCheck = Program.AvatarManager.CheckIfEmailIsAlreadyInUse(avatar.Email, false);
                if (emailCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Email '{avatar.Email}' is already registered to another account." }, HttpStatusCode.Conflict);
            }

            // Update avatar properties from UpdateRequest
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.AvatarType) && Avatar.AvatarType.Value == AvatarType.Wizard)
            {
                if (Enum.TryParse<AvatarType>(avatar.AvatarType, out var avatarType))
                    existingAvatar.AvatarType = new EnumValue<AvatarType>(avatarType);
            }

            // Use AvatarManager for business logic
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.SaveAvatarAsync(existingAvatar));
        }

        /// <summary>
        /// Update the given avatar using their email address.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByEmail(UpdateRequest avatar, string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateByEmail(avatar, email);
        }

        /// <summary>
        /// Update the given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="username"></param>
        [Authorize]
        [HttpPost("update-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByUsername(UpdateRequest avatar, string username)
        {
            // users can update their own account and admins can update any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            // Load existing avatar by username and update with new data
            var existingAvatarResult = await Program.AvatarManager.LoadAvatarAsync(username);
            if (existingAvatarResult.IsError || existingAvatarResult.Result == null)
                return HttpResponseHelper.FormatResponse(existingAvatarResult, HttpStatusCode.NotFound);

            var existingAvatar = existingAvatarResult.Result;
            
            // Enforce uniqueness before applying username/email changes
            if (!string.IsNullOrEmpty(avatar.Username) && !string.Equals(avatar.Username, existingAvatar.Username, StringComparison.OrdinalIgnoreCase))
            {
                var usernameCheck = Program.AvatarManager.CheckIfUsernameIsAlreadyInUse(avatar.Username);
                if (usernameCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Username '{avatar.Username}' is already taken." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Email) && !string.Equals(avatar.Email, existingAvatar.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailCheck = Program.AvatarManager.CheckIfEmailIsAlreadyInUse(avatar.Email, false);
                if (emailCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Email '{avatar.Email}' is already registered to another account." }, HttpStatusCode.Conflict);
            }

            // Update avatar properties from UpdateRequest
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.AvatarType) && Avatar.AvatarType.Value == AvatarType.Wizard)
            {
                if (Enum.TryParse<AvatarType>(avatar.AvatarType, out var avatarType))
                    existingAvatar.AvatarType = new EnumValue<AvatarType>(avatarType);
            }

            // Use AvatarManager for business logic
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.SaveAvatarAsync(existingAvatar));
        }

        /// <summary>
        /// Update the given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        [Authorize]
        [HttpPost("update-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByUsername(UpdateRequest avatar, string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateByUsername(avatar, username);
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetail(AvatarDetail avatarDetail, Guid id)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.UpdateAvatarDetailAsync(id, avatarDetail));
        }

        /// <summary>
        ///     Update the given avatar detail by the avatar's id. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="avatarDetail">The avatar detail to update.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetail(Guid id, AvatarDetail avatarDetail, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateAvatarDetail(avatarDetail, id);
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar email address. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByEmail(AvatarDetail avatarDetail, string email)
        {
            // users can update their own account and admins can update any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.UpdateAvatarDetailByEmailAsync(email, avatarDetail));
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar email address. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByEmail(AvatarDetail avatarDetail, string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateAvatarDetailByEmail(avatarDetail, email);
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar username. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByUsername(AvatarDetail avatarDetail, string username)
        {
            // users can update their own account and admins can update any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.UpdateAvatarDetailByUsernameAsync(username, avatarDetail));
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar username. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByUsername(AvatarDetail avatarDetail, string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateAvatarDetailByUsername(avatarDetail, username);
        }

        /// <summary>
        ///     Delete the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}")]
        public async Task<OASISHttpResponseMessage<bool>> Delete(Guid id)
        {
            // users can delete their own account and admins can delete any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.DeleteAvatarAsync(id));
        }

        /// <summary>
        ///     Delete the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> Delete(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await Delete(id);
        }

        /// <summary>
        ///     Delete the given avatar using their username.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="username">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByUsername(string username)
        {
            // users can delete their own account and admins can delete any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.DeleteAvatarByUsernameAsync(username));
        }

        /// <summary>
        ///     Delete the given avatar using their username.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await DeleteByUsername(username);
        }

        /// <summary>
        ///     Delete the given avatar using their email.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="email">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByEmail(string email)
        {
            // users can delete their own account and admins can delete any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.DeleteAvatarByEmailAsync(email));
        }

        /// <summary>
        ///     Delete the given avatar using their email.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await DeleteByUsername(email);
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonById(Guid id)
        {
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarUmaJsonByIdAsync(id));
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetUmaJsonById(id);
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByUsername(string username)
        {
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarUmaJsonByUsernameAsync(username));
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetUmaJsonByUsername(username);
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their email.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByEmail(string email)
        {
            return HttpResponseHelper.FormatResponse(await AvatarManager.GetAvatarUmaJsonByEmailAsync(email));
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their email.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> GetUmaJsonByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetUmaJsonByEmail(email);
        }

        /// <summary>
        /// Get's the logged in avatar.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-logged-in-avatar")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetLoggedInAvatar()
        {
            /* JwtMiddleware sets HttpContext.Items["Avatar"] from the JWT "id" claim. AvatarManager.LoggedInAvatar is process-global and can be another user (last full login) — never use it alone for JWT-authenticated requests. */
            var avatar = Avatar ?? AvatarManager.LoggedInAvatar;
            if (avatar == null)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> { IsError = true, Message = "Not authenticated." }, HttpStatusCode.Unauthorized);
            return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> { Result = avatar });
        }

         /// <summary>
        /// Gets the logged-in avatar with XP (AvatarDetail). Used by STAR API GET /api/avatar/current so clients can refresh XP after beam-in.
        /// </summary>
        [Authorize]
        [HttpGet("get-logged-in-avatar-with-xp")]
        public async Task<OASISHttpResponseMessage<LoggedInAvatarResponse>> GetLoggedInAvatarWithXp()
        {
            var avatar = Avatar ?? AvatarManager.LoggedInAvatar;
            if (avatar == null)
                return HttpResponseHelper.FormatResponse(new OASISResult<LoggedInAvatarResponse> { IsError = true, Message = "Not authenticated." }, HttpStatusCode.Unauthorized);
            var detailResult = await Program.AvatarManager.LoadAvatarDetailAsync(avatar.Id);
            if (detailResult.IsError || detailResult.Result == null)
            {
                _logger.LogWarning(
                    "[Quest] GetLoggedInAvatarWithXp: LoadAvatarDetailAsync failed for avatar {AvatarId}: {Message}. Returning error (do not return HTTP 200 with xp=0 — that makes clients think profile refresh succeeded with empty XP/quest).",
                    avatar.Id,
                    detailResult.Message ?? "(no message)");
                return HttpResponseHelper.FormatResponse(
                    new OASISResult<LoggedInAvatarResponse>
                    {
                        IsError = true,
                        Message = detailResult.Message ?? "Failed to load avatar detail (XP and active quest are stored on AvatarDetail).",
                        DetailedMessage = detailResult.DetailedMessage
                    },
                    HttpStatusCode.BadRequest);
            }

            var detail = detailResult.Result;
            var xp = detail.XP;
            var activeQuestId = detail.ActiveQuestId;
            var activeObjectiveId = detail.ActiveObjectiveId;
            _logger.LogInformation("[Quest] GetLoggedInAvatarWithXp loaded detail for avatar {AvatarId}: XP={Xp}, ActiveQuestId={QuestId}, ActiveObjectiveId={ObjectiveId} (from storage)", avatar.Id, xp, activeQuestId, activeObjectiveId);
            _logger.LogInformation("[Quest] GetLoggedInAvatarWithXp returning for avatar {AvatarId}: XP={Xp}, ActiveQuestId={QuestId}, ActiveObjectiveId={ObjectiveId}", avatar.Id, xp, activeQuestId, activeObjectiveId);
            StarLog($"GetLoggedInAvatarWithXp returning: ActiveQuestId={activeQuestId}, ActiveObjectiveId={activeObjectiveId}");
            var response = new LoggedInAvatarResponse
            {
                Id = avatar.Id,
                Username = avatar.Username ?? string.Empty,
                Email = avatar.Email ?? string.Empty,
                FirstName = avatar.FirstName ?? string.Empty,
                LastName = avatar.LastName ?? string.Empty,
                XP = xp,
                ActiveQuestId = activeQuestId,
                ActiveObjectiveId = activeObjectiveId
            };
            return HttpResponseHelper.FormatResponse(new OASISResult<LoggedInAvatarResponse> { Result = response });
        }

        /// <summary>
        /// Get's the logged in avatar.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-logged-in-avatar/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> GetLoggedInAvatar(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await GetLoggedInAvatar();
        }

        /// <summary>
        /// Add experience points to the logged-in avatar (e.g. from game actions like killing monsters). Only works for logged-in users.
        /// Amount 0 is allowed: returns current XP without changing it (used by clients to refresh XP cache after beam-in).
        /// </summary>
        /// <param name="request">Body with amount (non-negative integer).</param>
        /// <returns>New total XP after adding (or current XP if amount is 0).</returns>
        [Authorize]
        [HttpPost("add-xp")]
        public async Task<OASISHttpResponseMessage<AddXpResponse>> AddXp([FromBody] AddXpRequest request)
        {
            if (request == null || request.Amount < 0)
                return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { IsError = true, Message = "Amount must be a non-negative integer." }, HttpStatusCode.BadRequest);

            var avatarId = Avatar?.Id ?? Guid.Empty;
            if (avatarId == Guid.Empty)
                return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { IsError = true, Message = "Not authenticated." }, HttpStatusCode.Unauthorized);

            var loadResult = await Program.AvatarManager.LoadAvatarDetailAsync(avatarId);
            if (loadResult.IsError || loadResult.Result == null)
                return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { IsError = true, Message = loadResult.Message ?? "Failed to load avatar detail." }, HttpStatusCode.BadRequest);

            var detail = loadResult.Result;
            if (request.Amount > 0)
            {
                detail.XP = detail.XP + request.Amount;
                if (detail.XP < 0)
                    detail.XP = 0;
                var updateResult = await Program.AvatarManager.UpdateAvatarDetailAsync(avatarId, detail);
                if (updateResult.IsError)
                    return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { IsError = true, Message = updateResult.Message ?? "Failed to update avatar XP." }, HttpStatusCode.BadRequest);
            }

            var newTotal = detail.XP;
            return HttpResponseHelper.FormatResponse(new OASISResult<AddXpResponse> { Result = new AddXpResponse { NewTotal = newTotal }, IsError = false });
        }

        /// <summary>
        /// Sets the active quest and objective for the logged-in avatar (tracker state). Persisted on AvatarDetail so they are restored after beam-in.
        /// </summary>
        [Authorize]
        [HttpPost("set-active-quest")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<bool>> SetActiveQuest([FromBody] SetActiveQuestRequest request)
        {
            _logger.LogInformation("[Quest] SetActiveQuest called: ActiveQuestId={QuestId}, ActiveObjectiveId={ObjectiveId}", request?.ActiveQuestId, request?.ActiveObjectiveId);
            StarLog($"SetActiveQuest called: ActiveQuestId={request?.ActiveQuestId}, ActiveObjectiveId={request?.ActiveObjectiveId}");

            var avatarId = Avatar?.Id ?? Guid.Empty;
            if (avatarId == Guid.Empty)
            {
                _logger.LogWarning("[Quest] SetActiveQuest rejected: not authenticated.");
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "Not authenticated." }, HttpStatusCode.Unauthorized);
            }

            var loadResult = await Program.AvatarManager.LoadAvatarDetailAsync(avatarId);
            if (loadResult.IsError || loadResult.Result == null)
            {
                _logger.LogWarning("[Quest] SetActiveQuest failed to load detail: {Message}", loadResult.Message);
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = loadResult.Message ?? "Failed to load avatar detail." }, HttpStatusCode.BadRequest);
            }

            var detail = loadResult.Result;
            detail.ActiveQuestId = request?.ActiveQuestId;
            detail.ActiveObjectiveId = request?.ActiveObjectiveId;

            var updateResult = await Program.AvatarManager.UpdateAvatarDetailAsync(avatarId, detail);
            if (updateResult.IsError)
            {
                _logger.LogWarning("[Quest] SetActiveQuest update failed: {Message}", updateResult.Message);
                return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = updateResult.Message ?? "Failed to update active quest." }, HttpStatusCode.BadRequest);
            }

            _logger.LogInformation("[Quest] SetActiveQuest saved for avatar {AvatarId}: ActiveQuestId={QuestId}, ActiveObjectiveId={ObjectiveId}", avatarId, detail.ActiveQuestId, detail.ActiveObjectiveId);
            StarLog($"SetActiveQuest saved OK for avatar {avatarId}: ActiveQuestId={detail.ActiveQuestId}, ActiveObjectiveId={detail.ActiveObjectiveId}");
            return HttpResponseHelper.FormatResponse(new OASISResult<bool> { Result = true, IsError = false });
        }
    }
}