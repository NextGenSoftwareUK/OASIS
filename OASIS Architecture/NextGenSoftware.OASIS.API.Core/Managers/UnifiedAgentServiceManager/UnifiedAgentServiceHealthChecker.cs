using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.UnifiedAgentServiceManager
{
    /// <summary>
    /// Monitors and checks health of unified agent services
    /// Automatically removes unhealthy services and tracks service status
    /// </summary>
    public class UnifiedAgentServiceHealthChecker : IDisposable
    {
        private readonly Dictionary<string, IUnifiedAgentService> _services = new Dictionary<string, IUnifiedAgentService>();
        private readonly Dictionary<string, UnifiedServiceHealth> _healthCache = new Dictionary<string, UnifiedServiceHealth>();
        private readonly Timer _healthCheckTimer;
        private readonly int _healthCheckIntervalMs;
        private readonly int _healthCheckTimeoutMs;
        private readonly int _maxConsecutiveFailures;

        /// <summary>
        /// Event fired when a service becomes unhealthy
        /// </summary>
        public event EventHandler<ServiceHealthEventArgs> OnServiceUnhealthy;

        /// <summary>
        /// Event fired when a service becomes healthy
        /// </summary>
        public event EventHandler<ServiceHealthEventArgs> OnServiceHealthy;

        /// <summary>
        /// Event fired when a service is removed due to health issues
        /// </summary>
        public event EventHandler<ServiceHealthEventArgs> OnServiceRemoved;

        public UnifiedAgentServiceHealthChecker(
            int healthCheckIntervalMs = 30000,  // 30 seconds default
            int healthCheckTimeoutMs = 5000,    // 5 seconds timeout
            int maxConsecutiveFailures = 3)      // Remove after 3 failures
        {
            _healthCheckIntervalMs = healthCheckIntervalMs;
            _healthCheckTimeoutMs = healthCheckTimeoutMs;
            _maxConsecutiveFailures = maxConsecutiveFailures;

            // Start periodic health checks
            _healthCheckTimer = new Timer(PerformHealthChecks, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_healthCheckIntervalMs));
        }

        /// <summary>
        /// Register a service for health monitoring
        /// </summary>
        public void RegisterService(IUnifiedAgentService service)
        {
            if (service == null || string.IsNullOrEmpty(service.ServiceId))
                return;

            lock (_services)
            {
                _services[service.ServiceId] = service;
                _healthCache[service.ServiceId] = new UnifiedServiceHealth
                {
                    Status = UnifiedServiceStatus.Available,
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Unregister a service from health monitoring
        /// </summary>
        public void UnregisterService(string serviceId)
        {
            lock (_services)
            {
                _services.Remove(serviceId);
                _healthCache.Remove(serviceId);
            }
        }

        /// <summary>
        /// Check health of a specific service
        /// </summary>
        public async Task<OASISResult<UnifiedServiceHealth>> CheckServiceHealthAsync(string serviceId)
        {
            var result = new OASISResult<UnifiedServiceHealth>();

            try
            {
                IUnifiedAgentService service;
                lock (_services)
                {
                    if (!_services.ContainsKey(serviceId))
                    {
                        OASISErrorHandling.HandleError(ref result, $"Service {serviceId} not found");
                        return result;
                    }
                    service = _services[serviceId];
                }

                var healthResult = await CheckServiceHealthInternalAsync(service);
                
                // Update cache
                lock (_healthCache)
                {
                    _healthCache[serviceId] = healthResult.Result;
                }

                // Update service status
                service.Status = healthResult.Result.Status;
                service.Health = healthResult.Result;
                service.LastHealthCheck = DateTime.UtcNow;

                // Fire events if status changed
                var previousHealth = _healthCache.ContainsKey(serviceId) ? _healthCache[serviceId] : null;
                if (previousHealth != null)
                {
                    if (previousHealth.IsHealthy && !healthResult.Result.IsHealthy)
                    {
                        OnServiceUnhealthy?.Invoke(this, new ServiceHealthEventArgs
                        {
                            ServiceId = serviceId,
                            Service = service,
                            Health = healthResult.Result
                        });
                    }
                    else if (!previousHealth.IsHealthy && healthResult.Result.IsHealthy)
                    {
                        OnServiceHealthy?.Invoke(this, new ServiceHealthEventArgs
                        {
                            ServiceId = serviceId,
                            Service = service,
                            Health = healthResult.Result
                        });
                    }
                }

                result.Result = healthResult.Result;
                result.Message = $"Health check completed for {serviceId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error checking service health: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get health status for all registered services
        /// </summary>
        public Dictionary<string, UnifiedServiceHealth> GetAllServiceHealth()
        {
            lock (_healthCache)
            {
                return _healthCache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        /// <summary>
        /// Get health status for a specific service
        /// </summary>
        public UnifiedServiceHealth GetServiceHealth(string serviceId)
        {
            lock (_healthCache)
            {
                return _healthCache.ContainsKey(serviceId) ? _healthCache[serviceId] : null;
            }
        }

        /// <summary>
        /// Perform health checks on all registered services
        /// </summary>
        private async void PerformHealthChecks(object state)
        {
            List<IUnifiedAgentService> servicesToCheck;
            lock (_services)
            {
                servicesToCheck = _services.Values.ToList();
            }

            var healthCheckTasks = servicesToCheck.Select(async service =>
            {
                try
                {
                    var healthResult = await CheckServiceHealthInternalAsync(service);
                    
                    lock (_healthCache)
                    {
                        var previousHealth = _healthCache.ContainsKey(service.ServiceId) ? _healthCache[service.ServiceId] : null;
                        _healthCache[service.ServiceId] = healthResult.Result;

                        // Update service status
                        service.Status = healthResult.Result.Status;
                        service.Health = healthResult.Result;
                        service.LastHealthCheck = DateTime.UtcNow;

                        // Track consecutive failures
                        if (!healthResult.Result.IsHealthy)
                        {
                            var failureCount = previousHealth?.Metrics?.ContainsKey("ConsecutiveFailures") == true
                                ? (int)previousHealth.Metrics["ConsecutiveFailures"]
                                : 0;
                            
                            failureCount++;
                            healthResult.Result.Metrics["ConsecutiveFailures"] = failureCount;

                            // Remove service if too many failures
                            if (failureCount >= _maxConsecutiveFailures)
                            {
                                OnServiceRemoved?.Invoke(this, new ServiceHealthEventArgs
                                {
                                    ServiceId = service.ServiceId,
                                    Service = service,
                                    Health = healthResult.Result
                                });

                                // Remove from registry
                                lock (_services)
                                {
                                    _services.Remove(service.ServiceId);
                                }
                                _healthCache.Remove(service.ServiceId);
                            }
                        }
                        else
                        {
                            // Reset failure count on success
                            if (healthResult.Result.Metrics.ContainsKey("ConsecutiveFailures"))
                                healthResult.Result.Metrics["ConsecutiveFailures"] = 0;
                        }

                        // Fire events for status changes
                        if (previousHealth != null)
                        {
                            if (previousHealth.IsHealthy && !healthResult.Result.IsHealthy)
                            {
                                OnServiceUnhealthy?.Invoke(this, new ServiceHealthEventArgs
                                {
                                    ServiceId = service.ServiceId,
                                    Service = service,
                                    Health = healthResult.Result
                                });
                            }
                            else if (!previousHealth.IsHealthy && healthResult.Result.IsHealthy)
                            {
                                OnServiceHealthy?.Invoke(this, new ServiceHealthEventArgs
                                {
                                    ServiceId = service.ServiceId,
                                    Service = service,
                                    Health = healthResult.Result
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other services
                    OASISErrorHandling.HandleError($"Error checking health for {service.ServiceId}: {ex.Message}", ex);
                }
            });

            await Task.WhenAll(healthCheckTasks);
        }

        /// <summary>
        /// Internal method to check service health with timeout
        /// </summary>
        private async Task<OASISResult<UnifiedServiceHealth>> CheckServiceHealthInternalAsync(IUnifiedAgentService service)
        {
            var result = new OASISResult<UnifiedServiceHealth>();
            var startTime = DateTime.UtcNow;

            try
            {
                // Use timeout to prevent hanging
                var healthCheckTask = service.CheckHealthAsync();
                var timeoutTask = Task.Delay(_healthCheckTimeoutMs);

                var completedTask = await Task.WhenAny(healthCheckTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    result.Result = new UnifiedServiceHealth
                    {
                        Status = UnifiedServiceStatus.Unhealthy,
                        ResponseTimeMs = _healthCheckTimeoutMs,
                        CheckedAt = DateTime.UtcNow,
                        ErrorMessage = $"Health check timed out after {_healthCheckTimeoutMs}ms"
                    };
                }
                else
                {
                    // Health check completed
                    var healthResult = await healthCheckTask;
                    var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                    if (healthResult != null && healthResult.Result != null)
                    {
                        result.Result = healthResult.Result;
                        result.Result.ResponseTimeMs = responseTime;
                        result.Result.CheckedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        result.Result = new UnifiedServiceHealth
                        {
                            Status = UnifiedServiceStatus.Unhealthy,
                            ResponseTimeMs = responseTime,
                            CheckedAt = DateTime.UtcNow,
                            ErrorMessage = "Health check returned null result"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                result.Result = new UnifiedServiceHealth
                {
                    Status = UnifiedServiceStatus.Unhealthy,
                    ResponseTimeMs = responseTime,
                    CheckedAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                };
            }

            return result;
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
        }
    }

    /// <summary>
    /// Event arguments for service health events
    /// </summary>
    public class ServiceHealthEventArgs : EventArgs
    {
        public string ServiceId { get; set; }
        public IUnifiedAgentService Service { get; set; }
        public UnifiedServiceHealth Health { get; set; }
    }
}

