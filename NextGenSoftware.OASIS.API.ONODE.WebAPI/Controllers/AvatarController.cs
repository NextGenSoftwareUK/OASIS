using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AvatarController : OASISControllerBase
    {
        private readonly IAvatarService _avatarService;
        private KeyManager _keyManager = null;

        //public KeyManager KeyManager
        //{
        //    get
        //    {
        //        if (_keyManager == null)
        //        {
        //            OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

        //            if (result.IsError)
        //                ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

        //            _keyManager = new KeyManager(result.Result, Program.AvatarManager);
        //        }

        //        return _keyManager;
        //    }
        //}

        public AvatarController(IAvatarService avatarService)
        {
            _avatarService = avatarService;
        }

        [HttpGet("GetTerms")]
        public async Task<OASISResult<string>> GetTerms()
        {
            return await _avatarService.GetTerms();
        }

        [Authorize]
        [HttpGet("GetAvatarImage/{id}")]
        public async Task<OASISResult<AvatarImage>> GetAvatarImageById(Guid id)
        {
            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<AvatarImage>() {Result = null, IsError = true, Message = "Unauthorized"};
            return await _avatarService.GetAvatarImageById(id);
        }

        [Authorize]
        [HttpGet("GetAvatarImageByUsername/{username}")]
        public async Task<OASISResult<AvatarImage>> GetAvatarImageByUsername(string username)
        {
            // users can get their own account and admins can get any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<AvatarImage> {IsError = true, Message = "Unauthorized"};
            return await _avatarService.GetAvatarImageByUsername(username);
        }

        [Authorize]
        [HttpGet("GetAvatarImageByEmail/{email}")]
        public async Task<OASISResult<AvatarImage>> GetAvatarImageByEmail(string email)
        {
            // users can get their own account and admins can get any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<AvatarImage> {IsError = true, Message = "Unauthorized"};
            return await _avatarService.GetAvatarImageByEmail(email);
        }

        [Authorize]
        [HttpPost("Upload2DAvatarImage")]
        public async Task<OASISResult<string>> Upload2DAvatarImage(AvatarImage avatarImage)
        {
            // users can get their own account and admins can get any account
            if (avatarImage.AvatarId != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<string>()
                    {Result = "Image not uploaded", Message = "Unauthorized", IsError = true};
            return await _avatarService.Upload2DAvatarImage(avatarImage);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAvatarDetail/{id:guid}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetail(Guid id)
        {
            return await _avatarService.GetAvatarDetail(id);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAvatarDetailByEmail/{email}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByEmail(string email)
        {
            return await _avatarService.GetAvatarDetailByEmail(email);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAvatarDetailByUsername/{username}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByUsername(string username)
        {
            return await _avatarService.GetAvatarDetailByUsername(username);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAllAvatarDetails")]
        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails()
        {
            return await _avatarService.GetAllAvatarDetails();
        }

        /// <summary>
        ///     Get's all avatars (only works for logged in &amp; authenticated Wizards (Admins)).
        /// </summary>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAll")]
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAll()
        {
            return await _avatarService.GetAll();
        }

        /// <summary>
        ///     Get's all avatars (only works for logged in &amp; authenticated Wizards (Admins)) for a given provider. Pass in the
        ///     provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or
        ///     true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType" description="test desc"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAll/{providerType}")]
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAll(ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetAll();
        }

        /// <summary>
        ///     Get's the avatar for the given id. You must be logged in &amp; authenticated for this to work.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetById/{id}")]
        public async Task<OASISResult<IAvatar>> GetById(Guid id)
        {
            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar> {Result = null, Message = "Unauthorized", IsError = true};
            return await _avatarService.GetById(id);
        }

        [Authorize]
        [HttpGet("GetByUsername/{username}")]
        public async Task<OASISResult<IAvatar>> GetByUsername(string username)
        {
            // users can get their own account and admins can get any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar> {Message = "Unauthorized", IsError = true};
            return await _avatarService.GetByUsername(username);
        }

        [Authorize]
        [HttpGet("GetByEmail/{email}")]
        public async Task<OASISResult<IAvatar>> GetByEmail(string email)
        {
            // users can get their own account and admins can get any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar> {Message = "Unauthorized", IsError = true};
            return await _avatarService.GetByEmail(email);
        }

        /// <summary>
        ///     Get's the avatar for the given id. You must be logged in &amp; authenticated for this to work. Pass in the provider
        ///     you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for
        ///     it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetById/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> GetById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetById(id);
        }

        /// <summary>
        ///     Search avatars for the given search term. Coming soon...
        /// </summary>
        /// <param name="searchParams"></param>
        /// <returns></returns>
        [HttpGet("Search/{searchParams}")]
        public async Task<OASISResult<ISearchResults>> Search(ISearchParams searchParams)
        {
            return await _avatarService.Search(searchParams);
        }

        /// <summary>
        ///     Search avatars for the given search term. Coming soon... Pass in the provider you wish to use. Set the setglobally
        ///     flag to false for this provider to be used only for this request or true for it to be used for all future requests
        ///     too.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpGet("Search/{searchParams}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<ISearchResults>> Search(ISearchParams searchParams, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await _avatarService.Search(searchParams);
        }


        /// <summary>
        ///     Authenticate and log in using the given avatar credentials. Pass in the provider you wish to use. Set the
        ///     setglobally flag to false for this provider to be used only for this request or true for it to be used for all
        ///     future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("authenticate/{providerType}/{setGlobally}")]
        public async Task<OASISResult<AuthenticateResponse>> Authenticate(AuthenticateRequest model,
        //public async Task<OASISResult<IAvatar>> Authenticate(AuthenticateRequest model,
            ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Authenticate(model);
        }


        /// <summary>
        ///     Authenticate and log in using the given avatar credentials.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("authenticate")]
        public async Task<OASISResult<AuthenticateResponse>> Authenticate(AuthenticateRequest model)
        //public async Task<OASISResult<IAvatar>> Authenticate(AuthenticateRequest model)
        {
            var response = await _avatarService.Authenticate(model, ipAddress());

            if (!response.IsError && response.Result.Avatar != null)
                setTokenCookie(response.Result.Avatar.RefreshToken);

            //if (!response.IsError && response.Result != null)
            //    setTokenCookie(response.Result.RefreshToken);

            return response;
        }

        [HttpPost("AuthenticateToken/{token}")]
        public async Task<OASISResult<string>> Authenticate(string token)
        {
            return await _avatarService.ValidateAccountToken(token);
        }

        /// <summary>
        ///     Refresh and generate a new JWT Security Token. This will only work if you are already logged in &amp;
        ///     authenticated.
        /// </summary>
        /// <returns></returns>
        [HttpPost("refresh-token")]
        public async Task<OASISResult<IAvatar>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = await _avatarService.RefreshToken(refreshToken, ipAddress());
            setTokenCookie(response.Result.RefreshToken);
            return response;
        }

        /// <summary>
        ///     Refresh and generate a new JWT Security Token. This will only work if you are already logged in &amp;
        ///     authenticated. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used
        ///     only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("refresh-token/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> RefreshToken(ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await RefreshToken();
        }

        /// <summary>
        ///     Revoke a given JWT Token (for example, if a user logs out). They must be logged in &amp; authenticated for this
        ///     method to work.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<OASISResult<string>> RevokeToken(RevokeTokenRequest model)
        {
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return new OASISResult<string>() {Result = "Token is required", IsError = true};

            // users can revoke their own tokens and admins can revoke any tokens
            if (!Avatar.OwnsToken(token) && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<string>() {Result = "Unauthorized", IsError = true};

            return await _avatarService.RevokeToken(token, ipAddress());
        }

        /// <summary>
        ///     Revoke a given JWT Token (for example, if a user logs out). They must be logged in &amp; authenticated for this
        ///     method to work. This will only work if you are already logged &amp; authenticated. Pass in the provider you wish to
        ///     use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used
        ///     for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("revoke-token/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> RevokeToken(RevokeTokenRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await RevokeToken(model);
        }

        /// <summary>
        ///     Register a new avatar.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("register")]
        public async Task<OASISResult<IAvatar>> Register(RegisterRequest model)
        {
            return await _avatarService.RegisterAsync(model, Request.Headers["origin"]);
        }

        /// <summary>
        ///     Register a new avatar. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to
        ///     be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("register/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> Register(RegisterRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Register(model);
        }


        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. This method is used by
        ///     the link in the email.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("verify-email")]
        public async Task<OASISResult<bool>> VerifyEmail(string token)
        {
            return await _avatarService.VerifyEmail(token);
        }

        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. This method is used by
        ///     the REST API or other methods that need to POST the data rather than GET.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("verify-email")]
        public async Task<OASISResult<bool>> VerifyEmail(VerifyEmailRequest model)
        {
            return await VerifyEmail(model.Token);
        }

        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. Pass in the provider you
        ///     wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to
        ///     be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("verify-email/{providerType}/{setGlobally}")]
        public async Task<OASISResult<bool>> VerifyEmail(VerifyEmailRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await VerifyEmail(model);
        }

        /// <summary>
        ///     This will send a password reset email allowing the user to reset their password. Call the
        ///     avatar/validate-reset-token method passing in the reset token received in the email.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("forgot-password")]
        public async Task<OASISResult<string>> ForgotPassword(ForgotPasswordRequest model)
        {
            return await _avatarService.ForgotPassword(model, Request.Headers["origin"]);
        }

        /// <summary>
        ///     This will send a password reset email allowing the user to reset their password. Call the
        ///     avatar/validate-reset-token method passing in the reset token received in the email. Pass in the provider you wish
        ///     to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be
        ///     used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("forgot-password/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> ForgotPassword(ForgotPasswordRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await ForgotPassword(model);
        }

        /// <summary>
        ///     Call this method passing in the reset token received in the forgotten password email after first calling the
        ///     avatar/forgot-password method.
        /// </summary>
        /// <param name="model"></param>
        /// < returns></returns>
        [HttpPost("validate-reset-token")]
        public async Task<OASISResult<string>> ValidateResetToken(ValidateResetTokenRequest model)
        {
            return await _avatarService.ValidateResetToken(model);
        }

        /// <summary>
        ///     Call this method passing in the reset token received in the forgotten password email after first calling the
        ///     avatar/forgot-password method.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("reset-password")]
        public async Task<OASISResult<string>> ResetPassword(ResetPasswordRequest model)
        {
            return await _avatarService.ResetPassword(model);
        }

        /// <summary>
        ///     Call this method passing in the reset token received in the forgotten password email after first calling the
        ///     avatar/forgot-password method. Pass in the provider you wish to use. Set the setglobally flag to false for this
        ///     provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("reset-password/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> ResetPassword(ResetPasswordRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await ResetPassword(model);
        }

        /// <summary>
        ///     Allows a Wizard(Admin) to create new avatars including other wizards.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("Create/{model}")]
        public async Task<OASISResult<IAvatar>> Create(CreateRequest model)
        {
            return await _avatarService.Create(model);
        }

        /// <summary>
        ///     Allows a Wizard(Admin) to create new avatars including other wizards. Pass in the provider you wish to use. Set the
        ///     setglobally flag to false for this provider to be used only for this request or true for it to be used for all
        ///     future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("Create/{model}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> Create(CreateRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await _avatarService.Create(model);
        }

        /// <summary>
        ///     Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma
        ///     was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game
        ///     where the karma was earnt. They must be logged in &amp; authenticated for this method to work.
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddKarmaToAvatar/{avatarId}")]
        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatar(Guid avatarId,
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            return await _avatarService.AddKarmaToAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        ///     Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma
        ///     was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game
        ///     where the karma was earnt. They must be logged in &amp; authenticated for this method to work.
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally">
        ///     Set this to false for this provider to be used only for this request or true for it to be
        ///     used for all future requests too.
        /// </param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddKarmaToAvatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatar(
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await AddKarmaToAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        ///     Remove karma from the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType =
        ///     The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game,
        ///     karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{avatarId}")]
        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatar(Guid avatarId,
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            return await _avatarService.RemoveKarmaFromAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        ///     Remove karma from the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType =
        ///     The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game,
        ///     karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost. Pass in the
        ///     provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or
        ///     true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally">
        ///     Set this to false for this provider to be used only for this request or true for it to be
        ///     used for all future requests too.
        /// </param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatar(
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await RemoveKarmaFromAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        ///     Update the given avatar. They must be logged in &amp; authenticated for this method to work.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("Update/{id}")]
        public async Task<OASISResult<IAvatar>> Update(UpdateRequest avatar, Guid id)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar>() {Result = null, IsError = true, Message = "Unauthorized"};
            return await _avatarService.Update(id, avatar);
        }

        [Authorize]
        [HttpPost("UpdateByEmail/{email}")]
        public async Task<OASISResult<IAvatar>> UpdateByEmail(UpdateRequest avatar, string email)
        {
            // users can update their own account and admins can update any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar>() {Result = null, IsError = true, Message = "Unauthorized"};
            return await _avatarService.UpdateByEmail(email, avatar);
        }

        [Authorize]
        [HttpPost("UpdateByUsername/{email}")]
        public async Task<OASISResult<IAvatar>> UpdateByUsername(UpdateRequest avatar, string username)
        {
            // users can update their own account and admins can update any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar>() {Result = null, IsError = true, Message = "Unauthorized"};
            return await _avatarService.UpdateByUsername(username, avatar);
        }

        /// <summary>
        ///     Update the given avatar. They must be logged in &amp; authenticated for this method to work. Pass in the provider
        ///     you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for
        ///     it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="avatar">The avatar to update.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("Update/{id}/{providerType}/{setGlobally}")]
        //public ActionResult<IAvatar> Update(Guid id, Core.Avatar avatar, ProviderType providerType, bool setGlobally = false)
        public async Task<OASISResult<IAvatar>> Update(Guid id, UpdateRequest avatar, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Update(avatar, id);
        }


        /// <summary>
        ///     Delete the given avatar. They must be logged in &amp; authenticated for this method to work.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}")]
        public async Task<OASISResult<bool>> Delete(Guid id)
        {
            // users can delete their own account and admins can delete any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<bool> {Result = false, IsError = true};
            return await _avatarService.Delete(id);
        }

        [Authorize]
        [HttpDelete("DeleteByUsername/{username}")]
        public async Task<OASISResult<bool>> DeleteByUsername(string username)
        {
            // users can delete their own account and admins can delete any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<bool> {IsError = true, Message = "Unauthorized", Result = false};
            return await _avatarService.DeleteByUsername(username);
        }

        [Authorize]
        [HttpDelete("DeleteByEmail/{email}")]
        public async Task<OASISResult<bool>> DeleteByEmail(string email)
        {
            // users can delete their own account and admins can delete any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<bool> {IsError = true, Message = "Unauthorized", Result = false};
            return await _avatarService.DeleteByEmail(email);
        }

        /// <summary>
        ///     Delete the given avatar. They must be logged in &amp; authenticated for this method to work. Pass in the provider
        ///     you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for
        ///     it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<bool>> Delete(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Delete(id);
        }

        /// <summary>
        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        /// </summary>
        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("LinkProviderPublicKeyToAvatar")]
        public OASISResult<bool> LinkProviderPublicKeyToAvatar(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            bool isValid;
            ProviderType providerType;
            Guid avatarID;
            OASISResult<bool> result;

            (isValid, providerType, avatarID, result) = ValidateLinkProviderKeyToAvatarParams(linkProviderKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPublicKeyToAvatar(avatarID,
                   providerType, linkProviderKeyToAvatarParams.ProviderKey);
            else
                return result;
        }


        /// <summary>
        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        /// </summary>
        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("LinkProviderPublicKeyToAvatarByUsername")]
        public OASISResult<bool> LinkProviderPublicKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            bool isValid;
            ProviderType providerType;
            Guid avatarID;
            OASISResult<bool> result;

            (isValid, providerType, avatarID, result) = ValidateLinkProviderKeyToAvatarParams(linkProviderKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPublicKeyToAvatar(linkProviderKeyToAvatarParams.AvatarUsername,
                   providerType, linkProviderKeyToAvatarParams.ProviderKey);
            else
                return result;

            //object providerTypeObject;

            //if (!Enum.TryParse(typeof(ProviderType), providerType, out providerTypeObject))
            //    return (new OASISResult<bool> { IsError = true, Message = $"The given ProviderType {providerType} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}", Result = false });

            //return KeyManager.LinkProviderPublicKeyToAvatar(avatarUsername, (ProviderType)providerTypeObject, providerKey);
        }

        
        /// <summary>
        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        /// </summary>
        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("LinkProviderPublicKeyToAvatar")]
        public OASISResult<bool> LinkProviderPublicKeyToAvatar(string avatarUsername, string providerType, string providerKey)
        {
            //TODO: Check if this works? :)
            object providerTypeObject;

            if (!Enum.TryParse(typeof(ProviderType), providerType, out providerTypeObject))
                return (new OASISResult<bool> { IsError = true, Message = $"The given ProviderType {providerType} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}", Result = false });

            return KeyManager.LinkProviderPublicKeyToAvatar(avatarUsername, (ProviderType)providerTypeObject, providerKey);
        }

        /// <summary>
        ///     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
        /// </summary>
        /// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("LinkProviderPrivateKeyToAvatar")]
        public OASISResult<bool> LinkProviderPrivateKeyToAvatar(
            LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        {
            bool isValid;
            ProviderType providerType;
            Guid avatarID;
            OASISResult<bool> result;

            (isValid, providerType, avatarID, result) = ValidateLinkProviderKeyToAvatarParams(linkProviderPrivateKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPrivateKeyToAvatar(avatarID,
                   providerType, linkProviderPrivateKeyToAvatarParams.ProviderKey);
            else
                return result;
        }

        /// <summary>
        ///     Get's a given avatar's unique storage key for the given provider type.
        /// </summary>
        /// <param name="avatarId">The Avatar's avatarId.</param>
        /// <param name="providerType">The provider type to retreive the unique storage key for.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GetProviderUniqueStorageKeyForAvatar")]
        public OASISResult<string> GetProviderUniqueStorageKeyForAvatar(Guid avatarId, ProviderType providerType)
        {
            return KeyManager.GetProviderUniqueStorageKeyForAvatar(avatarId, providerType);
        }

        /// <summary>
        ///     Get's a given avatar's unique storage key for the given provider type.
        /// </summary>
        /// <param name="username">The Avatar's username.</param>
        /// <param name="providerType">The provider type to retreive the unique storage key for.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GetProviderUniqueStorageKeyForAvatar")]
        public OASISResult<string> GetProviderUniqueStorageKeyForAvatar(string username, ProviderType providerType)
        {
            return KeyManager.GetProviderUniqueStorageKeyForAvatar(username, providerType);
        }

        /// <summary>
        ///     Get's a given avatar's private key for the given provider type.
        /// </summary>
        /// <param name="avatarId">The Avatar's id.</param>
        /// <param name="providerType">The provider type to retreive the private key for.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GetProviderPrivateKeyForAvatar")]
        public OASISResult<string> GetProviderPrivateKeyForAvatar(Guid avatarId, ProviderType providerType)
        {
            return KeyManager.GetProviderPrivateKeyForAvatar(avatarId, providerType);
        }

        /// <summary>
        ///     Get's a given avatar's private key for the given provider type.
        /// </summary>
        /// <param name="username">The Avatar's username.</param>
        /// <param name="providerType">The provider type to retreive the private key for.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GetProviderPrivateKeyForAvatar")]
        public OASISResult<string> GetProviderPrivateKeyForAvatar(string username, ProviderType providerType)
        {
            return KeyManager.GetProviderPrivateKeyForAvatar(username, providerType);
        }

        /// <summary>
        ///     Get's a given avatar's public keys for the given provider type.
        /// </summary>
        /// <param name="avatarId">The Avatar's id.</param>
        /// <param name="providerType">The provider type to retreive the public keys for.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GetProviderPublicKeysForAvatar")]
        public OASISResult<List<string>> GetProviderPublicKeysForAvatar(Guid avatarId, ProviderType providerType)
        {
            return KeyManager.GetProviderPublicKeysForAvatar(avatarId, providerType);
        }

        /// <summary>
        ///     Get's a given avatar's public keys for the given provider type.
        /// </summary>
        /// <param name="username">The Avatar's username.</param>
        /// <param name="providerType">The provider type to retreive the public keys for.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GetProviderPublicKeysForAvatar")]
        public OASISResult<List<string>> GetProviderPublicKeysForAvatar(string username, ProviderType providerType)
        {
            return KeyManager.GetProviderPublicKeysForAvatar(username, providerType);
        }

        /*
        /// <summary>
        ///     Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{id:Guid}/{telosAccountName}")]
        public async Task<OASISResult<IAvatarDetail>> LinkTelosAccountToAvatar(Guid id, string telosAccountName)
        {
            return await _avatarService.LinkProviderKeyToAvatar(id, ProviderType.TelosOASIS, telosAccountName);
        }

        /// <summary>
        ///     Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<OASISResult<IAvatarDetail>> LinkTelosAccountToAvatar2(
            LinkProviderKeyToAvatar linkProviderKeyToAvatar)
        {
            return await _avatarService.LinkProviderKeyToAvatar(linkProviderKeyToAvatar.AvatarID,
                ProviderType.TelosOASIS, linkProviderKeyToAvatar.ProviderUniqueStorageKey);
        }


        /// <summary>
        ///     Link's a given eosioAccountName to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosioAccountName}")]
        public async Task<OASISResult<IAvatarDetail>> LinkEOSIOAccountToAvatar(Guid avatarId, string eosioAccountName)
        {
            return await _avatarService.LinkProviderKeyToAvatar(avatarId, ProviderType.EOSIOOASIS, eosioAccountName);
        }

        /// <summary>
        ///     Link's a given holochain AgentID to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="holochainAgentID"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{holochainAgentID}")]
        public async Task<OASISResult<IAvatarDetail>> LinkHolochainAgentIDToAvatar(Guid avatarId,
            string holochainAgentID)
        {
            return await _avatarService.LinkProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentID);
        }*/

        ///// <summary>
        /////     Get's the provider key for the given avatar and provider type.
        ///// </summary>
        ///// <param name="avatarUsername">The avatar username.</param>
        ///// <param name="providerType">The provider type.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("{avatarUsername}/{providerType}")]
        //public async Task<OASISResult<string>> GetProviderKeyForAvatar(string avatarUsername, ProviderType providerType)
        //{
        //    //return await _avatarService.GetProviderKeyForAvatar(avatarUsername, providerType);
        //    return await Program.AvatarManager.GetProviderKeyForAvatar(avatarUsername, providerType);
        //}


        [Authorize]
        [HttpGet("GetUMAJsonById/{id}")]
        public async Task<OASISResult<string>> GetUmaJsonById(Guid id)
        {
            return await _avatarService.GetAvatarUmaJsonById(id);
        }

        [Authorize]
        [HttpGet("GetUMAJsonByUsername/{username}")]
        public async Task<OASISResult<string>> GetUmaJsonByUsername(string username)
        {
            return await _avatarService.GetAvatarUmaJsonByUsername(username);
        }

        [Authorize]
        [HttpGet("GetUMAJsonByMail/{mail}")]
        public async Task<OASISResult<string>> GetUmaJsonMail(string mail)
        {
            return await _avatarService.GetAvatarUmaJsonByMail(mail);
        }

        [Authorize]
        [HttpGet("GetAvatarByJwt")]
        public async Task<OASISResult<IAvatar>> GetAvatarByJwt()
        {
            return await _avatarService.GetAvatarByJwt();
        }

        private void setTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string ipAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            return HttpContext.Connection.RemoteIpAddress != null
                ? HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString()
                : string.Empty;
        }

        private (bool, ProviderType, Guid, OASISResult<bool>) ValidateLinkProviderKeyToAvatarParams(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            object providerType;
            Guid avatarID;

            if (!Enum.TryParse(typeof(ProviderType), linkProviderKeyToAvatarParams.ProviderType, out providerType))
                return (false, ProviderType.None, Guid.Empty, new OASISResult<bool> { IsError = true, Message = $"The given ProviderType {linkProviderKeyToAvatarParams.ProviderType} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}", Result = false });

            if (!Guid.TryParse(linkProviderKeyToAvatarParams.AvatarID, out avatarID))
                return (false, ProviderType.None, Guid.Empty, new OASISResult<bool> { IsError = true, Message = $"The given AvatarID {linkProviderKeyToAvatarParams.AvatarID} is not a valid Guid.", Result = false });

            return (true, (ProviderType)providerType, avatarID, null);
        }
    }
}