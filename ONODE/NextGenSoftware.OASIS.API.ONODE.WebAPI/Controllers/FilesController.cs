using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
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
        public async Task<OASISResult<StoredFile>> UploadFile(string fileName, [FromBody] byte[] fileData, string contentType, [FromForm] Dictionary<string, object> metadata = null)
        {
            if (fileData == null || fileData.Length == 0)
                return new OASISResult<StoredFile> { IsError = true, Message = "The request body is required. Please provide the file data (byte array)." };
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
            if (metadata == null)
                return new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON object with metadata key-value pairs." };
            return await FilesManager.Instance.UpdateFileMetadataAsync(AvatarId, fileId, metadata);
        }

        /// <summary>
        /// Upload a file to IPFS via Pinata (multipart/form-data).
        /// Returns the IPFS hash/URL that can be used for NFT metadata.
        /// </summary>
        [Authorize]
        [HttpPost("upload")]
        public async Task<OASISResult<string>> UploadFileToIPFS(IFormFile file, string provider = "PinataOASIS")
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (file == null || file.Length == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "No file provided");
                    return result;
                }

                // Read file data
                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                // Use PinataOASIS provider
                if (provider == "PinataOASIS" || string.IsNullOrEmpty(provider))
                {
                    PinataOASIS pinata = new PinataOASIS();
                    
                    // Activate provider if not already activated
                    if (!pinata.IsProviderActivated)
                    {
                        var activateResult = pinata.ActivateProvider();
                        if (activateResult.IsError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Failed to activate Pinata provider: {activateResult.Message}");
                            return result;
                        }
                    }

                    // Upload to Pinata/IPFS
                    var uploadResult = await pinata.UploadFileToPinataAsync(fileData, file.FileName, file.ContentType);
                    
                    if (uploadResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, uploadResult.Message);
                        return result;
                    }

                    // Return IPFS URL (using Pinata gateway)
                    string ipfsHash = uploadResult.Result;
                    string ipfsUrl = $"https://gateway.pinata.cloud/ipfs/{ipfsHash}";
                    result.Result = ipfsUrl;
                    result.Message = "File uploaded to IPFS successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Provider '{provider}' is not yet supported. Please use 'PinataOASIS'.");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error uploading file: {ex.Message}");
            }

            return result;
        }
    }
}

