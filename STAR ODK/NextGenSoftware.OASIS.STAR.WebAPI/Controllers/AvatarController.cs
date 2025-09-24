using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvatarController : STARControllerBase
    {
        private readonly HttpClient _httpClient;

        public AvatarController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Authenticate with the WEB4 OASIS API and set the JWT token for future requests
        /// </summary>
        /// <param name="model">Authentication request with username and password</param>
        /// <returns>Authentication result with JWT token</returns>
        [HttpPost("authenticate")]
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
        /// Get the current authenticated avatar
        /// </summary>
        /// <returns>Current avatar information</returns>
        [HttpGet("current")]
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

