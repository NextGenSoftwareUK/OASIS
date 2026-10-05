using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.Storage
{
    [Route("api/pinata")]
    [ApiController]
    [Authorize]
    public class PinataController : OASISControllerBase
    {
        private PinataOASIS _pinata;

        private PinataOASIS Pinata
        {
            get
            {
                if (_pinata == null)
                {
                    _pinata = new PinataOASIS(OASISBootLoader.OASISBootLoader.OASISDNA);
                    if (!_pinata.IsProviderActivated)
                    {
                        _pinata.ActivateProvider();
                    }
                }

                return _pinata;
            }
        }

        [HttpPost("upload-file")]
        public async Task<OASISResult<string>> UploadFile([FromBody] UploadFileRequest request)
        {
            var result = new OASISResult<string>();

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Base64))
                {
                    result.Message = "Invalid request body.";
                    result.IsError = true;
                    return result;
                }

                var data = Convert.FromBase64String(request.Base64);
                var fileName = string.IsNullOrWhiteSpace(request.FileName) ? $"upload_{Guid.NewGuid()}" : request.FileName;

                var upload = await Pinata.UploadFileToPinataAsync(data, fileName, request.ContentType ?? "application/octet-stream");
                if (upload.IsError)
                {
                    return upload;
                }

                result.Result = Pinata.GetFileUrl(upload.Result);
                result.Message = "File uploaded to Pinata successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
            }

            return result;
        }

        [HttpPost("upload-json")]
        public async Task<OASISResult<string>> UploadJson([FromBody] UploadJsonRequest request)
        {
            var result = new OASISResult<string>();

            try
            {
                if (request?.Content == null)
                {
                    result.Message = "Invalid request body.";
                    result.IsError = true;
                    return result;
                }

                var upload = await Pinata.UploadJsonToPinataAsync(request.Content, request.Name);
                if (upload.IsError)
                {
                    return upload;
                }

                result.Result = Pinata.GetFileUrl(upload.Result);
                result.Message = "JSON uploaded to Pinata successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
            }

            return result;
        }

        public class UploadFileRequest
        {
            public string Base64 { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
        }

        public class UploadJsonRequest
        {
            public object Content { get; set; }
            public string Name { get; set; }
        }
    }
}
