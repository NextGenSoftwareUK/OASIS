using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class VideoController : OASISControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly OASISDNA _oasisDNA;

        public VideoController()
        {
            _httpClient = new HttpClient();
            _oasisDNA = OASISDNAManager.OASISDNA;
        }

        /// <summary>
        /// Start's a video call. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("start-video-call")]
        public async Task<OASISResult<string>> StartVideoCall([FromBody] List<Guid> participantIds, [FromQuery] string callName = null)
        {
            // Use VideoManager for business logic
            return await VideoManager.Instance.StartVideoCallAsync(Avatar.Id, participantIds, VideoCallType.Group, callName);
        }

        /// <summary>
        /// Join an existing video call
        /// </summary>
        /// <param name="callId">Video call session ID</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("join-call/{callId}")]
        public async Task<OASISResult<bool>> JoinVideoCall(string callId, [FromBody] string connectionDetails = null)
        {
            // Use VideoManager for business logic
            return await VideoManager.Instance.JoinVideoCallAsync(callId, Avatar.Id, connectionDetails);
        }

        /// <summary>
        /// End a video call
        /// </summary>
        /// <param name="callId">Video call session ID</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("end-call/{callId}")]
        public async Task<OASISResult<bool>> EndVideoCall(string callId)
        {
            // Use VideoManager for business logic
            return await VideoManager.Instance.EndVideoCallAsync(callId, Avatar.Id);
        }

        /// <summary>
        /// Generate video using LTX.io API (text-to-video or image-to-video)
        /// Wraps the MCP ltx_generate_video tool functionality
        /// </summary>
        /// <param name="request">Video generation request with mode, prompt, and parameters</param>
        /// <returns>Generated video as base64 encoded MP4</returns>
        [Authorize]
        [HttpPost("generate")]
        [ProducesResponseType(typeof(OASISResult<VideoGenerationResponse>), 200)]
        [ProducesResponseType(typeof(OASISResult<string>), 400)]
        public async Task<OASISResult<VideoGenerationResponse>> GenerateVideo([FromBody] VideoGenerationRequest request)
        {
            OASISResult<VideoGenerationResponse> result = new OASISResult<VideoGenerationResponse>();

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Request body is required");
                    return result;
                }

                // Validate mode
                if (request.Mode != "text-to-video" && request.Mode != "image-to-video")
                {
                    OASISErrorHandling.HandleError(ref result, "Mode must be 'text-to-video' or 'image-to-video'");
                    return result;
                }

                // Validate required fields
                if (request.Mode == "text-to-video" && string.IsNullOrWhiteSpace(request.Prompt))
                {
                    OASISErrorHandling.HandleError(ref result, "Prompt is required for text-to-video mode");
                    return result;
                }

                if (request.Mode == "image-to-video" && string.IsNullOrWhiteSpace(request.ImageUrl))
                {
                    OASISErrorHandling.HandleError(ref result, "ImageUrl is required for image-to-video mode");
                    return result;
                }

                // Get LTX API configuration from OASIS_DNA.json
                string ltxApiToken = null;
                string ltxApiUrl = "https://api.ltx.video/v1";

                // Try to get from OASIS_DNA.json (similar to OpenAI config)
                if (_oasisDNA?.OASIS?.AI?.LTX != null)
                {
                    ltxApiToken = _oasisDNA.OASIS.AI.LTX.ApiToken;
                    if (!string.IsNullOrEmpty(_oasisDNA.OASIS.AI.LTX.ApiUrl))
                        ltxApiUrl = _oasisDNA.OASIS.AI.LTX.ApiUrl;
                }

                // Fallback to environment variable if not in DNA
                if (string.IsNullOrEmpty(ltxApiToken))
                {
                    ltxApiToken = Environment.GetEnvironmentVariable("LTX_API_TOKEN");
                }

                if (string.IsNullOrEmpty(ltxApiToken))
                {
                    OASISErrorHandling.HandleError(ref result, "LTX API token not configured. Please set LTX_API_TOKEN environment variable or configure in OASIS_DNA.json");
                    return result;
                }

                // Prepare LTX API request
                var ltxRequest = new
                {
                    prompt = request.Prompt,
                    model = request.Model ?? "ltx-2-fast",
                    duration = request.Duration ?? 5,
                    resolution = request.Resolution ?? "1920x1080",
                    aspect_ratio = request.AspectRatio ?? "16:9",
                    fps = request.Fps ?? 24
                };

                // Add image URL for image-to-video
                JObject ltxRequestJson = JObject.FromObject(ltxRequest);
                if (request.Mode == "image-to-video" && !string.IsNullOrEmpty(request.ImageUrl))
                {
                    ltxRequestJson["image_uri"] = request.ImageUrl;
                }

                // Determine endpoint
                string endpoint = request.Mode == "image-to-video"
                    ? $"{ltxApiUrl}/image-to-video"
                    : $"{ltxApiUrl}/text-to-video";

                // Create HTTP request
                var requestContent = new StringContent(
                    ltxRequestJson.ToString(),
                    Encoding.UTF8,
                    "application/json"
                );

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ltxApiToken}");

                // Set longer timeout for video generation (can take several minutes)
                _httpClient.Timeout = TimeSpan.FromMinutes(10);

                var response = await _httpClient.PostAsync(endpoint, requestContent);
                var responseBytes = await response.Content.ReadAsByteArrayAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = Encoding.UTF8.GetString(responseBytes);
                    OASISErrorHandling.HandleError(ref result, $"LTX API error ({response.StatusCode}): {errorContent}");
                    return result;
                }

                // Convert video bytes to base64
                string videoBase64 = Convert.ToBase64String(responseBytes);

                result.Result = new VideoGenerationResponse
                {
                    VideoBase64 = videoBase64,
                    VideoUrl = null, // Could optionally upload to IPFS here
                    Mode = request.Mode,
                    Duration = request.Duration ?? 5,
                    Resolution = request.Resolution ?? "1920x1080"
                };

                result.Message = "Video generated successfully";
            }
            catch (TaskCanceledException ex)
            {
                OASISErrorHandling.HandleError(ref result, "Video generation timed out. This can take several minutes. Please try again.", ex);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating video: {ex.Message}", ex);
            }

            return result;
        }
    }

    /// <summary>
    /// Request model for video generation
    /// </summary>
    public class VideoGenerationRequest
    {
        public string Mode { get; set; } // "text-to-video" or "image-to-video"
        public string Prompt { get; set; } // Required for text-to-video, optional for image-to-video
        public string ImageUrl { get; set; } // Required for image-to-video
        public string Model { get; set; } // "ltx-2-fast" or "ltx-2-pro"
        public int? Duration { get; set; } // 1-20 seconds
        public string Resolution { get; set; } // e.g., "1920x1080"
        public string AspectRatio { get; set; } // e.g., "16:9"
        public int? Fps { get; set; } // frames per second
    }

    /// <summary>
    /// Response model for video generation
    /// </summary>
    public class VideoGenerationResponse
    {
        public string VideoBase64 { get; set; }
        public string VideoUrl { get; set; } // Optional: IPFS URL if uploaded
        public string Mode { get; set; }
        public int Duration { get; set; }
        public string Resolution { get; set; }
    }
}
