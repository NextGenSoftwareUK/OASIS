//using System;
//using System.IO;
//using System.Data;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Text;
//using System.Linq;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using MailKit.Search;
//using Ipfs.Http;
//using Ipfs.Engine;
//using Ipfs;
//using NextGenSoftware.OASIS.API.Core;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Helpers;

//namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS
//{
//    public class IPFSOASIS : OASISStorageBase, IOASISStorage, IOASISNET
//    {
//        public string HostURI { get; set; }
//        public IpfsClient IPFSClient;
//        public IpfsEngine IPFSEngine; //= new IpfsEngine();
//        private List<IAvatar> AvatarsList;
//        private List<IAvatarDetail> AvatarsDetailsList;
//        private List<IHolon> HolonsList;
//        private string avatarFileAddress;
//        private string holonFileAddress;
//        private string avatarDetailsFileAddress;
//        private Dictionary<Guid, string> _idLookup = new Dictionary<Guid, string>();
//        private string _idLookUpIPFSAddress = "";

//        public IPFSOASIS(string hostURI, string idLookUpIPFSAddress)
//        {
//            this.ProviderName = "IPFSOASIS";
//            this.ProviderDescription = "IPFS Provider";
//            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.IPFSOASIS);
//            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
//            this.HostURI = hostURI;

//            //TODO: Load from OASISDNA.json config file under IPFS section or another local JSON file...
//            _idLookUpIPFSAddress = idLookUpIPFSAddress;

//            AvatarsList = new List<IAvatar>();
//            AvatarsDetailsList = new List<IAvatarDetail>();
//            HolonsList = new List<IHolon>();
//        }

//        public override void ActivateProvider()
//        {
//            IPFSClient = new IpfsClient(HostURI);

//            if (!string.IsNullOrEmpty(_idLookUpIPFSAddress))
//            {
//                string json =  LoadStringToJson(_idLookUpIPFSAddress).Result;
//                _idLookup = JArray.Parse(json).ToObject<Dictionary<Guid, string>>();
//            }

//            base.ActivateProvider();
//        }

//        public override void DeActivateProvider()
//        {
//            IPFSClient.ShutdownAsync();
//            IPFSClient = null;
//            base.DeActivateProvider();
//        }


//        public async Task<string> LoadFileToJson(string address)
//        {
//            using (var stream = await IPFSClient.FileSystem.ReadFileAsync(address))
//            {
//                using (var ms = new MemoryStream())
//                {
//                    stream.CopyTo(ms);
//                    ms.ToArray();
//                    return Encoding.ASCII.GetString(ms.ToArray());
//                }
//            }
//        }
//        public async Task<string> LoadStringToJson(string address)
//        {

//            string text = await IPFSClient.FileSystem.ReadAllTextAsync((Cid)address);

//            return text;
//        }


//        public async Task<string> SaveJsonToFile<T>(List<T> list)
//        {
//            string json = JsonConvert.SerializeObject(list);

//            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);
//            return (string)fsn.Id;
//        }

//        public async Task<string> SaveAvatarToFile<T>(IAvatar avatar)
//        {
//            //If we have a previous version of this avatar saved, then add a pointer back to the previous version.
//            if (_idLookup.ContainsKey(avatar.Id))
//                avatar.PreviousVersionProviderKey[Core.Enums.ProviderType.IPFSOASIS] = _idLookup[avatar.Id];

//            string json = JsonConvert.SerializeObject(avatar);
//            var fsn = await IPFSClient.FileSystem.AddTextAsync(json);

//            _idLookup[avatar.Id] = fsn.Id;
//            json = JsonConvert.SerializeObject(_idLookup);
//            _idLookUpIPFSAddress = await IPFSClient.FileSystem.AddTextAsync(json);

//            //TODO: Store the _idLookUpIPFSAddress in OASISDNA.json config file under IPFS section.
//            //      Or in another local JSON file...

//            return (string)fsn.Id;
//        }

//        public async Task<string> SaveTextToFile(string text)
//        {

//            var fsn = await IPFSClient.FileSystem.AddTextAsync(text);
//            return (string)fsn.Id;
//        }


//        public override IAvatar LoadAvatar(string username, string password)
//        {
//            return LoadAvatarAsync(username, password).Result;
//        }

//        public override IAvatar SaveAvatar(IAvatar Avatar)
//        {
//            return SaveAvatarAsync(Avatar).Result;
//        }

//        //public override async Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
//        //{
//        //    if (AvatarsList == null)
//        //        AvatarsList = new List<IAvatar>();

