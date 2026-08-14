using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using BC = BCrypt.Net.BCrypt;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    [Obsolete("AvatarService is being phased out. Controllers should call AvatarManager directly.")]
    public partial class AvatarService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;
        private readonly OASISDNA _OASISDNA;

        public AvatarService(
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _OASISDNA = OASISBootLoader.OASISBootLoader.OASISDNA;
        }

        private AvatarManager AvatarManager => Program.AvatarManager;
        private SearchManager _searchManager = null;

        public SearchManager SearchManager
        {
            get
            {
                if (_searchManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _searchManager = new SearchManager(result.Result);
                }

                return _searchManager;
            }
        }

        // MIGRATED — AvatarController reads OASISDNA.OASIS.Terms directly (no service call needed)
        //public async Task<OASISResult<string>> GetTerms()
        //{
        //    return await Task.Run(() =>
        //    {
        //        var response = new OASISResult<string>();
        //        try
        //        {
        //            response.Result = _OASISDNA.OASIS.Terms;
        //        }
        //        catch (Exception e)
        //        {
        //            response.Exception = e;
        //            response.Message = e.Message;
        //            response.IsError = true;
        //            OASISErrorHandling.HandleError(ref response, e.Message);
        //        }

        //        return response;
        //    });
        //}

    }
}
