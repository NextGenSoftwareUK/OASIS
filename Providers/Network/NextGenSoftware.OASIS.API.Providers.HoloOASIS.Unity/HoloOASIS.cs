using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity
{
    /// <summary>
    /// Unity-compatible HoloOASIS composition. The transport is injected so Unity/mobile hosts can
    /// own platform lifecycle and secure conductor connectivity without referencing UnityEngine from
    /// the provider assembly.
    /// </summary>
    public sealed class HoloOASIS : global::NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS
    {
        public HoloOASIS(HoloNETClientAdmin adminClient, HoloNETClientAppAgent appAgentClient,
            OASISDNA oasisDNA = null, string holoNetworkUri = "https://holo.host",
            bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETOrmReflection = true)
            : base(adminClient, appAgentClient, oasisDNA, holoNetworkUri, useLocalNode,
                useHoloNetwork, useHoloNETOrmReflection)
        {
        }

        public HoloOASIS(string conductorAdminUri, string conductorAppAgentUri,
            OASISDNA oasisDNA = null, string holoNetworkUri = "https://holo.host",
            bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETOrmReflection = true)
            : base(conductorAdminUri, conductorAppAgentUri, oasisDNA, holoNetworkUri, useLocalNode,
                useHoloNetwork, useHoloNETOrmReflection)
        {
        }
    }
}