//        //    AvatarsList.Add(Avatar);


//        //    avatarFileAddress = await SaveJsonToFile<IAvatar>(AvatarsList);

//        //    return Avatar;
//        //}

//        public override async Task<IAvatar> SaveAvatarAsync(IAvatar avatar)
//        {
//            if (AvatarsList == null)
//                AvatarsList = new List<IAvatar>();

//            AvatarsList.Add(avatar);


//            avatarFileAddress = await SaveJsonToFile<IAvatar>(AvatarsList);
//            avatar.ProviderKey[Core.Enums.ProviderType.IPFSOASIS] = avatarDetailsFileAddress;
//            await SaveJsonToFile<IAvatar>(AvatarsList);

//            return avatar;
//        }

//        public override IHolon SaveHolon(IHolon holon)
//        {
//            return SaveHolonAsync(holon).Result;
//        }

//        public override async Task<IHolon> SaveHolonAsync(IHolon holon)
//        {
//            if (HolonsList == null)
//                HolonsList = new List<IHolon>();

//            HolonsList.Add(holon);

//            holonFileAddress = await SaveJsonToFile<IHolon>(HolonsList);

//            return holon;
//        }

//        public override IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons)
//        {
//            return SaveHolonsAsync(holons).Result;
//        }

//        public override async Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> holons)
//        {
//            if (HolonsList == null)
//                HolonsList = new List<IHolon>();

//            HolonsList.AddRange(holons);

//            holonFileAddress = await SaveJsonToFile<IHolon>(HolonsList);

//            return holons;
//        }

//        public override async Task<ISearchResults> SearchAsync(ISearchParams searchTerm)
//        {
//            ISearchResults result = (ISearchResults)new SearchResults();

//            IEnumerable<IAvatar> Avatars = await LoadAllAvatarsAsync();

//            IEnumerable<IHolon> Holons = await LoadAllHolonsAsync();

//            Avatars = Avatars.Where(a => a.Name.Contains(searchTerm.SearchQuery) | a.Description.Contains(searchTerm.SearchQuery)).ToList();
//            Holons = Holons.Where(h => h.Name.Contains(searchTerm.SearchQuery) | h.Description.Contains(searchTerm.SearchQuery)).ToList();

//            foreach (var h in Holons)
//                result.SearchResultHolons.Add((Holon)h);

//            foreach (var ava in Avatars)
//                result.SearchResultHolons.Add((Holon)ava);

//            return result;
//        }

//        public override IHolon LoadHolon(Guid id)
//        {
//            return LoadHolonAsync(id).Result;
//        }

//        public override async Task<IHolon> LoadHolonAsync(Guid id)
//        {
//            string json = "";

//            json = await LoadStringToJson(holonFileAddress);

//            HolonsList = JArray.Parse(json).ToObject<List<Holon>>().ToList<IHolon>();

//            IHolon holon = HolonsList.Where(a => a.Id == id).FirstOrDefault();

//            return holon;
//        }

//        public override IHolon LoadHolon(string providerKey)
//        {
//            return LoadHolonAsync(providerKey).Result;
//        }

//        public override async Task<IHolon> LoadHolonAsync(string providerKey)
//        {
//            string json = "";

//            json = await LoadStringToJson(holonFileAddress);

//            HolonsList = JArray.Parse(json).ToObject<List<Holon>>().ToList<IHolon>();

//            IHolon holon = HolonsList.Where(a => a.ProviderKey.Where(b => b.Value == providerKey).Any()).FirstOrDefault();

//            return holon;
//        }

//        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
//        {
//            return LoadHolonsForParentAsync(id, type).Result;
//        }

//        public override async Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All)
//        {
//            string json = "";

//            json = await LoadStringToJson(holonFileAddress);

//            HolonsList = JArray.Parse(json).ToObject<List<Holon>>().ToList<IHolon>();

//            IEnumerable<IHolon> holons = HolonsList.Where(a => a.ParentHolonId == id && a.HolonType == type).AsEnumerable();

//            return holons;
//        }

//        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
//        {
//            return LoadHolonsForParentAsync(providerKey, type).Result.Result;
//        }

//        public override bool DeleteAvatar(Guid id, bool softDelete = true)
//        {
//            return DeleteAvatarAsync(id, softDelete).Result;
//        }

//        public override bool DeleteAvatar(string providerKey, bool softDelete = true)
//        {
//            return DeleteAvatarAsync(providerKey, softDelete).Result;
//        }

