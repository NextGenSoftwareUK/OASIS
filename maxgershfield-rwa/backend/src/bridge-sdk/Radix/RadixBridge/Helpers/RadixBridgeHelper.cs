namespace RadixBridge.Helpers;

/// <summary>
/// Provides helper methods related to the Radix network, including network constants and utility functions.
/// </summary>
public static class RadixBridgeHelper
{
    public const string StokeNet = "stokenet";
    public const string MainNet = "mainnet";

    public const string MainNetXrdAddress =
        "resource_rdx1tknxxxxxxxxxradxrdxxxxxxxxx009923554798xxxxxxxxxradxrd";

    public const string StokeNetXrdAddress =
        "resource_tdx_2_1tknxxxxxxxxxradxrdxxxxxxxxx009923554798xxxxxxxxxtfd2jc";

    /// <summary>
    /// Generates a private key from the provided mnemonic (seed phrase).
    /// This method uses the SHA256 hash of the mnemonic to derive the private key.
    /// </summary>
    /// <param name="mnemonic">The mnemonic (seed phrase) used to derive the private key.</param>
    /// <returns>The generated private key associated with the mnemonic.</returns>
    public static PrivateKey GetPrivateKey(Mnemonic mnemonic)
        => new(SHA256.Create().ComputeHash(mnemonic.DeriveSeed()), Curve.ED25519);


    /// <summary>
    /// Generates a random nonce, typically used for transaction uniqueness.
    /// Nonces are used to ensure that transactions are unique and cannot be replayed.
    /// </summary>
    /// <returns>A random nonce value as a uint.</returns>
    public static uint RandomNonce()
        => (uint)RandomNumberGenerator.GetInt32(int.MaxValue);

    /// <summary>
    /// Sends a request to retrieve construction metadata, such as the current epoch, for a given network.
    /// </summary>
    /// <param name="client">The HTTP client used to send the request.</param>
    /// <param name="options">The Radix technical account bridge options.</param>
    /// <returns>A task representing the operation. The task result contains the current epoch response, or null if the request fails.</returns>
    public static async Task<CurrentEpochResponse?> GetConstructionMetadata(this HttpClient client,
        RadixTechnicalAccountBridgeOptions options)
    {
        try
        {
            var data = new
            {
                network = options.NetworkId == 0x01
                    ? MainNet
                    : StokeNet
            };


            Result<CurrentEpochResponse?> response = await HttpClientHelper.PostAsync<object, CurrentEpochResponse>(
                client,
                $"{options.HostUri}/core/lts/transaction/construction",
                data
            );

            return response.Value;
        }
        catch
        {
            return default;
        }
    }
}