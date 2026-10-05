namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;

/// <summary>
/// Validator for seed phrases
/// </summary>
public static class SeedPhraseValidator
{
    /// <summary>
    /// Validates if a seed phrase is in the correct format
    /// </summary>
    public static bool IsValidSeedPhrase(string seedPhrase)
    {
        if (string.IsNullOrWhiteSpace(seedPhrase))
            return false;

        var words = seedPhrase.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Seed phrases are typically 12, 15, 18, 21, or 24 words
        return words.Length is 12 or 15 or 18 or 21 or 24;
    }
}