//        public override async Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
//        {
//            string json = "";
//            try
//            {
//                json = await LoadStringToJson(avatarFileAddress);

//                AvatarsList = JArray.Parse(json).ToObject<List<Avatar>>().ToList<IAvatar>();
//                if (softDelete)
//                {
//                    AvatarsList.Where(a => a.Id == id).FirstOrDefault().PreviousVersionProviderKey.Add(Core.Enums.ProviderType.IPFSOASIS, avatarFileAddress);
//                    AvatarsList.Where(a => a.Id == id).FirstOrDefault().IsActive = false;
//                    AvatarsList.Where(a => a.Id == id).FirstOrDefault().DeletedDate = DateTime.Now;
//                }
//                else
//                {
//                    var avatar = AvatarsList.Where(a => a.Id == id).FirstOrDefault();
//                    AvatarsList.Remove(avatar);
//                }
//                avatarFileAddress = await SaveJsonToFile<IAvatar>(AvatarsList);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public override async Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
//        {
//            string json = "";
//            try
//            {
//                json = await LoadStringToJson(avatarFileAddress);

//                AvatarsList = JArray.Parse(json).ToObject<List<Avatar>>().ToList<IAvatar>();

//                if (softDelete)
//                {
//                    AvatarsList.Where(a => a.ProviderKey.Where(b => b.Value == providerKey).Any()).FirstOrDefault().PreviousVersionProviderKey.Add(Core.Enums.ProviderType.IPFSOASIS, avatarFileAddress);
//                    AvatarsList.Where(a => a.ProviderKey.Where(b => b.Value == providerKey).Any()).FirstOrDefault().IsActive = false;
//                    AvatarsList.Where(a => a.ProviderKey.Where(b => b.Value == providerKey).Any()).FirstOrDefault().DeletedDate = DateTime.Now;
//                }
//                else
//                {
//                    var avatar = AvatarsList.Where(a => a.ProviderKey.Where(b => b.Value == providerKey).Any()).FirstOrDefault();
//                    AvatarsList.Remove(avatar);
//                }
//                avatarFileAddress = await SaveJsonToFile<IAvatar>(AvatarsList);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }
//        public override bool DeleteHolon(Guid id, bool softDelete = true)
//        {
//            return DeleteHolonAsync(id, softDelete).Result;
//        }

//        public override bool DeleteHolon(string providerKey, bool softDelete = true)
//        {
//            return DeleteHolonAsync(providerKey, softDelete).Result;
//        }

//        public override async Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
//        {
//            string json = "";
//            try
//            {
//                json = await LoadStringToJson(holonFileAddress);

//                HolonsList = JArray.Parse(json).ToObject<List<Holon>>().ToList<IHolon>();

//                if (softDelete)
//                {
//                    HolonsList.Where(a => a.Id == id).FirstOrDefault().PreviousVersionProviderKey.Add(Core.Enums.ProviderType.IPFSOASIS, holonFileAddress);
//                    HolonsList.Where(a => a.Id == id).FirstOrDefault().IsActive = false;
//                    HolonsList.Where(a => a.Id == id).FirstOrDefault().DeletedDate = DateTime.Now;

//                }
//                else
//                {
//                    var holon = HolonsList.Where(a => a.Id == id).FirstOrDefault();
//                    HolonsList.Remove(holon);
//                }
//                holonFileAddress = await SaveJsonToFile<IHolon>(HolonsList);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public override async Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
//        {
//            string json = "";
//            try
//            {
//                json = await LoadStringToJson(holonFileAddress);

//                HolonsList = JArray.Parse(json).ToObject<List<Holon>>().ToList<IHolon>();


//                if (softDelete)
//                {
//                    HolonsList.Where(a => a.ProviderKey.Where(b => b.Value == providerKey).Any()).FirstOrDefault().PreviousVersionProviderKey.Add(Core.Enums.ProviderType.IPFSOASIS, holonFileAddress);
//                    HolonsList.Where(a => a.ProviderKey.Where(b => b.Value == providerKey).Any()).FirstOrDefault().IsActive = false;
//                    HolonsList.Where(a => a.ProviderKey.Where(b => b.Value == providerKey).Any()).FirstOrDefault().DeletedDate = DateTime.Now;
//                }
//                else
//                {
//                    var holon = HolonsList.Where(a => a.ProviderKey.Where(b => b.Value == providerKey).Any()).FirstOrDefault();
//                    HolonsList.Remove(holon);
//                }
//                holonFileAddress = await SaveJsonToFile<IHolon>(HolonsList);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
//        {
//            return LoadAllHolons(Type);
//        }

