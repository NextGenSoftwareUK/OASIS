using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Services
{
    /// <summary>
    /// Service for generating viewing keys for private position tracking
    /// </summary>
    public class ViewingKeyService : IViewingKeyService
    {
        /// <summary>
        /// Generates a viewing key hash for an avatar
        /// Uses a combination of avatar ID and timestamp for uniqueness
        /// </summary>
        public async Task<OASISResult<string>> GenerateViewingKeyAsync(
            Guid avatarId, 
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<string>();

            try
            {
                // Generate viewing key from avatar ID and timestamp
                // In a production system, this would use proper Zcash viewing key generation
                var keyMaterial = $"{avatarId}_{DateTime.UtcNow:O}_{Guid.NewGuid()}";
                
                // Hash the viewing key for storage (never store plain viewing keys)
                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyMaterial));
                    var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    
                    result.Result = hashString;
                    result.IsError = false;
                    return result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error generating viewing key: {ex.Message}", ex);
                return result;
            }
        }
    }
}

