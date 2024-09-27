
namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public enum HoloNETResponseType
    {
        ZomeResponse,
        Signal,
        AppInfo,
        AdminAgentPubKeyGenerated,
        AdminAppInstalled,
        AdminAppUninstalled,
        AdminAppEnabled,
        AdminAppDisabled,
        AdminZomeCallCapabilityGranted,
        AdminAppInterfaceAttached,
        AdminDnaRegistered,
        AdminDnaDefinitionReturned,
        AdminAppInterfacesListed,
        AdminAppsListed,
        AdminDnasListed,
        AdminCellIdsListed,
        AdminAgentInfoReturned,
        AdminAgentInfoAdded,
        AdminCoordinatorsUpdated,
        AdminCloneCellDeleted,
        AdminStateDumped,
        AdminFullStateDumped,
        AdminNetworkMetricsDumped,
        AdminNetworkStatsDumped,
        AdminStorageInfoReturned,
        AdminRecordsGrafted,
        AdminAdminInterfacesAdded,
        Error
    }
}