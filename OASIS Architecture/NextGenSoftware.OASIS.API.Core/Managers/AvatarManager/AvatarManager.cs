using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.CLI.Engine;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        private static AvatarManager _instance = null;
        private ProviderManagerConfig _config;
        private static IAvatar _loggedInAvatar = null;

        //public static IAvatar OASISSystemAccount { get; set; } //TODO: Later may need to actually create a avatar for this Id? So we can see which ids belong to OASIS Accounts outside of each ONODE (each ONODE has its own OASISDNA with its own system id's).

        public static IAvatar LoggedInAvatar 
        { 
            get
            {
                // Request-scoped avatar first (set by WEB4/WEB5 middleware) - safe for multiple concurrent clients
                var requestAvatar = OASISRequestContext.CurrentAvatar;
                if (requestAvatar != null)
                    return requestAvatar;
                var requestAvatarId = OASISRequestContext.CurrentAvatarId;
                if (requestAvatarId.HasValue && requestAvatarId.Value != Guid.Empty)
                    return new Avatar() { Id = requestAvatarId.Value };
                // Fallback to static for non-API callers (e.g. CLI, desktop)
                if (_loggedInAvatar == null && !string.IsNullOrEmpty(Instance.OASISDNA.OASIS.OASISSystemAccountId))
                    _loggedInAvatar = new Avatar() { Id = new Guid(Instance.OASISDNA.OASIS.OASISSystemAccountId) };
                return _loggedInAvatar;
            }
            set
            {
                _loggedInAvatar = value;
            }
        } 
        
        public static Dictionary<string, IAvatar> LoggedInAvatarSessions { get; set; }
        //public List<IOASISStorageProvider> OASISStorageProviders { get; set; }

        //TODO Implement this singleton pattern for other Managers...
        public static AvatarManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AvatarManager(ProviderManager.Instance.CurrentStorageProvider, ProviderManager.Instance.OASISDNA);

                return _instance;
            }
        }

        public ProviderManagerConfig Config
        {
            get
            {
                if (_config == null)
                    _config = new ProviderManagerConfig();

                return _config;
            }
        }

        //public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        //TODO: Not sure we want to pass the OASISDNA here?
        public AvatarManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        // TODO: Not sure if we want to move methods from the AvatarService in WebAPI here?
        // For integration with STAR and others like Unity can just call the REST API service?
        // What advantage is there to making it native through dll's? Would be slightly faster than having to make a HTTP request/response round trip...
        // BUT would STILL need to call out to a OASIS Storage Provider so depending if that was also running locally is how fast it would be...
        // For now just easier to call the REST API service from STAR... can come back to this later... :)
    }
}
