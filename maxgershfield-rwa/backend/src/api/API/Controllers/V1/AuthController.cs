namespace API.Controllers.V1;

using Application.Contracts;
using Application.DTOs.Account.OASIS;
using Application.DTOs.Account.Requests;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for OASIS Avatar API authentication
/// Handles user registration, login, and profile management using OASIS Avatar API
/// </summary>
[Route($"{ApiAddresses.Base}/auth")]
[ApiController]
public sealed class AuthController(IOASISAuthService oasisAuthService) : V1BaseController
{
    /// <summary>
    /// Register a new user via OASIS Avatar API
    /// </summary>
    /// <param name="request">Registration request containing username, email, and password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Auth response with JWT token and avatar information</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = new { message = GetErrorMessage } });
        }

        var result = await oasisAuthService.RegisterAsync(request);
        
        if (result.IsError)
        {
            return BadRequest(new { error = new { message = result.Message ?? "Registration failed" } });
        }

        if (result.Result == null)
        {
            return StatusCode(500, new { error = new { message = "Registration succeeded but no data returned" } });
        }

        // Return OASIS JWT token directly
        return Ok(new
        {
            token = result.Result.JwtToken,
            avatar = new
            {
                avatarId = result.Result.AvatarId,
                username = result.Result.Username,
                email = result.Result.Email,
                firstName = result.Result.FirstName,
                lastName = result.Result.LastName,
                fullName = result.Result.FullName
            },
            user = new
            {
                username = result.Result.Username,
                email = result.Result.Email,
                firstName = result.Result.FirstName,
                lastName = result.Result.LastName
            }
        });
    }

    /// <summary>
    /// Login user via OASIS Avatar API
    /// </summary>
    /// <param name="request">Login request containing email/username and password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Auth response with JWT token and avatar information</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = new { message = GetErrorMessage } });
        }

        var result = await oasisAuthService.LoginAsync(request);
        
        if (result.IsError)
        {
            return Unauthorized(new { error = new { message = result.Message ?? "Invalid email or password" } });
        }

        if (result.Result == null)
        {
            return StatusCode(500, new { error = new { message = "Login succeeded but no data returned" } });
        }

        // Return OASIS JWT token directly
        return Ok(new
        {
            token = result.Result.JwtToken,
            avatar = new
            {
                avatarId = result.Result.AvatarId,
                username = result.Result.Username,
                email = result.Result.Email,
                firstName = result.Result.FirstName,
                lastName = result.Result.LastName,
                fullName = result.Result.FullName
            },
            user = new
            {
                username = result.Result.Username,
                email = result.Result.Email,
                firstName = result.Result.FirstName,
                lastName = result.Result.LastName
            }
        });
    }

    /// <summary>
    /// Logout current user (client-side token removal)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Logout is handled client-side by removing token
        return Ok(new { success = true });
    }

    /// <summary>
    /// Get current authenticated user profile and avatar information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user profile with avatar and wallet information</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetMeAsync(CancellationToken cancellationToken)
    {
        // Extract avatar ID from JWT token claims
        string? avatarId = User.FindFirst("AvatarId")?.Value ?? 
                          User.FindFirst("avatarId")?.Value ??
                          User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(avatarId))
        {
            return Unauthorized(new { error = new { message = "Avatar ID not found in token" } });
        }

        var avatarResult = await oasisAuthService.GetAvatarAsync(avatarId);
        
        if (avatarResult.IsError || avatarResult.Result == null)
        {
            return NotFound(new { error = new { message = avatarResult.Message ?? "Avatar not found" } });
        }

        var avatar = avatarResult.Result;
        
        return Ok(new
        {
            avatar = new
            {
                avatarId = avatar.AvatarId,
                username = avatar.Username,
                email = avatar.Email,
                firstName = avatar.FirstName,
                lastName = avatar.LastName,
                fullName = avatar.FullName,
                wallets = avatar.Wallets
            },
            user = new
            {
                username = avatar.Username,
                email = avatar.Email,
                firstName = avatar.FirstName,
                lastName = avatar.LastName
            },
            wallets = avatar.Wallets ?? new List<WalletInfo>()
        });
    }

    /// <summary>
    /// Link a wallet address to the current user's OASIS Avatar
    /// </summary>
    /// <param name="request">Wallet linking request containing wallet address and network</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response with wallet information</returns>
    [HttpPost("link-wallet")]
    public async Task<IActionResult> LinkWalletAsync(
        [FromBody] LinkWalletRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = new { message = GetErrorMessage } });
        }

        // Extract avatar ID from JWT token claims
        string? avatarId = User.FindFirst("AvatarId")?.Value ?? 
                          User.FindFirst("avatarId")?.Value ??
                          User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(avatarId))
        {
            return Unauthorized(new { error = new { message = "Avatar ID not found in token" } });
        }

        var result = await oasisAuthService.LinkWalletAsync(avatarId, request.WalletAddress, request.Network);
        
        if (result.IsError)
        {
            return BadRequest(new { error = new { message = result.Message ?? "Failed to link wallet" } });
        }

        return Ok(new
        {
            success = true,
            wallet = new
            {
                address = request.WalletAddress,
                network = request.Network
            }
        });
    }
}



