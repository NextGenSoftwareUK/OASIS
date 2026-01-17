using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages file operations including upload, download, storage, and metadata management
    /// </summary>
    public partial class FilesManager : OASISManager
    {
        private static FilesManager _instance;
        private readonly Dictionary<Guid, List<StoredFile>> _userFiles;
        private readonly Dictionary<string, byte[]> _fileStorage;

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
            _userFiles = new Dictionary<Guid, List<StoredFile>>();
            _fileStorage = new Dictionary<string, byte[]>();
        }

        /// <summary>
        /// Get all files stored for an avatar
        /// </summary>
        public async Task<OASISResult<List<StoredFile>>> GetAllFilesForAvatarAsync(Guid avatarId)
        {
            var result = new OASISResult<List<StoredFile>>();
            try
            {
                if (!_userFiles.ContainsKey(avatarId))
                {
                    _userFiles[avatarId] = new List<StoredFile>();
                }

                result.Result = _userFiles[avatarId];
                result.Message = "Files retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving files: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Upload a file for an avatar
        /// </summary>
        public async Task<OASISResult<StoredFile>> UploadFileAsync(Guid avatarId, string fileName, byte[] fileData, string contentType, Dictionary<string, object> metadata = null)
        {
            var result = new OASISResult<StoredFile>();
            try
            {
                var fileId = Guid.NewGuid();
                var storedFile = new StoredFile
                {
                    Id = fileId,
                    AvatarId = avatarId,
                    FileName = fileName,
                    ContentType = contentType,
                    Size = fileData.Length,
                    UploadedAt = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                // Store file data
                _fileStorage[fileId.ToString()] = fileData;

                // Add to user's file list
                if (!_userFiles.ContainsKey(avatarId))
                    _userFiles[avatarId] = new List<StoredFile>();

                _userFiles[avatarId].Add(storedFile);

                result.Result = storedFile;
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

        /// <summary>
        /// Download a file by ID
        /// </summary>
        public async Task<OASISResult<FileDownload>> DownloadFileAsync(Guid avatarId, Guid fileId)
        {
            var result = new OASISResult<FileDownload>();
            try
            {
                if (!_userFiles.ContainsKey(avatarId))
                {
                    result.IsError = true;
                    result.Message = "No files found for this avatar";
                    return result;
                }

                var file = _userFiles[avatarId].FirstOrDefault(f => f.Id == fileId);
                if (file == null)
                {
                    result.IsError = true;
                    result.Message = "File not found";
                    return result;
                }

                if (!_fileStorage.ContainsKey(fileId.ToString()))
                {
                    result.IsError = true;
                    result.Message = "File data not found";
                    return result;
                }

                var fileDownload = new FileDownload
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Data = _fileStorage[fileId.ToString()],
                    Size = file.Size
                };

                result.Result = fileDownload;
                result.Message = "File downloaded successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error downloading file: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        public async Task<OASISResult<bool>> DeleteFileAsync(Guid avatarId, Guid fileId)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_userFiles.ContainsKey(avatarId))
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "No files found for this avatar";
                    return result;
                }

                var file = _userFiles[avatarId].FirstOrDefault(f => f.Id == fileId);
                if (file == null)
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "File not found";
                    return result;
                }

                // Remove from user's file list
                _userFiles[avatarId].Remove(file);

                // Remove from storage
                if (_fileStorage.ContainsKey(fileId.ToString()))
                {
                    _fileStorage.Remove(fileId.ToString());
                }

                result.Result = true;
                result.Message = "File deleted successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error deleting file: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Get file metadata
        /// </summary>
        public async Task<OASISResult<StoredFile>> GetFileMetadataAsync(Guid avatarId, Guid fileId)
        {
            var result = new OASISResult<StoredFile>();
            try
            {
                if (!_userFiles.ContainsKey(avatarId))
                {
                    result.IsError = true;
                    result.Message = "No files found for this avatar";
                    return result;
                }

                var file = _userFiles[avatarId].FirstOrDefault(f => f.Id == fileId);
                if (file == null)
                {
                    result.IsError = true;
                    result.Message = "File not found";
                    return result;
                }

                result.Result = file;
                result.Message = "File metadata retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving file metadata: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Update file metadata
        /// </summary>
        public async Task<OASISResult<bool>> UpdateFileMetadataAsync(Guid avatarId, Guid fileId, Dictionary<string, object> metadata)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_userFiles.ContainsKey(avatarId))
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "No files found for this avatar";
                    return result;
                }

                var file = _userFiles[avatarId].FirstOrDefault(f => f.Id == fileId);
                if (file == null)
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "File not found";
                    return result;
                }

                file.Metadata = metadata;
                file.ModifiedAt = DateTime.UtcNow;

                result.Result = true;
                result.Message = "File metadata updated successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error updating file metadata: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
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
