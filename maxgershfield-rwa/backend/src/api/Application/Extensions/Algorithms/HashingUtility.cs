namespace Application.Extensions.Algorithms;

/// <summary>
/// Provides utility methods for hashing algorithms.
/// This class includes methods to compute various hash values for strings, such as SHA256.
/// </summary>
public static class HashingUtility
{
    /// <summary>
    /// Computes the SHA256 hash of the given input string.
    /// </summary>
    /// <param name="input">The string input to hash.</param>
    /// <returns>The SHA256 hash as a hexadecimal string in lowercase.</returns>
    public static string ComputeSha256Hash(string input)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLower();
    }
}