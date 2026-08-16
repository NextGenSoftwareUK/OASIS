using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/avatar")]
    [ApiController]
    public partial class AvatarProfileController : OASISControllerBase
    {
        private AvatarManager AvatarManager => Program.AvatarManager;
        private readonly ILogger<AvatarProfileController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly object StarLogLock = new object();

        public AvatarProfileController(ILogger<AvatarProfileController> logger, IConfiguration configuration, IWebHostEnvironment env)
        {
            _logger = logger;
            _configuration = configuration;
            _env = env;
        }

        private void StarLog(string message, LogLevel level = LogLevel.Information)
        {
            var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] [STAR] {message}";
            _logger.Log(level, "[STAR] {Message}", message);
            try
            {
                var dir = string.IsNullOrEmpty(_env?.ContentRootPath) ? AppContext.BaseDirectory : _env.ContentRootPath;
                if (string.IsNullOrEmpty(dir)) dir = System.IO.Directory.GetCurrentDirectory() ?? ".";
                var path = System.IO.Path.Combine(dir, "star_api.log");
                lock (StarLogLock)
                    System.IO.File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { }
        }


        /// <summary>
        /// Get's the terms &amp; services agreement for creating an avatar and joining the OASIS.
        /// </summary>
    }
}
