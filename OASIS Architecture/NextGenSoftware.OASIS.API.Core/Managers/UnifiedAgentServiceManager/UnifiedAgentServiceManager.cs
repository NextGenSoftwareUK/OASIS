using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers.UnifiedAgentServiceManager
{
    /// <summary>
    /// Unified service manager that abstracts A2A, OpenSERV, and SERV infrastructure
    /// Provides unified registration, discovery, routing, and health checking
    /// </summary>
    public class UnifiedAgentServiceManager : OASISManager
    {
        private static UnifiedAgentServiceManager _instance;
        private readonly Dictionary<string, IUnifiedAgentService> _serviceCache = new Dictionary<string, IUnifiedAgentService>();
        private readonly UnifiedAgentServiceRouter _router;
        private readonly UnifiedAgentServiceHealthChecker _healthChecker;

        public static UnifiedAgentServiceManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UnifiedAgentServiceManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public UnifiedAgentServiceManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) 
            : base(OASISStorageProvider, OASISDNA)
        {
            _router = new UnifiedAgentServiceRouter();
            _healthChecker = new UnifiedAgentServiceHealthChecker();

            // Subscribe to health checker events
            _healthChecker.OnServiceUnhealthy += HealthChecker_OnServiceUnhealthy;
            _healthChecker.OnServiceRemoved += HealthChecker_OnServiceRemoved;
        }

        /// <summary>
        /// Register a unified agent service
        /// </summary>
        public async Task<OASISResult<bool>> RegisterServiceAsync(IUnifiedAgentService service)
        {
            var result = new OASISResult<bool>();

            try
            {
                if (service == null || string.IsNullOrEmpty(service.ServiceId))
                {
                    OASISErrorHandling.HandleError(ref result, "Service is null or ServiceId is empty");
                    return result;
                }

                // Set registration timestamp if not set
                if (service.RegisteredAt == default)
                    service.RegisteredAt = DateTime.UtcNow;

                // Add to cache
                lock (_serviceCache)
                {
                    _serviceCache[service.ServiceId] = service;
                }

                // Register with router
                _router.RegisterService(service);

                // Register with health checker
                _healthChecker.RegisterService(service);

                result.Result = true;
                result.Message = $"Service {service.ServiceName} registered successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error registering service: {ex.Message}", ex);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public async Task<OASISResult<bool>> UnregisterServiceAsync(string serviceId)
        {
            var result = new OASISResult<bool>();

            try
            {
                lock (_serviceCache)
                {
                    if (_serviceCache.ContainsKey(serviceId))
                    {
                        _serviceCache.Remove(serviceId);
                    }
                }

                _router.UnregisterService(serviceId);
                _healthChecker.UnregisterService(serviceId);

                result.Result = true;
                result.Message = $"Service {serviceId} unregistered successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unregistering service: {ex.Message}", ex);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Discover services by capability
        /// </summary>
        public async Task<OASISResult<List<IUnifiedAgentService>>> DiscoverServicesAsync(
            string capability,
            bool healthyOnly = true)
        {
            var result = new OASISResult<List<IUnifiedAgentService>>();

            try
            {
                var services = _router.GetServicesForCapability(capability);

                if (healthyOnly)
                {
                    services = services
                        .Where(s => s.Status == UnifiedServiceStatus.Available || 
                                   s.Status == UnifiedServiceStatus.Busy)
                        .ToList();
                }

                result.Result = services;
                result.Message = $"Found {services.Count} services for capability: {capability}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error discovering services: {ex.Message}", ex);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Discover services by multiple capabilities
        /// </summary>
        public async Task<OASISResult<List<IUnifiedAgentService>>> DiscoverServicesAsync(
            List<string> capabilities,
            bool healthyOnly = true)
        {
            var result = new OASISResult<List<IUnifiedAgentService>>();

            try
            {
                var allServices = new HashSet<IUnifiedAgentService>();

                foreach (var capability in capabilities)
                {
                    var services = _router.GetServicesForCapability(capability);
                    foreach (var service in services)
                    {
                        allServices.Add(service);
                    }
                }

                var serviceList = allServices.ToList();

                if (healthyOnly)
                {
                    serviceList = serviceList
                        .Where(s => s.Status == UnifiedServiceStatus.Available || 
                                   s.Status == UnifiedServiceStatus.Busy)
                        .ToList();
                }

                result.Result = serviceList;
                result.Message = $"Found {serviceList.Count} services for capabilities: {string.Join(", ", capabilities)}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error discovering services: {ex.Message}", ex);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Get a service by ID
        /// </summary>
        public async Task<OASISResult<IUnifiedAgentService>> GetServiceAsync(string serviceId)
        {
            var result = new OASISResult<IUnifiedAgentService>();

            try
            {
                lock (_serviceCache)
                {
                    if (_serviceCache.ContainsKey(serviceId))
                    {
                        result.Result = _serviceCache[serviceId];
                        result.Message = "Service found";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Service {serviceId} not found");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting service: {ex.Message}", ex);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Get all registered services
        /// </summary>
        public async Task<OASISResult<List<IUnifiedAgentService>>> GetAllServicesAsync(bool healthyOnly = false)
        {
            var result = new OASISResult<List<IUnifiedAgentService>>();

            try
            {
                List<IUnifiedAgentService> services;
                lock (_serviceCache)
                {
                    services = _serviceCache.Values.ToList();
                }

                if (healthyOnly)
                {
                    services = services
                        .Where(s => s.Status == UnifiedServiceStatus.Available || 
                                   s.Status == UnifiedServiceStatus.Busy)
                        .ToList();
                }

                result.Result = services;
                result.Message = $"Found {services.Count} services";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting all services: {ex.Message}", ex);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Route a service request
        /// </summary>
        public async Task<OASISResult<IUnifiedAgentService>> RouteServiceAsync(
            string serviceName,
            UnifiedAgentServiceRouter.RoutingStrategy strategy = UnifiedAgentServiceRouter.RoutingStrategy.LeastBusy,
            Dictionary<string, object> routingParams = null)
        {
            return await _router.RouteServiceAsync(serviceName, strategy, routingParams);
        }

        /// <summary>
        /// Execute a service request (route and execute)
        /// </summary>
        public async Task<OASISResult<object>> ExecuteServiceAsync(
            string serviceName,
            Dictionary<string, object> parameters,
            UnifiedAgentServiceRouter.RoutingStrategy strategy = UnifiedAgentServiceRouter.RoutingStrategy.LeastBusy)
        {
            var result = new OASISResult<object>();

            try
            {
                // Route to appropriate service
                var routeResult = await RouteServiceAsync(serviceName, strategy);
                if (routeResult.IsError || routeResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to route service: {routeResult.Message}");
                    return result;
                }

                // Execute service
                var service = routeResult.Result;
                var executeResult = await service.ExecuteServiceAsync(serviceName, parameters);

                if (executeResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Service execution failed: {executeResult.Message}");
                    return result;
                }

                result.Result = executeResult.Result;
                result.Message = $"Service executed successfully via {service.ServiceName}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing service: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Check health of a specific service
        /// </summary>
        public async Task<OASISResult<UnifiedServiceHealth>> CheckServiceHealthAsync(string serviceId)
        {
            return await _healthChecker.CheckServiceHealthAsync(serviceId);
        }

        /// <summary>
        /// Get health status for all services
        /// </summary>
        public Dictionary<string, UnifiedServiceHealth> GetAllServiceHealth()
        {
            return _healthChecker.GetAllServiceHealth();
        }

        /// <summary>
        /// Get routing statistics
        /// </summary>
        public Dictionary<string, object> GetRoutingStats()
        {
            return _router.GetRoutingStats();
        }

        /// <summary>
        /// Clear service cache
        /// </summary>
        public void ClearCache()
        {
            lock (_serviceCache)
            {
                _serviceCache.Clear();
            }
        }

        #region Event Handlers

        private void HealthChecker_OnServiceUnhealthy(object sender, ServiceHealthEventArgs e)
        {
            // Log or handle unhealthy service
            OASISErrorHandling.HandleError($"Service {e.ServiceId} is unhealthy: {e.Health?.ErrorMessage}");
        }

        private void HealthChecker_OnServiceRemoved(object sender, ServiceHealthEventArgs e)
        {
            // Remove from cache
            lock (_serviceCache)
            {
                _serviceCache.Remove(e.ServiceId);
            }

            OASISErrorHandling.HandleError($"Service {e.ServiceId} removed due to health issues");
        }

        #endregion
    }
}





























