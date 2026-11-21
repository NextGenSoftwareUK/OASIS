using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class DataController
    {
        public sealed record HolonCopyRequest(Guid HolonId, string SourceProvider, string DestinationProvider);

        /// <summary>Lists registered storage providers for an explicit copy.</summary>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("copy-holon/providers")]
        public OASISResult<string[]> GetHolonCopyProviders() =>
            new(ProviderManager.Instance.GetStorageProviders()
                .Select(provider => provider.ProviderType.Value.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());

        /// <summary>Copies a holon graph between two explicitly selected storage providers.</summary>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("copy-holon")]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status200OK)]
        public async Task<ActionResult<OASISResult<Guid>>> CopyHolon([FromBody] HolonCopyRequest request)
        {
            if (request == null || request.HolonId == Guid.Empty ||
                !TryReadCopyProvider(request.SourceProvider, out var source) ||
                !TryReadCopyProvider(request.DestinationProvider, out var destination) ||
                source == destination)
            {
                return BadRequest(new OASISResult<Guid>
                {
                    IsError = true,
                    Message = "Choose a holon ID and two distinct storage providers."
                });
            }

            var copied = await HolonManager.SendHolonAsync<Holon>(request.HolonId, AvatarId, source, destination);
            if (copied.IsError || !copied.IsSaved || copied.Result?.Id != request.HolonId)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new OASISResult<Guid>
                {
                    IsError = true,
                    ErrorCode = copied.ErrorCode,
                    Message = "The requested provider-to-provider copy was not confirmed."
                });
            }

            return Ok(new OASISResult<Guid>(copied.Result.Id) { IsSaved = true, SavedCount = 1 });
        }

        private static bool TryReadCopyProvider(string name, out ProviderType type) =>
            Enum.TryParse(name, ignoreCase: true, out type) &&
            type is not (ProviderType.None or ProviderType.Default or ProviderType.All) &&
            string.Equals(Enum.GetName(type), name, StringComparison.OrdinalIgnoreCase);
    }
}
