using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// Avatar endpoints for authenticating against the WEB4 OASIS API and accessing the current STAR session avatar.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AvatarController : STARControllerBase
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Creates a new instance of <see cref="AvatarController"/>.
        /// </summary>
        /// <param name="httpClient">HTTP client used to call WEB4 OASIS API endpoints.</param>
        public AvatarController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Authenticates with the WEB4 OASIS API and returns a JWT token for subsequent requests.
        /// </summary>
        /// <param name="model">Authentication request containing username and password.</param>
        /// <returns>
        /// 200 OK with <see cref="AuthenticateResponse"/> on success; 400 BadRequest on failure with details.
        /// </returns>
        [HttpPost("authenticate")]
        [ProducesResponseType(typeof(OASISResult<AuthenticateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest model)
        {
            try
            {
                // Call the WEB4 OASIS API authenticate endpoint
                var authenticateRequest = new
                {
                    username = model.Username,
                    password = model.Password
                };

                var json = JsonConvert.SerializeObject(authenticateRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // TODO: Make this configurable - get from appsettings.json
                var response = await _httpClient.PostAsync("https://localhost:5002/api/avatar/authenticate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var authResult = JsonConvert.DeserializeObject<OASISResult<AuthenticateResponse>>(responseContent);
                    
                    if (!authResult.IsError && authResult.Result != null)
                    {
                        // Set the JWT token in the response headers for the client to use
                        Response.Headers["Authorization"] = $"Bearer {authResult.Result.JwtToken}";
                        
                        return Ok(authResult);
                    }
                    else
                    {
                        return BadRequest(authResult);
                    }
                }
                else
                {
                    return BadRequest(new OASISResult<string>
                    {
                        IsError = true,
                        Message = $"Authentication failed with status: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<string>
                {
                    IsError = true,
                    Message = $"Error during authentication: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets the current authenticated avatar from the STAR session context.
        /// </summary>
        /// <returns>
        /// 200 OK with <see cref="IAvatar"/> when authenticated; 401 Unauthorized if no JWT present.
        /// </returns>
        [HttpGet("current")]
        [ProducesResponseType(typeof(OASISResult<IAvatar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentAvatar()
        {
            if (Avatar == null)
            {
                return Unauthorized(new OASISResult<string>
                {
                    IsError = true,
                    Message = "No authenticated avatar found. Please authenticate first."
                });
            }

            return Ok(new OASISResult<IAvatar>
            {
                Result = Avatar,
                IsError = false,
                Message = "Avatar retrieved successfully"
            });
        }
    }

    public class AuthenticateRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AuthenticateResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
        public bool IsBeamedIn { get; set; }
        public DateTime? LastBeamedIn { get; set; }
    }
}