//        public IEnumerable<IPlayer> GetPlayersNearMe()
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IAvatar> LoadAllAvatars()
//        {
//            return LoadAllAvatarsAsync().Result;
//        }

//        public override async Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
//        {
//            string json = "";

//            try
//            {
//                json = await LoadStringToJson(avatarFileAddress);
//            }
//            catch (Exception ex)
//            {

//            }
//            AvatarsList = JArray.Parse(json).ToObject<List<Avatar>>().ToList<IAvatar>();

//            return AvatarsList.AsEnumerable();

//        }

//        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.Holon)
//        {
//            return LoadAllHolonsAsync(type).Result;
//        }

//        public override async Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.Holon)
//        {
//            string json = "";

//            try
//            {
//                json = await LoadStringToJson(holonFileAddress);
//            }
//            catch (Exception ex)
//            {
//            }
//            HolonsList = JArray.Parse(json).ToObject<List<Holon>>().ToList<IHolon>();



//            return HolonsList.Where(a => a.HolonType == type).ToList();
//        }


//        public override IAvatar LoadAvatar(Guid Id)
//        {
//            return LoadAvatarAsync(Id).Result;
//        }

//        public override IAvatar LoadAvatar(string username)
//        {
//            return LoadAvatarAsync(username).Result;
//        }

//        public override async Task<IAvatar> LoadAvatarAsync(string providerKey)
//        {
//            string json = "";

//            json = await LoadStringToJson(avatarFileAddress);

//            AvatarsList = JArray.Parse(json).ToObject<List<Avatar>>().ToList<IAvatar>();

//            IAvatar avatar = AvatarsList.Where(a => a.ProviderKey.Where(b => b.Value == providerKey).Any()).FirstOrDefault();

//            return avatar;
//        }

//        public override async Task<IAvatar> LoadAvatarAsync(Guid Id)
//        {
//            string json = "";

//            json = await LoadStringToJson(avatarFileAddress);

//            AvatarsList = JArray.Parse(json).ToObject<List<Avatar>>().ToList<IAvatar>();

//            IAvatar avatar = AvatarsList.Where(a => a.Id == Id).FirstOrDefault();

//            return avatar;
//        }

//        public override async Task<IAvatar> LoadAvatarAsync(string username, string password)
//        {
//            string json = "";


//            json = await LoadStringToJson(avatarFileAddress);

//            AvatarsList = JArray.Parse(json).ToObject<List<Avatar>>().ToList<IAvatar>();



//            //    AvatarsList = JArray.Parse(json).ToObject<List<Avatar>>().ToList<IAvatar>();

//            IAvatar avatar = AvatarsList.Where(a => a.Username == username && a.Password == password).FirstOrDefault();

//            return avatar;
//        }

//        public override IAvatar LoadAvatarForProviderKey(string providerKey)
//        {
//            return LoadAvatarForProviderKeyAsync(providerKey).Result;
//        }

//        public override async Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
//        {
//            return await LoadAvatarAsync(providerKey);
//        }


//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All)
//        {
//            string json = "";
//            OASISResult<IEnumerable<IHolon>> res = new OASISResult<IEnumerable<IHolon>>();

//            json = await LoadStringToJson(holonFileAddress);

//            HolonsList = JArray.Parse(json).ToObject<List<Holon>>().ToList<IHolon>();

//            res.Result = HolonsList.Where(a => a.ProviderKey.Where(a => a.Value == providerKey).Any() && a.HolonType == type).ToList();

//            return res;
//        }

//        public override IAvatarDetail LoadAvatarDetail(Guid id)
//        {
//            return LoadAvatarDetailAsync(id).Result;
//        }

//        public override async Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
//        {
//            string json = "";

//            json = await LoadStringToJson(avatarDetailsFileAddress);

//            AvatarsDetailsList = (List<IAvatarDetail>)JsonConvert.DeserializeObject(json);

//            IAvatarDetail avatar = AvatarsDetailsList.Where(a => a.Id == id).FirstOrDefault();

//            return avatar;
//        }

//        public override IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
//        {
//            return LoadAllAvatarDetailsAsync().Result;
//        }

//        public override async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
//        {
//            string json = "";

//            json = await LoadStringToJson(avatarDetailsFileAddress);

//            AvatarsDetailsList = (List<IAvatarDetail>)JsonConvert.DeserializeObject(json);

