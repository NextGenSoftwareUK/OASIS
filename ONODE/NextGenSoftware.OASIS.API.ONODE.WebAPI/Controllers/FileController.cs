using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/file")]
    public class FileController : OASISControllerBase
    {
        private readonly ILogger<FileController> _logger;

        public FileController(ILogger<FileController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Upload a file to a specified provider (IPFS, Pinata, Holochain, etc.)
        /// </summary>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status500InternalServerError)]
        public async Task<OASISResult<string>> UploadFileAsync(IFormFile file, [FromForm] string provider = "PinataOASIS")
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (file == null || file.Length == 0)
                {
                    result.IsError = true;
                    result.Message = "No file provided";
                    return result;
                }

                // Parse provider type
                if (!Enum.TryParse<ProviderType>(provider, out ProviderType providerType))
                {
                    result.IsError = true;
                    result.Message = $"Invalid provider type: {provider}";
                    return result;
                }

                // Read file data
                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                // Upload to provider based on type
                string fileUrl = null;
                string ipfsHash = null;

                switch (providerType)
                {
                    case ProviderType.PinataOASIS:
                        var pinataProvider = new PinataOASIS();
                        if (!pinataProvider.IsProviderActivated)
                        {
                            pinataProvider.ActivateProvider();
                        }
                        
                        if (pinataProvider.IsProviderActivated)
                        {
                            var uploadResult = await pinataProvider.UploadFileToPinataAsync(fileData, file.FileName, file.ContentType);
                            if (!uploadResult.IsError && !string.IsNullOrEmpty(uploadResult.Result))
                            {
                                ipfsHash = uploadResult.Result;
                                fileUrl = $"https://gateway.pinata.cloud/ipfs/{ipfsHash}";
                            }
                            else
                            {
                                result.IsError = true;
                                result.Message = uploadResult.Message ?? "Failed to upload file to Pinata";
                                return result;
                            }
                        }
                        else
                        {
                            result.IsError = true;
                            result.Message = "PinataOASIS provider failed to activate. Please check your Pinata API credentials in OASIS_DNA.json";
                            return result;
                        }
                        break;

                    case ProviderType.IPFSOASIS:
                    case ProviderType.HoloOASIS:
                    default:
                        result.IsError = true;
                        result.Message = $"Provider {provider} is not yet supported for file uploads. Please use PinataOASIS.";
                        return result;
                }

                result.Result = fileUrl ?? ipfsHash;
                result.Message = "File uploaded successfully";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                result.IsError = true;
                result.Message = $"An error occurred while uploading the file: {ex.Message}";
                result.Exception = ex;
                return result;
            }
        }
    }
}

