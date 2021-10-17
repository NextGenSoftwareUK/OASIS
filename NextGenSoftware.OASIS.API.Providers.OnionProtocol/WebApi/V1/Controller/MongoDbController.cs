using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OASIS_Onion.WebApi.V1.Controller.Entity
{
    [Route("api/v1/MongoDb")]
    public class MongoDbController : Microsoft.AspNetCore.Mvc.Controller
    {
        private MongoDBOASIS _db = null;

        public MongoDbController(IAvatarRepository ar, IAvatarDetailRepository adr, IHolonRepository hr, ISearchDataRepository sdr, IMapper mapper)
        {
            _db = new MongoDBOASIS(ar, adr, hr, sdr, mapper);
        }

        [HttpGet]
        [Route("/LoadAllAvatarsAsync")]
        public async Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            return await _db.LoadAllAvatarsAsync();
        }

        [HttpGet]
        [Route("/LoadAllAvatars")]
        public IEnumerable<IAvatar> LoadAllAvatars()
        {
            return _db.LoadAllAvatars();
        }

        [HttpGet]
        [Route("/LoadAvatarByEmail")]
        public IAvatar LoadAvatarByEmail([FromBody] string avatarEmail)
        {
            return _db.LoadAvatarByEmail(avatarEmail);
        }

        [HttpGet]
        [Route("/LoadAvatarByUsername")]
        public IAvatar LoadAvatarByUsername([FromBody] string avatarUsername)
        {
            return _db.LoadAvatarByUsername(avatarUsername);
        }

        [HttpGet]
        [Route("/LoadAvatarAsync")]
        public async Task<IAvatar> LoadAvatarAsync([FromBody] string username)
        {
            return await _db.LoadAvatarAsync(username);
        }

        [HttpGet]
        [Route("/LoadAvatarByUsernameAsync")]
        public async Task<IAvatar> LoadAvatarByUsernameAsync([FromBody] string avatarUsername)
        {
            return await _db.LoadAvatarByUsernameAsync(avatarUsername);
        }

        [HttpGet]
        [Route("/LoadAvatar")]
        public IAvatar LoadAvatar([FromBody] string username)
        {
            return _db.LoadAvatar(username);
        }

        [HttpGet]
        [Route("/LoadAvatarAsync/{id}")]
        public async Task<IAvatar> LoadAvatarAsync(Guid id)
        {
            return await _db.LoadAvatarAsync(id);
        }

        [HttpGet]
        [Route("/LoadAvatarByEmailAsync")]
        public async Task<IAvatar> LoadAvatarByEmailAsync([FromBody] string avatarEmail)
        {
            return await _db.LoadAvatarByEmailAsync(avatarEmail);
        }

        [HttpGet]
        [Route("/LoadAvatar/{id}")]
        public IAvatar LoadAvatar(Guid id)
        {
            return _db.LoadAvatar(id);
        }

        [HttpGet]
        [Route("/LoadAvatarAsync")]
        public async Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            return await _db.LoadAvatarAsync(username, password);
        }

        [HttpGet]
        [Route("/LoadAvatar")]
        public IAvatar LoadAvatar(string username, string password)
        {
            return _db.LoadAvatar(username, password);
        }

        [HttpPost]
        [Route("/SaveAvatarAsync")]
        public async Task<IAvatar> SaveAvatarAsync([FromBody] Avatar avatar)
        {
            IAvatar avatar1 = avatar as IAvatar;
            return await _db.SaveAvatarAsync(avatar1);
        }

        [HttpPost]
        [Route("/SaveAvatar")]
        public IAvatar SaveAvatar([FromBody] Avatar avatar)
        {
            IAvatar avatar1 = avatar as IAvatar;
            return _db.SaveAvatar(avatar1);
        }

        [HttpDelete]
        [Route("/DeleteAvatarByUsername")]
        public bool DeleteAvatarByUsername(string avatarUsername, bool softDelete)
        {
            return _db.DeleteAvatarByUsername(avatarUsername, softDelete);
        }

        [HttpDelete]
        [Route("/DeleteAvatarAsync")]
        public async Task<bool> DeleteAvatarAsync(Guid id, bool softDelete)
        {
            return await _db.DeleteAvatarAsync(id, softDelete);
        }

        [HttpDelete]
        [Route("/DeleteAvatarByEmailAsync")]
        public async Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            return await _db.DeleteAvatarByEmailAsync(avatarEmail, softDelete);
        }

        [HttpDelete]
        [Route("/DeleteAvatarByUsernameAsync")]
        public async Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            return await _db.DeleteAvatarByUsernameAsync(avatarUsername, softDelete);
        }

        [HttpDelete]
        [Route("/DeleteAvatar")]
        public bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            return _db.DeleteAvatar(id, softDelete);
        }

        [HttpDelete]
        [Route("/DeleteAvatarByEmail")]
        public bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return _db.DeleteAvatarByEmail(avatarEmail, softDelete);
        }

        [HttpGet]
        [Route("/LoadAvatarForProviderKeyAsync")]
        public async Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
        {
            return await _db.LoadAvatarForProviderKeyAsync(providerKey);
        }

        [HttpGet]
        [Route("/LoadAvatarForProviderKey")]
        public IAvatar LoadAvatarForProviderKey(string providerKey)
        {
            return _db.LoadAvatarForProviderKey(providerKey);
        }

        [HttpDelete]
        [Route("/DeleteAvatar")]
        public bool DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return _db.DeleteAvatar(providerKey, softDelete);
        }

        [HttpDelete]
        [Route("/DeleteAvatarAsync")]
        public async Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await _db.DeleteAvatarAsync(providerKey, softDelete);
        }

        [HttpGet]
        [Route("/SearchAsync")]
        public async Task<ISearchResults> SearchAsync([FromBody] SearchParams searchTerm)
        {
            return await _db.SearchAsync(searchTerm);
        }

        [HttpGet]
        [Route("/LoadAvatarDetailByUsername")]
        public IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername)
        {
            return _db.LoadAvatarDetailByUsername(avatarUsername);
        }

        [HttpGet]
        [Route("/LoadAvatarDetailAsync")]
        public Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
        {
            return _db.LoadAvatarDetailAsync(id);
        }

        [HttpGet]
        [Route("/LoadAvatarDetailByUsernameAsync")]
        public async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername)
        {
            return await _db.LoadAvatarDetailByUsernameAsync(avatarUsername);
        }

        [HttpGet]
        [Route("/LoadAvatarDetailByEmailAsync")]
        public async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail)
        {
            return await _db.LoadAvatarDetailByEmailAsync(avatarEmail);
        }

        [HttpGet]
        [Route("/LoadAllAvatarDetailsAsync")]
        public async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
        {
            return await _db.LoadAllAvatarDetailsAsync();
        }

        [HttpGet]
        [Route("/LoadAvatarDetail")]
        public IAvatarDetail LoadAvatarDetail(Guid id)
        {
            return _db.LoadAvatarDetail(id);
        }

        [HttpGet]
        [Route("/LoadAvatarDetailByEmail")]
        public IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail)
        {
            return _db.LoadAvatarDetailByEmail(avatarEmail);
        }

        [HttpGet]
        [Route("/LoadAllAvatarDetails")]
        public IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
        {
            return _db.LoadAllAvatarDetails();
        }

        [HttpGet]
        [Route("/LoadHolonAsync")]
        public async Task<IHolon> LoadHolonAsync(Guid id)
        {
            return await _db.LoadHolonAsync(id);
        }

        [HttpGet]
        [Route("/LoadHolon")]
        public IHolon LoadHolon(Guid id)
        {
            return _db.LoadHolon(id);
        }

        [HttpGet]
        [Route("/LoadHolonAsync")]
        public async Task<IHolon> LoadHolonAsync(string providerKey)
        {
            return await _db.LoadHolonAsync(providerKey);
        }

        [HttpGet]
        [Route("/LoadHolon")]
        public IHolon LoadHolon(string providerKey)
        {
            return _db.LoadHolon(providerKey);
        }

        [HttpGet]
        [Route("/LoadHolonsForParentAsync")]
        public async Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All)
        {
            return await _db.LoadHolonsForParentAsync(id, type);
        }

        [HttpGet]
        [Route("/LoadHolonsForParent")]
        public IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
        {
            return _db.LoadHolonsForParent(id, type);
        }

        [HttpGet]
        [Route("/LoadHolonsForParentAsync")]
        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All)
        {
            return await _db.LoadHolonsForParentAsync(providerKey, type);
        }

        [HttpGet]
        [Route("/LoadHolonsForParent")]
        public IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
        {
            return _db.LoadHolonsForParent(providerKey, type);
        }

        [HttpGet]
        [Route("/LoadAllHolonsAsync")]
        public async Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.All)
        {
            return await _db.LoadAllHolonsAsync(type);
        }

        [HttpGet]
        [Route("/LoadAllHolons")]
        public IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.All)
        {
            return _db.LoadAllHolons(type);
        }

        [HttpPost]
        [Route("/SaveHolonAsync")]
        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true)
        {
            return await _db.SaveHolonAsync(holon, saveChildrenRecursive);
        }

        [HttpPost]
        [Route("/SaveHolon")]
        public OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true)
        {
            return _db.SaveHolon(holon, saveChildrenRecursive);
        }

        [HttpPost]
        [Route("/SaveHolons")]
        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
        {
            return _db.SaveHolons(holons, saveChildrenRecursive);
        }

        [HttpPost]
        [Route("/SaveHolonsAsync")]
        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
        {
            return await _db.SaveHolonsAsync(holons, saveChildrenRecursive);
        }

        [HttpDelete]
        [Route("/DeleteHolon")]
        public bool DeleteHolon(Guid id, bool softDelete = true)
        {
            return _db.DeleteHolon(id, softDelete);
        }

        [HttpDelete]
        [Route("/DeleteHolonAsync")]
        public async Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            return await _db.DeleteHolonAsync(id, softDelete);
        }

        [HttpDelete]
        [Route("/DeleteHolon")]
        public bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            return _db.DeleteHolon(providerKey, softDelete);
        }

        [HttpDelete]
        [Route("/DeleteHolonAsync")]
        public async Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            return await _db.DeleteHolonAsync(providerKey, softDelete);
        }
    }
}