using System.Transactions;

namespace Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for managing distributed transactions across database and blockchain operations.
/// </summary>
public static class TransactionExtensions
{
    /// <summary>
    /// Creates a new TransactionScope with appropriate settings for distributed transactions.
    /// </summary>
    /// <returns>A configured TransactionScope instance.</returns>
    public static TransactionScope CreateTransactionScope()
    {
        var options = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromMinutes(5) // Adjusted timeout for blockchain operations
        };
        return new TransactionScope(TransactionScopeOption.Required, options, TransactionScopeAsyncFlowOption.Enabled);
    }
}
