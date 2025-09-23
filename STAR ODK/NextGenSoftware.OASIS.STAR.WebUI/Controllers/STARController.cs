using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.STAR.WebUI.Services;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.WebUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class STARController : ControllerBase
    {
        private readonly ISTARService _starService;

        public STARController(ISTARService starService)
        {
            _starService = starService;
        }

        [HttpPost("ignite")]
        public async Task<ActionResult<OASISResult<IOmiverse>>> IgniteSTAR()
        {
            var result = await _starService.IgniteSTARAsync();
            return Ok(result);
        }

        [HttpPost("extinguish")]
        public async Task<ActionResult<OASISResult<bool>>> ExtinguishStar()
        {
            var result = await _starService.ExtinguishStarAsync();
            return Ok(result);
        }

        [HttpGet("status")]
        public async Task<ActionResult<OASISResult<bool>>> GetSTARStatus()
        {
            var result = await _starService.IsSTARIgnitedAsync();
            return Ok(result);
        }

        [HttpGet("avatar/current")]
        public async Task<ActionResult<OASISResult<IAvatar>>> GetBeamedInAvatar()
        {
            var result = await _starService.GetBeamedInAvatarAsync();
            return Ok(result);
        }

        [HttpPost("avatar/beam-in")]
        public async Task<ActionResult<OASISResult<IAvatar>>> BeamInAvatar()
        {
            var result = await _starService.BeamInAvatarAsync();
            return Ok(result);
        }

        [HttpPost("avatar/create")]
        public async Task<ActionResult<OASISResult<IAvatar>>> CreateAvatar([FromBody] CreateAvatarRequest request)
        {
            var result = await _starService.CreateAvatarAsync(request.Username, request.Email, request.Password);
            return Ok(result);
        }

        [HttpGet("avatar/{id}")]
        public async Task<ActionResult<OASISResult<IAvatar>>> GetAvatar(Guid id)
        {
            var result = await _starService.LoadAvatarAsync(id);
            return Ok(result);
        }

        [HttpGet("avatar/username/{username}")]
        public async Task<ActionResult<OASISResult<IAvatar>>> GetAvatarByUsername(string username)
        {
            var result = await _starService.LoadAvatarAsync(username);
            return Ok(result);
        }

        [HttpPost("avatar/login")]
        public async Task<ActionResult<OASISResult<IAvatar>>> LoginAvatar([FromBody] LoginAvatarRequest request)
        {
            var result = await _starService.LoadAvatarAsync(request.Username, request.Password);
            return Ok(result);
        }

        [HttpPut("avatar")]
        public async Task<ActionResult<OASISResult<IAvatar>>> SaveAvatar([FromBody] IAvatar avatar)
        {
            var result = await _starService.SaveAvatarAsync(avatar);
            return Ok(result);
        }

        [HttpDelete("avatar/{id}")]
        public async Task<ActionResult<OASISResult<bool>>> DeleteAvatar(Guid id)
        {
            var result = await _starService.DeleteAvatarAsync(id);
            return Ok(result);
        }

        [HttpGet("avatars")]
        public async Task<ActionResult<OASISResult<List<IAvatar>>>> GetAllAvatars()
        {
            var result = await _starService.LoadAllAvatarsAsync();
            return Ok(result);
        }

        [HttpGet("avatars/search")]
        public async Task<ActionResult<OASISResult<List<IAvatar>>>> SearchAvatars([FromQuery] string searchTerm)
        {
            var result = await _starService.SearchAvatarsAsync(searchTerm);
            return Ok(result);
        }

        [HttpGet("karma/{avatarId}")]
        public async Task<ActionResult<OASISResult<IKarmaAkashicRecord>>> GetKarma(Guid avatarId)
        {
            var result = await _starService.GetKarmaAsync(avatarId);
            return Ok(result);
        }

        [HttpPost("karma/{avatarId}/add")]
        public async Task<ActionResult<OASISResult<IKarmaAkashicRecord>>> AddKarma(Guid avatarId, [FromBody] int karma)
        {
            var result = await _starService.AddKarmaAsync(avatarId, karma);
            return Ok(result);
        }

        [HttpPost("karma/{avatarId}/remove")]
        public async Task<ActionResult<OASISResult<IKarmaAkashicRecord>>> RemoveKarma(Guid avatarId, [FromBody] int karma)
        {
            var result = await _starService.RemoveKarmaAsync(avatarId, karma);
            return Ok(result);
        }

        [HttpPost("karma/{avatarId}/set")]
        public async Task<ActionResult<OASISResult<IKarmaAkashicRecord>>> SetKarma(Guid avatarId, [FromBody] int karma)
        {
            var result = await _starService.SetKarmaAsync(avatarId, karma);
            return Ok(result);
        }

        [HttpGet("karma")]
        public async Task<ActionResult<OASISResult<List<IKarmaAkashicRecord>>>> GetAllKarma()
        {
            var result = await _starService.GetAllKarmaAsync();
            return Ok(result);
        }

        [HttpGet("karma/between")]
        public async Task<ActionResult<OASISResult<List<IKarmaAkashicRecord>>>> GetKarmaBetween([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            var result = await _starService.GetKarmaBetweenAsync(fromDate, toDate);
            return Ok(result);
        }

        [HttpGet("karma/above/{karmaLevel}")]
        public async Task<ActionResult<OASISResult<List<IKarmaAkashicRecord>>>> GetKarmaAbove(int karmaLevel)
        {
            var result = await _starService.GetKarmaAboveAsync(karmaLevel);
            return Ok(result);
        }

        [HttpGet("karma/below/{karmaLevel}")]
        public async Task<ActionResult<OASISResult<List<IKarmaAkashicRecord>>>> GetKarmaBelow(int karmaLevel)
        {
            var result = await _starService.GetKarmaBelowAsync(karmaLevel);
            return Ok(result);
        }
    }

    public class CreateAvatarRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginAvatarRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
