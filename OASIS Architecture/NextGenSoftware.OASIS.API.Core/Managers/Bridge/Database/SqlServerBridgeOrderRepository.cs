using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Database;

/// <summary>
/// SQL Server implementation of the bridge order repository
/// </summary>
public class SqlServerBridgeOrderRepository : IBridgeOrderRepository
{
    private readonly string _connectionString;

    public SqlServerBridgeOrderRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<OASISResult<Guid>> CreateOrderAsync(
        CreateBridgeOrderRequest request,
        CancellationToken token = default)
    {
        var result = new OASISResult<Guid>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            using var command = new SqlCommand("CreateBridgeOrder", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var orderId = Guid.NewGuid();
            
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@FromChain", request.FromChain);
            command.Parameters.AddWithValue("@ToChain", request.ToChain);
            command.Parameters.AddWithValue("@FromAmount", request.FromAmount);
            command.Parameters.AddWithValue("@ToAmount", request.ToAmount);
            command.Parameters.AddWithValue("@ExchangeRate", request.ExchangeRate);
            command.Parameters.AddWithValue("@FromAddress", request.FromAddress);
            command.Parameters.AddWithValue("@ToAddress", request.ToAddress);
            command.Parameters.AddWithValue("@ExpiresInMinutes", request.ExpiresInMinutes);
            command.Parameters.AddWithValue("@CreatedBy", (object)request.CreatedBy ?? DBNull.Value);
            
            var orderIdParam = new SqlParameter("@OrderId", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(orderIdParam);

            await command.ExecuteNonQueryAsync(token);
            
            result.Result = (Guid)orderIdParam.Value;
            result.IsError = false;
            result.Message = "Bridge order created successfully";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error creating bridge order: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<BridgeOrder>> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken token = default)
    {
        var result = new OASISResult<BridgeOrder>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                SELECT OrderId, UserId, FromChain, ToChain, FromAmount, ToAmount, ExchangeRate,
                       FromAddress, ToAddress, WithdrawTransactionId, DepositTransactionId,
                       Status, IsCompleted, IsFailed, IsRolledBack, ErrorMessage, FailureReason, RollbackReason,
                       CreatedAt, UpdatedAt, WithdrawCompletedAt, DepositCompletedAt,
                       CompletedAt, FailedAt, RolledBackAt, ExpiresAt
                FROM BridgeOrders
                WHERE OrderId = @OrderId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);

            using var reader = await command.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token))
            {
                result.Result = MapBridgeOrder(reader);
                result.IsError = false;
                result.Message = "Order retrieved successfully";
            }
            else
            {
                result.IsError = true;
                result.Message = "Order not found";
            }
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error retrieving order: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<IEnumerable<BridgeOrder>>> GetOrdersByUserIdAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken token = default)
    {
        var result = new OASISResult<IEnumerable<BridgeOrder>>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            using var command = new SqlCommand("GetBridgeOrdersByUser", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@PageNumber", pageNumber);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            var orders = new List<BridgeOrder>();
            using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                orders.Add(MapBridgeOrder(reader));
            }

            result.Result = orders;
            result.IsError = false;
            result.Message = $"Retrieved {orders.Count} orders";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error retrieving user orders: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<bool>> UpdateOrderStatusAsync(
        Guid orderId,
        string status,
        string errorMessage = null,
        CancellationToken token = default)
    {
        var result = new OASISResult<bool>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            using var command = new SqlCommand("UpdateBridgeOrderStatus", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@ErrorMessage", (object)errorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@ModifiedBy", DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync(token);
            
            result.Result = rowsAffected > 0;
            result.IsError = !result.Result;
            result.Message = result.Result 
                ? "Order status updated successfully" 
                : "Order not found";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error updating order status: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<bool>> UpdateWithdrawTransactionAsync(
        Guid orderId,
        string transactionId,
        DateTime? completedAt = null,
        CancellationToken token = default)
    {
        var result = new OASISResult<bool>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                UPDATE BridgeOrders
                SET WithdrawTransactionId = @TransactionId,
                    WithdrawCompletedAt = @CompletedAt,
                    UpdatedAt = GETUTCDATE()
                WHERE OrderId = @OrderId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@TransactionId", transactionId);
            command.Parameters.AddWithValue("@CompletedAt", (object)completedAt ?? DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync(token);
            
            result.Result = rowsAffected > 0;
            result.IsError = !result.Result;
            result.Message = result.Result 
                ? "Withdraw transaction updated" 
                : "Order not found";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error updating withdraw transaction: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<bool>> UpdateDepositTransactionAsync(
        Guid orderId,
        string transactionId,
        DateTime? completedAt = null,
        CancellationToken token = default)
    {
        var result = new OASISResult<bool>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                UPDATE BridgeOrders
                SET DepositTransactionId = @TransactionId,
                    DepositCompletedAt = @CompletedAt,
                    UpdatedAt = GETUTCDATE()
                WHERE OrderId = @OrderId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@TransactionId", transactionId);
            command.Parameters.AddWithValue("@CompletedAt", (object)completedAt ?? DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync(token);
            
            result.Result = rowsAffected > 0;
            result.IsError = !result.Result;
            result.Message = result.Result 
                ? "Deposit transaction updated" 
                : "Order not found";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error updating deposit transaction: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<bool>> CompleteOrderAsync(
        Guid orderId,
        CancellationToken token = default)
    {
        return await UpdateOrderStatusAsync(orderId, "Completed", null, token);
    }

    public async Task<OASISResult<bool>> FailOrderAsync(
        Guid orderId,
        string reason,
        CancellationToken token = default)
    {
        var result = new OASISResult<bool>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                UPDATE BridgeOrders
                SET Status = 'Failed',
                    IsFailed = 1,
                    FailureReason = @Reason,
                    ErrorMessage = @Reason,
                    FailedAt = GETUTCDATE(),
                    UpdatedAt = GETUTCDATE()
                WHERE OrderId = @OrderId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@Reason", reason);

            var rowsAffected = await command.ExecuteNonQueryAsync(token);
            
            result.Result = rowsAffected > 0;
            result.IsError = !result.Result;
            result.Message = result.Result 
                ? "Order marked as failed" 
                : "Order not found";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error failing order: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<bool>> RollbackOrderAsync(
        Guid orderId,
        string reason,
        CancellationToken token = default)
    {
        var result = new OASISResult<bool>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                UPDATE BridgeOrders
                SET Status = 'RolledBack',
                    IsRolledBack = 1,
                    RollbackReason = @Reason,
                    RolledBackAt = GETUTCDATE(),
                    UpdatedAt = GETUTCDATE()
                WHERE OrderId = @OrderId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@Reason", reason);

            var rowsAffected = await command.ExecuteNonQueryAsync(token);
            
            result.Result = rowsAffected > 0;
            result.IsError = !result.Result;
            result.Message = result.Result 
                ? "Order rolled back" 
                : "Order not found";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error rolling back order: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<Guid>> RecordBalanceCheckAsync(
        Guid orderId,
        string chainType,
        string chain,
        string address,
        decimal? balanceBefore,
        decimal? balanceAfter,
        bool isSufficient,
        CancellationToken token = default)
    {
        var result = new OASISResult<Guid>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                INSERT INTO BridgeOrderBalances (
                    BalanceId, OrderId, ChainType, Chain, Address,
                    BalanceBefore, BalanceAfter, BalanceChange, IsSufficient, CheckedAt
                )
                VALUES (
                    @BalanceId, @OrderId, @ChainType, @Chain, @Address,
                    @BalanceBefore, @BalanceAfter, @BalanceChange, @IsSufficient, GETUTCDATE()
                )";

            var balanceId = Guid.NewGuid();
            decimal? balanceChange = null;
            if (balanceBefore.HasValue && balanceAfter.HasValue)
            {
                balanceChange = balanceAfter.Value - balanceBefore.Value;
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@BalanceId", balanceId);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@ChainType", chainType);
            command.Parameters.AddWithValue("@Chain", chain);
            command.Parameters.AddWithValue("@Address", address);
            command.Parameters.AddWithValue("@BalanceBefore", (object)balanceBefore ?? DBNull.Value);
            command.Parameters.AddWithValue("@BalanceAfter", (object)balanceAfter ?? DBNull.Value);
            command.Parameters.AddWithValue("@BalanceChange", (object)balanceChange ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsSufficient", isSufficient);

            await command.ExecuteNonQueryAsync(token);
            
            result.Result = balanceId;
            result.IsError = false;
            result.Message = "Balance check recorded";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error recording balance check: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<IEnumerable<BridgeOrder>>> GetActiveOrdersAsync(
        CancellationToken token = default)
    {
        var result = new OASISResult<IEnumerable<BridgeOrder>>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                SELECT OrderId, UserId, FromChain, ToChain, FromAmount, ToAmount, ExchangeRate,
                       FromAddress, ToAddress, WithdrawTransactionId, DepositTransactionId,
                       Status, IsCompleted, IsFailed, IsRolledBack, ErrorMessage, FailureReason, RollbackReason,
                       CreatedAt, UpdatedAt, WithdrawCompletedAt, DepositCompletedAt,
                       CompletedAt, FailedAt, RolledBackAt, ExpiresAt
                FROM ActiveBridgeOrders
                ORDER BY CreatedAt DESC";

            using var command = new SqlCommand(query, connection);
            var orders = new List<BridgeOrder>();
            
            using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                orders.Add(MapBridgeOrder(reader));
            }

            result.Result = orders;
            result.IsError = false;
            result.Message = $"Retrieved {orders.Count} active orders";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error retrieving active orders: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<IEnumerable<BridgeOrder>>> GetExpiredOrdersAsync(
        CancellationToken token = default)
    {
        var result = new OASISResult<IEnumerable<BridgeOrder>>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                SELECT OrderId, UserId, FromChain, ToChain, FromAmount, ToAmount, ExchangeRate,
                       FromAddress, ToAddress, WithdrawTransactionId, DepositTransactionId,
                       Status, IsCompleted, IsFailed, IsRolledBack, ErrorMessage, FailureReason, RollbackReason,
                       CreatedAt, UpdatedAt, WithdrawCompletedAt, DepositCompletedAt,
                       CompletedAt, FailedAt, RolledBackAt, ExpiresAt
                FROM BridgeOrders
                WHERE IsCompleted = 0 
                  AND IsFailed = 0 
                  AND ExpiresAt IS NOT NULL 
                  AND ExpiresAt < GETUTCDATE()
                ORDER BY ExpiresAt DESC";

            using var command = new SqlCommand(query, connection);
            var orders = new List<BridgeOrder>();
            
            using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                orders.Add(MapBridgeOrder(reader));
            }

            result.Result = orders;
            result.IsError = false;
            result.Message = $"Retrieved {orders.Count} expired orders";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error retrieving expired orders: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<Guid>> LogTransactionEventAsync(
        Guid orderId,
        string logLevel,
        string eventType,
        string message,
        string transactionId = null,
        string chain = null,
        CancellationToken token = default)
    {
        var result = new OASISResult<Guid>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                INSERT INTO BridgeTransactionLog (
                    LogId, OrderId, LogLevel, EventType, Message,
                    TransactionId, Chain, LoggedAt
                )
                VALUES (
                    @LogId, @OrderId, @LogLevel, @EventType, @Message,
                    @TransactionId, @Chain, GETUTCDATE()
                )";

            var logId = Guid.NewGuid();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LogId", logId);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@LogLevel", logLevel);
            command.Parameters.AddWithValue("@EventType", eventType);
            command.Parameters.AddWithValue("@Message", message);
            command.Parameters.AddWithValue("@TransactionId", (object)transactionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Chain", (object)chain ?? DBNull.Value);

            await command.ExecuteNonQueryAsync(token);
            
            result.Result = logId;
            result.IsError = false;
            result.Message = "Event logged";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error logging event: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<Guid>> RecordExchangeRateAsync(
        string fromToken,
        string toToken,
        decimal rate,
        string source,
        int validForMinutes = 5,
        CancellationToken token = default)
    {
        var result = new OASISResult<Guid>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            using var command = new SqlCommand("RecordExchangeRate", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@FromToken", fromToken);
            command.Parameters.AddWithValue("@ToToken", toToken);
            command.Parameters.AddWithValue("@Rate", rate);
            command.Parameters.AddWithValue("@Source", source);
            command.Parameters.AddWithValue("@ValidForMinutes", validForMinutes);

            await command.ExecuteNonQueryAsync(token);
            
            result.Result = Guid.NewGuid(); // RateId
            result.IsError = false;
            result.Message = "Exchange rate recorded";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error recording exchange rate: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<decimal>> GetLatestExchangeRateAsync(
        string fromToken,
        string toToken,
        CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                SELECT TOP 1 Rate
                FROM ExchangeRateHistory
                WHERE FromToken = @FromToken 
                  AND ToToken = @ToToken
                  AND (ValidUntil IS NULL OR ValidUntil > GETUTCDATE())
                ORDER BY FetchedAt DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@FromToken", fromToken);
            command.Parameters.AddWithValue("@ToToken", toToken);

            var rateObj = await command.ExecuteScalarAsync(token);
            if (rateObj != null && rateObj != DBNull.Value)
            {
                result.Result = Convert.ToDecimal(rateObj);
                result.IsError = false;
                result.Message = "Exchange rate retrieved";
            }
            else
            {
                result.IsError = true;
                result.Message = "No recent exchange rate found";
            }
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error getting exchange rate: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    public async Task<OASISResult<UserBridgeOrderStats>> GetUserStatsAsync(
        Guid userId,
        CancellationToken token = default)
    {
        var result = new OASISResult<UserBridgeOrderStats>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token);

            const string query = @"
                SELECT UserId, TotalOrders, CompletedOrders, FailedOrders, PendingOrders,
                       TotalVolume, FirstOrderAt, LastOrderAt
                FROM UserBridgeOrderSummary
                WHERE UserId = @UserId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token))
            {
                result.Result = new UserBridgeOrderStats
                {
                    UserId = reader.GetGuid(0),
                    TotalOrders = reader.GetInt32(1),
                    CompletedOrders = reader.GetInt32(2),
                    FailedOrders = reader.GetInt32(3),
                    PendingOrders = reader.GetInt32(4),
                    TotalVolume = reader.GetDecimal(5),
                    FirstOrderAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    LastOrderAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                };
                result.IsError = false;
                result.Message = "User stats retrieved";
            }
            else
            {
                result.IsError = true;
                result.Message = "User not found";
            }
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error getting user stats: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    private static BridgeOrder MapBridgeOrder(SqlDataReader reader)
    {
        return new BridgeOrder
        {
            OrderId = reader.GetGuid(0),
            UserId = reader.GetGuid(1),
            FromChain = reader.GetString(2),
            ToChain = reader.GetString(3),
            FromAmount = reader.GetDecimal(4),
            ToAmount = reader.GetDecimal(5),
            ExchangeRate = reader.GetDecimal(6),
            FromAddress = reader.GetString(7),
            ToAddress = reader.GetString(8),
            WithdrawTransactionId = reader.IsDBNull(9) ? null : reader.GetString(9),
            DepositTransactionId = reader.IsDBNull(10) ? null : reader.GetString(10),
            Status = reader.GetString(11),
            IsCompleted = reader.GetBoolean(12),
            IsFailed = reader.GetBoolean(13),
            IsRolledBack = reader.GetBoolean(14),
            ErrorMessage = reader.IsDBNull(15) ? null : reader.GetString(15),
            FailureReason = reader.IsDBNull(16) ? null : reader.GetString(16),
            RollbackReason = reader.IsDBNull(17) ? null : reader.GetString(17),
            CreatedAt = reader.GetDateTime(18),
            UpdatedAt = reader.GetDateTime(19),
            WithdrawCompletedAt = reader.IsDBNull(20) ? null : reader.GetDateTime(20),
            DepositCompletedAt = reader.IsDBNull(21) ? null : reader.GetDateTime(21),
            CompletedAt = reader.IsDBNull(22) ? null : reader.GetDateTime(22),
            FailedAt = reader.IsDBNull(23) ? null : reader.GetDateTime(23),
            RolledBackAt = reader.IsDBNull(24) ? null : reader.GetDateTime(24),
            ExpiresAt = reader.IsDBNull(25) ? null : reader.GetDateTime(25)
        };
    }
}






