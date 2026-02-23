using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    /// <summary>
    /// Avatar management endpoints for user registration, authentication, profile management, and session handling.
    /// Supports all OASIS providers with automatic failover and load balancing.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AvatarController : OASISControllerBase
    {
        // Directly use AvatarManager instead of service layer
        private AvatarManager AvatarManager => Program.AvatarManager;
        
        // Temporary service access for methods not yet migrated (being phased out)
        // Note: AvatarService is being phased out, use AvatarManager directly
        // private IAvatarService _avatarService => Program.AvatarService;
        
        public AvatarController()
        {
        }

        /// <summary>
        /// Register a new avatar with the OASIS system.
        /// </summary>
        /// <param name="model">Registration details including username, email, password, and optional provider preferences.</param>
        /// <returns>OASIS result containing the newly created avatar or error details.</returns>
        /// <response code="200">Avatar successfully registered</response>
        /// <response code="400">Invalid registration data or user already exists</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IAvatar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IAvatar>> Register(RegisterRequest model)
        {
            // Call AvatarManager directly
            var result = await AvatarManager.RegisterAsync(
                model.Title,
                model.FirstName,
                model.LastName,
                model.Email,
                model.Password,
                model.Username,
                model.AvatarType != null ? (AvatarType)Enum.Parse(typeof(AvatarType), model.AvatarType) : AvatarType.User,
                OASISType.OASISAPIREST
            );
            
            return HttpResponseHelper.FormatResponse(result);
        }

        /// <summary>
        ///     Register a new avatar. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to
        ///     be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model">Registration details including username, email, password, and optional provider preferences.</param>
        /// <param name="providerType">The OASIS provider type to use for registration.</param>
        /// <param name="setGlobally">Whether to set this provider globally for all future requests.</param>
        /// <returns>OASIS result containing the newly created avatar or error details.</returns>
        /// <response code="200">Avatar successfully registered</response>
        /// <response code="400">Invalid registration data or user already exists</response>
        [HttpPost("register/{providerType}/{setGlobally}")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IAvatar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IAvatar>> Register(RegisterRequest model, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await Register(model);
        }


        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. This method is used by
        ///     the link in the email.
        /// </summary>
        /// <param name="token">The verification token sent via email.</param>
        /// <returns>OASIS result indicating whether email verification was successful.</returns>
        /// <response code="200">Email verification completed (success or failure)</response>
        /// <response code="400">Invalid or expired verification token</response>
        [HttpGet("verify-email")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> VerifyEmail(string token)
        {
            return HttpResponseHelper.FormatResponse(AvatarManager.VerifyEmail(token));
        }

        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. This method is used by
        ///     the link in the email. 
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to
        ///     be used for all future requests too.
        /// </summary>
        /// <param name="token">The verification token sent via email.</param>
        /// <param name="providerType">The OASIS provider type to use for verification.</param>
        /// <param name="setGlobally">Whether to set this provider globally for all future requests.</param>
        /// <returns>OASIS result indicating whether email verification was successful.</returns>
        /// <response code="200">Email verification completed (success or failure)</response>
        /// <response code="400">Invalid or expired verification token</response>
        [HttpGet("verify-email/{providerType}/{setGlobally}")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> VerifyEmail(string token, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return HttpResponseHelper.FormatResponse(AvatarManager.VerifyEmail(token));
        }

        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. This method is used by
        ///     the REST API or other methods that need to POST the data rather than GET.
        /// </summary>
        /// <param name="model">The verification request containing the token.</param>
        /// <returns>OASIS result indicating whether email verification was successful.</returns>
        /// <response code="200">Email verification completed (success or failure)</response>
        /// <response code="400">Invalid or expired verification token</response>
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> VerifyEmail(VerifyEmailRequest model)
        {
            return await VerifyEmail(model.Token);
        }

        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. 
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to
        ///     be used for all future requests too.
        /// </summary>
        /// <param name="model">The verification request containing the token.</param>
        /// <param name="providerType">The OASIS provider type to use for verification.</param>
        /// <param name="setGlobally">Whether to set this provider globally for all future requests.</param>
        /// <returns>OASIS result indicating whether email verification was successful.</returns>
        /// <response code="200">Email verification completed (success or failure)</response>
        /// <response code="400">Invalid or expired verification token</response>
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [HttpPost("verify-email/{providerType}/{setGlobally}")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> VerifyEmail(VerifyEmailRequest model, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await VerifyEmail(model);
        }

        /// <summary>
        /// Authenticate and log in using avatar credentials.
        /// </summary>
        /// <param name="request">Authentication request containing username/email and password.</param>
        /// <returns>OASIS result containing authenticated avatar with JWT token or error details.</returns>
        /// <response code="200">Authentication successful</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="400">Invalid request data</response>
        [HttpPost("authenticate")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IAvatar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IAvatar>> Authenticate(AuthenticateRequest request)
        {
            OASISConfigResult<IAvatar> configResult = await ConfigureOASISEngineAsync<IAvatar>(request);

            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            var result = await Program.AvatarManager.AuthenticateAsync(request.Username, request.Password, ipAddress(), configResult.AutoReplicationMode, configResult.AutoFailOverMode, configResult.AutoLoadBalanceMode, request.WaitForAutoReplicationResult);
            ResetOASISSettings(request, configResult);

            if (!result.IsError && result.Result != null)
            {
                setTokenCookie(result.Result.RefreshToken);
                return HttpResponseHelper.FormatResponse(result, HttpStatusCode.OK, request.ShowDetailedSettings);
            }
            else
                return HttpResponseHelper.FormatResponse(result, HttpStatusCode.Unauthorized, request.ShowDetailedSettings);
        }

        /// <summary>
        /// Authenticate and log in using the given avatar credentials. 
        /// Pass in the provider you wish to use.
        /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
        /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <param name="autoFailOverMode"></param>
        /// <param name="autoReplicationMode"></param>
        /// <param name="autoLoadBalanceMode"></param>
        /// <param name="autoFailOverProviders"></param>
        /// <param name="autoReplicationProviders"></param>
        /// <param name="autoLoadBalanceProviders"></param>
        /// <param name="waitForAutoReplicationResult"></param>
        /// <param name="showDetailedSettings"></param>
        /// <returns></returns>
        [HttpPost("authenticate/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{AutoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> Authenticate(AuthenticateRequest model, string providerType, bool setGlobally = false, string autoReplicationMode = "default", string autoFailOverMode = "default", string autoLoadBalanceMode = "default", string autoReplicationProviders = "default", string autoFailOverProviders = "default", string autoLoadBalanceProviders = "default", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
        {
            model.ProviderType = providerType;
            model.SetGlobally = setGlobally;
            model.ShowDetailedSettings = showDetailedSettings;
            model.WaitForAutoReplicationResult = waitForAutoReplicationResult;
            model.AutoReplicationProviders = autoReplicationProviders;
            model.AutoFailOverProviders = autoFailOverProviders;
            model.AutoLoadBalanceProviders = autoLoadBalanceProviders;
            model.AutoReplicationMode = autoReplicationMode;
            model.AutoFailOverMode = autoFailOverMode;
            model.AutoLoadBalanceMode = autoLoadBalanceMode;

            return await Authenticate(model);
        }

        /// <summary>
        /// Authenticate and log in using the given JWT Token.
        /// </summary>
        /// <param name="JWTToken"></param>
        /// <returns></returns>
        [HttpPost("authenticate-token/{JWTToken}")]
        public async Task<OASISHttpResponseMessage<string>> Authenticate(string JWTToken)
        {
            // Use AvatarManager for JWT token validation
            var result = AvatarManager.ValidateAccountToken(JWTToken);
            return HttpResponseHelper.FormatResponse(result);
        }

        /// <summary>
        /// Authenticate and log in using the given JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="JWTToken"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("authenticate-token/{JWTToken}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> Authenticate(string JWTToken, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await Authenticate(JWTToken);
        }

        /// <summary>
        ///     Refresh and generate a new JWT Security Token. This will only work if you are already logged in &amp;
        ///     authenticated.
        /// </summary>
        /// <returns></returns>
        [HttpPost("refresh-token")]
        public async Task<OASISHttpResponseMessage<IAvatar>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = AvatarManager.RefreshToken(refreshToken, ipAddress());

            if (!response.IsError && response.Result != null)
                setTokenCookie(response.Result.RefreshToken);

            return HttpResponseHelper.FormatResponse(response);
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
        public async Task<OASISHttpResponseMessage<IAvatar>> RefreshToken(ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await RefreshToken();
        }

        /// <summary>
        ///     Revoke a given JWT Token (for example, if a user logs out). 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<OASISHttpResponseMessage<string>> RevokeToken(RevokeTokenRequest model)
        {
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return HttpResponseHelper.FormatResponse(new OASISResult<string>() { Result = "Token is required", IsError = true });

            // users can revoke their own tokens and admins can revoke any tokens
            if (!Avatar.OwnsToken(token) && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<string>() { Result = "Unauthorized", IsError = true }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(AvatarManager.RevokeToken(token, ipAddress()));
        }

        /// <summary>
        ///     Revoke a given JWT Token (for example, if a user logs out). They must be logged in &amp; authenticated for this
        ///     method to work. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used
        ///     for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("revoke-token/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> RevokeToken(RevokeTokenRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await RevokeToken(model);
        }

        /// <summary>
        ///     This will send a password reset email allowing the user to reset their password. Call the
        ///     avatar/validate-reset-token method passing in the reset token received in the email.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("forgot-password")]
        public async Task<OASISHttpResponseMessage<string>> ForgotPassword(ForgotPasswordRequest model)
        {
            //return HttpResponseHelper.FormatResponse(await Program.AvatarManager.ForgotPassword(model, Request.Headers["origin"]));
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.ForgotPasswordAsync(model.Email));
        }

        /// <summary>
        ///     This will send a password reset email allowing the user to reset their password. Call the
        ///     avatar/validate-reset-token method passing in the reset token received in the email. 
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("forgot-password/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> ForgotPassword(ForgotPasswordRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await ForgotPassword(model);
        }

        /// <summary>
        ///     Call this method passing in the reset token received in the forgotten password email after first calling the
        ///     avatar/forgot-password method.
        /// </summary>
        /// <param name="model"></param>
        /// < returns></returns>
        [HttpPost("validate-reset-token")]
        public async Task<OASISHttpResponseMessage<string>> ValidateResetToken(ValidateResetTokenRequest model)
        {
            return HttpResponseHelper.FormatResponse(AvatarManager.ValidateResetToken(model.Token));
        }

        /// <summary>
        ///     Call this method passing in the reset token received in the forgotten password email after first calling the
        ///     avatar/forgot-password method.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// < returns></returns>
        [HttpPost("validate-reset-token/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<string>> ValidateResetToken(ValidateResetTokenRequest model, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await ValidateResetToken(model);
        }

        /// <summary>
        ///     Call this method passing in the reset token received in the forgotten password email after first calling the
        ///     avatar/forgot-password method.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("reset-password")]
        public async Task<OASISHttpResponseMessage<string>> ResetPassword(ResetPasswordRequest model)
        {
            return HttpResponseHelper.FormatResponse(await AvatarManager.Instance.ResetPasswordAsync(model.Token, model.OldPassword, model.NewPassword));
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
        public async Task<OASISHttpResponseMessage<string>> ResetPassword(ResetPasswordRequest model, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await ResetPassword(model);
        }

        /// <summary>
        ///     Allows a Wizard(Admin) to create new avatars including other wizards.
        ///     Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        // OBSOLETE: Create method is deprecated - use Register endpoint instead
        /*
        [Authorize(AvatarType.Wizard)]
        [HttpPost("create/{model}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> Create(CreateRequest model)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> 
                    { 
                        IsError = true, 
                        Message = "Username, email, and password are required" 
                    });
                }

                // Check if username already exists
                var existingAvatar = await Program.AvatarManager.LoadAvatarAsync(model.Username);
                if (existingAvatar.Result != null)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> 
                    { 
                        IsError = true, 
                        Message = "Username already exists" 
                    });
                }

                // Check if email already exists
                var existingAvatarByEmail = await Program.AvatarManager.LoadAvatarByEmailAsync(model.Email);
                if (existingAvatarByEmail.Result != null)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> 
                    { 
                        IsError = true, 
                        Message = "Email already exists" 
                    });
                }

                // Create new avatar
                var avatar = new Avatar
                {
                    Username = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Password = model.Password,
                    AvatarType = !string.IsNullOrEmpty(model.AvatarType) ? new EnumValue<AvatarType>(Enum.Parse<AvatarType>(model.AvatarType)) : new EnumValue<AvatarType>(AvatarType.User),
                    CreatedDate = DateTime.UtcNow
                };

                // Save avatar - convert to IAvatar interface
                var result = await Program.AvatarManager.SaveAvatarAsync((IAvatar)avatar);
                
                if (result.IsError)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> 
                    { 
                        IsError = true, 
                        Message = result.Message 
                    });
                }

                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> 
                { 
                    Result = result.Result, 
                    IsError = false 
                });
            }
            catch (Exception ex)
            {
                return HandleExceptionForWeb4<IAvatar>(ex, "CreateAvatar");
            }
        }
        */

        /// <summary>
        ///     Allows a Wizard(Admin) to create new avatars including other wizards.
        ///     Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all
        ///     future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        // OBSOLETE: Create method is deprecated - use Register endpoint instead
        /*
        [Authorize(AvatarType.Wizard)]
        [HttpPost("create/{model}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> Create(CreateRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await Create(model);
        }
        */

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
            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait> { IsError = true, Message = "AvatarService.GetAvatarPortraitById not yet migrated to AvatarManager" });
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
            // users can get their own account and admins can get any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait> { IsError = true, Message = "AvatarService.GetAvatarPortraitByUsername not yet migrated to AvatarManager" });
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
            // users can get their own account and admins can get any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<AvatarPortrait> { IsError = true, Message = "AvatarService.GetAvatarPortraitByEmail not yet migrated to AvatarManager" });
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
            // users can get their own account and admins can get any account
            if (avatarPortrait.AvatarId != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "AvatarService.UploadAvatarPortrait not yet migrated to AvatarManager" });
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
        [Authorize(AvatarType.Wizard)]
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
        [Authorize(AvatarType.Wizard)]
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
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-avatar-detail-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByEmail(string email)
        {
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
        [Authorize(AvatarType.Wizard)]
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
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-avatar-detail-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> GetAvatarDetailByUsername(string username)
        {
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
        [Authorize(AvatarType.Wizard)]
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

            //return await _avatarService.GetByUsername(username);
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.LoadAvatarAsync(username));
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
        [HttpPost("search/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<ISearchResults>> SearchAvatar(SearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await SearchAvatar(searchParams);
        }

        /// <summary>
        ///     Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma
        ///     was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game
        ///     where the karma was earnt. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
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
        ///     was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game
        ///     where the karma was earnt.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
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
        ///     karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
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
        ///     karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or
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
            
            // Update avatar properties from UpdateRequest
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.AvatarType)) 
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
            
            // Update avatar properties from UpdateRequest
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.AvatarType)) 
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
            
            // Update avatar properties from UpdateRequest
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.AvatarType)) 
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
            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<string> { IsError = true, Message = "AvatarService.GetAvatarUmaJsonById not yet migrated to AvatarManager" });
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
            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<string> { IsError = true, Message = "AvatarService.GetAvatarUmaJsonByUsername not yet migrated to AvatarManager" });
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
            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<string> { IsError = true, Message = "AvatarService.GetAvatarUmaJsonByEmail not yet migrated to AvatarManager" });
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
            return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> { Result = AvatarManager.LoggedInAvatar });
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



        ///// <summary>
        /////     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        ///// </summary>
        ///// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("LinkProviderPublicKeyToAvatarByAvatarId")]
        //public OASISHttpResponseMessage<bool> LinkProviderPublicKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        //{
        //    bool isValid;
        //    string errorMessage = "";
        //    ProviderType providerTypeToLinkTo;
        //    ProviderType providerTypeToLoadAvatarFrom;
        //    Guid avatarID;

        //    (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderKeyToAvatarParams);

        //    if (isValid)
        //        return KeyManager.LinkProviderPublicKeyToAvatar(avatarID, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
        //    else
        //        return new OASISHttpResponseMessage<bool>(false) { IsError = true, Message = errorMessage };
        //}


        ///// <summary>
        /////     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        ///// </summary>
        ///// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("LinkProviderPublicKeyToAvatarByUsername")]
        //public OASISHttpResponseMessage<bool> LinkProviderPublicKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        //{
        //    bool isValid;
        //    string errorMessage = "";
        //    ProviderType providerTypeToLinkTo;
        //    ProviderType providerTypeToLoadAvatarFrom;
        //    Guid avatarID;

        //    (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderKeyToAvatarParams);

        //    if (isValid)
        //        return KeyManager.LinkProviderPublicKeyToAvatar(linkProviderKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
        //    else
        //        return new OASISHttpResponseMessage<bool>(false) { IsError = true, Message = errorMessage };
        //}

        ///// <summary>
        /////     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
        ///// </summary>
        ///// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("LinkProviderPrivateKeyToAvatarByAvatarId")]
        //public OASISHttpResponseMessage<bool> LinkProviderPrivateKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        //{
        //    bool isValid;
        //    string errorMessage = "";
        //    ProviderType providerTypeToLinkTo;
        //    ProviderType providerTypeToLoadAvatarFrom;
        //    Guid avatarID;

        //    (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderPrivateKeyToAvatarParams);

        //    if (isValid)
        //        return KeyManager.LinkProviderPrivateKeyToAvatar(avatarID, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
        //    else
        //        return new OASISHttpResponseMessage<bool>(false) { IsError = true, Message = errorMessage };
        //}

        ///// <summary>
        /////     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
        ///// </summary>
        ///// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("LinkProviderPrivateKeyToAvatarByUsername")]
        //public OASISHttpResponseMessage<bool> LinkProviderPrivateKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        //{
        //    bool isValid;
        //    string errorMessage = "";
        //    ProviderType providerTypeToLinkTo;
        //    ProviderType providerTypeToLoadAvatarFrom;
        //    Guid avatarID;

        //    (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderPrivateKeyToAvatarParams);

        //    if (isValid)
        //        return KeyManager.LinkProviderPrivateKeyToAvatar(linkProviderPrivateKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
        //    else
        //        return new OASISHttpResponseMessage<bool>(false) { IsError = true, Message = errorMessage };
        //}

        ///*
        ///// <summary>
        /////     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        ///// </summary>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GenerateKeyPairAndLinkProviderKeysToAvatar")]
        //public OASISHttpResponseMessage<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatar(Guid avatarId, string providerTypeToLinkTo, string providerTypeToloadAvatarFrom)
        //{
        //    object providerTypeToLinkToObject = null;
        //    object providerTypeToLoadAvatarFromObject = null;
        //    ProviderType providerTypeToLinkToEnumValue = ProviderType.Default;
        //    ProviderType providerTypeToloadAvatarFromEnumValue = ProviderType.Default;

        //    if (string.IsNullOrEmpty(providerTypeToLinkTo))
        //        return (new OASISHttpResponseMessage<KeyPair> { IsError = true, Message = $"The providerTypeToLinkTo param cannot be null. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

        //    if (!string.IsNullOrEmpty(providerTypeToLinkTo) && !Enum.TryParse(typeof(ProviderType), providerTypeToLinkTo, out providerTypeToLinkToObject))
        //        return (new OASISHttpResponseMessage<KeyPair> { IsError = true, Message = $"The given providerTypeToLinkTo {providerTypeToLinkTo} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

        //    if (!string.IsNullOrEmpty(providerTypeToloadAvatarFrom) && !Enum.TryParse(typeof(ProviderType), providerTypeToloadAvatarFrom, out providerTypeToLoadAvatarFromObject))
        //        return (new OASISHttpResponseMessage<KeyPair> { IsError = true, Message = $"The given providerTypeToloadAvatarFrom {providerTypeToloadAvatarFrom} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

        //    if (providerTypeToLinkToObject != null)
        //        providerTypeToLinkToEnumValue = (ProviderType)providerTypeToLinkToObject;

        //    if (providerTypeToLoadAvatarFromObject != null)
        //        providerTypeToloadAvatarFromEnumValue = (ProviderType)providerTypeToLoadAvatarFromObject;

        //    return KeyManager.GenerateKeyPairAndLinkProviderKeysToAvatar(avatarId, providerTypeToLinkToEnumValue, providerTypeToloadAvatarFromEnumValue);
        //}*/


        ///// <summary>
        /////     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        ///// </summary>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId")]
        //public OASISHttpResponseMessage<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        //{
        //    bool isValid;
        //    string errorMessage = "";
        //    ProviderType providerTypeToLinkTo;
        //    ProviderType providerTypeToLoadAvatarFrom;
        //    Guid avatarID;

        //    (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

        //    if (isValid)
        //        return KeyManager.GenerateKeyPairAndLinkProviderKeysToAvatar(avatarID, providerTypeToLinkTo, providerTypeToLoadAvatarFrom);
        //    else
        //        return new OASISHttpResponseMessage<KeyPair>() { IsError = true, Message = errorMessage };
        //}

        ///// <summary>
        /////     Get's a given avatar's unique storage key for the given provider type.
        ///// </summary>
        ///// <param name="avatarId">The Avatar's avatarId.</param>
        ///// <param name="providerType">The provider type to retreive the unique storage key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderUniqueStorageKeyForAvatar")]
        //public OASISHttpResponseMessage<string> GetProviderUniqueStorageKeyForAvatar(Guid avatarId, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderUniqueStorageKeyForAvatar(avatarId, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's unique storage key for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <param name="providerType">The provider type to retreive the unique storage key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderUniqueStorageKeyForAvatar")]
        //public OASISHttpResponseMessage<string> GetProviderUniqueStorageKeyForAvatar(string username, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderUniqueStorageKeyForAvatar(username, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's private key for the given provider type.
        ///// </summary>
        ///// <param name="avatarId">The Avatar's id.</param>
        ///// <param name="providerType">The provider type to retreive the private key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderPrivateKeyForAvatar")]
        //public OASISHttpResponseMessage<string> GetProviderPrivateKeyForAvatar(Guid avatarId, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPrivateKeyForAvatar(avatarId, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's private key for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <param name="providerType">The provider type to retreive the private key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderPrivateKeyForAvatar")]
        //public OASISHttpResponseMessage<string> GetProviderPrivateKeyForAvatar(string username, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPrivateKeyForAvatar(username, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's public keys for the given provider type.
        ///// </summary>
        ///// <param name="avatarId">The Avatar's id.</param>
        ///// <param name="providerType">The provider type to retreive the public keys for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderPublicKeysForAvatar")]
        //public OASISHttpResponseMessage<List<string>> GetProviderPublicKeysForAvatar(Guid avatarId, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPublicKeysForAvatar(avatarId, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's public keys for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <param name="providerType">The provider type to retreive the public keys for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderPublicKeysForAvatar")]
        //public OASISHttpResponseMessage<List<string>> GetProviderPublicKeysForAvatar(string username, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPublicKeysForAvatar(username, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's public keys for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetAllProviderPublicKeysForAvatar")]
        //public OASISHttpResponseMessage<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatar(string username)
        //{
        //    return KeyManager.GetAllProviderPublicKeysForAvatar(username);
        //}

        ///// <summary>
        /////     Generate's a new unique private/public keypair for a given provider type.
        ///// </summary>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GenerateKeyPairForProvider")]
        //public OASISHttpResponseMessage<KeyPair> GenerateKeyPairForProvider(ProviderType providerType)
        //{
        //    return KeyManager.GenerateKeyPair(providerType);
        //}

        ///// <summary>
        /////     Generate's a new unique private/public keypair.
        ///// </summary>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GenerateKeyPair")]
        //public OASISHttpResponseMessage<KeyPair> GenerateKeyPair(string keyPrefix)
        //{
        //    return KeyManager.GenerateKeyPair(keyPrefix);
        //}








        /*
        /// <summary>
        ///     Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{id:Guid}/{telosAccountName}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> LinkTelosAccountToAvatar(Guid id, string telosAccountName)
        {
            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "AvatarService.LinkProviderKeyToAvatar not yet migrated to AvatarManager" });
        }

        /// <summary>
        ///     Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> LinkTelosAccountToAvatar2(
            LinkProviderKeyToAvatar linkProviderKeyToAvatar)
        {
            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "AvatarService.LinkProviderKeyToAvatar not yet migrated to AvatarManager" });
        }


        /// <summary>
        ///     Link's a given eosioAccountName to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosioAccountName}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> LinkEOSIOAccountToAvatar(Guid avatarId, string eosioAccountName)
        {
            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "AvatarService.LinkProviderKeyToAvatar not yet migrated to AvatarManager" });
        }

        /// <summary>
        ///     Link's a given holochain AgentID to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="holochainAgentID"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{holochainAgentID}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> LinkHolochainAgentIDToAvatar(Guid avatarId,
            string holochainAgentID)
        {
            // TODO: Replace with AvatarManager equivalent
            return HttpResponseHelper.FormatResponse(new OASISResult<bool> { IsError = true, Message = "AvatarService.LinkProviderKeyToAvatar not yet migrated to AvatarManager" });
        }*/

        ///// <summary>
        /////     Get's the provider key for the given avatar and provider type.
        ///// </summary>
        ///// <param name="avatarUsername">The avatar username.</param>
        ///// <param name="providerType">The provider type.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("{avatarUsername}/{providerType}")]
        //public async Task<OASISHttpResponseMessage<string>> GetProviderKeyForAvatar(string avatarUsername, ProviderType providerType)
        //{
        //    //return await _avatarService.GetProviderKeyForAvatar(avatarUsername, providerType);
        //    return await Program.AvatarManager.GetProviderKeyForAvatar(avatarUsername, providerType);
        //}

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

        //private string GetProviderList(string listName, string providerList)
        //{
        //    OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.GetProvidersFromList(listName, providerList);

        //    if (listResult.WarningCount > 0)
        //        return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Message = listResult.Message }, HttpStatusCode.BadRequest);
        //    else
        //        currentAutoFailOverList = ProviderManager.GetProviderListAsString(listResult.Result.ToList());
        //}

        #region Session Management - OASIS SSO System 🚀

        /// <summary>
        /// Get all active sessions for a specific avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>List of active sessions</returns>
        [HttpGet("{avatarId}/sessions")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionManagement>> GetAvatarSessions(Guid avatarId)
        {
            try
            {
                var result = await AvatarManager.GetAvatarSessionsAsync(avatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionManagement>
                {
                    IsError = true,
                    Message = $"Error retrieving avatar sessions: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Logout avatar from specific sessions (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionIds">List of session IDs to logout</param>
        /// <returns>Success status</returns>
        [HttpPost("{avatarId}/sessions/logout")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<bool>> LogoutAvatarSessions(Guid avatarId, [FromBody] List<string> sessionIds)
        {
            try
            {
                var result = await AvatarManager.LogoutAvatarSessionsAsync(avatarId, sessionIds);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error logging out avatar sessions: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Logout avatar from all sessions (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{avatarId}/sessions/logout-all")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<bool>> LogoutAllAvatarSessions(Guid avatarId)
        {
            try
            {
                var result = await AvatarManager.LogoutAllAvatarSessionsAsync(avatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error logging out all avatar sessions: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Create a new session for an avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionData">Session information</param>
        /// <returns>Created session</returns>
        [HttpPost("{avatarId}/sessions")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>> CreateAvatarSession(Guid avatarId, [FromBody] NextGenSoftware.OASIS.API.Core.Objects.Avatar.CreateSessionRequest sessionData)
        {
            try
            {
                var result = await AvatarManager.CreateAvatarSessionAsync(avatarId, sessionData);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>
                {
                    IsError = true,
                    Message = $"Error creating avatar session: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Update an existing session (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="sessionData">Updated session information</param>
        /// <returns>Updated session</returns>
        [HttpPut("{avatarId}/sessions/{sessionId}")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>> UpdateAvatarSession(Guid avatarId, string sessionId, [FromBody] NextGenSoftware.OASIS.API.Core.Objects.Avatar.UpdateSessionRequest sessionData)
        {
            try
            {
                var result = await AvatarManager.UpdateAvatarSessionAsync(avatarId, sessionId, sessionData);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>
                {
                    IsError = true,
                    Message = $"Error updating avatar session: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Get session statistics for an avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Session statistics</returns>
        [HttpGet("{avatarId}/sessions/stats")]
        [Authorize]
        public async Task<OASISHttpResponseMessage<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionStats>> GetAvatarSessionStats(Guid avatarId)
        {
            try
            {
                var result = await AvatarManager.GetAvatarSessionStatsAsync(avatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionStats>
                {
                    IsError = true,
                    Message = $"Error retrieving avatar session stats: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #region Avatar Inventory Management

        /// <summary>
        /// Gets all inventory items owned by the authenticated avatar.
        /// This is the avatar's actual inventory (items they own), not items they created.
        /// Inventory is shared across all games, apps, websites, and services.
        /// </summary>
        [HttpGet("inventory")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IEnumerable<IInventoryItem>>> GetAvatarInventory()
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.GetAvatarInventoryAsync(AvatarId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                {
                    IsError = true,
                    Message = $"Error loading avatar inventory: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Adds an item to the avatar's inventory.
        /// The item can be from the STARNET store (created by anyone) or a new item.
        /// Accepts InventoryItem object directly (ASP.NET Core automatically deserializes JSON).
        /// Matches AvatarManager.AddItemToAvatarInventoryAsync signature.
        /// </summary>
        [HttpPost("inventory")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IInventoryItem>> AddItemToAvatarInventory([FromBody] InventoryItem inventoryItem)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                if (inventoryItem == null)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem>
                    {
                        IsError = true,
                        Message = "The request body is required. Please provide a valid Inventory Item object with Name, Description, and optional HolonSubType."
                    }, HttpStatusCode.BadRequest);
                }

                // Ensure HolonType is set if not provided
                if (inventoryItem.HolonType == HolonType.None)
                {
                    inventoryItem.HolonType = HolonType.InventoryItem;
                }

                var result = await AvatarManager.AddItemToAvatarInventoryAsync(AvatarId, inventoryItem);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem>
                {
                    IsError = true,
                    Message = $"Error adding item to inventory: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Removes an item from the avatar's inventory.
        /// </summary>
        [HttpDelete("inventory/{itemId}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> RemoveItemFromAvatarInventory(Guid itemId)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.RemoveItemFromAvatarInventoryAsync(AvatarId, itemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error removing item from inventory: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Checks if the avatar has a specific item in their inventory.
        /// </summary>
        [HttpGet("inventory/{itemId}/has")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> AvatarHasItem(Guid itemId)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.AvatarHasItemAsync(AvatarId, itemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error checking if avatar has item: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Checks if the avatar has a specific item by name in their inventory.
        /// </summary>
        [HttpGet("inventory/has-by-name")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> AvatarHasItemByName([FromQuery] string itemName)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.AvatarHasItemByNameAsync(AvatarId, itemName);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error checking if avatar has item by name: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Searches the avatar's inventory by name or description.
        /// </summary>
        [HttpGet("inventory/search")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IEnumerable<IInventoryItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IEnumerable<IInventoryItem>>> SearchAvatarInventory([FromQuery] string searchTerm)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.SearchAvatarInventoryAsync(AvatarId, searchTerm);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IEnumerable<IInventoryItem>>
                {
                    IsError = true,
                    Message = $"Error searching inventory: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Gets a specific item from the avatar's inventory by ID.
        /// </summary>
        [HttpGet("inventory/{itemId}")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IInventoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IInventoryItem>> GetAvatarInventoryItem(Guid itemId)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }

                var result = await AvatarManager.GetAvatarInventoryItemAsync(AvatarId, itemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<IInventoryItem>
                {
                    IsError = true,
                    Message = $"Error getting inventory item: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Sends an item from the authenticated avatar's inventory to another avatar.
        /// Target is the recipient's username or avatar Id. Works for all items (STAR and local).
        /// </summary>
        [HttpPost("inventory/send-to-avatar")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> SendItemToAvatar([FromBody] SendItemRequest request)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "AvatarId is required. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }
                if (request == null || string.IsNullOrWhiteSpace(request.Target) || string.IsNullOrWhiteSpace(request.ItemName))
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Target and ItemName are required."
                    }, HttpStatusCode.BadRequest);
                }
                var quantity = request.Quantity < 1 ? 1 : request.Quantity;
                var result = await AvatarManager.SendItemToAvatarAsync(AvatarId, request.Target.Trim(), request.ItemName.Trim(), quantity, request.ItemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error sending item to avatar: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Sends an item from the authenticated avatar's inventory to a clan.
        /// Target is the clan name (or username when clan resolution is not yet implemented). Works for all items (STAR and local).
        /// </summary>
        [HttpPost("inventory/send-to-clan")]
        [Authorize]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<bool>> SendItemToClan([FromBody] SendItemRequest request)
        {
            try
            {
                if (AvatarId == Guid.Empty)
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "AvatarId is required. Please authenticate or provide X-Avatar-Id header."
                    }, HttpStatusCode.BadRequest);
                }
                if (request == null || string.IsNullOrWhiteSpace(request.Target) || string.IsNullOrWhiteSpace(request.ItemName))
                {
                    return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Target (clan name) and ItemName are required."
                    }, HttpStatusCode.BadRequest);
                }
                var quantity = request.Quantity < 1 ? 1 : request.Quantity;
                var result = await AvatarManager.SendItemToClanAsync(AvatarId, request.Target.Trim(), request.ItemName.Trim(), quantity, request.ItemId);
                return HttpResponseHelper.FormatResponse(result);
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error sending item to clan: {ex.Message}",
                    Exception = ex
                }, HttpStatusCode.InternalServerError);
            }
        }

        #endregion
    }
}