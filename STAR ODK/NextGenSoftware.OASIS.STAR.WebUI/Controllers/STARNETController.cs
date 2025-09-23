using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.STAR.WebUI.Services;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.WebUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class STARNETController : ControllerBase
    {
        private readonly ISTARNETService _starNetService;

        public STARNETController(ISTARNETService starNetService)
        {
            _starNetService = starNetService;
        }

        #region OAPPs

        [HttpPost("oapps")]
        public async Task<ActionResult<OASISResult<OAPP>>> CreateOAPP([FromBody] CreateSTARNETItemRequest request)
        {
            var result = await _starNetService.CreateOAPPAsync(request.Name, request.Description, request.Type, request.ProviderType);
            return Ok(result);
        }

        [HttpPut("oapps/{id}")]
        public async Task<ActionResult<OASISResult<OAPP>>> EditOAPP(Guid id, [FromBody] EditSTARNETItemRequest request)
        {
            var result = await _starNetService.EditOAPPAsync(id, request.Name, request.Description, request.ProviderType);
            return Ok(result);
        }

        [HttpDelete("oapps/{id}")]
        public async Task<ActionResult<OASISResult<bool>>> DeleteOAPP(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DeleteOAPPAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("oapps/{id}/download-install")]
        public async Task<ActionResult<OASISResult<OAPP>>> DownloadAndInstallOAPP(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DownloadAndInstallOAPPAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("oapps/{id}/uninstall")]
        public async Task<ActionResult<OASISResult<bool>>> UninstallOAPP(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.UninstallOAPPAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("oapps/{id}/publish")]
        public async Task<ActionResult<OASISResult<bool>>> PublishOAPP(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.PublishOAPPAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("oapps/{id}/unpublish")]
        public async Task<ActionResult<OASISResult<bool>>> UnpublishOAPP(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.UnpublishOAPPAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("oapps/{id}/republish")]
        public async Task<ActionResult<OASISResult<bool>>> RepublishOAPP(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.RepublishOAPPAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("oapps/{id}/activate")]
        public async Task<ActionResult<OASISResult<bool>>> ActivateOAPP(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ActivateOAPPAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("oapps/{id}/deactivate")]
        public async Task<ActionResult<OASISResult<bool>>> DeactivateOAPP(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DeactivateOAPPAsync(id, providerType);
            return Ok(result);
        }

        [HttpGet("oapps/{id}")]
        public async Task<ActionResult<OASISResult<OAPP>>> ShowOAPP(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ShowOAPPAsync(id, providerType);
            return Ok(result);
        }

        [HttpGet("oapps")]
        public async Task<ActionResult<OASISResult<List<OAPP>>>> ListAllOAPPs([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllOAPPsAsync(providerType);
            return Ok(result);
        }

        [HttpGet("oapps/created-by-me")]
        public async Task<ActionResult<OASISResult<List<OAPP>>>> ListAllOAPPsCreatedByMe([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllOAPPsCreatedByBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("oapps/installed")]
        public async Task<ActionResult<OASISResult<List<InstalledOAPP>>>> ListAllOAPPsInstalled([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllOAPPsInstalledForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("oapps/uninstalled")]
        public async Task<ActionResult<OASISResult<List<OAPP>>>> ListAllOAPPsUninstalled([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllOAPPsUninstalledForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("oapps/unpublished")]
        public async Task<ActionResult<OASISResult<List<OAPP>>>> ListAllOAPPsUnpublished([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllOAPPsUnpublishedForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("oapps/deactivated")]
        public async Task<ActionResult<OASISResult<List<OAPP>>>> ListAllOAPPsDeactivated([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllOAPPsDeactivatedForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("oapps/search")]
        public async Task<ActionResult<OASISResult<List<OAPP>>>> SearchOAPPs([FromQuery] string searchTerm, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.SearchOAPPsAsync(searchTerm, providerType);
            return Ok(result);
        }

        #endregion

        #region Quests

        [HttpPost("quests")]
        public async Task<ActionResult<OASISResult<Quest>>> CreateQuest([FromBody] CreateSTARNETItemRequest request)
        {
            var result = await _starNetService.CreateQuestAsync(request.Name, request.Description, request.Type, request.ProviderType);
            return Ok(result);
        }

        [HttpPut("quests/{id}")]
        public async Task<ActionResult<OASISResult<Quest>>> EditQuest(Guid id, [FromBody] EditSTARNETItemRequest request)
        {
            var result = await _starNetService.EditQuestAsync(id, request.Name, request.Description, request.ProviderType);
            return Ok(result);
        }

        [HttpDelete("quests/{id}")]
        public async Task<ActionResult<OASISResult<bool>>> DeleteQuest(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DeleteQuestAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("quests/{id}/download-install")]
        public async Task<ActionResult<OASISResult<Quest>>> DownloadAndInstallQuest(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DownloadAndInstallQuestAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("quests/{id}/uninstall")]
        public async Task<ActionResult<OASISResult<bool>>> UninstallQuest(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.UninstallQuestAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("quests/{id}/publish")]
        public async Task<ActionResult<OASISResult<bool>>> PublishQuest(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.PublishQuestAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("quests/{id}/unpublish")]
        public async Task<ActionResult<OASISResult<bool>>> UnpublishQuest(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.UnpublishQuestAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("quests/{id}/republish")]
        public async Task<ActionResult<OASISResult<bool>>> RepublishQuest(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.RepublishQuestAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("quests/{id}/activate")]
        public async Task<ActionResult<OASISResult<bool>>> ActivateQuest(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ActivateQuestAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("quests/{id}/deactivate")]
        public async Task<ActionResult<OASISResult<bool>>> DeactivateQuest(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DeactivateQuestAsync(id, providerType);
            return Ok(result);
        }

        [HttpGet("quests/{id}")]
        public async Task<ActionResult<OASISResult<Quest>>> ShowQuest(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ShowQuestAsync(id, providerType);
            return Ok(result);
        }

        [HttpGet("quests")]
        public async Task<ActionResult<OASISResult<List<Quest>>>> ListAllQuests([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllQuestsAsync(providerType);
            return Ok(result);
        }

        [HttpGet("quests/created-by-me")]
        public async Task<ActionResult<OASISResult<List<Quest>>>> ListAllQuestsCreatedByMe([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllQuestsCreatedByBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("quests/installed")]
        public async Task<ActionResult<OASISResult<List<InstalledQuest>>>> ListAllQuestsInstalled([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllQuestsInstalledForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("quests/uninstalled")]
        public async Task<ActionResult<OASISResult<List<Quest>>>> ListAllQuestsUninstalled([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllQuestsUninstalledForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("quests/unpublished")]
        public async Task<ActionResult<OASISResult<List<Quest>>>> ListAllQuestsUnpublished([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllQuestsUnpublishedForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("quests/deactivated")]
        public async Task<ActionResult<OASISResult<List<Quest>>>> ListAllQuestsDeactivated([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllQuestsDeactivatedForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("quests/search")]
        public async Task<ActionResult<OASISResult<List<Quest>>>> SearchQuests([FromQuery] string searchTerm, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.SearchQuestsAsync(searchTerm, providerType);
            return Ok(result);
        }

        #endregion

        #region NFTs

        [HttpPost("nfts")]
        public async Task<ActionResult<OASISResult<STARNFT>>> CreateNFT([FromBody] CreateSTARNETItemRequest request)
        {
            var result = await _starNetService.CreateNFTAsync(request.Name, request.Description, request.Type, request.ProviderType);
            return Ok(result);
        }

        [HttpPut("nfts/{id}")]
        public async Task<ActionResult<OASISResult<STARNFT>>> EditNFT(Guid id, [FromBody] EditSTARNETItemRequest request)
        {
            var result = await _starNetService.EditNFTAsync(id, request.Name, request.Description, request.ProviderType);
            return Ok(result);
        }

        [HttpDelete("nfts/{id}")]
        public async Task<ActionResult<OASISResult<bool>>> DeleteNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DeleteNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("nfts/{id}/download-install")]
        public async Task<ActionResult<OASISResult<STARNFT>>> DownloadAndInstallNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DownloadAndInstallNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("nfts/{id}/uninstall")]
        public async Task<ActionResult<OASISResult<bool>>> UninstallNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.UninstallNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("nfts/{id}/publish")]
        public async Task<ActionResult<OASISResult<bool>>> PublishNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.PublishNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("nfts/{id}/unpublish")]
        public async Task<ActionResult<OASISResult<bool>>> UnpublishNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.UnpublishNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("nfts/{id}/republish")]
        public async Task<ActionResult<OASISResult<bool>>> RepublishNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.RepublishNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("nfts/{id}/activate")]
        public async Task<ActionResult<OASISResult<bool>>> ActivateNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ActivateNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("nfts/{id}/deactivate")]
        public async Task<ActionResult<OASISResult<bool>>> DeactivateNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DeactivateNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpGet("nfts/{id}")]
        public async Task<ActionResult<OASISResult<STARNFT>>> ShowNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ShowNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpGet("nfts")]
        public async Task<ActionResult<OASISResult<List<STARNFT>>>> ListAllNFTs([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllNFTsAsync(providerType);
            return Ok(result);
        }

        [HttpGet("nfts/created-by-me")]
        public async Task<ActionResult<OASISResult<List<STARNFT>>>> ListAllNFTsCreatedByMe([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllNFTsCreatedByBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("nfts/installed")]
        public async Task<ActionResult<OASISResult<List<InstalledNFT>>>> ListAllNFTsInstalled([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllNFTsInstalledForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("nfts/uninstalled")]
        public async Task<ActionResult<OASISResult<List<STARNFT>>>> ListAllNFTsUninstalled([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllNFTsUninstalledForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("nfts/unpublished")]
        public async Task<ActionResult<OASISResult<List<STARNFT>>>> ListAllNFTsUnpublished([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllNFTsUnpublishedForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("nfts/deactivated")]
        public async Task<ActionResult<OASISResult<List<STARNFT>>>> ListAllNFTsDeactivated([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllNFTsDeactivatedForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("nfts/search")]
        public async Task<ActionResult<OASISResult<List<STARNFT>>>> SearchNFTs([FromQuery] string searchTerm, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.SearchNFTsAsync(searchTerm, providerType);
            return Ok(result);
        }

        [HttpPost("nfts/mint")]
        public async Task<ActionResult<OASISResult<STARNFT>>> MintNFT([FromBody] CreateSTARNETItemRequest request)
        {
            var result = await _starNetService.MintNFTAsync(request.Name, request.Description, request.Type, request.ProviderType);
            return Ok(result);
        }

        [HttpPost("nfts/convert/{web4NftId}")]
        public async Task<ActionResult<OASISResult<STARNFT>>> ConvertNFT(Guid web4NftId, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ConvertNFTAsync(web4NftId, providerType);
            return Ok(result);
        }

        [HttpPost("nfts/{id}/clone")]
        public async Task<ActionResult<OASISResult<STARNFT>>> CloneNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.CloneNFTAsync(id, providerType);
            return Ok(result);
        }

        #endregion

        #region GeoNFTs

        [HttpPost("geonfts")]
        public async Task<ActionResult<OASISResult<STARGeoNFT>>> CreateGeoNFT([FromBody] CreateSTARNETItemRequest request)
        {
            var result = await _starNetService.CreateGeoNFTAsync(request.Name, request.Description, request.Type, request.ProviderType);
            return Ok(result);
        }

        [HttpPut("geonfts/{id}")]
        public async Task<ActionResult<OASISResult<STARGeoNFT>>> EditGeoNFT(Guid id, [FromBody] EditSTARNETItemRequest request)
        {
            var result = await _starNetService.EditGeoNFTAsync(id, request.Name, request.Description, request.ProviderType);
            return Ok(result);
        }

        [HttpDelete("geonfts/{id}")]
        public async Task<ActionResult<OASISResult<bool>>> DeleteGeoNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DeleteGeoNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("geonfts/{id}/download-install")]
        public async Task<ActionResult<OASISResult<STARGeoNFT>>> DownloadAndInstallGeoNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DownloadAndInstallGeoNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("geonfts/{id}/uninstall")]
        public async Task<ActionResult<OASISResult<bool>>> UninstallGeoNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.UninstallGeoNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("geonfts/{id}/publish")]
        public async Task<ActionResult<OASISResult<bool>>> PublishGeoNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.PublishGeoNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("geonfts/{id}/unpublish")]
        public async Task<ActionResult<OASISResult<bool>>> UnpublishGeoNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.UnpublishGeoNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("geonfts/{id}/republish")]
        public async Task<ActionResult<OASISResult<bool>>> RepublishGeoNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.RepublishGeoNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("geonfts/{id}/activate")]
        public async Task<ActionResult<OASISResult<bool>>> ActivateGeoNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ActivateGeoNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("geonfts/{id}/deactivate")]
        public async Task<ActionResult<OASISResult<bool>>> DeactivateGeoNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.DeactivateGeoNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpGet("geonfts/{id}")]
        public async Task<ActionResult<OASISResult<STARGeoNFT>>> ShowGeoNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ShowGeoNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpGet("geonfts")]
        public async Task<ActionResult<OASISResult<List<STARGeoNFT>>>> ListAllGeoNFTs([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllGeoNFTsAsync(providerType);
            return Ok(result);
        }

        [HttpGet("geonfts/created-by-me")]
        public async Task<ActionResult<OASISResult<List<STARGeoNFT>>>> ListAllGeoNFTsCreatedByMe([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllGeoNFTsCreatedByBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("geonfts/installed")]
        public async Task<ActionResult<OASISResult<List<InstalledGeoNFT>>>> ListAllGeoNFTsInstalled([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllGeoNFTsInstalledForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("geonfts/uninstalled")]
        public async Task<ActionResult<OASISResult<List<STARGeoNFT>>>> ListAllGeoNFTsUninstalled([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllGeoNFTsUninstalledForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("geonfts/unpublished")]
        public async Task<ActionResult<OASISResult<List<STARGeoNFT>>>> ListAllGeoNFTsUnpublished([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllGeoNFTsUnpublishedForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("geonfts/deactivated")]
        public async Task<ActionResult<OASISResult<List<STARGeoNFT>>>> ListAllGeoNFTsDeactivated([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllGeoNFTsDeactivatedForBeamedInAvatarAsync(providerType);
            return Ok(result);
        }

        [HttpGet("geonfts/search")]
        public async Task<ActionResult<OASISResult<List<STARGeoNFT>>>> SearchGeoNFTs([FromQuery] string searchTerm, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.SearchGeoNFTsAsync(searchTerm, providerType);
            return Ok(result);
        }

        [HttpPost("geonfts/mint")]
        public async Task<ActionResult<OASISResult<STARGeoNFT>>> MintGeoNFT([FromBody] CreateSTARNETItemRequest request)
        {
            var result = await _starNetService.MintGeoNFTAsync(request.Name, request.Description, request.Type, request.ProviderType);
            return Ok(result);
        }

        [HttpPost("geonfts/{id}/burn")]
        public async Task<ActionResult<OASISResult<bool>>> BurnGeoNFT(Guid id, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.BurnGeoNFTAsync(id, providerType);
            return Ok(result);
        }

        [HttpPost("geonfts/import")]
        public async Task<ActionResult<OASISResult<STARGeoNFT>>> ImportGeoNFT([FromBody] ImportGeoNFTRequest request)
        {
            var result = await _starNetService.ImportGeoNFTAsync(request.FilePath, request.ProviderType);
            return Ok(result);
        }

        [HttpPost("geonfts/{id}/export")]
        public async Task<ActionResult<OASISResult<bool>>> ExportGeoNFT(Guid id, [FromBody] ExportGeoNFTRequest request)
        {
            var result = await _starNetService.ExportGeoNFTAsync(id, request.FilePath, request.ProviderType);
            return Ok(result);
        }

        #endregion

        // Additional endpoints for Missions, Chapters, Celestial Bodies, etc. would follow the same pattern...
        // For brevity, I'll include a few more key ones:

        #region Missions

        [HttpPost("missions")]
        public async Task<ActionResult<OASISResult<Mission>>> CreateMission([FromBody] CreateSTARNETItemRequest request)
        {
            var result = await _starNetService.CreateMissionAsync(request.Name, request.Description, request.Type, request.ProviderType);
            return Ok(result);
        }

        [HttpGet("missions")]
        public async Task<ActionResult<OASISResult<List<Mission>>>> ListAllMissions([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllMissionsAsync(providerType);
            return Ok(result);
        }

        [HttpGet("missions/search")]
        public async Task<ActionResult<OASISResult<List<Mission>>>> SearchMissions([FromQuery] string searchTerm, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.SearchMissionsAsync(searchTerm, providerType);
            return Ok(result);
        }

        #endregion

        #region Chapters

        [HttpPost("chapters")]
        public async Task<ActionResult<OASISResult<Chapter>>> CreateChapter([FromBody] CreateSTARNETItemRequest request)
        {
            var result = await _starNetService.CreateChapterAsync(request.Name, request.Description, request.Type, request.ProviderType);
            return Ok(result);
        }

        [HttpGet("chapters")]
        public async Task<ActionResult<OASISResult<List<Chapter>>>> ListAllChapters([FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.ListAllChaptersAsync(providerType);
            return Ok(result);
        }

        [HttpGet("chapters/search")]
        public async Task<ActionResult<OASISResult<List<Chapter>>>> SearchChapters([FromQuery] string searchTerm, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            var result = await _starNetService.SearchChaptersAsync(searchTerm, providerType);
            return Ok(result);
        }

        #endregion
    }

    public class CreateSTARNETItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object Type { get; set; } = new object();
        public ProviderType ProviderType { get; set; } = ProviderType.Default;
    }

    public class EditSTARNETItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ProviderType ProviderType { get; set; } = ProviderType.Default;
    }

    public class ImportGeoNFTRequest
    {
        public string FilePath { get; set; } = string.Empty;
        public ProviderType ProviderType { get; set; } = ProviderType.Default;
    }

    public class ExportGeoNFTRequest
    {
        public string FilePath { get; set; } = string.Empty;
        public ProviderType ProviderType { get; set; } = ProviderType.Default;
    }
}
