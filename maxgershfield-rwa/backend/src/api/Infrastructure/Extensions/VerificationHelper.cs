namespace Infrastructure.Extensions;

/// <summary>
/// Helper class for generating verification codes.
/// </summary>
public static class VerificationHelper
{
    // Static random instance to generate random numbers
    private static readonly Random Random = new();

    /// <summary>
    /// Generates a 6-digit verification code.
    /// </summary>
    /// <returns>A random 6-digit verification code.</returns>
    public static long GenerateVerificationCode()
        => Random.NextInt64(100000, 999999);
}