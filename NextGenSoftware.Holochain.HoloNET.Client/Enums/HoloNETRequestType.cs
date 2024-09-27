
namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public enum HoloNETRequestType
    {
        ZomeCall,
        Signal,
        AppInfo,
        AdminGenerateAgentPubKey,
        AdminInstallApp,
        AdminUninstallApp,
        AdminEnableApp,
        AdminDisableApp,
        AdminGrantZomeCallCapability,
        AdminAttachAppInterface,
        AdminListApps,
        AdminListDnas,
        AdminListAppInterfaces,
        AdminListCellIds,
        AdminRegisterDna,
        AdminUpdateCoordinators,
        AdminDumpFullState,
        AdminDumpState,
        AdminGetDnaDefinition,
        AdminAgentInfo,
        AdminAddAgentInfo,
        AdminDeleteClonedCell,
        AdminStorageInfo,
        AdminDumpNetworkStats,
        AdminDumpNetworkMetrics,
        AdminGraftRecords,
        AdminAddAdminInterfaces,
    }
}