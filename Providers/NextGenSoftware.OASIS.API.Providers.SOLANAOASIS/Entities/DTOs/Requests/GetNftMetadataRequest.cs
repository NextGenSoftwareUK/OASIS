namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;

public sealed class GetNftMetadataRequest(string accountAddress)
{
    public string AccountAddress { get; set; } = accountAddress;
}