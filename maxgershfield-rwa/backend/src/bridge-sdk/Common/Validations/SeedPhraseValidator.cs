namespace Common.Validations;

public static partial class SeedPhraseValidator
{
    // Regex to validate seed phrase words (using only English letters)
    [GeneratedRegex(@"^[a-zA-Z]+$")]
    private static partial Regex SeedPhraseRegex();

    /// <summary>
    /// Validates the format of words in a seed phrase using a regex.
    /// </summary>
    /// <param name="seedPhrase">The seed phrase to validate.</param>
    /// <param name="expectedWordCount">The expected number of words in the seed phrase.</param>
    /// <returns>True if the seed phrase is valid, otherwise false.</returns>
    public static bool IsValidSeedPhrase(string seedPhrase, int expectedWordCount = 24)
    {
        if (string.IsNullOrWhiteSpace(seedPhrase))
            return false;

        // Split the seed phrase into words and check the length
        string[] words = seedPhrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != expectedWordCount)
            return false;

        // Check if all words match the regex (only English letters)
        return words.All(word => SeedPhraseRegex().IsMatch(word));
    }
}