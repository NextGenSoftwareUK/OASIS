using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.UnifiedAgentServiceManager
{
    /// <summary>
    /// Routes service requests to appropriate agent services based on routing strategies
    /// Supports load balancing and intelligent routing
    /// </summary>
    public class UnifiedAgentServiceRouter
    {
        /// <summary>
        /// Routing strategies available
        /// </summary>
        public enum RoutingStrategy
        {
            RoundRobin,         // Distribute requests evenly
            LeastBusy,          // Route to least busy service
            FastestResponse,    // Route to fastest responding service
            Random,             // Random selection
            Priority            // Route based on service priority
        }

        private readonly Dictionary<string, List<IUnifiedAgentService>> _serviceRegistry = new Dictionary<string, List<IUnifiedAgentService>>();
        private readonly Dictionary<string, int> _roundRobinCounters = new Dictionary<string, int>();
        private readonly Dictionary<string, DateTime> _lastRequestTime = new Dictionary<string, DateTime>();

        /// <summary>
        /// Register a service for routing
        /// </summary>
        public void RegisterService(IUnifiedAgentService service)
        {
            if (service == null || string.IsNullOrEmpty(service.ServiceId))
                return;

            foreach (var capability in service.Capabilities ?? new List<string>())
            {
                if (!_serviceRegistry.ContainsKey(capability))
                    _serviceRegistry[capability] = new List<IUnifiedAgentService>();

                // Remove if already exists (update)
                _serviceRegistry[capability].RemoveAll(s => s.ServiceId == service.ServiceId);
                _serviceRegistry[capability].Add(service);
            }
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public void UnregisterService(string serviceId)
        {
            foreach (var capability in _serviceRegistry.Keys.ToList())
            {
                _serviceRegistry[capability].RemoveAll(s => s.ServiceId == serviceId);
                if (_serviceRegistry[capability].Count == 0)
                    _serviceRegistry.Remove(capability);
            }

            _roundRobinCounters.Remove(serviceId);
            _lastRequestTime.Remove(serviceId);
        }

        /// <summary>
        /// Route a service request to an appropriate service
        /// </summary>
        public async Task<OASISResult<IUnifiedAgentService>> RouteServiceAsync(
            string serviceName,
            RoutingStrategy strategy = RoutingStrategy.LeastBusy,
            Dictionary<string, object> routingParams = null)
        {
            var result = new OASISResult<IUnifiedAgentService>();

            try
            {
                if (!_serviceRegistry.ContainsKey(serviceName) || _serviceRegistry[serviceName].Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, $"No services available for: {serviceName}");
                    return result;
                }

                var availableServices = _serviceRegistry[serviceName]
                    .Where(s => s.Status == UnifiedServiceStatus.Available || s.Status == UnifiedServiceStatus.Busy)
                    .ToList();

                if (availableServices.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, $"No available services for: {serviceName}");
                    return result;
                }

                IUnifiedAgentService selectedService = null;

                switch (strategy)
                {
                    case RoutingStrategy.RoundRobin:
                        selectedService = SelectRoundRobin(serviceName, availableServices);
                        break;

                    case RoutingStrategy.LeastBusy:
                        selectedService = await SelectLeastBusyAsync(availableServices);
                        break;

                    case RoutingStrategy.FastestResponse:
                        selectedService = await SelectFastestResponseAsync(availableServices);
                        break;

                    case RoutingStrategy.Random:
                        selectedService = SelectRandom(availableServices);
                        break;

                    case RoutingStrategy.Priority:
                        selectedService = SelectByPriority(availableServices, routingParams);
                        break;

                    default:
                        selectedService = availableServices.FirstOrDefault();
                        break;
                }

                if (selectedService == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to select service for: {serviceName}");
                    return result;
                }

                _lastRequestTime[selectedService.ServiceId] = DateTime.UtcNow;
                result.Result = selectedService;
                result.Message = $"Routed to service: {selectedService.ServiceName} using {strategy} strategy";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error routing service: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get all services for a capability
        /// </summary>
        public List<IUnifiedAgentService> GetServicesForCapability(string capability)
        {
            if (_serviceRegistry.ContainsKey(capability))
                return _serviceRegistry[capability].ToList();
            return new List<IUnifiedAgentService>();
        }

        /// <summary>
        /// Get routing statistics
        /// </summary>
        public Dictionary<string, object> GetRoutingStats()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalCapabilities"] = _serviceRegistry.Count,
                ["TotalServices"] = _serviceRegistry.Values.SelectMany(s => s).Distinct().Count(),
                ["LastRequestTimes"] = _lastRequestTime.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
            };

            return stats;
        }

        #region Private Routing Methods

        private IUnifiedAgentService SelectRoundRobin(string serviceName, List<IUnifiedAgentService> services)
        {
            if (!_roundRobinCounters.ContainsKey(serviceName))
                _roundRobinCounters[serviceName] = 0;

            var index = _roundRobinCounters[serviceName] % services.Count;
            _roundRobinCounters[serviceName] = (_roundRobinCounters[serviceName] + 1) % services.Count;

            return services[index];
        }

        private async Task<IUnifiedAgentService> SelectLeastBusyAsync(List<IUnifiedAgentService> services)
        {
            // Check health for all services to get current status
            var healthChecks = new List<(IUnifiedAgentService service, UnifiedServiceHealth health)>();

            foreach (var service in services)
            {
                try
                {
                    var healthResult = await service.CheckHealthAsync();
                    if (healthResult != null && healthResult.Result != null)
                    {
                        healthChecks.Add((service, healthResult.Result));
                    }
                }
                catch
                {
                    // Skip services that fail health check
                }
            }

            // Select service with best health (available status, lowest response time)
            var bestService = healthChecks
                .Where(h => h.health.IsHealthy)
                .OrderBy(h => h.health.ResponseTimeMs)
                .ThenBy(h => h.service.Status == UnifiedServiceStatus.Available ? 0 : 1)
                .FirstOrDefault();

            return bestService.service ?? services.FirstOrDefault();
        }

        private async Task<IUnifiedAgentService> SelectFastestResponseAsync(List<IUnifiedAgentService> services)
        {
            var responseTimes = new List<(IUnifiedAgentService service, long responseTime)>();

            foreach (var service in services)
            {
                try
                {
                    var startTime = DateTime.UtcNow;
                    var healthResult = await service.CheckHealthAsync();
                    var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                    if (healthResult != null && healthResult.Result != null && healthResult.Result.IsHealthy)
                    {
                        responseTimes.Add((service, responseTime));
                    }
                }
                catch
                {
                    // Skip services that fail
                }
            }

            if (responseTimes.Count == 0)
                return services.FirstOrDefault();

            return responseTimes.OrderBy(r => r.responseTime).First().service;
        }

        private IUnifiedAgentService SelectRandom(List<IUnifiedAgentService> services)
        {
            var random = new Random();
            return services[random.Next(services.Count)];
        }

        private IUnifiedAgentService SelectByPriority(List<IUnifiedAgentService> services, Dictionary<string, object> routingParams)
        {
            // Priority can be based on metadata or routing params
            // For now, prioritize by status (Available > Busy)
            return services
                .OrderBy(s => s.Status == UnifiedServiceStatus.Available ? 0 : 1)
                .ThenBy(s => s.Health?.ResponseTimeMs ?? long.MaxValue)
                .FirstOrDefault();
        }

        #endregion
    }
}





























