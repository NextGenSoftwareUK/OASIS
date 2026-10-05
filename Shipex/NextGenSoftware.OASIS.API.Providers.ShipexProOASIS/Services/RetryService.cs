using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services
{
    /// <summary>
    /// Service for handling retry logic with exponential backoff.
    /// Manages failed operations and dead letter queue.
    /// </summary>
    public class RetryService
    {
        private readonly ILogger<RetryService> _logger;
        private readonly int _maxRetries;
        private readonly int _baseDelayMs;

        public RetryService(
            ILogger<RetryService> logger = null,
            int maxRetries = 3,
            int baseDelayMs = 1000)
        {
            _logger = logger;
            _maxRetries = maxRetries;
            _baseDelayMs = baseDelayMs;
        }

        /// <summary>
        /// Executes an operation with retry logic using exponential backoff.
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        public async Task<OASISResult<T>> ExecuteWithRetryAsync<T>(
            Func<Task<OASISResult<T>>> operation,
            string operationName,
            CancellationToken cancellationToken = default)
        {
            int attempt = 0;
            Exception lastException = null;

            while (attempt <= _maxRetries)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new OASISResult<T>
                        {
                            IsError = true,
                            Message = "Operation cancelled"
                        };
                    }

                    var result = await operation();

                    // If successful, return result
                    if (!result.IsError)
                    {
                        if (attempt > 0)
                        {
                            _logger?.LogInformation($"{operationName} succeeded after {attempt} retries");
                        }
                        return result;
                    }

                    // If error but not retryable, return immediately
                    if (!IsRetryableError(result.Message))
                    {
                        _logger?.LogWarning($"{operationName} failed with non-retryable error: {result.Message}");
                        return result;
                    }

                    lastException = new Exception(result.Message);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger?.LogWarning(ex, $"{operationName} attempt {attempt + 1} failed with exception");

                    // If not retryable, return immediately
                    if (!IsRetryableException(ex))
                    {
                        return new OASISResult<T>
                        {
                            IsError = true,
                            Message = $"Non-retryable error: {ex.Message}"
                        };
                    }
                }

                attempt++;

                // If we've exhausted retries, break
                if (attempt > _maxRetries)
                    break;

                // Calculate exponential backoff delay
                var delayMs = CalculateBackoffDelay(attempt);
                _logger?.LogInformation($"{operationName} retry {attempt}/{_maxRetries} after {delayMs}ms delay");

                await Task.Delay(delayMs, cancellationToken);
            }

            // All retries exhausted
            _logger?.LogError($"{operationName} failed after {_maxRetries} retries. Last error: {lastException?.Message}");
            
            return new OASISResult<T>
            {
                IsError = true,
                Message = $"Operation failed after {_maxRetries} retries: {lastException?.Message}"
            };
        }

        /// <summary>
        /// Calculates exponential backoff delay in milliseconds.
        /// Formula: baseDelay * 2^(attempt-1)
        /// </summary>
        /// <param name="attempt">Current attempt number (1-based)</param>
        /// <returns>Delay in milliseconds</returns>
        private int CalculateBackoffDelay(int attempt)
        {
            // Exponential backoff: baseDelay * 2^(attempt-1)
            // Attempt 1: baseDelay * 1 = 1s
            // Attempt 2: baseDelay * 2 = 2s
            // Attempt 3: baseDelay * 4 = 4s
            var delay = _baseDelayMs * (int)Math.Pow(2, attempt - 1);
            
            // Cap at 30 seconds
            return Math.Min(delay, 30000);
        }

        /// <summary>
        /// Determines if an error message indicates a retryable error.
        /// </summary>
        private bool IsRetryableError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return false;

            var lowerError = errorMessage.ToLowerInvariant();
            
            // Retryable errors
            var retryablePatterns = new[]
            {
                "timeout",
                "connection",
                "network",
                "temporary",
                "rate limit",
                "server error",
                "503",
                "502",
                "504",
                "429"
            };

            foreach (var pattern in retryablePatterns)
            {
                if (lowerError.Contains(pattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if an exception is retryable.
        /// </summary>
        private bool IsRetryableException(Exception ex)
        {
            // Retryable exceptions
            return ex is TimeoutException ||
                   ex is System.Net.Http.HttpRequestException ||
                   (ex is TaskCanceledException && ex.InnerException is TimeoutException);
        }

        /// <summary>
        /// Creates a FailedOperation record for dead letter queue.
        /// </summary>
        public FailedOperation CreateFailedOperation(
            string operationType,
            string operationId,
            object operationData,
            Exception exception,
            int retryCount)
        {
            return new FailedOperation
            {
                FailedOperationId = Guid.NewGuid(),
                OperationType = operationType,
                OperationId = operationId,
                OperationData = operationData,
                ErrorMessage = exception?.Message ?? "Unknown error",
                StackTrace = exception?.StackTrace ?? string.Empty,
                RetryCount = retryCount,
                MaxRetries = _maxRetries,
                FirstFailedAt = DateTime.UtcNow,
                LastRetryAt = DateTime.UtcNow,
                IsInDeadLetterQueue = retryCount >= _maxRetries,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}




