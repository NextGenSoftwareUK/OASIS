#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

public sealed class RadixClient(
    IHttpClientFactory clientFactory,
    ILogger<RadixClient> logger
)
{
    private readonly HttpClient _httpClient = clientFactory.CreateClient(nameof(RadixClient));
    private readonly ILogger<RadixClient> _logger = logger;

    public Task<OASISResult<TransactionDetails>> CreateAsync(ulong numericId, string guidId, string infoJson, OASISEntityType entityType, string ownerBadge, CancellationToken token = default)
    {
        var data = new
        {
            numeric_id = numericId,
            guid_id = guidId,
            info_json = infoJson,
            entity_type = entityType.ToString()
        };

        return _httpClient.SendTransactionAsync("/create/", data, ownerBadge, token);
    }

    public async Task<OASISResult<OASISEntity?>> GetAsync(ulong numericId)
    {
        var response = await _httpClient.GetAsync($"/get/{numericId}");
        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();
        return new(JsonSerializer.Deserialize<OASISEntity>(jsonResponse));
    }

    public Task<OASISResult<TransactionDetails>> UpdateAsync(ulong numericId, string? infoJson, OASISEntityType? entityType, string ownerBadge, CancellationToken token = default)
    {
        var data = new
        {
            numeric_id = numericId,
            info_json = infoJson,
            entity_type = entityType?.ToString()
        };

        return _httpClient.SendTransactionAsync("/update/", data, ownerBadge, token);
    }

    public Task<OASISResult<TransactionDetails>> DeleteAsync(ulong numericId, string ownerBadge, CancellationToken token = default)
    {
        return _httpClient.SendTransactionAsync<object?>($"/delete/{numericId}", data: null, ownerBadge, token);
    }

    public Task<OASISResult<TransactionDetails>> CreateProposalAsync(string description, ulong duration, string ownerBadge)
    {
        var data = new { description, duration };
        return _httpClient.SendTransactionAsync("/create_proposal", data, ownerBadge);
    }

    public Task<OASISResult<TransactionDetails>> VoteProposalAsync(ulong proposalId, bool support, decimal voterTokens)
    {
        var data = new { proposal_id = proposalId, support, voter_tokens = voterTokens };
        return _httpClient.SendTransactionAsync("/vote_proposal", data);
    }

    public Task<OASISResult<TransactionDetails>> ExecuteProposalAsync(ulong proposalId, string ownerBadge)
    {
        var data = new { proposal_id = proposalId };
        return _httpClient.SendTransactionAsync("/execute_proposal", data, ownerBadge);
    }

    public Task<OASISResult<TransactionDetails>> SendTokensAsync(string recipientAddress, decimal amount, string ownerBadge)
    {
        var data = new { recipient = recipientAddress, amount };
        return _httpClient.SendTransactionAsync("/send_tokens", data, ownerBadge);
    }

    public Task<OASISResult<TransactionDetails>> BurnTokensAsync(decimal amount, string ownerBadge)
    {
        var data = new { amount };
        return _httpClient.SendTransactionAsync("/burn_tokens", data, ownerBadge);
    }

    public Task<OASISResult<TransactionDetails>> MintNFTAsync(string name, string description, decimal paymentAmount, string ownerBadge)
    {
        var data = new { name, description, payment = paymentAmount };
        return _httpClient.SendTransactionAsync("/mint_nft", data, ownerBadge);
    }

    public async Task<OASISResult<string>> SendNFTAsync(string recipientAddress, string nftId, string ownerBadge)
    {
        OASISResult<string> result = new(functionName: nameof(SendNFTAsync));

        OASISResult<TransactionDetails> sendNftRequestResult = new();
        try
        {
            SendNftRequest requestData = new(recipientAddress, nftId);
            sendNftRequestResult = await _httpClient.SendTransactionAsync("/send_nft", requestData, ownerBadge);

            if (sendNftRequestResult is { IsError: true } or { Result.IsSuccessfullResponse: false })
            {
                _logger.SendNFTFailed(sendNftRequestResult.Exception, recipientAddress, nftId, sendNftRequestResult.Result);

                result.IsSaved = false;
                result.IsError = true;

                return result;
            }

            result.Result = sendNftRequestResult.Result.ResponseMessageRaw;
            result.IsSaved = true;
            result.IsError = false;
        }
        catch
        {
            _logger.SendNFTFailed(sendNftRequestResult.Exception, recipientAddress, nftId, sendNftRequestResult.Result);

            OASISErrorHandling.HandleError(
                ref result,
                errorMessage: sendNftRequestResult.Message,
                log: false,
                innerResult: sendNftRequestResult,
                ex: sendNftRequestResult.Exception);
        }

        return result;
    }
}

