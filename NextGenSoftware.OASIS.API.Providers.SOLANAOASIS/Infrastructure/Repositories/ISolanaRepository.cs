namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

public interface ISolanaRepository
{
    Task<string> CreateAsync<T>(T entity) where T : SolanaBaseDto, new();
    Task<string> UpdateAsync<T>(T entity) where T : SolanaBaseDto, new();
    Task<bool> DeleteAsync(string transactionHashReference);
    Task<T> GetAsync<T>(string transactionHashReference) where T : SolanaBaseDto, new();
}