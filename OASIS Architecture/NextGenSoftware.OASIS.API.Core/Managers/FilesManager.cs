using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class FilesManager : OASISManager
    {
        private const string FileTypeKey = "oasisFileType";
        private const string FileTypeValue = "file";

        private static FilesManager _instance;

        public static FilesManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FilesManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public FilesManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
        }

        public async Task<OASISResult<List<StoredFile>>> GetAllFilesForAvatarAsync(Guid avatarId)
        {
            var result = new OASISResult<List<StoredFile>>();
            try
            {
                var holonsResult = await HolonManager.Instance.LoadHolonsByMetaDataAsync("CreatedByAvatarId", avatarId.ToString());
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    result.Result = new List<StoredFile>();
                    result.Message = "No files found.";
                    return result;
                }

                result.Result = holonsResult.Result
                    .Where(h => h.MetaData != null &&
                                h.MetaData.ContainsKey(FileTypeKey) &&
                                h.MetaData[FileTypeKey]?.ToString() == FileTypeValue &&
                                !h.IsDeleted)
                    .Select(HolonToStoredFile)
                    .ToList();

                result.Message = "Files retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving files: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<StoredFile>> UploadFileAsync(Guid avatarId, string fileName, byte[] fileData, string contentType, Dictionary<string, object> metadata = null)
        {
            var result = new OASISResult<StoredFile>();
            if (fileData == null)
            {
                result.IsError = true;
                result.Message = "The file data is required. Please provide a valid byte array.";
                return result;
            }
            try
            {
                var holon = new Holon
                {
                    Name = fileName,
                    MetaData = new Dictionary<string, object>
                    {
                        { FileTypeKey,    FileTypeValue },
                        { "fileName",     fileName },
                        { "contentType",  contentType },
                        { "size",         fileData.Length.ToString() },
                        { "uploadedAt",   DateTime.UtcNow.ToString("o") },
                        { "data",         fileData }
                    }
                };

                if (metadata != null)
                    foreach (var kv in metadata)
                        holon.MetaData[kv.Key] = kv.Value;

                var holonResult = await HolonManager.Instance.SaveHolonAsync(holon, avatarId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = $"Error uploading file: {holonResult.Message}";
                    return result;
                }

                result.Result = HolonToStoredFile(holonResult.Result);
                result.Message = "File uploaded successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error uploading file: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<FileDownload>> DownloadFileAsync(Guid avatarId, Guid fileId)
        {
            var result = new OASISResult<FileDownload>();
            try
            {
                var holonResult = await HolonManager.Instance.LoadHolonAsync(fileId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = holonResult.Message ?? "File not found";
                    return result;
                }

                var holon = holonResult.Result;
                if (holon.MetaData == null || !holon.MetaData.ContainsKey("data"))
                {
                    result.IsError = true;
                    result.Message = "File data not found";
                    return result;
                }

                byte[] data;
                var rawData = holon.MetaData["data"];
                if (rawData is byte[] bytes)
                    data = bytes;
                else if (rawData is string b64)
                    data = Convert.FromBase64String(b64);
                else
                {
                    result.IsError = true;
                    result.Message = "File data is in an unexpected format";
                    return result;
                }

                result.Result = new FileDownload
                {
                    FileName    = holon.MetaData.ContainsKey("fileName")    ? holon.MetaData["fileName"]?.ToString()    : holon.Name,
                    ContentType = holon.MetaData.ContainsKey("contentType") ? holon.MetaData["contentType"]?.ToString() : "application/octet-stream",
                    Data        = data,
                    Size        = data.Length
                };
                result.Message = "File downloaded successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error downloading file: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<bool>> DeleteFileAsync(Guid avatarId, Guid fileId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var deleteResult = await HolonManager.Instance.DeleteHolonAsync(fileId, avatarId);
                if (deleteResult.IsError)
                {
                    result.IsError = true;
                    result.Result  = false;
                    result.Message = deleteResult.Message ?? "Error deleting file";
                    return result;
                }
                result.Result  = true;
                result.Message = "File deleted successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result  = false;
                result.Message = $"Error deleting file: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<StoredFile>> GetFileMetadataAsync(Guid avatarId, Guid fileId)
        {
            var result = new OASISResult<StoredFile>();
            try
            {
                var holonResult = await HolonManager.Instance.LoadHolonAsync(fileId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = holonResult.Message ?? "File not found";
                    return result;
                }
                result.Result  = HolonToStoredFile(holonResult.Result);
                result.Message = "File metadata retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving file metadata: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<bool>> UpdateFileMetadataAsync(Guid avatarId, Guid fileId, Dictionary<string, object> metadata)
        {
            var result = new OASISResult<bool>();
            if (metadata == null)
            {
                result.IsError = true;
                result.Result  = false;
                result.Message = "The metadata is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
            try
            {
                var holonResult = await HolonManager.Instance.LoadHolonAsync(fileId);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    result.IsError = true;
                    result.Result  = false;
                    result.Message = holonResult.Message ?? "File not found";
                    return result;
                }

                var holon = holonResult.Result;
                foreach (var kv in metadata)
                    holon.MetaData[kv.Key] = kv.Value;

                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon, avatarId);
                result.IsError = saveResult.IsError;
                result.Result  = !saveResult.IsError;
                result.Message = saveResult.IsError ? saveResult.Message : "File metadata updated successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result  = false;
                result.Message = $"Error updating file metadata: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        private static StoredFile HolonToStoredFile(IHolon holon)
        {
            var meta = holon.MetaData ?? new Dictionary<string, object>();

            long size = 0;
            if (meta.ContainsKey("size") && long.TryParse(meta["size"]?.ToString(), out var s))
                size = s;

            DateTime uploadedAt = holon.CreatedDate == DateTime.MinValue ? DateTime.MinValue : holon.CreatedDate;
            if (meta.ContainsKey("uploadedAt") && DateTime.TryParse(meta["uploadedAt"]?.ToString(), out var d))
                uploadedAt = d;

            return new StoredFile
            {
                Id          = holon.Id,
                AvatarId    = holon.CreatedByAvatarId,
                FileName    = meta.ContainsKey("fileName")    ? meta["fileName"]?.ToString()    : holon.Name,
                ContentType = meta.ContainsKey("contentType") ? meta["contentType"]?.ToString() : "",
                Size        = size,
                UploadedAt  = uploadedAt,
                ModifiedAt  = holon.ModifiedDate,
                Metadata    = meta.Where(kv => kv.Key != "data").ToDictionary(kv => kv.Key, kv => kv.Value)
            };
        }
    }

    public class StoredFile
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class FileDownload
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
        public long Size { get; set; }
    }
}
