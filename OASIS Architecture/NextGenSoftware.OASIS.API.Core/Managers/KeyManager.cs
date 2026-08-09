using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Cryptography.ECDSA;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Rijndael256;
using static NextGenSoftware.Utilities.KeyHelper;
using KeyPair = NextGenSoftware.OASIS.API.Core.Objects.KeyPair;
using Rijndael = Rijndael256.Rijndael;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    //TODO: Add Async version of all methods and add IKeyManager Interface.
    public partial class KeyManager : OASISManager
    {
        private readonly object _lockObject = new object();
        private readonly Dictionary<Guid, List<Key>> _avatarKeys;
        private readonly Dictionary<Guid, List<KeyUsage>> _keyUsage;

        private static Dictionary<string, string> _avatarIdToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, List<string>> _avatarIdToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
        //private static Dictionary<string, List<string>> _avatarIdToProviderPrivateKeyLookup = new Dictionary<string, List<string>>();
        private static Dictionary<string, string> _avatarUsernameToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, List<string>> _avatarUsernameToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
        //private static Dictionary<string, List<string>> _avatarUsernameToProviderPrivateKeyLookup = new Dictionary<string, List<string>>();
        private static Dictionary<string, string> _avatarEmailToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, List<string>> _avatarEmailToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
        //private static Dictionary<string, List<string>> _avatarEmailToProviderPrivateKeyLookup = new Dictionary<string, List<string>>();
        private static Dictionary<string, Guid> _providerUniqueStorageKeyToAvatarIdLookup = new Dictionary<string, Guid>();
        private static Dictionary<string, Guid> _providerPublicKeyToAvatarIdLookup = new Dictionary<string, Guid>();
        //private static Dictionary<string, Guid> _providerPrivateKeyToAvatarIdLookup = new Dictionary<string, Guid>();
        private static Dictionary<string, string> _providerUniqueStorageKeyToAvatarUsernameLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _providerPublicKeyToAvatarUsernameLookup = new Dictionary<string, string>();
        //private static Dictionary<string, string> _providerPrivateKeyToAvatarUsernameLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _providerUniqueStorageKeyToAvatarEmailLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _providerPublicKeyToAvatarEmailLookup = new Dictionary<string, string>();
        //private static Dictionary<string, string> _providerPrivateKeyToAvatarEmailLookup = new Dictionary<string, string>();
        private static Dictionary<string, IAvatar> _providerUniqueStorageKeyToAvatarLookup = new Dictionary<string, IAvatar>();
        private static Dictionary<string, IAvatar> _providerPublicKeyToAvatarLookup = new Dictionary<string, IAvatar>();
        //private static Dictionary<string, IAvatar> _providerPrivateKeyToAvatarLookup = new Dictionary<string, IAvatar>();
        private static KeyManager _instance = null;

        public static KeyManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new KeyManager(ProviderManager.Instance.CurrentStorageProvider);
                    //_instance = new KeyManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.Instance);

                return _instance;
            }
        }

        public WifUtility WifUtility { get; set; } = new WifUtility();
        //public AvatarManager AvatarManager { get; set; }

        public AvatarManager AvatarManager
        {
            get
            {
                return AvatarManager.Instance;
            }
        }


        //public KeyManager(IOASISStorageProvider OASISStorageProvider, AvatarManager avatarManager, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        //{
        //    AvatarManager = avatarManager;
        //}

        public KeyManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        //public static void Init(IOASISStorageProvider OASISStorageProvider, AvatarManager avatarManager, OASISDNA OASISDNA = null) 
        //{
        //    AvatarManager = avatarManager;
        //}

        //TODO: Implement later (Cache Disabled).
        //public bool IsCacheEnabled { get; set; } = true;

    }
}