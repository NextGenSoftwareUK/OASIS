using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Configuration;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class ProviderManager : OASISManager
    {
        private static ProviderManager _instance = null;
        private List<IOASISProvider> _registeredProviders = new List<IOASISProvider>();
        private List<EnumValue<ProviderType>> _registeredProviderTypes = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverList { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverListForCheckIfOASISSystemAccountExists { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverListForAvatarLogin { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverListForCheckIfEmailAlreadyInUse { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverListForCheckIfUsernameAlreadyInUse { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverLocalList { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providersThatAreAutoReplicating { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoLoadBalanceList { get; set; } = new List<EnumValue<ProviderType>>();
        private bool _setProviderGlobally = false;

        public EnumValue<ProviderType> CurrentStorageProviderType { get; private set; } = new EnumValue<ProviderType>(ProviderType.Default);
        public EnumValue<ProviderCategory> CurrentStorageProviderCategory { get; private set; } = new EnumValue<ProviderCategory>(ProviderCategory.None);
        public OASISProviderBootType OASISProviderBootType { get; set; } = OASISProviderBootType.Hot;

        public bool IsAutoReplicationEnabled { get; set; } = true;
        public bool IsAutoLoadBalanceEnabled { get; set; } = true;
        public bool IsAutoFailOverEnabled { get; set; } = true;
        /// <summary>When true with a non-empty <see cref="GetProviderAutoFailOverLocalList"/>, native/offline-first hosts may walk local storage providers in order (OASISDNA AutoFailOverLocalProviders).</summary>
        public bool IsAutoFailOverLocalProvidersEnabled { get; set; }
        //public bool IsAutoFailOverEnabledForAvatarLogin { get; set; } = true;
        //public bool IsAutoFailOverEnabledForCheckIfEmailAlreadyInUse { get; set; } = true;
        //public bool IsAutoFailOverEnabledForCheckIfUsernameAlreadyInUse { get; set; } = true;

        //public  string CurrentStorageProviderName
        //{
        //    get
        //    {
        //        return Enum.GetName(CurrentStorageProviderType);
        //    }
        //}

        // public  string[] DefaultProviderTypes { get; set; }

        public IOASISStorageProvider DefaultGlobalStorageProvider { get; set; }

        public IOASISStorageProvider CurrentStorageProvider { get; private set; } //TODO: Need to work this out because in future there can be more than one provider active at a time.

        public bool OverrideProviderType { get; set; } = false;
       // public bool SupressLoggingWhenSwitchingProviders { get; set; } = false;


        //public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        public static ProviderManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ProviderManager(null);

                return _instance;
            }
        }

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public ProviderManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

    }
}
