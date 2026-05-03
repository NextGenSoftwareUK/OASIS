using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class VideoController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public VideoController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        public VideoController()
        {
            //_settings = OASISSettings.Value;
        }

        /// <summary>
        /// Start's a video call. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("start-video-call")]
        public async Task<OASISResult<string>> StartVideoCall([FromBody] List<Guid> participantIds, [FromQuery] string callName = null)
        {
            if (participantIds == null || participantIds.Count == 0)
                return new OASISResult<string> { IsError = true, Message = "The request body is required. Please provide a valid JSON array of participant IDs (at least one)." };
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
    }
}
