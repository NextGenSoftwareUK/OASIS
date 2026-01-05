namespace ScGen.Lib.Shared.Options;

public sealed class RadixOptions
{
    public bool UseResim { get; set; }
    public string Profile { get; set; } = KeyNames.Default;
    public string AccountAddress { get; set; } = string.Empty;
    public bool AutoMintXrd { get; set; }
}