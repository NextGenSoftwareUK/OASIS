using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.CLI.Engine;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager
    {
        public OASISResult<IAvatar> Authenticate(string username, string password, string ipAddress)
        {
            //They can log in with either username, email or a public key linked to the avatar.
            OASISResult<IAvatar> result = null;

            try
            {
                //Temp supress logging to the console in case STAR CLI is creating a new avatar...
                //CLIEngine.SupressConsoleLogging = true;

                //Temp disable the OASIS HyperDrive so it returns fast and does not attempt to find the avatar across all providers! ;-)
                //TODO: May want to fine tune how we handle this in future?
                //bool isAutoFailOverEnabled = ProviderManager.Instance.IsAutoFailOverEnabled;
                //ProviderManager.Instance.IsAutoFailOverEnabled = false;

                List<EnumValue<ProviderType>> currentProviderFailOverList = ProviderManager.Instance.GetProviderAutoFailOverList();
                ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(ProviderManager.Instance.GetProviderAutoFailOverListForAvatarLogin());

                //First try by username...
                result = LoadAvatar(username, false, false);

                if (result.Result == null)
                {
                    //Now try by email...
                    result = LoadAvatarByEmail(username, false, false);

                    if (result.Result == null)
                    {
                        //Finally by Public Key...
                        OASISResult<IAvatar> publicKeyResult = LoadAvatarByPublicKeyForProvider(username);
                        if (!publicKeyResult.IsError && publicKeyResult.Result != null)
                            result.Result = publicKeyResult.Result;
                    }
                }

                if (result.Result == null)
                    result.Message = $"This avatar does not exist. Please contact support or create a new avatar.";
                else
                {
                    result = ProcessAvatarLogin(result, password);

                    //TODO: Come back to this.
                    //if (OASISDNA.OASIS.Security.AvatarPassword.)

                    if (result.Result != null & !result.IsError)
                    {
                        var jwtToken = GenerateJWTToken(result.Result);
                        var refreshToken = generateRefreshToken(ipAddress);

                        if (result.Result.RefreshTokens == null)
                            result.Result.RefreshTokens = new List<RefreshToken>();

                        result.Result.RefreshTokens.Add(refreshToken);
                        result.Result.JwtToken = jwtToken;
                        result.Result.RefreshToken = refreshToken.Token;
                        result.Result.LastBeamedIn = DateTime.Now;
                        result.Result.IsBeamedIn = true;

                        LoggedInAvatar = result.Result;
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(result.Result);

                        if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
                        {
                            result.Result = HideAuthDetails(saveAvatarResult.Result, false, true, false, false);
                            result.IsSaved = true;
                            result.Message = "Avatar Successfully Authenticated.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured in Authenticate method in AvatarManager whilst saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    }
                    else
                        result.Result = null;
                }

                ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(currentProviderFailOverList);
                //ProviderManager.Instance.IsAutoFailOverEnabled = isAutoFailOverEnabled;
                //CLIEngine.SupressConsoleLogging = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in Authenticate method in AvatarManager. Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public delegate OASISResult<IAvatar> SaveAvatarFunction(IAvatar avatar);

        public async Task<OASISResult<IAvatar>> AuthenticateAsync(string username, string password, string ipAddress, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false)
        {
            //They can log in with either username, email or a public key linked to the avatar.
            OASISResult<IAvatar> result = null;

            try
            {
                //Temp supress logging to the console in case STAR CLI is creating a new avatar...
                //CLIEngine.SupressConsoleLogging = true;

                //Temp disable the OASIS HyperDrive so it returns fast and does not attempt to find the avatar across all providers! ;-)
                //TODO: May want to fine tune how we handle this in future?
                //bool isAutoFailOverEnabled = ProviderManager.Instance.IsAutoFailOverEnabled;
                //ProviderManager.Instance.IsAutoFailOverEnabled = false;

                List<EnumValue<ProviderType>> currentProviderFailOverList = ProviderManager.Instance.GetProviderAutoFailOverList();
                ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(ProviderManager.Instance.GetProviderAutoFailOverListForAvatarLogin());

                //First try by username...
                result = await LoadAvatarAsync(username, false, false);

                if (result.Result == null)
                {
                    //Now try by email...
                    result = await LoadAvatarByEmailAsync(username, false, false);

                    if (result.Result == null)
                    {
                        //Finally by Public Key...
                        OASISResult<IAvatar> publicKeyResult = await LoadAvatarByPublicKeyForProviderAsync(username);
                        if (!publicKeyResult.IsError && publicKeyResult.Result != null)
                            result.Result = publicKeyResult.Result;
                    }
                }

                if (result.Result == null)
                    result.Message = $"This avatar does not exist. Please contact support or create a new avatar.";
                else
                {
                    //ProcessAvatarLogin(result, username, password, ipAddress, (result.Result) => { SaveAvatar(result.Result); }) ;
                    // ProcessAvatarLogin(result, username, password, ipAddress, SaveAvatar);
                    result = ProcessAvatarLogin(result, password);

                    //TODO: Come back to this.
                    //if (OASISDNA.OASIS.Security.AvatarPassword.)

                    if (result.Result != null & !result.IsError)
                    {
                        var jwtToken = GenerateJWTToken(result.Result);
                        var refreshToken = generateRefreshToken(ipAddress);

                        if (result.Result.RefreshTokens == null)
                            result.Result.RefreshTokens = new List<RefreshToken>();

                        result.Result.RefreshTokens.Add(refreshToken);
                        result.Result.JwtToken = jwtToken;
                        result.Result.RefreshToken = refreshToken.Token;
                        result.Result.LastBeamedIn = DateTime.Now;
                        result.Result.IsBeamedIn = true;

                        LoggedInAvatar = result.Result;
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(result.Result, autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult);

                        if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
                        {
                            result.Result = HideAuthDetails(saveAvatarResult.Result, false, true, false, false);
                            result.IsSaved = true;
                            result.Message = "Avatar Successfully Authenticated.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured in AuthenticateAsync method in AvatarManager whilst saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    }
                    else
                        result.Result = null;
                }


                ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(currentProviderFailOverList);
                //ProviderManager.Instance.IsAutoFailOverEnabled = isAutoFailOverEnabled;
                //CLIEngine.SupressConsoleLogging = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in AuthenticateAsync method in AvatarManager. Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        /// <summary>
        /// Issues a short-lived server-side nonce for DID challenge-response authentication.
        /// The caller must sign this nonce and submit it to <see cref="AuthenticateWithDIDAsync"/>.
        /// Requires DIDEnabled = true in OASISDNA.Security.
        /// </summary>
        public OASISResult<string> GenerateDIDChallenge(string did)
        {
            OASISResult<string> result = new OASISResult<string>();
            try
            {
                if (!OASISDNA.OASIS.Security.DIDEnabled)
                {
                    OASISErrorHandling.HandleError(ref result, "DID authentication is not enabled in OASISDNA.Security.DIDEnabled.");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(did))
                {
                    OASISErrorHandling.HandleError(ref result, "DID is required.");
                    return result;
                }

                result.Result  = DIDChallengeStore.Issue(did);
                result.Message = $"Challenge issued. Sign SHA-256 of this value with your DID private key and submit to /authenticate-did within {DIDChallengeStore.NonceTtlSeconds} seconds.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating DID challenge: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Authenticates an avatar using W3C DID-based challenge-response (secp256k1 ECDSA).
        /// The client signs SHA-256(challenge) with their DID private key; the server verifies
        /// against the DIDPublicKey stored on the avatar and issues a JWT on success.
        ///
        /// Flow:
        ///   1. Client obtains a short-lived challenge string from a dedicated /challenge endpoint (or passes one).
        ///   2. Client signs SHA-256(challenge) with their secp256k1 DID private key.
        ///   3. Client calls this method with (did, challenge, signatureHex).
        ///   4. Server looks up the avatar by DID, recovers the public key from the signature,
        ///      compares it to DIDPublicKey, and issues a JWT if they match.
        /// </summary>
        public async Task<OASISResult<IAvatar>> AuthenticateWithDIDAsync(string did, string challenge, string signatureHex, string ipAddress)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                if (!OASISDNA.OASIS.Security.DIDEnabled)
                {
                    OASISErrorHandling.HandleError(ref result, "DID authentication is not enabled in OASISDNA.Security.DIDEnabled.");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(did) || string.IsNullOrWhiteSpace(challenge) || string.IsNullOrWhiteSpace(signatureHex))
                {
                    OASISErrorHandling.HandleError(ref result, "DID, challenge and signature are all required.");
                    return result;
                }

                // Load all avatars and find by DID (did:oasis:<id> maps to the avatar Id)
                IAvatar avatar = null;
                if (did.StartsWith("did:oasis:") && Guid.TryParse(did["did:oasis:".Length..], out Guid avatarId))
                {
                    var loadResult = await LoadAvatarAsync(avatarId, false, false);
                    if (!loadResult.IsError && loadResult.Result != null)
                        avatar = loadResult.Result;
                }

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "No avatar found for the provided DID.");
                    return result;
                }

                if (string.IsNullOrEmpty(avatar.DIDPublicKey))
                {
                    OASISErrorHandling.HandleError(ref result, "This avatar does not have a DID public key registered. Please set DIDPublicKey on the avatar first.");
                    return result;
                }

                if (!avatar.IsVerified)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar has not been verified. Please check your email.");
                    return result;
                }

                if (!avatar.IsActive)
                {
                    OASISErrorHandling.HandleError(ref result, "This avatar is no longer active. Please contact support.");
                    return result;
                }

                // Validate the nonce was server-issued and hasn't expired or been replayed
                if (!DIDChallengeStore.ConsumeIfValid(did, challenge))
                {
                    OASISErrorHandling.HandleError(ref result, "DID challenge is invalid, expired, or has already been used. Please request a new challenge.");
                    return result;
                }

                // Verify the ECDsa P-256 signature against the stored public key
                bool signatureValid = VerifyDIDSignature(challenge, signatureHex, avatar.DIDPublicKey);
                if (!signatureValid)
                {
                    OASISErrorHandling.HandleError(ref result, "DID signature verification failed.");
                    return result;
                }

                result.Result = avatar;
                var jwtToken     = GenerateJWTToken(avatar);
                var refreshToken = generateRefreshToken(ipAddress);

                if (avatar.RefreshTokens == null)
                    avatar.RefreshTokens = new List<RefreshToken>();

                avatar.RefreshTokens.Add(refreshToken);
                avatar.JwtToken     = jwtToken;
                avatar.RefreshToken = refreshToken.Token;
                avatar.LastBeamedIn = DateTime.Now;
                avatar.IsBeamedIn   = true;
                LoggedInAvatar      = avatar;

                var saveResult = await SaveAvatarAsync(avatar);
                if (!saveResult.IsError && saveResult.IsSaved)
                {
                    result.Result  = HideAuthDetails(saveResult.Result, false, true, false, false);
                    result.IsSaved = true;
                    result.Message = "Avatar Successfully Authenticated via DID.";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"DID auth succeeded but failed to save avatar state: {saveResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error in AuthenticateWithDIDAsync: {ex.Message}", ex);
                result.Result = null;
            }
            return result;
        }

        /// <summary>
        /// Verifies an ECDsa P-256 signature produced by signing SHA-256(challenge).
        ///
        /// DIDPublicKey format: Base64-encoded SubjectPublicKeyInfo (DER), produced by:
        ///   using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ///   string pubKeyBase64 = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
        ///
        /// Signature format: Base64-encoded IEEE P1363 signature (64 bytes: R[32] || S[32]).
        /// </summary>
        private bool VerifyDIDSignature(string challenge, string signatureBase64, string storedPublicKeyBase64)
        {
            try
            {
                byte[] challengeHash = System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(challenge));

                byte[] sigBytes    = Convert.FromBase64String(signatureBase64);
                byte[] pubKeyBytes = Convert.FromBase64String(storedPublicKeyBase64);

                using var ecdsa = System.Security.Cryptography.ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(pubKeyBytes, out _);
                return ecdsa.VerifyHash(challengeHash, sigBytes);
            }
            catch
            {
                return false;
            }
        }

    }
}
