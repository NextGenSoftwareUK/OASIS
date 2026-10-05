namespace Application.Extensions.CheckingUtility;

/// <summary>
/// Provides extension methods for checking and validating <see cref="RwaToken"/> objects.
/// </summary>
public static class RwaTokenChecking
{
    /// <summary>
    /// Checks if the properties of the given <see cref="RwaToken"/> match those of the <see cref="UpdateRwaTokenRequest"/>.
    /// </summary>
    /// <param name="rwaToken">The current <see cref="RwaToken"/> instance to compare.</param>
    /// <param name="request">The <see cref="UpdateRwaTokenRequest"/> containing updated properties.</param>
    /// <returns>
    /// <c>true</c> if all properties are equal (case-insensitive for strings, date-only for <see cref="DateTime"/>); 
    /// <c>false</c> otherwise, including when either parameter is <c>null</c>.
    /// </returns>
    public static bool AreRwaTokensEqual(this RwaToken rwaToken, UpdateRwaTokenRequest request)
    {
        return request.AssetType == rwaToken.AssetType &&
               string.Equals(request.AssetDescription, rwaToken.AssetDescription, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(request.ProofOfOwnershipDocument, rwaToken.ProofOfOwnershipDocument, StringComparison.OrdinalIgnoreCase) &&
               request.Price == rwaToken.Price &&
               request.Royalty == rwaToken.Royalty &&
               string.Equals(request.OwnerContact, rwaToken.OwnerContact, StringComparison.OrdinalIgnoreCase) &&
               request.ValuationDate == rwaToken.ValuationDate;
    }
}