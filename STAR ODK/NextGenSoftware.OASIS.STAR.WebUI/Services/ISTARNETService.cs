using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.WebUI.Services
{
    public interface ISTARNETService
    {
        // OAPPs
        Task<OASISResult<OAPP>> CreateOAPPAsync(string name, string description, object oappType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<OAPP>> EditOAPPAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteOAPPAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<OAPP>> DownloadAndInstallOAPPAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallOAPPAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishOAPPAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishOAPPAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishOAPPAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateOAPPAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateOAPPAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<OAPP>> ShowOAPPAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPP>>> ListAllOAPPsCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPP>>> ListAllOAPPsAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledOAPP>>> ListAllOAPPsInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPP>>> ListAllOAPPsUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPP>>> ListAllOAPPsUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPP>>> ListAllOAPPsDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPP>>> SearchOAPPsAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // Quests
        Task<OASISResult<Quest>> CreateQuestAsync(string name, string description, object questType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Quest>> EditQuestAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteQuestAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Quest>> DownloadAndInstallQuestAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallQuestAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishQuestAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishQuestAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishQuestAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateQuestAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateQuestAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Quest>> ShowQuestAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Quest>>> ListAllQuestsCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Quest>>> ListAllQuestsAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledQuest>>> ListAllQuestsInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Quest>>> ListAllQuestsUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Quest>>> ListAllQuestsUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Quest>>> ListAllQuestsDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Quest>>> SearchQuestsAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // NFTs
        Task<OASISResult<STARNFT>> CreateNFTAsync(string name, string description, object nftType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARNFT>> EditNFTAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARNFT>> DownloadAndInstallNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARNFT>> ShowNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARNFT>>> ListAllNFTsCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARNFT>>> ListAllNFTsAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledNFT>>> ListAllNFTsInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARNFT>>> ListAllNFTsUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARNFT>>> ListAllNFTsUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARNFT>>> ListAllNFTsDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARNFT>>> SearchNFTsAsync(string searchTerm, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARNFT>> MintNFTAsync(string name, string description, object nftType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARNFT>> ConvertNFTAsync(Guid web4NftId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARNFT>> CloneNFTAsync(Guid nftId, ProviderType providerType = ProviderType.Default);

        // GeoNFTs
        Task<OASISResult<STARGeoNFT>> CreateGeoNFTAsync(string name, string description, object geoNftType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARGeoNFT>> EditGeoNFTAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteGeoNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARGeoNFT>> DownloadAndInstallGeoNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallGeoNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishGeoNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishGeoNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishGeoNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateGeoNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateGeoNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARGeoNFT>> ShowGeoNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARGeoNFT>>> ListAllGeoNFTsCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARGeoNFT>>> ListAllGeoNFTsAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledGeoNFT>>> ListAllGeoNFTsInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARGeoNFT>>> ListAllGeoNFTsUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARGeoNFT>>> ListAllGeoNFTsUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARGeoNFT>>> ListAllGeoNFTsDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARGeoNFT>>> SearchGeoNFTsAsync(string searchTerm, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARGeoNFT>> MintGeoNFTAsync(string name, string description, object geoNftType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> BurnGeoNFTAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARGeoNFT>> ImportGeoNFTAsync(string filePath, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ExportGeoNFTAsync(Guid id, string filePath, ProviderType providerType = ProviderType.Default);

        // Missions
        Task<OASISResult<Mission>> CreateMissionAsync(string name, string description, object missionType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Mission>> EditMissionAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteMissionAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Mission>> DownloadAndInstallMissionAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallMissionAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishMissionAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishMissionAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishMissionAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateMissionAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateMissionAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Mission>> ShowMissionAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Mission>>> ListAllMissionsCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Mission>>> ListAllMissionsAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledMission>>> ListAllMissionsInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Mission>>> ListAllMissionsUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Mission>>> ListAllMissionsUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Mission>>> ListAllMissionsDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Mission>>> SearchMissionsAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // Chapters
        Task<OASISResult<Chapter>> CreateChapterAsync(string name, string description, object chapterType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Chapter>> EditChapterAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteChapterAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Chapter>> DownloadAndInstallChapterAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallChapterAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishChapterAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishChapterAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishChapterAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateChapterAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateChapterAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Chapter>> ShowChapterAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Chapter>>> ListAllChaptersCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Chapter>>> ListAllChaptersAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledChapter>>> ListAllChaptersInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Chapter>>> ListAllChaptersUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Chapter>>> ListAllChaptersUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Chapter>>> ListAllChaptersDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Chapter>>> SearchChaptersAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // Celestial Bodies
        Task<OASISResult<STARCelestialBody>> CreateCelestialBodyAsync(string name, string description, object celestialBodyType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARCelestialBody>> EditCelestialBodyAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteCelestialBodyAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARCelestialBody>> DownloadAndInstallCelestialBodyAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallCelestialBodyAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishCelestialBodyAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishCelestialBodyAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishCelestialBodyAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateCelestialBodyAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateCelestialBodyAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARCelestialBody>> ShowCelestialBodyAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialBody>>> ListAllCelestialBodiesCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialBody>>> ListAllCelestialBodiesAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledCelestialBody>>> ListAllCelestialBodiesInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialBody>>> ListAllCelestialBodiesUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialBody>>> ListAllCelestialBodiesUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialBody>>> ListAllCelestialBodiesDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialBody>>> SearchCelestialBodiesAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // Celestial Spaces
        Task<OASISResult<STARCelestialSpace>> CreateCelestialSpaceAsync(string name, string description, object celestialSpaceType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARCelestialSpace>> EditCelestialSpaceAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteCelestialSpaceAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARCelestialSpace>> DownloadAndInstallCelestialSpaceAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallCelestialSpaceAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishCelestialSpaceAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishCelestialSpaceAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishCelestialSpaceAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateCelestialSpaceAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateCelestialSpaceAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<STARCelestialSpace>> ShowCelestialSpaceAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialSpace>>> ListAllCelestialSpacesCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialSpace>>> ListAllCelestialSpacesAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledCelestialSpace>>> ListAllCelestialSpacesInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialSpace>>> ListAllCelestialSpacesUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialSpace>>> ListAllCelestialSpacesUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialSpace>>> ListAllCelestialSpacesDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<STARCelestialSpace>>> SearchCelestialSpacesAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // Runtimes
        Task<OASISResult<Runtime>> CreateRuntimeAsync(string name, string description, object runtimeType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Runtime>> EditRuntimeAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteRuntimeAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Runtime>> DownloadAndInstallRuntimeAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallRuntimeAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishRuntimeAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishRuntimeAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishRuntimeAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateRuntimeAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateRuntimeAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Runtime>> ShowRuntimeAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Runtime>>> ListAllRuntimesCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Runtime>>> ListAllRuntimesAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledRuntime>>> ListAllRuntimesInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Runtime>>> ListAllRuntimesUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Runtime>>> ListAllRuntimesUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Runtime>>> ListAllRuntimesDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Runtime>>> SearchRuntimesAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // Libraries
        Task<OASISResult<Library>> CreateLibraryAsync(string name, string description, object libraryType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Library>> EditLibraryAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteLibraryAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Library>> DownloadAndInstallLibraryAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallLibraryAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishLibraryAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishLibraryAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishLibraryAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateLibraryAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateLibraryAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Library>> ShowLibraryAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Library>>> ListAllLibrariesCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Library>>> ListAllLibrariesAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledLibrary>>> ListAllLibrariesInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Library>>> ListAllLibrariesUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Library>>> ListAllLibrariesUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Library>>> ListAllLibrariesDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Library>>> SearchLibrariesAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // OAPP Templates
        Task<OASISResult<OAPPTemplate>> CreateOAPPTemplateAsync(string name, string description, object templateType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<OAPPTemplate>> EditOAPPTemplateAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteOAPPTemplateAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<OAPPTemplate>> DownloadAndInstallOAPPTemplateAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallOAPPTemplateAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishOAPPTemplateAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishOAPPTemplateAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishOAPPTemplateAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateOAPPTemplateAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateOAPPTemplateAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<OAPPTemplate>> ShowOAPPTemplateAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPPTemplate>>> ListAllOAPPTemplatesCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPPTemplate>>> ListAllOAPPTemplatesAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledOAPPTemplate>>> ListAllOAPPTemplatesInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPPTemplate>>> ListAllOAPPTemplatesUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPPTemplate>>> ListAllOAPPTemplatesUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPPTemplate>>> ListAllOAPPTemplatesDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<OAPPTemplate>>> SearchOAPPTemplatesAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // Inventory Items
        Task<OASISResult<InventoryItem>> CreateInventoryItemAsync(string name, string description, object itemType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<InventoryItem>> EditInventoryItemAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteInventoryItemAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<InventoryItem>> DownloadAndInstallInventoryItemAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallInventoryItemAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishInventoryItemAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishInventoryItemAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishInventoryItemAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateInventoryItemAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateInventoryItemAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<InventoryItem>> ShowInventoryItemAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InventoryItem>>> ListAllInventoryItemsCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InventoryItem>>> ListAllInventoryItemsAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledInventoryItem>>> ListAllInventoryItemsInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InventoryItem>>> ListAllInventoryItemsUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InventoryItem>>> ListAllInventoryItemsUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InventoryItem>>> ListAllInventoryItemsDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InventoryItem>>> SearchInventoryItemsAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // Plugins
        Task<OASISResult<Plugin>> CreatePluginAsync(string name, string description, object pluginType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Plugin>> EditPluginAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeletePluginAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Plugin>> DownloadAndInstallPluginAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallPluginAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishPluginAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishPluginAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishPluginAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivatePluginAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivatePluginAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<Plugin>> ShowPluginAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Plugin>>> ListAllPluginsCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Plugin>>> ListAllPluginsAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledPlugin>>> ListAllPluginsInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Plugin>>> ListAllPluginsUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Plugin>>> ListAllPluginsUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Plugin>>> ListAllPluginsDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<Plugin>>> SearchPluginsAsync(string searchTerm, ProviderType providerType = ProviderType.Default);

        // GeoHotSpots
        Task<OASISResult<GeoHotSpot>> CreateGeoHotSpotAsync(string name, string description, object hotSpotType, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<GeoHotSpot>> EditGeoHotSpotAsync(Guid id, string name, string description, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteGeoHotSpotAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<GeoHotSpot>> DownloadAndInstallGeoHotSpotAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UninstallGeoHotSpotAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> PublishGeoHotSpotAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> UnpublishGeoHotSpotAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> RepublishGeoHotSpotAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> ActivateGeoHotSpotAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeactivateGeoHotSpotAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<GeoHotSpot>> ShowGeoHotSpotAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<GeoHotSpot>>> ListAllGeoHotSpotsCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<GeoHotSpot>>> ListAllGeoHotSpotsAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<InstalledGeoHotSpot>>> ListAllGeoHotSpotsInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<GeoHotSpot>>> ListAllGeoHotSpotsUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<GeoHotSpot>>> ListAllGeoHotSpotsUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<GeoHotSpot>>> ListAllGeoHotSpotsDeactivatedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<List<GeoHotSpot>>> SearchGeoHotSpotsAsync(string searchTerm, ProviderType providerType = ProviderType.Default);
    }
}