public readonly record struct TransactionDetails
{
    public string Reason { get; init; }
    public string ResponseMessageRaw { get; init; }

    public bool IsSuccessfullResponse { get; init; }
    public int StatusCode { get; init; }
    public int ErrorCode { get; init; }
}

internal static partial class RadixLoggerExtensions
{
    public const int EventIdOffset = 1000;

    [LoggerMessage(EventIdOffset, LogLevel.Error, "Failed to send NFT {NftId} to {Recipient} because of unexpected error. Transaction Details: {TnxDetails}. See exception for more details.")]
    public static partial void SendNFTFailed(this ILogger logger, Exception ex, string recipient, string nftId, TransactionDetails tnxDetails);
}

public readonly record struct SendNftRequest
{
    public SendNftRequest(string recipient, string nftId)
    {
        ArgumentException.ThrowIfNullOrEmpty(recipient);
        ArgumentException.ThrowIfNullOrEmpty(nftId);

        Recipient = recipient;
        NftId = nftId;
    }

    [JsonPropertyName("recipient")]
    public string Recipient { get; init; }

    [JsonPropertyName("nft_id")]
    public string NftId { get; init; }
}

internal static class HttpClientExtension
{
    private readonly static JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static async Task<OASISResult<TransactionDetails>> SendTransactionAsync<T>(this HttpClient httpClient, string endpoint, T data, string? ownerBadge = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        await using MemoryStream memoryStream = new();
        await JsonSerializer.SerializeAsync(memoryStream, data, options: _options, cancellationToken: token);

        memoryStream.Position = 0;

        using StreamContent content = new(memoryStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);

        if (ownerBadge is not null)
        {
            content.Headers.Add("Authorization", ownerBadge);
        }

        using HttpResponseMessage response = await httpClient.PostAsync(endpoint, content, token)
            .ConfigureAwait(continueOnCapturedContext: false);

        response.EnsureSuccessStatusCode();

        TransactionDetails txnDetails = await response.Content.ReadFromJsonAsync<TransactionDetails>(token)
            .ConfigureAwait(continueOnCapturedContext: false);

        return new(txnDetails);
    }
}

public interface IBlockchainAdapter
{
    Task<string> SendTransactionAsync(string transactionData, CancellationToken token = default);

    Task<TransactionStatus> GetTransactionStatusAsync(string transactionId, CancellationToken token = default);

    Task<decimal> GetBalanceAsync(string address, CancellationToken token = default);

    Task<string> SignTransactionAsync(string data, string privateKey, CancellationToken token = default);

    Task<BlockchainEvent> MonitorEventsAsync(EventFilter filter, CancellationToken token = default);
}

public enum TransactionStatus
{
    Pending,
    Confirmed,
    Failed
}

public readonly record struct BlockchainEvent
{
    public string EventId { get; init; }
    public string Description { get; init; }
    public DateTime Timestamp { get; init; }
}

public readonly record struct EventFilter
{
    public string Address { get; init; }
    public string EventType { get; init; }
}

public abstract class BlockchainAdapterBase : IBlockchainAdapter
{
    public abstract Task<string> SendTransactionAsync(string transactionData, CancellationToken token = default);

    public abstract Task<TransactionStatus> GetTransactionStatusAsync(string transactionId, CancellationToken token = default);

    public abstract Task<decimal> GetBalanceAsync(string address, CancellationToken token = default);

    public abstract Task<string> SignTransactionAsync(string data, string privateKey, CancellationToken token = default);

    public abstract Task<BlockchainEvent> MonitorEventsAsync(EventFilter filter, CancellationToken token = default);

    protected virtual string FormatTransactionData(string data, CancellationToken token = default)
    {
        return data;
    }

    protected virtual async Task RetryPolicyAsync(Func<Task> action, int maxRetries = 3, CancellationToken token = default)
    {
        int attempts = 0;
        while (attempts < maxRetries)
        {
            try
            {
                await action();
                return;
            }
            catch
            {
                attempts++;
                if (attempts >= maxRetries) throw;
            }
        }
    }
}

public class RadixAdapter(string apiEndpoint) : BlockchainAdapterBase
{
    public override async Task<string> SendTransactionAsync(string transactionData, CancellationToken token = default)
    {
        await Task.Delay(100, token);
        return Guid.NewGuid().ToString();
    }

