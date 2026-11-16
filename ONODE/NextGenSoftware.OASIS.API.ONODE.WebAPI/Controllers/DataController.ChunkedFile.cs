using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class DataController
    {
        /// <summary>
        /// Saves one chunk of a large file and returns its id. Pass the ids, in order, to complete-chunked-file.
        /// </summary>
        [Authorize]
        [HttpPost("save-file-chunk")]
        public async Task<OASISHttpResponseMessage<Guid>> SaveFileChunk(SaveFileChunkRequest request)
        {
            if (request.Data == null || request.Data.Length == 0 || request.Data.Length > ChunkedFileManifest.MaxChunkBytes)
                return TestDataHelper.CreateErrorResponse<Guid>(
                    $"Chunk data must be between 1 byte and {ChunkedFileManifest.MaxChunkBytes} bytes.", Guid.Empty, HttpStatusCode.BadRequest);

            OASISConfigResult<Guid> configResult = ConfigureOASISEngine<Guid>(request);
            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            OASISResult<Guid> response = await HolonManager.SaveFileAsync(request.Data, AvatarId);
            ResetOASISSettings(request, configResult);

            return HttpResponseHelper.FormatResponse(response, HttpStatusCode.OK, request.ShowDetailedSettings);
        }

        /// <summary>
        /// Joins previously saved chunks into one file and returns the file's id (the manifest holon's id).
        /// Every chunk must have been saved by the caller.
        /// </summary>
        [Authorize]
        [HttpPost("complete-chunked-file")]
        public async Task<OASISHttpResponseMessage<Guid>> CompleteChunkedFile(CompleteChunkedFileRequest request)
        {
            var manifest = new ChunkedFileManifest
            {
                FileName = request.FileName,
                FileExtension = request.FileExtension,
                MimeType = request.MimeType,
                TotalSize = request.TotalSize,
                Sha256 = request.Sha256,
                ChunkIds = request.ChunkIds,
            };

            var invalid = manifest.Validate();
            if (invalid != null)
                return TestDataHelper.CreateErrorResponse<Guid>(invalid, Guid.Empty, HttpStatusCode.BadRequest);

            OASISConfigResult<Guid> configResult = ConfigureOASISEngine<Guid>(request);
            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            try
            {
                // A manifest grants read access to its chunks, so only the uploader's own chunks may be referenced.
                foreach (var chunkId in manifest.ChunkIds)
                {
                    var chunk = await HolonManager.LoadHolonAsync(chunkId);
                    if (chunk == null || chunk.IsError || chunk.Result == null || chunk.Result.CreatedByAvatarId != AvatarId)
                        return TestDataHelper.CreateErrorResponse<Guid>(
                            $"Chunk {chunkId} was not found or was not uploaded by you.", Guid.Empty, HttpStatusCode.BadRequest);
                }

                OASISResult<Guid> response = await HolonManager.SaveFileAsync(manifest.ToBytes(), AvatarId);
                return HttpResponseHelper.FormatResponse(response, HttpStatusCode.OK, request.ShowDetailedSettings);
            }
            finally
            {
                ResetOASISSettings(request, configResult);
            }
        }

        /// <summary>
        /// Describes a file. Chunked files return their chunk count (fetch each with load-file-chunk);
        /// ordinary files return their bytes inline.
        /// </summary>
        [Authorize]
        [HttpPost("load-file-info")]
        public async Task<OASISHttpResponseMessage<FileInfoResponse>> LoadFileInfo(LoadFileRequest request)
        {
            OASISConfigResult<FileInfoResponse> configResult = ConfigureOASISEngine<FileInfoResponse>(request);
            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            try
            {
                var denied = await CheckFileReadAccessAsync<FileInfoResponse>(request.Id);
                if (denied != null) return denied;

                var file = await HolonManager.LoadFileAsync(request.Id, AvatarId);
                var result = new OASISResult<FileInfoResponse> { IsError = file.IsError, Message = file.Message };

                if (!file.IsError && file.Result != null)
                {
                    var manifest = ChunkedFileManifest.TryParse(file.Result);
                    result.Result = manifest == null
                        ? new FileInfoResponse { IsChunked = false, TotalSize = file.Result.LongLength, Data = file.Result }
                        : new FileInfoResponse
                        {
                            IsChunked = true,
                            FileName = manifest.FileName,
                            FileExtension = manifest.FileExtension,
                            MimeType = manifest.MimeType,
                            TotalSize = manifest.TotalSize,
                            Sha256 = manifest.Sha256,
                            ChunkCount = manifest.ChunkIds.Count,
                        };
                }

                return HttpResponseHelper.FormatResponse(result, HttpStatusCode.OK, request.ShowDetailedSettings);
            }
            finally
            {
                ResetOASISSettings(request, configResult);
            }
        }

        /// <summary>Loads chunk <c>Index</c> (zero-based) of a chunked file. Access is checked on the file itself.</summary>
        [Authorize]
        [HttpPost("load-file-chunk")]
        public async Task<OASISHttpResponseMessage<byte[]>> LoadFileChunk(LoadFileChunkRequest request)
        {
            OASISConfigResult<byte[]> configResult = ConfigureOASISEngine<byte[]>(request);
            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            try
            {
                var denied = await CheckFileReadAccessAsync<byte[]>(request.Id);
                if (denied != null) return denied;

                var file = await HolonManager.LoadFileAsync(request.Id, AvatarId);
                if (file.IsError || file.Result == null)
                    return HttpResponseHelper.FormatResponse(file, HttpStatusCode.OK, request.ShowDetailedSettings);

                var manifest = ChunkedFileManifest.TryParse(file.Result);
                if (manifest == null)
                    return TestDataHelper.CreateErrorResponse<byte[]>("This file is not chunked; use load-file.", null, HttpStatusCode.BadRequest);
                if (request.Index < 0 || request.Index >= manifest.ChunkIds.Count)
                    return TestDataHelper.CreateErrorResponse<byte[]>("Chunk index is out of range.", null, HttpStatusCode.BadRequest);

                var chunk = await HolonManager.LoadFileAsync(manifest.ChunkIds[request.Index], AvatarId);
                return HttpResponseHelper.FormatResponse(chunk, HttpStatusCode.OK, request.ShowDetailedSettings);
            }
            finally
            {
                ResetOASISSettings(request, configResult);
            }
        }

        /// <summary>Returns a 404/403 response when the caller may not read the file, otherwise null. Wizards bypass.</summary>
        private async Task<OASISHttpResponseMessage<T>> CheckFileReadAccessAsync<T>(Guid fileId)
        {
            if (Avatar?.AvatarType?.Value == AvatarType.Wizard)
                return null;

            var holon = await HolonManager.LoadHolonAsync(fileId);
            if (holon == null || holon.IsError || holon.Result == null)
                return TestDataHelper.CreateErrorResponse<T>("File not found.", default, HttpStatusCode.NotFound);
            if (!HolonAccess.CanRead(holon.Result, AvatarId))
                return TestDataHelper.CreateErrorResponse<T>("Forbidden. You do not have permission to access this file.", default, HttpStatusCode.Forbidden);
            return null;
        }

        /// <summary>
        /// Rebuilds a chunked file in memory so older clients calling load-file still get the whole file.
        /// Returns the original result untouched when the bytes are not a manifest.
        /// </summary>
        private async Task<OASISResult<byte[]>> ExpandChunkedFileAsync(OASISResult<byte[]> file)
        {
            if (file == null || file.IsError || file.Result == null)
                return file;

            var manifest = ChunkedFileManifest.TryParse(file.Result);
            if (manifest == null)
                return file;

            using var assembled = new MemoryStream(checked((int)Math.Min(manifest.TotalSize, int.MaxValue)));
            foreach (var chunkId in manifest.ChunkIds)
            {
                var chunk = await HolonManager.LoadFileAsync(chunkId, AvatarId);
                if (chunk.IsError || chunk.Result == null)
                    return new OASISResult<byte[]> { IsError = true, Message = $"Chunk {chunkId} could not be loaded: {chunk.Message}" };
                assembled.Write(chunk.Result, 0, chunk.Result.Length);
            }

            var bytes = assembled.ToArray();
            if (!string.IsNullOrEmpty(manifest.Sha256) &&
                !string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), manifest.Sha256, StringComparison.OrdinalIgnoreCase))
                return new OASISResult<byte[]> { IsError = true, Message = "The reassembled file failed its checksum." };

            return new OASISResult<byte[]> { Result = bytes, Message = file.Message };
        }
    }
}
