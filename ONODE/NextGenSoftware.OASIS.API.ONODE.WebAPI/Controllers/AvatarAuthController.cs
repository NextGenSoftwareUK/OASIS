using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    /// <summary>AvatarAuthController endpoints — part of the Avatar API surface at api/avatar.</summary>
    [Route("api/avatar")]
    [ApiController]
    public class AvatarAuthController : OASISControllerBase
    {
        private AvatarManager AvatarManager => Program.AvatarManager;
        private readonly ILogger<AvatarAuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly object StarLogLock = new object();

        public AvatarAuthController(ILogger<AvatarAuthController> logger, IConfiguration configuration, IWebHostEnvironment env)
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

        [HttpPost("register")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IAvatar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IAvatar>> Register(RegisterRequest model)
        {
            bool callerIsWizard = Avatar?.AvatarType.Value == AvatarType.Wizard;
            // Only a Wizard caller may specify a non-User avatar type; all other registrations are forced to User
            AvatarType avatarType = callerIsWizard && model.AvatarType != null
                ? (AvatarType)Enum.Parse(typeof(AvatarType), model.AvatarType)
                : AvatarType.User;

            // Diagnostic: log boot/DNA/provider state into the response so failures are self-explaining
            var diagMessages = new System.Collections.Generic.List<string>();
            diagMessages.Add($"[DIAG] IsOASISBooted={OASISBootLoader.OASISBootLoader.IsOASISBooted}");
            diagMessages.Add($"[DIAG] OASISDNAPath={OASISBootLoader.OASISBootLoader.OASISDNAPath}");
            diagMessages.Add($"[DIAG] DNA file exists: {System.IO.File.Exists(OASISBootLoader.OASISBootLoader.OASISDNAPath)}");
            diagMessages.Add($"[DIAG] OASISDNA loaded: {OASISBootLoader.OASISBootLoader.OASISDNA != null}");
            if (OASISBootLoader.OASISBootLoader.OASISDNA != null)
            {
                var sp = OASISBootLoader.OASISBootLoader.OASISDNA.OASIS?.StorageProviders;
                diagMessages.Add($"[DIAG] AutoFailOverEnabled={sp?.AutoFailOverEnabled} AutoFailOverProviders={sp?.AutoFailOverProviders}");
                var mongoConn = sp?.MongoDBOASIS?.ConnectionString;
                diagMessages.Add($"[DIAG] MongoDB ConnectionString set: {!string.IsNullOrEmpty(mongoConn)}");
            }
            var failOverList = NextGenSoftware.OASIS.API.Core.Managers.ProviderManager.Instance.GetProviderAutoFailOverList();
            diagMessages.Add($"[DIAG] Active failover provider count: {failOverList?.Count ?? 0}");
            if (failOverList != null)
                foreach (var p in failOverList)
                    diagMessages.Add($"[DIAG] Failover provider: {p.Name}");

            var result = await AvatarManager.RegisterAsync(
                model.Title,
                model.FirstName,
                model.LastName,
                model.Email,
                model.Password,
                model.Username,
                avatarType,
                OASISType.OASISAPIREST,
                callerIsWizard: callerIsWizard,
                suppressVerificationEmail: callerIsWizard && model.SuppressVerificationEmail
            );

            result.InnerMessages.InsertRange(0, diagMessages);
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
        /// Generate a short-lived server-side nonce to use as the challenge in DID authentication.
        /// Sign the returned nonce with your DID private key (SHA-256 of the nonce string) and submit
        /// it together with the nonce to POST /authenticate-did within 5 minutes.
        /// Requires DIDEnabled = true in OASISDNA.Security.
        /// </summary>
        /// <param name="did">The avatar's W3C DID (did:oasis:&lt;avatarId&gt;)</param>
        [HttpGet("did-challenge/{did}")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public OASISHttpResponseMessage<string> GetDIDChallenge(string did)
        {
            var result = Program.AvatarManager.GenerateDIDChallenge(did);
            return HttpResponseHelper.FormatResponse(result, result.IsError ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        }

        /// <summary>
        /// Authenticate using a W3C Decentralized Identifier (DID) and secp256k1 challenge-response.
        /// The client signs SHA-256(challenge) with their P-256 DID private key and submits the
        /// 64-byte IEEE P1363 signature (Base64-encoded) together with the challenge string.
        /// The server verifies the signature against the DIDPublicKey stored on the avatar and,
        /// on success, issues a JWT token exactly like the standard authenticate endpoint.
        /// Requires DIDEnabled = true in OASISDNA.Security.
        /// </summary>
        /// <param name="request">DID, challenge string, and hex-encoded signature.</param>
        /// <returns>Avatar with JWT and refresh tokens on success.</returns>
        [HttpPost("authenticate-did")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<IAvatar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISHttpResponseMessage<IAvatar>> AuthenticateWithDID([FromBody] AuthenticateWithDIDRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.DID) ||
                string.IsNullOrWhiteSpace(request.Challenge) || string.IsNullOrWhiteSpace(request.Signature))
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar> { IsError = true, Message = "DID, Challenge and Signature are required." }, HttpStatusCode.BadRequest);

            var result = await Program.AvatarManager.AuthenticateWithDIDAsync(request.DID, request.Challenge, request.Signature, ipAddress());

            if (!result.IsError && result.Result != null)
            {
                setTokenCookie(result.Result.RefreshToken);
                return HttpResponseHelper.FormatResponse(result, HttpStatusCode.OK);
            }

            return HttpResponseHelper.FormatResponse(result, HttpStatusCode.Unauthorized);
        }

        /// <summary>
        ///     Refresh and generate a new JWT Security Token. This will only work if you are already logged in &amp;
        ///     authenticated.
        /// </summary>
        /// <returns></returns>
        [HttpPost("refresh-token")]
        public async Task<OASISHttpResponseMessage<IAvatar>> RefreshToken([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? body = null)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(refreshToken))
                refreshToken = body?.RefreshToken;
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
        public async Task<OASISHttpResponseMessage<IAvatar>> RefreshToken(ProviderType providerType, bool setGlobally = false, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? body = null)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await RefreshToken(body);
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
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.ForgotPasswordAsync(model.Email, returnUrl: model.ReturnUrl));
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

        private void setTokenCookie(string token)
        {
            var refreshDays = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.Security?.RefreshTokenExpirationDays ?? 7;
            if (refreshDays <= 0) refreshDays = 7;
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(refreshDays)
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

    }
}