    public override async Task<TransactionStatus> GetTransactionStatusAsync(string transactionId, CancellationToken token = default)
    {
        await Task.Delay(100, token);
        return TransactionStatus.Confirmed;
    }

    public override async Task<decimal> GetBalanceAsync(string address, CancellationToken token = default)
    {
        await Task.Delay(100, token);
        return 1000.00m;
    }

    public override async Task<string> SignTransactionAsync(string data, string privateKey, CancellationToken token = default)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
    }

    public override async Task<BlockchainEvent> MonitorEventsAsync(EventFilter filter, CancellationToken token = default)
    {
        await Task.Delay(100, token);
        return new BlockchainEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Description = "Radix event",
            Timestamp = DateTime.UtcNow
        };
    }
}

public class SolanaAdapter(string apiEndpoint) : BlockchainAdapterBase
{
    public override async Task<string> SendTransactionAsync(string transactionData, CancellationToken token = default)
    {
        await Task.Delay(100, token);
        return Guid.NewGuid().ToString();
    }

    public override async Task<TransactionStatus> GetTransactionStatusAsync(string transactionId, CancellationToken token = default)
    {
        await Task.Delay(100, token);
        return TransactionStatus.Confirmed;
    }

    public override async Task<decimal> GetBalanceAsync(string address, CancellationToken token = default)
    {
        await Task.Delay(100, token);
        return 1000.00m;
    }

    public override async Task<string> SignTransactionAsync(string data, string privateKey, CancellationToken token = default)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
    }

    public override async Task<BlockchainEvent> MonitorEventsAsync(EventFilter filter, CancellationToken token = default)
    {
        await Task.Delay(100, token);
        return new BlockchainEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Description = "Solana event",
            Timestamp = DateTime.UtcNow
        };
    }
}

public static class BlockchainAdapterFactory
{
    public static IBlockchainAdapter GetAdapter(string blockchainType, string apiEndpoint) =>
        blockchainType.ToLower() switch
        {
            "radix" => new RadixAdapter(apiEndpoint),
            "solana" => new SolanaAdapter(apiEndpoint),
            _ => throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.")
        };
}

public interface IBridgeService
{
    Task<string> BurnAsync(string sourceNetwork, string transactionData, CancellationToken token = default);
    Task<string> MintAsync(string targetNetwork, string transactionData, CancellationToken token = default);
}

public class BridgeService : IBridgeService
{
    public async Task<string> BurnAsync(string sourceNetwork, string transactionData, CancellationToken token = default)
    {
        var adapter = BlockchainAdapterFactory.GetAdapter(sourceNetwork, GetApiEndpoint(sourceNetwork));

        string transactionId = await adapter.SendTransactionAsync(transactionData, token);

        TransactionStatus status;
        do
        {
            await Task.Delay(5000, token);
            status = await adapter.GetTransactionStatusAsync(transactionId, token);
        }
        while (status == TransactionStatus.Pending);

        if (status != TransactionStatus.Confirmed)
        {
            throw new Exception($"Transaction failed or not confirmed: {transactionId}");
        }

        return transactionId;
    }

    public async Task<string> MintAsync(string targetNetwork, string transactionData, CancellationToken token = default)
    {
        IBlockchainAdapter adapter = BlockchainAdapterFactory.GetAdapter(targetNetwork, GetApiEndpoint(targetNetwork));

        string transactionId = await adapter.SendTransactionAsync(transactionData, token);
        TransactionStatus status;
        do
        {
            await Task.Delay(5000, token);
            status = await adapter.GetTransactionStatusAsync(transactionId, token);
        }
        while (status == TransactionStatus.Pending);

        if (status != TransactionStatus.Confirmed)
        {
            throw new Exception($"Transaction failed or not confirmed: {transactionId}");
        }

        return transactionId;
    }

    private static string GetApiEndpoint(string network) =>
        network.ToLower() switch
        {
            "radix" => "https://radix.api.endpoint",
            "solana" => "https://solana.api.endpoint",
            _ => throw new NotSupportedException($"Network {network} is not supported.")
        };
}

public interface ITransactionStatusService
{
    Task<TransactionStatusResult> CheckSourceTransactionStatusAsync(string sourceNetwork, string transactionId, CancellationToken token = default);
    Task<TransactionStatusResult> CheckTargetTransactionStatusAsync(string targetNetwork, string transactionId, CancellationToken token = default);
}

public readonly record struct TransactionStatusResult
{
    public bool IsSuccess { get; init; }
    public TransactionStatus Status { get; init; }
    public string Message { get; init; }
}

