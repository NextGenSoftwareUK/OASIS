using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a string value
    /// </summary>
    Task<string> EncryptAsync(string plainText);
    
    /// <summary>
    /// Decrypts an encrypted string value
    /// </summary>
    Task<string> DecryptAsync(string encryptedText);
}




