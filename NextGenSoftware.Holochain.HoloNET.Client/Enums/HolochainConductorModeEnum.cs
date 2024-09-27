
namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public enum HolochainConductorModeEnum
    {
        /// <summary>
        /// If this is set you then need to spcecify the full path to the holochain.exe in the 'FullPathToExternalHolochainConductorBinary' setting on the IHoloNETDNA (if 'HolochainConductorToUse' is set to 'HolochainProductionConductor') or the full path to the hc.exe in the 'FullPathToExternalHCToolBinary' setting (if 'HolochainConductorToUse' is set to 'HcDevTool'). 
        /// </summary>
        UseExternal,

        /// <summary>
        /// This will use the internal embedded holochain.exe (if 'HolochainConductorToUse' is set to 'HolochainProductionConductor') or hc.exe (if 'HolochainConductorToUse' is set to 'HcDevTool') if the app is using the [NextGenSoftware.Holochain.HoloNET.Client.Embedded](https://www.nuget.org/packages/NextGenSoftware.Holochain.HoloNET.Client.Embedded) package, otherwise it will throw an exception.
        /// </summary>
        UseEmbedded,

        /// <summary>
        /// This will use the installed version of holochain.exe on the target machine. (if 'HolochainConductorToUse' is set to 'HolochainProductionConductor') or hc.exe (if 'HolochainConductorToUse' is set to 'HcDevTool'). This is the default option.
        /// </summary>
        UseSystemGlobal
    }
}