//            return AvatarsDetailsList;
//        }

//        public override IAvatarDetail SaveAvatarDetail(IAvatarDetail Avatar)
//        {
//            return SaveAvatarDetailAsync(Avatar).Result;
//        }

//        public override async Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail Avatar)
//        {
//            AvatarsDetailsList.Add(Avatar);

//            avatarDetailsFileAddress = await SaveJsonToFile<IAvatarDetail>(AvatarsDetailsList);

//            return Avatar;
//        }

//        public override IAvatar LoadAvatarByUsername(string avatarUsername)
//        {
//            return LoadAvatarByUsernameAsync(avatarUsername).Result;
//        }

//        public override async Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail)
//        {
//            IEnumerable<IAvatar> Avatars = await LoadAllAvatarsAsync();

//            IAvatar avatar = Avatars.Where(a => a.Email == avatarEmail).FirstOrDefault();

//            return avatar;
//        }

//        public override async Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername)
//        {
//            IEnumerable<IAvatar> Avatars = await LoadAllAvatarsAsync();

//            IAvatar avatar = Avatars.Where(a => a.Username == avatarUsername).FirstOrDefault();

//            return avatar;
//        }

//        public override IAvatar LoadAvatarByEmail(string avatarEmail)
//        {
//            return LoadAvatarByEmailAsync(avatarEmail).Result;
//        }

//        public override IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail)
//        {
//            return LoadAvatarDetailByEmailAsync(avatarEmail).Result;
//        }

//        public override IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername)
//        {
//            return LoadAvatarDetailByEmailAsync(avatarUsername).Result;
//        }

//        public override async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername)
//        {
//            IEnumerable<IAvatarDetail> AvatarDetail = await LoadAllAvatarDetailsAsync();

//            IAvatarDetail avatarDetail = AvatarDetail.Where(a => a.Username == avatarUsername).FirstOrDefault();

//            return avatarDetail;
//        }

//        public override async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail)
//        {
//            IEnumerable<IAvatarDetail> AvatarDetail = await LoadAllAvatarDetailsAsync();

//            IAvatarDetail avatarDetail = AvatarDetail.Where(a => a.Email == avatarEmail).FirstOrDefault();

//            return avatarDetail;
//        }

//        public override bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
//        {
//            return DeleteAvatarByUsernameAsync(avatarEmail).Result;
//        }

//        public override bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
//        {
//            return DeleteAvatarByUsernameAsync(avatarUsername).Result;
//        }

//        public override async Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
//        {
//            string json = "";
//            try
//            {
//                json = await LoadStringToJson(avatarFileAddress);

//                AvatarsList = JArray.Parse(json).ToObject<List<Avatar>>().ToList<IAvatar>();
//                if (softDelete)
//                {
//                    AvatarsList.Where(a => a.Email == avatarEmail).FirstOrDefault().PreviousVersionProviderKey.Add(Core.Enums.ProviderType.IPFSOASIS, avatarFileAddress);
//                    AvatarsList.Where(a => a.Email == avatarEmail).FirstOrDefault().IsActive = false;
//                    AvatarsList.Where(a => a.Email == avatarEmail).FirstOrDefault().DeletedDate = DateTime.Now;

//                }
//                else
//                {
//                    var avatar = AvatarsList.Where(a => a.Email == avatarEmail).FirstOrDefault();
//                    AvatarsList.Remove(avatar);
//                }
//                avatarFileAddress = await SaveJsonToFile<IAvatar>(AvatarsList);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public override async Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
//        {
//            string json = "";
//            try
//            {
//                json = await LoadStringToJson(avatarFileAddress);

//                AvatarsList = JArray.Parse(json).ToObject<List<Avatar>>().ToList<IAvatar>();

//                if (softDelete)
//                {
//                    AvatarsList.Where(a => a.Username == avatarUsername).FirstOrDefault().PreviousVersionProviderKey.Add(Core.Enums.ProviderType.IPFSOASIS, avatarFileAddress);
//                    AvatarsList.Where(a => a.Username == avatarUsername).FirstOrDefault().IsActive = false;
//                    AvatarsList.Where(a => a.Username == avatarUsername).FirstOrDefault().DeletedDate = DateTime.Now;
//                }
//                else 
//                {
//                    var avatar = AvatarsList.Where(a => a.Username == avatarUsername).FirstOrDefault();
//                    AvatarsList.Remove(avatar);
//                }
//                avatarFileAddress = await SaveJsonToFile(AvatarsList);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }
//    }
//}