public class TransactionStatusService : ITransactionStatusService
{
    public async Task<TransactionStatusResult> CheckSourceTransactionStatusAsync(string sourceNetwork, string transactionId, CancellationToken token = default)
    {
        IBlockchainAdapter adapter = BlockchainAdapterFactory.GetAdapter(sourceNetwork, GetApiEndpoint(sourceNetwork));
        return await CheckTransactionStatusAsync(adapter, transactionId, "Source Network", token);
    }

    public async Task<TransactionStatusResult> CheckTargetTransactionStatusAsync(string targetNetwork, string transactionId, CancellationToken token = default)
    {
        IBlockchainAdapter adapter = BlockchainAdapterFactory.GetAdapter(targetNetwork, GetApiEndpoint(targetNetwork));
        return await CheckTransactionStatusAsync(adapter, transactionId, "Target Network", token);
    }

    private static async Task<TransactionStatusResult> CheckTransactionStatusAsync(IBlockchainAdapter adapter, string transactionId, string networkType, CancellationToken token = default)
    {
        try
        {
            TransactionStatus status = await adapter.GetTransactionStatusAsync(transactionId, token);
            return status switch
            {
                TransactionStatus.Confirmed => new TransactionStatusResult
                {
                    IsSuccess = true,
                    Status = status,
                    Message = $"{networkType}: Transaction {transactionId} confirmed."
                },
                TransactionStatus.Pending => new TransactionStatusResult
                {
                    IsSuccess = false,
                    Status = status,
                    Message = $"{networkType}: Transaction {transactionId} is still pending."
                },
                TransactionStatus.Failed => new TransactionStatusResult
                {
                    IsSuccess = false,
                    Status = status,
                    Message = $"{networkType}: Transaction {transactionId} failed."
                },
                _ => new TransactionStatusResult
                {
                    IsSuccess = false,
                    Status = status,
                    Message = $"{networkType}: Unknown status for transaction {transactionId}."
                }
            };
        }
        catch (Exception ex)
        {
            return new TransactionStatusResult
            {
                IsSuccess = false,
                Status = TransactionStatus.Failed,
                Message = $"{networkType}: Error while checking transaction {transactionId}. {ex.Message}"
            };
        }
    }

    private static string GetApiEndpoint(string network)
        => network.ToLower() switch
        {
            "radix" => "https://radix.api.endpoint",
            "solana" => "https://solana.api.endpoint",
            _ => throw new NotSupportedException($"Network {network} is not supported.")
        };
}

public interface ITransactionRollbackService
{
    Task<bool> RollbackBurnAsync(string network, string transactionId, CancellationToken token = default);
    Task<bool> RollbackMintAsync(string network, string transactionId, CancellationToken token = default);
}

