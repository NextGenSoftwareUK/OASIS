using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop
{
    /// <summary>Desktop HoloOASIS provider backed by the canonical HoloNET client.</summary>
    public sealed class HoloOASIS : global::NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS
    {
        public HoloOASIS(HoloNETClientAdmin adminClient, HoloNETClientAppAgent appAgentClient,
            OASISDNA oasisDNA = null, string holoNetworkUri = "https://holo.host",
            bool useLocalNode = true, bool useHoloNetwork = false, bool useHoloNETORMReflection = true)
            : base(adminClient, appAgentClient, oasisDNA, holoNetworkUri, useLocalNode,
                useHoloNetwork, useHoloNETORMReflection) { }

        public HoloOASIS(string conductorAdminUri, string conductorAppUri,
            OASISDNA oasisDNA = null, string holoNetworkUri = "https://holo.host",
            bool useLocalNode = true, bool useHoloNetwork = false, bool useHoloNETORMReflection = true)
            : base(conductorAdminUri, conductorAppUri, oasisDNA, holoNetworkUri, useLocalNode,
                useHoloNetwork, useHoloNETORMReflection) { }
    }
}
