//using System.Text.Json;
//using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS
{
    public partial class LocalFileOASIS : OASISStorageProviderBase, IOASISLocalStorageProvider
    {
        private static readonly JsonSerializerSettings AvatarDeserializeSettings = new JsonSerializerSettings
        {
            Converters = { new ProviderWalletsJsonConverter() }
        };
        //private string _filePath = "wallets.json";
        private string _filePath = "";
        private string _basePath = "";
        private string _avatarFolderPath = "";
        private string _avatarDetailFolderPath = "";
        private string _holonDirectory = "";

        public event EventDelegates.StorageProviderError OnStorageProviderError;

        public LocalFileOASIS(string filePath = "")
        {
            this.ProviderName = "LocalFileOASIS";
            this.ProviderDescription = "LocalFile Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LocalFileOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocal);

            if (!string.IsNullOrEmpty(filePath))
                _filePath = filePath;

            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OASIS", "LocalFileOASIS");
            _avatarFolderPath = Path.Combine(_basePath, "Avatars");
            _avatarDetailFolderPath = Path.Combine(_basePath, "AvatarDetails");
            _holonDirectory = Path.Combine(_basePath, "Holons");
            
            // Ensure directories exist
            if (!Directory.Exists(_avatarFolderPath))
                Directory.CreateDirectory(_avatarFolderPath);
            if (!Directory.Exists(_avatarDetailFolderPath))
                Directory.CreateDirectory(_avatarDetailFolderPath);
            if (!Directory.Exists(_holonDirectory))
                Directory.CreateDirectory(_holonDirectory);
        }

    }
}