public class TransactionRollbackService : ITransactionRollbackService
{
    public async Task<bool> RollbackBurnAsync(string network, string transactionId, CancellationToken token = default)
    {
        IBlockchainAdapter adapter = BlockchainAdapterFactory.GetAdapter(network, GetApiEndpoint(network));

        try
        {
            TransactionStatus status = await adapter.GetTransactionStatusAsync(transactionId, token);
            if (status == TransactionStatus.Confirmed)
            {
                string compensationData = "{ action: 'compensate', transactionId: '" + transactionId + "' }";
                string compensationTxId = await adapter.SendTransactionAsync(compensationData, token);

                Console.WriteLine($"Rollback Burn successful, Compensation TxId: {compensationTxId}");
                return true;
            }
            else if (status == TransactionStatus.Pending)
            {
                Console.WriteLine($"Cannot rollback Burn: Transaction {transactionId} is still pending.");
                return false;
            }
            else
            {
                Console.WriteLine($"Burn transaction {transactionId} already failed, no rollback needed.");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during Burn rollback: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RollbackMintAsync(string network, string transactionId, CancellationToken token = default)
    {
        IBlockchainAdapter adapter = BlockchainAdapterFactory.GetAdapter(network, GetApiEndpoint(network));

        try
        {
            TransactionStatus status = await adapter.GetTransactionStatusAsync(transactionId, token);
            if (status == TransactionStatus.Confirmed)
            {
                Console.WriteLine($"Cannot rollback Mint: Transaction {transactionId} already confirmed.");
                return false;
            }
            else if (status == TransactionStatus.Pending)
            {
                string cancelData = "{ action: 'cancel', transactionId: '" + transactionId + "' }";
                await adapter.SendTransactionAsync(cancelData, token);

                Console.WriteLine($"Rollback Mint successful for TransactionId: {transactionId}");
                return true;
            }
            else
            {
                Console.WriteLine($"Mint transaction {transactionId} already failed, no rollback needed.");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during Mint rollback: {ex.Message}");
            return false;
        }
    }

    private static string GetApiEndpoint(string network) =>
        network.ToLower() switch
        {
            "radix" => "https://radix.api.endpoint",
            "solana" => "https://solana.api.endpoint",
            _ => throw new NotSupportedException($"Network {network} is not supported.")
        };
}


public static class RetryHelper
{
    public static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, int initialDelayMs = 1000, CancellationToken token = default)
    {
        int attempt = 0;
        Exception lastException = null;

        while (attempt < maxRetries)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                if (attempt >= maxRetries)
                {
                    Console.WriteLine($"Action failed after {attempt} attempts: {ex.Message}");
                    throw;
                }

                int delay = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                Console.WriteLine($"Retrying after {delay}ms (Attempt {attempt}/{maxRetries})...");
                await Task.Delay(delay);
            }
        }

        throw new InvalidOperationException("Unreachable code", lastException);
    }
}

// public abstract class BlockchainAdapterBase : IBlockchainAdapter
// {
//     public async Task<string> SendTransactionAsync(string transactionData)
//     {
//         return await RetryHelper.ExecuteWithRetryAsync(async () =>
//         {
//             Console.WriteLine($"Sending transaction with data: {transactionData}");
//             string transactionId = await PerformSendTransactionAsync(transactionData);
//             Console.WriteLine($"Transaction sent successfully: {transactionId}");
//             return transactionId;
//         });
//     }

//     public async Task<TransactionStatus> GetTransactionStatusAsync(string transactionId)
//     {
//         return await RetryHelper.ExecuteWithRetryAsync(async () =>
//         {
//             Console.WriteLine($"Checking transaction status for: {transactionId}");
//             var status = await PerformGetTransactionStatusAsync(transactionId);
//             Console.WriteLine($"Transaction status: {status}");
//             return status;
//         });
//     }

//     public async Task<decimal> GetBalanceAsync(string address)
//     {
//         return await RetryHelper.ExecuteWithRetryAsync(async () =>
//         {
//             Console.WriteLine($"Getting balance for address: {address}");
//             var balance = await PerformGetBalanceAsync(address);
//             Console.WriteLine($"Balance retrieved: {balance}");
//             return balance;
//         });
//     }

//     protected abstract Task<string> PerformSendTransactionAsync(string transactionData);
//     protected abstract Task<TransactionStatus> PerformGetTransactionStatusAsync(string transactionId);
//     protected abstract Task<decimal> PerformGetBalanceAsync(string address);
// }

public class TransactionRecord
{
    public int Id { get; set; }
    public string TransactionId { get; set; }
    public string Network { get; set; }
    public string Type { get; set; }
    public string Status { get; set; }
    public decimal Amount { get; set; }
    public string SourceAddress { get; set; }
    public string TargetAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public interface ITransactionRepository
{
    Task<int> CreateTransactionAsync(TransactionRecord transaction);
    Task<TransactionRecord> GetTransactionByIdAsync(string transactionId);
    Task<List<TransactionRecord>> GetTransactionsByStatusAsync(string status);
    Task UpdateTransactionStatusAsync(string transactionId, string status);
}

public class TransactionRepository(string connectionString) : ITransactionRepository
{
    private readonly string _connectionString = connectionString;

    public async Task<int> CreateTransactionAsync(TransactionRecord transaction)
    {
        const string query = @"
                INSERT INTO Transactions (TransactionId, Network, Type, Status, Amount, SourceAddress, TargetAddress, CreatedAt, UpdatedAt)
                VALUES (@TransactionId, @Network, @Type, @Status, @Amount, @SourceAddress, @TargetAddress, @CreatedAt, @UpdatedAt);
                SELECT SCOPE_IDENTITY();";

        using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(query, transaction);
    }

    public async Task<TransactionRecord> GetTransactionByIdAsync(string transactionId)
    {
        const string query = "SELECT * FROM Transactions WHERE TransactionId = @TransactionId";

        using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<TransactionRecord>(query, new { TransactionId = transactionId });
    }

    public async Task<List<TransactionRecord>> GetTransactionsByStatusAsync(string status)
    {
        const string query = "SELECT * FROM Transactions WHERE Status = @Status";

        using var connection = new SqlConnection(_connectionString);
        return (await connection.QueryAsync<TransactionRecord>(query, new { Status = status })).AsList();
    }

