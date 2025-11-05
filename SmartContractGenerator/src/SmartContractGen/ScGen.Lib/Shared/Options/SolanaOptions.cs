namespace ScGen.Lib.Shared.Options;

public sealed class SolanaOptions
{
    public string RpcUrl { get; set; } = string.Empty;
    public string KeyPairPath { get; set; } = string.Empty;
    public bool UseLocalValidator { get; set; } = true;
    public string Pubkey { get; set; } = string.Empty;
}