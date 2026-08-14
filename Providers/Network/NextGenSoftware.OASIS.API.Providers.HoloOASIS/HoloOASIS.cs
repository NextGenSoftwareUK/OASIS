using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using DataHelper = NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers.DataHelper;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public partial class HoloOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar, IOASISLocalStorageProvider
    {
        private const string HOLO_NETWORK_URI = "https://holo.host";
        private const string OASIS_HAPP_ID = "oasis";
        private const string OASIS_HAPP_PATH = "OASIS_hAPP\\oasis.happ";
        private const string OASIS_HAPP_ROLE_NAME = "oasis";
        private const string ZOME_LOAD_AVATAR_BY_ID_FUNCTION = "get_avatar_by_id";
        private const string ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION = "get_avatar_by_username";
        private const string ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION = "get_avatar_by_email";
        private const string ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION = "get_avatar_detail_by_id";
        private const string ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION = "get_avatar_detail_by_username";
        private const string ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION = "get_avatar_detail_by_email";
        private const string ZOME_LOAD_ALL_AVATARS_FUNCTION = "get_all_avatars";
        private const string ZOME_LOAD_ALL_AVATARS_DETAILS_FUNCTION = "get_all_avatar_details";
        private const string ZOME_DELETE_AVATAR_BY_ID_FUNCTION = "delete_avatar_by_id";
        private const string ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION = "delete_avatar_by_username";
        private const string ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION = "delete_avatar_by_email";
        private const string ZOME_LOAD_HOLON_BY_ID_FUNCTION = "get_holon_by_id";
        private const string ZOME_LOAD_HOLON_BY_PROVIDER_KEY_FUNCTION = "get_holon_by_provider_key";
        private const string ZOME_LOAD_HOLON_BY_CUSTOM_KEY_FUNCTION = "get_holon_by_custom_key";
        private const string ZOME_LOAD_HOLON_BY_META_DATA_FUNCTION = "get_holon_by_meta_data";
        private const string ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION = "get_holons_for_parent_by_id";
        private const string ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION = "get_holons_for_parent_by_provider_key";
        private const string ZOME_LOAD_HOLONS_FOR_PARENT_BY_CUSTOM_KEY_FUNCTION = "get_holons_for_parent_by_custom_key";
        private const string ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION = "get_holons_for_parent_by_meta_data";
        private const string ZOME_LOAD_ALL_HOLONS_FUNCTION = "get_all_holons";
        private const string ZOME_SAVE_ALL_HOLONS_FUNCTION = "save_all_holons";
        private const string ZOME_DELETE_HOLON_BY_ID_FUNCTION = "delete_holon_by_id";
        private const string ZOME_DELETE_HOLON_BY_PROVIDER_KEY_FUNCTION = "delete_holon_by_provider_key";
        private const string ZOME_DELETE_HOLON_BY_CUSTOM_KEY_FUNCTION = "delete_holon_by_custom_key";
        private const string ZOME_DELETE_HOLON_BY_META_DATA_FUNCTION = "delete_holon_by_meta_data";

        private AvatarRepository _avatarRepository = null;
        private AvatarDetailRepository _avatarDetailRepository = null;
        private HolonRepository _holonRepository = null;
        private GenericRepository _genericRepository = null;
        private string _holochainConductorAppAgentURI = "";
        private readonly HttpClient _httpClient = new HttpClient();

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void AvatarSaved(object sender, AvatarSavedEventArgs e);
        public event AvatarSaved OnPlayerAvatarSaved;

        public delegate void AvatarLoaded(object sender, AvatarLoadedEventArgs e);
        public event AvatarLoaded OnPlayerAvatarLoaded;

        public IHoloNETClientAdmin HoloNETClientAdmin { get; private set; }
        public IHoloNETClientAppAgent HoloNETClientAppAgent { get; private set; }
        public bool UseLocalNode { get; private set; }
        public bool UseHoloNetwork { get; private set; }
        public string HoloNetworkURI { get; private set; }
        public bool UseHoloNETORMReflection { get; private set; }
        private OASISDNA _oasisDNA;

        public HoloOASIS(HoloNETClientAdmin holoNETClientAdmin, HoloNETClientAppAgent holoNETClientAppAgent, OASISDNA oasisDNA = null, string holoNetworkURI = HOLO_NETWORK_URI, bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETORMReflection = true)
        {
            this.HoloNETClientAdmin = holoNETClientAdmin;
            this.HoloNETClientAppAgent = holoNETClientAppAgent;
            this._oasisDNA = oasisDNA;
            this.HoloNetworkURI = holoNetworkURI;
            this.UseLocalNode = useLocalNode;
            this.UseHoloNetwork = useHoloNetwork;
            this.UseHoloNETORMReflection = useHoloNETORMReflection;
            Initialize();
        }

        public HoloOASIS(string holochainConductorAdminURI, OASISDNA oasisDNA = null, string holoNetworkURI = HOLO_NETWORK_URI, bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETORMReflection = true)
        {
            this._oasisDNA = oasisDNA;
            this.HoloNetworkURI = holoNetworkURI;
            this.UseLocalNode = useLocalNode;
            this.UseHoloNetwork = useHoloNetwork;
            this.UseHoloNETORMReflection = useHoloNETORMReflection;
            HoloNETClientAdmin = new HoloNETClientAdmin(new HoloNETDNA() { HolochainConductorAdminURI = holochainConductorAdminURI });
            Initialize();
        }

        public HoloOASIS(string holochainConductorAdminURI, string holochainConductorAppAgentURI, OASISDNA oasisDNA = null, string holoNetworkURI = HOLO_NETWORK_URI, bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETORMReflection = true)
        {
            this._oasisDNA = oasisDNA;
            _holochainConductorAppAgentURI = holochainConductorAppAgentURI;
            this.HoloNetworkURI = holoNetworkURI;
            this.UseLocalNode = useLocalNode;
            this.UseHoloNetwork = useHoloNetwork;
            this.UseHoloNETORMReflection = useHoloNETORMReflection;
            HoloNETClientAdmin = new HoloNETClientAdmin(new HoloNETDNA() { HolochainConductorAdminURI = holochainConductorAdminURI});
            Initialize();
        }

        private async Task Initialize()
        {
            this.ProviderName = "HoloOASIS";
            this.ProviderDescription = "Holochain Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.HoloOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);

            DataHelper.UseReflection = this.UseHoloNETORMReflection;
            _avatarRepository = new AvatarRepository();
            _avatarDetailRepository = new AvatarDetailRepository();
            _holonRepository = new HolonRepository();
            _genericRepository = new GenericRepository(HoloNETClientAppAgent, this.UseHoloNETORMReflection);
        }

        private void HoloNETClientAdmin_OnError(object sender, HoloNETErrorEventArgs e)
        {
            HandleError("Error Occured in HoloOASIS Provider With HoloNETClientAdmin_OnError Event Handler.", null, e);
        }

        private void HoloNETClientAppAgent_OnError(object sender, HoloNETErrorEventArgs e)
        {
            HandleError("Error Occured in HoloOASIS Provider With HoloNETClientAppAgent_OnError Event Handler.", null, e);
        }
    }
}
