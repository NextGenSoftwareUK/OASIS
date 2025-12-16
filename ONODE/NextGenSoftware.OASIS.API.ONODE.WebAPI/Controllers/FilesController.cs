using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    // Original scaffold (kept for reference):
    //using System.Threading.Tasks;
    //using Microsoft.AspNetCore.Mvc;
    //using Microsoft.Extensions.Options;
    //using NextGenSoftware.OASIS.API.Core.Helpers;
    //using NextGenSoftware.OASIS.API.Core.Managers;
    //using NextGenSoftware.OASIS.API.DNA;
    //using NextGenSoftware.OASIS.Common;
    //using System;
    //using System.Collections.Generic;
    //
    //namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
    //{
    //    [ApiController]
    //    [Route("api/files")]
    //    public class FilesController : OASISControllerBase
    //    {
    //        /*
    //        OASISDNA _settings;
    //
    //        public FilesController(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
    //        {
    //            _settings = OASISSettings.Value;
    //        }*/
    //
    //        public FilesController()
    //        {
    //            
    //        }
    //
    //        /// <summary>
    //        /// Get's all files stored for the currently logged in avatar
    //        /// </summary>
    //        /// <returns></returns>
    //        [Authorize]
    //        [HttpGet("get-all-files-stored-for-current-logged-in-avatar")]
    //        public async Task<OASISResult<List<StoredFile>>> GetAllFilesStoredForCurrentLoggedInAvatar()
    //        {
    //            // Use FilesManager for business logic
    //            return await FilesManager.Instance.GetAllFilesForAvatarAsync(Avatar.Id);
    //        }
    //
    //        /// <summary>
    //        /// Upload a file for the current avatar
    //        /// </summary>
    //        /// <param name="fileName">Name of the file</param>
    //        /// <param name="fileData">File data as byte array</param>
    //        /// <param name="contentType">MIME type of the file</param>
    //        /// <param name="metadata">Additional metadata for the file</param>
    //        /// <returns></returns>
    //        [Authorize]
    //        [HttpPost("upload-file")]
    //        public async Task<OASISResult<StoredFile>> UploadFile(string fileName, byte[] fileData, string contentType, Dictionary<string, object> metadata = null)
    //        {
    //            // Use FilesManager for business logic
    //            return await FilesManager.Instance.UploadFileAsync(Avatar.Id, fileName, fileData, contentType, metadata);
    //        }
    //
    //        /// <summary>
    //        /// Download a file by ID
    //        /// </summary>
    //        /// <param name="fileId">ID of the file to download</param>
    //        /// <returns></returns>
    //        [Authorize]
    //        [HttpGet("download-file/{fileId}")]
    //        public async Task<OASISResult<FileDownload>> DownloadFile(Guid fileId)
    //        {
    //            // Use FilesManager for business logic
    //            return await FilesManager.Instance.DownloadFileAsync(Avatar.Id, fileId);
    //        }
    //
    //        /// <summary>
    //        /// Delete a file by ID
    //        /// </summary>
    //        /// <param name="fileId">ID of the file to delete</param>
    //        /// <returns></returns>
    //        [Authorize]
    //        [HttpDelete("delete-file/{fileId}")]
    //        public async Task<OASISResult<bool>> DeleteFile(Guid fileId)
    //        {
    //            // Use FilesManager for business logic
    //            return await FilesManager.Instance.DeleteFileAsync(Avatar.Id, fileId);
    //        }
    //
    //        /// <summary>
    //        /// Get file metadata by ID
    //        /// </summary>
    //        /// <param name="fileId">ID of the file</param>
    //        /// <returns></returns>
    //        [Authorize]
    //        [HttpGet("file-metadata/{fileId}")]
    //        public async Task<OASISResult<StoredFile>> GetFileMetadata(Guid fileId)
    //        {
    //            // Use FilesManager for business logic
    //            return await FilesManager.Instance.GetFileMetadataAsync(Avatar.Id, fileId);
    //        }
    //
    //        /// <summary>
    //        /// Update file metadata
    //        /// </summary>
    //        /// <param name="fileId">ID of the file</param>
    //        /// <param name="metadata">New metadata for the file</param>
    //        /// <returns></returns>
    //        [Authorize]
    //        [HttpPut("update-file-metadata/{fileId}")]
    //        public async Task<OASISResult<bool>> UpdateFileMetadata(Guid fileId, [FromBody] Dictionary<string, object> metadata)
    //        {
    //            // Use FilesManager for business logic
    //            return await FilesManager.Instance.UpdateFileMetadataAsync(Avatar.Id, fileId, metadata);
    //        }
    //    }
    //}

    [ApiController]
    [Route("api/files")]
    public class FilesController : OASISControllerBase
    {
        /// <summary>
        /// Get all files stored for the currently logged in avatar.
        /// </summary>
        [Authorize]
        [HttpGet("get-all-files-stored-for-current-logged-in-avatar")]
        public async Task<OASISResult<List<StoredFile>>> GetAllFilesStoredForCurrentLoggedInAvatar()
        {
            return await FilesManager.Instance.GetAllFilesForAvatarAsync(AvatarId);
        }

        /// <summary>
        /// Upload a file for the current avatar.
        /// </summary>
        [Authorize]
        [HttpPost("upload-file")]
        public async Task<OASISResult<StoredFile>> UploadFile(string fileName, byte[] fileData, string contentType, Dictionary<string, object> metadata = null)
        {
            return await FilesManager.Instance.UploadFileAsync(AvatarId, fileName, fileData, contentType, metadata);
        }

        /// <summary>
        /// Download a file by ID.
        /// </summary>
        [Authorize]
        [HttpGet("download-file/{fileId}")]
        public async Task<OASISResult<FileDownload>> DownloadFile(Guid fileId)
        {
            return await FilesManager.Instance.DownloadFileAsync(AvatarId, fileId);
        }

        /// <summary>
        /// Delete a file by ID.
        /// </summary>
        [Authorize]
        [HttpDelete("delete-file/{fileId}")]
        public async Task<OASISResult<bool>> DeleteFile(Guid fileId)
        {
            return await FilesManager.Instance.DeleteFileAsync(AvatarId, fileId);
        }

        /// <summary>
        /// Get file metadata by ID.
        /// </summary>
        [Authorize]
        [HttpGet("file-metadata/{fileId}")]
        public async Task<OASISResult<StoredFile>> GetFileMetadata(Guid fileId)
        {
            return await FilesManager.Instance.GetFileMetadataAsync(AvatarId, fileId);
        }

        /// <summary>
        /// Update file metadata.
        /// </summary>
        [Authorize]
        [HttpPut("update-file-metadata/{fileId}")]
        public async Task<OASISResult<bool>> UpdateFileMetadata(Guid fileId, [FromBody] Dictionary<string, object> metadata)
        {
            return await FilesManager.Instance.UpdateFileMetadataAsync(AvatarId, fileId, metadata);
        }
    }
}

