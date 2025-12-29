using Application.DTOs.Trust;

namespace Application.Contracts;

public interface ITrustService
{
    Task<TrustResponse> CreateTrustAsync(CreateTrustRequest request);
    Task<TrustResponse> GetTrustAsync(Guid id);
    Task<PropertyResponse> AddPropertyAsync(Guid trustId, AddPropertyRequest request);
    Task<ContractGenerationResponse> GenerateContractAsync(Guid trustId, GenerateContractRequest request);
    Task<IEnumerable<ContractGenerationResponse>> GetContractsAsync(Guid trustId);
}
