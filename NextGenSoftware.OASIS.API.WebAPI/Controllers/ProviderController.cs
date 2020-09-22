using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Accounts;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProviderController : OASISControllerBase
    {
        public ProviderController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {

        }

        [Authorize(Role.User)]
        [HttpGet]
        public ActionResult<IEnumerable<IOASISProvider>> GetAll()
        {
            return Ok(ProviderManager.GetAllProviders());
        }

        [Authorize(Role.User)]
        [HttpGet]
        public ActionResult<IOASISProvider> SetCurrentStorageProvider(IOASISProvider provider)
        {
            return Ok(ProviderManager.SetAndActivateCurrentStorageProvider(provider));
        }
    }
}
