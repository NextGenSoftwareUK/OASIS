using Ipfs.CoreApi;

namespace FileStorage;

public class IpfsOptions : AddFileOptions
{
    public string? Url { get; set; }
    public string? GatewayUrl { get; set; }
}