namespace ScGen.Lib.Shared.Options;

public sealed class EthereumOptions
{
    public string RpcUrl { get; set; } = null!;
    public string PrivateKey { get; set; } = null!;
    public BigInteger GasLimit { get; set; } = 3000000;
}