    public async Task UpdateTransactionStatusAsync(string transactionId, string status)
    {
        const string query = @"
                UPDATE Transactions
                SET Status = @Status, UpdatedAt = GETDATE()
                WHERE TransactionId = @TransactionId";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(query, new { TransactionId = transactionId, Status = status });
    }
}

public class TransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<string> BurnAsync(string network, string transactionData, decimal amount, string sourceAddress, string targetAddress)
    {
        var transactionId = "burn-" + Guid.NewGuid().ToString();

        // Создаем запись в БД
        await _transactionRepository.CreateTransactionAsync(new TransactionRecord
        {
            TransactionId = transactionId,
            Network = network,
            Type = "Burn",
            Status = "Pending",
            Amount = amount,
            SourceAddress = sourceAddress,
            TargetAddress = targetAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        try
        {
            // Выполняем транзакцию Burn (пример адаптера)
            string blockchainTransactionId = await SomeAdapter.SendTransactionAsync(transactionData);

            // Обновляем статус на Confirmed
            await _transactionRepository.UpdateTransactionStatusAsync(transactionId, "Confirmed");

            return blockchainTransactionId;
        }
        catch
        {
            // Обновляем статус на Failed
            await _transactionRepository.UpdateTransactionStatusAsync(transactionId, "Failed");
            throw;
        }
    }
}

public interface ITransactionCancelService
{
    Task<bool> CancelTransactionAsync(string transactionId, CancellationToken token = default);
}

public class TransactionCancelService(ITransactionRepository transactionRepository, ITransactionRollbackService rollbackService) : ITransactionCancelService
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly ITransactionRollbackService _rollbackService = rollbackService;

    public async Task<bool> CancelTransactionAsync(string transactionId)
    {
        var transaction = await _transactionRepository.GetTransactionByIdAsync(transactionId);
        if (transaction == null)
        {
            Console.WriteLine($"Transaction {transactionId} not found.");
            return false;
        }

        if (transaction.Status == "Confirmed")
        {
            Console.WriteLine($"Transaction {transactionId} is already confirmed. Cannot cancel.");
            return false;
        }

        if (transaction.Type == "Burn")
        {
            return await _rollbackService.RollbackBurnAsync(transaction.Network, transaction.TransactionId);
        }
        else if (transaction.Type == "Mint")
        {
            return await _rollbackService.RollbackMintAsync(transaction.Network, transaction.TransactionId);
        }

        return false;
    }
}

public interface ITransactionRepository
{
    // Остальные методы...

    /// <summary>
    /// Обновляет статус транзакции на отмененный.
    /// </summary>
    Task MarkTransactionAsCancelledAsync(string transactionId);
}

public class TransactionRepository : ITransactionRepository
{
    // Остальные методы...

    public async Task MarkTransactionAsCancelledAsync(string transactionId)
    {
        const string query = @"
                UPDATE Transactions
                SET Status = 'Cancelled', UpdatedAt = GETDATE()
                WHERE TransactionId = @TransactionId";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(query, new { TransactionId = transactionId });
    }
}

public class TransactionCancelService : ITransactionCancelService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionRollbackService _rollbackService;

    public TransactionCancelService(ITransactionRepository transactionRepository, ITransactionRollbackService rollbackService)
    {
        _transactionRepository = transactionRepository;
        _rollbackService = rollbackService;
    }

    public async Task<bool> CancelTransactionAsync(string transactionId)
    {
        var transaction = await _transactionRepository.GetTransactionByIdAsync(transactionId);
        if (transaction == null || transaction.Status == "Confirmed")
        {
            return false;
        }

        bool rollbackSuccess = false;
        if (transaction.Type == "Burn")
        {
            rollbackSuccess = await _rollbackService.RollbackBurnAsync(transaction.Network, transaction.TransactionId);
        }
        else if (transaction.Type == "Mint")
        {
            rollbackSuccess = await _rollbackService.RollbackMintAsync(transaction.Network, transaction.TransactionId);
        }

        if (rollbackSuccess)
        {
            await _transactionRepository.MarkTransactionAsCancelledAsync(transactionId);
        }

        return rollbackSuccess;
    }
}


public class TransactionHub : Hub
{
    public async Task NotifyTransactionStatus(string transactionId, string status)
    {
        await Clients.All.SendAsync("TransactionStatusUpdated", transactionId, status);
    }
}