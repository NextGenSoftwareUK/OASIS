using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

/// <summary>
/// Service for merchant authentication and authorization
/// </summary>
public class MerchantAuthService
{
    private readonly IShipexProRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MerchantAuthService> _logger;
    private readonly AvatarManager _avatarManager;

    public MerchantAuthService(
        IShipexProRepository repository,
        IConfiguration configuration,
        ILogger<MerchantAuthService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _avatarManager = new AvatarManager();
    }

    /// <summary>
    /// Register a new merchant
    /// </summary>
    public async Task<OASISResult<MerchantAuthResponse>> RegisterAsync(MerchantRegistrationRequest request)
    {
        try
        {
            // Check if merchant already exists
            var existingMerchant = await _repository.GetMerchantByEmailAsync(request.Email);
            if (!existingMerchant.IsError && existingMerchant.Result != null)
            {
                return new OASISResult<MerchantAuthResponse>
                {
                    IsError = true,
                    Message = "Merchant with this email already exists"
                };
            }

            // Create or get OASIS Avatar
            var avatarResult = await _avatarManager.LoadAvatarAsync(request.Username, request.Password);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                // Create new avatar if doesn't exist
                avatarResult = await _avatarManager.CreateAvatarAsync(
                    request.Username,
                    request.Email,
                    request.Password,
                    request.FirstName ?? "",
                    request.LastName ?? "");
            }

            if (avatarResult.IsError || avatarResult.Result == null)
            {
                return new OASISResult<MerchantAuthResponse>
                {
                    IsError = true,
                    Message = $"Failed to create OASIS Avatar: {avatarResult.Message}"
                };
            }

            // Generate API key
            var apiKey = GenerateApiKey();
            var apiKeyHash = HashApiKey(apiKey);

            // Create merchant record
            var merchant = new Merchant
            {
                MerchantId = Guid.NewGuid(),
                AvatarId = avatarResult.Result.Id,
                CompanyName = request.CompanyName,
                Email = request.Email,
                Username = request.Username,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                RateLimitTier = request.RateLimitTier,
                ApiKeyHash = apiKeyHash,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var saveResult = await _repository.SaveMerchantAsync(merchant);
            if (saveResult.IsError)
            {
                return new OASISResult<MerchantAuthResponse>
                {
                    IsError = true,
                    Message = $"Failed to save merchant: {saveResult.Message}"
                };
            }

            // Generate JWT token
            var jwtToken = GenerateJwtToken(merchant);

            return new OASISResult<MerchantAuthResponse>(new MerchantAuthResponse
            {
                MerchantId = merchant.MerchantId,
                JwtToken = jwtToken,
                ApiKey = apiKey,
                Email = merchant.Email,
                Username = merchant.Username,
                RateLimitTier = merchant.RateLimitTier,
                TokenExpiresAt = DateTime.UtcNow.AddHours(24)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering merchant");
            return new OASISResult<MerchantAuthResponse>
            {
                IsError = true,
                Message = $"Registration failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Login merchant and return JWT token
    /// </summary>
    public async Task<OASISResult<MerchantAuthResponse>> LoginAsync(MerchantLoginRequest request)
    {
        try
        {
            // Authenticate with OASIS Avatar
            var avatarResult = await _avatarManager.LoadAvatarAsync(request.Email, request.Password);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                return new OASISResult<MerchantAuthResponse>
                {
                    IsError = true,
                    Message = "Invalid email or password"
                };
            }

            // Get merchant by email
            var merchantResult = await _repository.GetMerchantByEmailAsync(request.Email);
            if (merchantResult.IsError || merchantResult.Result == null)
            {
                return new OASISResult<MerchantAuthResponse>
                {
                    IsError = true,
                    Message = "Merchant not found"
                };
            }

            var merchant = merchantResult.Result;
            if (!merchant.IsActive)
            {
                return new OASISResult<MerchantAuthResponse>
                {
                    IsError = true,
                    Message = "Merchant account is inactive"
                };
            }

            // Generate JWT token
            var jwtToken = GenerateJwtToken(merchant);

            return new OASISResult<MerchantAuthResponse>(new MerchantAuthResponse
            {
                MerchantId = merchant.MerchantId,
                JwtToken = jwtToken,
                Email = merchant.Email,
                Username = merchant.Username,
                RateLimitTier = merchant.RateLimitTier,
                TokenExpiresAt = DateTime.UtcNow.AddHours(24)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during merchant login");
            return new OASISResult<MerchantAuthResponse>
            {
                IsError = true,
                Message = $"Login failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Generate API key for merchant
    /// </summary>
    public async Task<OASISResult<string>> GenerateApiKeyAsync(Guid merchantId)
    {
        try
        {
            var merchantResult = await _repository.GetMerchantAsync(merchantId);
            if (merchantResult.IsError || merchantResult.Result == null)
            {
                return new OASISResult<string>
                {
                    IsError = true,
                    Message = "Merchant not found"
                };
            }

            var apiKey = GenerateApiKey();
            var apiKeyHash = HashApiKey(apiKey);

            var merchant = merchantResult.Result;
            merchant.ApiKeyHash = apiKeyHash;
            merchant.UpdatedAt = DateTime.UtcNow;

            var saveResult = await _repository.SaveMerchantAsync(merchant);
            if (saveResult.IsError)
            {
                return new OASISResult<string>
                {
                    IsError = true,
                    Message = $"Failed to save API key: {saveResult.Message}"
                };
            }

            return new OASISResult<string>(apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating API key");
            return new OASISResult<string>
            {
                IsError = true,
                Message = $"Failed to generate API key: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validate API key
    /// </summary>
    public async Task<OASISResult<Merchant>> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var apiKeyHash = HashApiKey(apiKey);
            
            // Now using the repository method implemented by Agent A
            var result = await _repository.GetMerchantByApiKeyHashAsync(apiKeyHash);
            
            if (result.IsError || result.Result == null)
            {
                return new OASISResult<Merchant>
                {
                    IsError = true,
                    Message = "Invalid API key"
                };
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return new OASISResult<Merchant>
            {
                IsError = true,
                Message = $"API key validation failed: {ex.Message}"
            };
        }
    }

    private string GenerateJwtToken(Merchant merchant)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? "YourSuperSecretKeyForJWTTokenGenerationThatShouldBeAtLeast32Characters";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, merchant.MerchantId.ToString()),
            new Claim(ClaimTypes.Email, merchant.Email),
            new Claim(ClaimTypes.Name, merchant.Username),
            new Claim("RateLimitTier", merchant.RateLimitTier),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "ShipexPro",
            audience: _configuration["Jwt:Audience"] ?? "ShipexPro",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateApiKey()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hash);
    }
}

