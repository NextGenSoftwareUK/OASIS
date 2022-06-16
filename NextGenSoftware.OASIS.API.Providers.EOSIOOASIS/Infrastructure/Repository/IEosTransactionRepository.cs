using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository
{
    public interface IEosTransactionRepository
    {
        Task<OASISResult<string>> SendTransaction(string fromAccountName, string toAccountName, decimal amount);
    }
}