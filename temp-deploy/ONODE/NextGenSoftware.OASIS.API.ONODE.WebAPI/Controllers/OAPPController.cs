//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.Common;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    /// <summary>
//    /// OAPP (OASIS Application) management endpoints for creating, managing, and deploying OASIS applications.
//    /// Provides comprehensive OAPP functionality including installation, configuration, and lifecycle management.
//    /// </summary>
//    [ApiController]
//    [Route("api/oapp")]
//    public class OAPPController : OASISControllerBase
//    {
//        public OAPPController()
//        {
//        }

//        /// <summary>
//        /// Get's all OApp's installed for the current logged in avatar
//        /// </summary>
//        /// <returns>List of installed OAPPs</returns>
//        [Authorize]
//        [HttpGet("installed")]
//        public async Task<OASISResult<List<OAPPInfo>>> GetAllOAPPsInstalledForCurrentLoggedInAvatar()
//        {
//            try
//            {
//                // TODO: Implement actual OAPP retrieval logic
//                var oapps = new List<OAPPInfo>();
                
//                var result = new OASISResult<List<OAPPInfo>>
//                {
//                    Result = oapps,
//                    IsError = false,
//                    Message = "OAPPs retrieved successfully"
//                };
//                return result;
//            }
//            catch (Exception ex)
//            {
//                return new OASISResult<List<OAPPInfo>>
//                {
//                    IsError = true,
//                    Message = $"Error retrieving OAPPs: {ex.Message}",
//                    Exception = ex
//                };
//            }
//        }

//        /// <summary>
//        /// Install a new OAPP
//        /// </summary>
//        /// <param name="oappRequest">OAPP installation request</param>
//        /// <returns>Installation result</returns>
//        [Authorize]
//        [HttpPost("install")]
//        public async Task<OASISResult<OAPPInfo>> InstallOAPP([FromBody] InstallOAPPRequest oappRequest)
//        {
//            try
//            {
//                // TODO: Implement actual OAPP installation logic
//                var oapp = new OAPPInfo
//                {
//                    Id = Guid.NewGuid(),
//                    Name = oappRequest.Name,
//                    Version = oappRequest.Version,
//                    InstalledAt = DateTime.UtcNow,
//                    IsActive = true
//                };

//                var result = new OASISResult<OAPPInfo>
//                {
//                    Result = oapp,
//                    IsError = false,
//                    Message = "OAPP installed successfully"
//                };
//                return result;
//            }
//            catch (Exception ex)
//            {
//                return new OASISResult<OAPPInfo>
//                {
//                    IsError = true,
//                    Message = $"Error installing OAPP: {ex.Message}",
//                    Exception = ex
//                };
//            }
//        }

//        /// <summary>
//        /// Uninstall an OAPP
//        /// </summary>
//        /// <param name="oappId">OAPP ID to uninstall</param>
//        /// <returns>Uninstallation result</returns>
//        [Authorize]
//        [HttpDelete("{oappId}")]
//        public async Task<OASISResult<bool>> UninstallOAPP(Guid oappId)
//        {
//            try
//            {
//                // TODO: Implement actual OAPP uninstallation logic
//                var result = new OASISResult<bool>
//                {
//                    Result = true,
//                    IsError = false,
//                    Message = "OAPP uninstalled successfully"
//                };
//                return result;
//            }
//            catch (Exception ex)
//            {
//                return new OASISResult<bool>
//                {
//                    IsError = true,
//                    Message = $"Error uninstalling OAPP: {ex.Message}",
//                    Exception = ex
//                };
//            }
//        }

//        /// <summary>
//        /// Update an OAPP
//        /// </summary>
//        /// <param name="oappId">OAPP ID to update</param>
//        /// <param name="updateRequest">Update request</param>
//        /// <returns>Update result</returns>
//        [Authorize]
//        [HttpPut("{oappId}")]
//        public async Task<OASISResult<OAPPInfo>> UpdateOAPP(Guid oappId, [FromBody] UpdateOAPPRequest updateRequest)
//        {
//            try
//            {
//                // TODO: Implement actual OAPP update logic
//                var oapp = new OAPPInfo
//                {
//                    Id = oappId,
//                    Name = updateRequest.Name,
//                    Version = updateRequest.Version,
//                    UpdatedAt = DateTime.UtcNow
//                };

//                var result = new OASISResult<OAPPInfo>
//                {
//                    Result = oapp,
//                    IsError = false,
//                    Message = "OAPP updated successfully"
//                };
//                return result;
//            }
//            catch (Exception ex)
//            {
//                return new OASISResult<OAPPInfo>
//                {
//                    IsError = true,
//                    Message = $"Error updating OAPP: {ex.Message}",
//                    Exception = ex
//                };
//            }
//        }

//        /// <summary>
//        /// Get OAPP statistics
//        /// </summary>
//        /// <returns>OAPP statistics</returns>
//        [Authorize]
//        [HttpGet("stats")]
//        public async Task<OASISResult<Dictionary<string, object>>> GetOAPPStats()
//        {
//            try
//            {
//                // TODO: Implement actual OAPP statistics logic
//                var stats = new Dictionary<string, object>
//                {
//                    ["totalOAPPs"] = 0,
//                    ["activeOAPPs"] = 0,
//                    ["inactiveOAPPs"] = 0,
//                    ["totalInstalls"] = 0
//                };

//                var result = new OASISResult<Dictionary<string, object>>
//                {
//                    Result = stats,
//                    IsError = false,
//                    Message = "OAPP statistics retrieved successfully"
//                };
//                return result;
//            }
//            catch (Exception ex)
//            {
//                return new OASISResult<Dictionary<string, object>>
//                {
//                    IsError = true,
//                    Message = $"Error retrieving OAPP statistics: {ex.Message}",
//                    Exception = ex
//                };
//            }
//        }
//    }

//    /// <summary>
//    /// OAPP information model
//    /// </summary>
//    public class OAPPInfo
//    {
//        public Guid Id { get; set; }
//        public string Name { get; set; }
//        public string Version { get; set; }
//        public DateTime InstalledAt { get; set; }
//        public DateTime? UpdatedAt { get; set; }
//        public bool IsActive { get; set; }
//    }

//    /// <summary>
//    /// Install OAPP request model
//    /// </summary>
//    public class InstallOAPPRequest
//    {
//        public string Name { get; set; }
//        public string Version { get; set; }
//        public string Source { get; set; }
//    }

//    /// <summary>
//    /// Update OAPP request model
//    /// </summary>
//    public class UpdateOAPPRequest
//    {
//        public string Name { get; set; }
//        public string Version { get; set; }
//    }
//}
