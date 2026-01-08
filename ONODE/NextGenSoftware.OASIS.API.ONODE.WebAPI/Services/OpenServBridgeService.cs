using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects.OpenServ;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Bridge service for integrating with OpenSERV AI agents
    /// Provides HTTP client wrapper with authentication, retry logic, and error handling
    /// </summary>
    public class OpenServBridgeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _defaultApiKey;
        private readonly string _baseUrl;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 1000;

        /// <summary>
        /// Initialize OpenSERV bridge service
        /// </summary>
        /// <param name="httpClient">HTTP client instance (can be injected via DI or created)</param>
        /// <param name="baseUrl">OpenSERV base URL (default: https://api.openserv.ai)</param>
        /// <param name="defaultApiKey">Default API key for authentication</param>
        public OpenServBridgeService(
            HttpClient httpClient = null,
            string baseUrl = "https://api.openserv.ai",
            string defaultApiKey = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            _baseUrl = baseUrl?.TrimEnd('/') ?? "https://api.openserv.ai";
            _defaultApiKey = defaultApiKey;
            
            // Set default timeout
            if (_httpClient.Timeout == TimeSpan.FromSeconds(100))
                _httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        /// <summary>
        /// Execute an OpenSERV agent request
        /// </summary>
        public async Task<OASISResult<OpenServAgentResponse>> ExecuteAgentAsync(
            OpenServAgentRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<OpenServAgentResponse>();
            var startTime = DateTime.UtcNow;

            try
            {
                // Validate request
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "OpenServAgentRequest is required");
                    return result;
                }

                if (string.IsNullOrEmpty(request.Endpoint))
                {
                    OASISErrorHandling.HandleError(ref result, "Endpoint is required");
                    return result;
                }

                // Build full URL
                var url = request.Endpoint.StartsWith("http") 
                    ? request.Endpoint 
                    : $"{_baseUrl}/{request.Endpoint.TrimStart('/')}";

                // Prepare request payload
                var payload = new
                {
                    agent_id = request.AgentId,
                    input = request.Input,
                    context = request.Context ?? new Dictionary<string, object>(),
                    parameters = request.Parameters ?? new Dictionary<string, object>()
                };

                // Execute with retry logic
                HttpResponseMessage response = null;
                Exception lastException = null;

                for (int attempt = 0; attempt < MaxRetries; attempt++)
                {
                    try
                    {
                        // Create HTTP request
                        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
                        {
                            Content = JsonContent.Create(payload)
                        };

                        // Add authentication header
                        var apiKey = request.ApiKey ?? _defaultApiKey;
                        if (!string.IsNullOrEmpty(apiKey))
                        {
                            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                        }
                        httpRequest.Headers.Add("Content-Type", "application/json");

                        // Send request
                        response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                        lastException = null;
                        break; // Success, exit retry loop
                    }
                    catch (HttpRequestException ex) when (attempt < MaxRetries - 1)
                    {
                        lastException = ex;
                        await Task.Delay(RetryDelayMs * (attempt + 1), cancellationToken);
                        LoggingManager.Log($"OpenSERV request failed (attempt {attempt + 1}/{MaxRetries}): {ex.Message}", Logging.LogType.Warning);
                    }
                    catch (TaskCanceledException ex) when (attempt < MaxRetries - 1)
                    {
                        lastException = ex;
                        await Task.Delay(RetryDelayMs * (attempt + 1), cancellationToken);
                        LoggingManager.Log($"OpenSERV request timeout (attempt {attempt + 1}/{MaxRetries}): {ex.Message}", Logging.LogType.Warning);
                    }
                }

                // Handle final exception
                if (lastException != null)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"OpenSERV request failed after {MaxRetries} attempts: {lastException.Message}", 
                        lastException);
                    return result;
                }

                // Handle HTTP response
                if (response == null)
                {
                    OASISErrorHandling.HandleError(ref result, "No response received from OpenSERV");
                    return result;
                }

                var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Try to deserialize JSON response
                        var agentResponse = System.Text.Json.JsonSerializer.Deserialize<OpenServAgentResponse>(responseContent);
                        if (agentResponse != null)
                        {
                            agentResponse.ExecutionTimeMs = executionTime;
                            result.Result = agentResponse;
                            result.Message = "OpenSERV agent executed successfully";
                        }
                        else
                        {
                            // If deserialization fails, treat as plain text response
                            result.Result = new OpenServAgentResponse
                            {
                                Success = true,
                                Result = responseContent,
                                ExecutionTimeMs = executionTime
                            };
                            result.Message = "OpenSERV agent executed successfully";
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // If JSON deserialization fails, treat as plain text
                        result.Result = new OpenServAgentResponse
                        {
                            Success = true,
                            Result = responseContent,
                            ExecutionTimeMs = executionTime
                        };
                        result.Message = "OpenSERV agent executed successfully";
                    }
                }
                else
                {
                    result.Result = new OpenServAgentResponse
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {responseContent}",
                        ExecutionTimeMs = executionTime
                    };
                    OASISErrorHandling.HandleError(ref result, 
                        $"OpenSERV request failed with status {response.StatusCode}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing OpenSERV agent: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Execute an OpenSERV workflow
        /// </summary>
        public async Task<OASISResult<OpenServWorkflowResponse>> ExecuteWorkflowAsync(
            OpenServWorkflowRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<OpenServWorkflowResponse>();
            var startTime = DateTime.UtcNow;

            try
            {
                // Validate request
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "OpenServWorkflowRequest is required");
                    return result;
                }

                if (string.IsNullOrEmpty(request.Endpoint))
                {
                    OASISErrorHandling.HandleError(ref result, "Endpoint is required");
                    return result;
                }

                // Build full URL
                var url = request.Endpoint.StartsWith("http") 
                    ? request.Endpoint 
                    : $"{_baseUrl}/{request.Endpoint.TrimStart('/')}";

                // Prepare request payload
                var payload = new
                {
                    workflow = request.WorkflowRequest,
                    agent_id = request.AgentId,
                    parameters = request.Parameters ?? new Dictionary<string, object>(),
                    context = request.Context ?? new Dictionary<string, object>()
                };

                // Execute with retry logic
                HttpResponseMessage response = null;
                Exception lastException = null;

                for (int attempt = 0; attempt < MaxRetries; attempt++)
                {
                    try
                    {
                        // Create HTTP request
                        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
                        {
                            Content = JsonContent.Create(payload)
                        };

                        // Add authentication header
                        var apiKey = request.ApiKey ?? _defaultApiKey;
                        if (!string.IsNullOrEmpty(apiKey))
                        {
                            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                        }
                        httpRequest.Headers.Add("Content-Type", "application/json");

                        // Send request
                        response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                        lastException = null;
                        break; // Success, exit retry loop
                    }
                    catch (HttpRequestException ex) when (attempt < MaxRetries - 1)
                    {
                        lastException = ex;
                        await Task.Delay(RetryDelayMs * (attempt + 1), cancellationToken);
                        LoggingManager.Log($"OpenSERV workflow request failed (attempt {attempt + 1}/{MaxRetries}): {ex.Message}", Logging.LogType.Warning);
                    }
                    catch (TaskCanceledException ex) when (attempt < MaxRetries - 1)
                    {
                        lastException = ex;
                        await Task.Delay(RetryDelayMs * (attempt + 1), cancellationToken);
                        LoggingManager.Log($"OpenSERV workflow request timeout (attempt {attempt + 1}/{MaxRetries}): {ex.Message}", Logging.LogType.Warning);
                    }
                }

                // Handle final exception
                if (lastException != null)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"OpenSERV workflow request failed after {MaxRetries} attempts: {lastException.Message}", 
                        lastException);
                    return result;
                }

                // Handle HTTP response
                if (response == null)
                {
                    OASISErrorHandling.HandleError(ref result, "No response received from OpenSERV");
                    return result;
                }

                var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Try to deserialize JSON response
                        var workflowResponse = System.Text.Json.JsonSerializer.Deserialize<OpenServWorkflowResponse>(responseContent);
                        if (workflowResponse != null)
                        {
                            workflowResponse.ExecutionTimeMs = executionTime;
                            result.Result = workflowResponse;
                            result.Message = "OpenSERV workflow executed successfully";
                        }
                        else
                        {
                            // If deserialization fails, treat as plain text response
                            result.Result = new OpenServWorkflowResponse
                            {
                                Success = true,
                                Result = responseContent,
                                Status = "completed",
                                ExecutionTimeMs = executionTime
                            };
                            result.Message = "OpenSERV workflow executed successfully";
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // If JSON deserialization fails, treat as plain text
                        result.Result = new OpenServWorkflowResponse
                        {
                            Success = true,
                            Result = responseContent,
                            Status = "completed",
                            ExecutionTimeMs = executionTime
                        };
                        result.Message = "OpenSERV workflow executed successfully";
                    }
                }
                else
                {
                    result.Result = new OpenServWorkflowResponse
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {responseContent}",
                        Status = "failed",
                        ExecutionTimeMs = executionTime
                    };
                    OASISErrorHandling.HandleError(ref result, 
                        $"OpenSERV workflow request failed with status {response.StatusCode}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing OpenSERV workflow: {ex.Message}", ex);
            }

            return result;
        }
    }
}

