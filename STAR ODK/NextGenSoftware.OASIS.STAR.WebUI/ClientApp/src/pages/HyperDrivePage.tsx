import React, { useState, useEffect } from 'react';
import { 
  HyperDriveConfig, 
  HyperDriveStatus, 
  DashboardData, 
  AnalyticsReport,
  OptimizationRecommendation,
  FailoverPrediction,
  LoadBalancingStrategy,
  Priority,
  SubscriptionPlanType
} from '../types/hyperDriveTypes';
import { hyperDriveService } from '../services/core/hyperDriveService';
import ProviderManager from '../components/HyperDrive/ProviderManager';
import DataPermissionsManager from '../components/HyperDrive/DataPermissionsManager';

const HyperDrivePage: React.FC = () => {
  const [activeTab, setActiveTab] = useState('dashboard');
  const [config, setConfig] = useState<HyperDriveConfig | null>(null);
  const [status, setStatus] = useState<HyperDriveStatus | null>(null);
  const [dashboard, setDashboard] = useState<DashboardData | null>(null);
  const [analytics, setAnalytics] = useState<AnalyticsReport | null>(null);
  const [recommendations, setRecommendations] = useState<OptimizationRecommendation[]>([]);
  const [failoverPredictions, setFailoverPredictions] = useState<FailoverPrediction | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [subscriptionPlan, setSubscriptionPlan] = useState<SubscriptionPlanType>(SubscriptionPlanType.Free);
  const [mode, setMode] = useState<string>('Legacy');

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const [configData, statusData, dashboardData, analyticsData, recommendationsData, predictionsData, modeData] = await Promise.all([
        hyperDriveService.getConfiguration(),
        hyperDriveService.getStatus(),
        hyperDriveService.getDashboardData(),
        hyperDriveService.getAnalyticsReport(),
        hyperDriveService.getAIRecommendations(),
        hyperDriveService.getFailurePredictions(),
        hyperDriveService.getMode()
      ]);

      setConfig(configData);
      setStatus(statusData);
      setDashboard(dashboardData);
      setAnalytics(analyticsData);
      setRecommendations(recommendationsData);
      setFailoverPredictions(predictionsData);
      setMode(modeData);
    } catch (err) {
      setError('Failed to load HyperDrive data');
      console.error('Error loading HyperDrive data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleConfigUpdate = async (updatedConfig: HyperDriveConfig) => {
    try {
      await hyperDriveService.updateConfiguration(updatedConfig);
      setConfig(updatedConfig);
      await loadData(); // Refresh all data
    } catch (err) {
      setError('Failed to update configuration');
      console.error('Error updating configuration:', err);
    }
  };

  const handleResetConfig = async () => {
    try {
      await hyperDriveService.resetConfiguration();
      await loadData();
    } catch (err) {
      setError('Failed to reset configuration');
      console.error('Error resetting configuration:', err);
    }
  };

  const renderDashboard = () => (
    <div className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-semibold text-gray-900">System Health</h3>
          <p className="text-3xl font-bold text-green-600">{dashboard?.systemHealth?.toFixed(1)}%</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-semibold text-gray-900">Active Providers</h3>
          <p className="text-3xl font-bold text-blue-600">{dashboard?.activeProviders}</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-semibold text-gray-900">Total Requests</h3>
          <p className="text-3xl font-bold text-purple-600">{dashboard?.totalRequests?.toLocaleString()}</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-semibold text-gray-900">Avg Response Time</h3>
          <p className="text-3xl font-bold text-orange-600">{dashboard?.performanceMetrics?.averageResponseTime?.toFixed(1)}ms</p>
        </div>
      </div>

      {dashboard?.alerts && dashboard.alerts.length > 0 && (
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Active Alerts</h3>
          <div className="space-y-2">
            {dashboard.alerts.map((alert, index) => (
              <div key={index} className={`p-3 rounded-lg ${
                alert.severity === 'Critical' ? 'bg-red-100 border-red-300' :
                alert.severity === 'High' ? 'bg-orange-100 border-orange-300' :
                'bg-yellow-100 border-yellow-300'
              } border`}>
                <div className="flex justify-between items-center">
                  <span className="font-medium">{alert.providerType}</span>
                  <span className={`px-2 py-1 rounded text-xs ${
                    alert.severity === 'Critical' ? 'bg-red-200 text-red-800' :
                    alert.severity === 'High' ? 'bg-orange-200 text-orange-800' :
                    'bg-yellow-200 text-yellow-800'
                  }`}>
                    {alert.severity}
                  </span>
                </div>
                <p className="text-sm text-gray-600 mt-1">{alert.message}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );

  const renderConfiguration = () => (
    <div className="space-y-6">
      {config && (
        <>
          {/* Mode Toggle */}
          <div className="bg-white p-6 rounded-lg shadow">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">🛠️ HyperDrive Mode</h3>
            <div className="flex items-center gap-4">
              <select
                value={mode}
                onChange={async (e) => {
                  const newMode = e.target.value;
                  setMode(newMode);
                  try { await hyperDriveService.setMode(newMode); } catch {}
                }}
                className="p-2 border border-gray-300 rounded-md"
              >
                <option value="Legacy">Legacy (v1)</option>
                <option value="OASISHyperDrive2">OASIS HyperDrive 2 (v2)</option>
              </select>
              <p className="text-sm text-gray-500">
                Legacy: auto-replication & auto-failover. V2: adds auto-load balancing and advanced AI/analytics.
              </p>
            </div>
            {/* v1 vs v2 quick comparison */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-6">
              <div className="border rounded-lg p-4">
                <h4 className="font-semibold text-gray-900 mb-2">Legacy (v1)</h4>
                <ul className="list-disc pl-5 text-sm text-gray-700 space-y-1">
                  <li>Auto-Replication</li>
                  <li>Auto-Failover</li>
                  <li>Basic provider lists (Failover/Replication)</li>
                </ul>
              </div>
              <div className="border rounded-lg p-4">
                <h4 className="font-semibold text-gray-900 mb-2">OASIS HyperDrive 2 (v2)</h4>
                <ul className="list-disc pl-5 text-sm text-gray-700 space-y-1">
                  <li>Auto-Load Balancing (round-robin, weighted, least-connections, latency-first)</li>
                  <li>Enhanced Auto-Replication (provider/data-type/schedule/cost/permissions)</li>
                  <li>Predictive Failover with escalation rules</li>
                  <li>AI Optimization & smart recommendations</li>
                  <li>Advanced analytics (performance, cost, predictive)</li>
                  <li>Subscription-aware quotas, alerts, notifications</li>
                  <li>Full WebAPI + UI configuration</li>
                </ul>
              </div>
            </div>
          </div>
          {/* Core HyperDrive Settings */}
          <div className="bg-white p-6 rounded-lg shadow">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">🚀 Core HyperDrive Settings</h3>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Default Strategy
                </label>
                <select 
                  value={config.defaultStrategy}
                  onChange={(e) => setConfig({...config, defaultStrategy: e.target.value as LoadBalancingStrategy})}
                  className="w-full p-2 border border-gray-300 rounded-md"
                >
                  {Object.values(LoadBalancingStrategy).map(strategy => (
                    <option key={strategy} value={strategy}>{strategy}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Max Retry Attempts
                </label>
                <input
                  type="number"
                  value={config.maxRetryAttempts}
                  onChange={(e) => setConfig({...config, maxRetryAttempts: parseInt(e.target.value)})}
                  className="w-full p-2 border border-gray-300 rounded-md"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Request Timeout (ms)
                </label>
                <input
                  type="number"
                  value={config.requestTimeoutMs}
                  onChange={(e) => setConfig({...config, requestTimeoutMs: parseInt(e.target.value)})}
                  className="w-full p-2 border border-gray-300 rounded-md"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Health Check Interval (ms)
                </label>
                <input
                  type="number"
                  value={config.healthCheckIntervalMs}
                  onChange={(e) => setConfig({...config, healthCheckIntervalMs: parseInt(e.target.value)})}
                  className="w-full p-2 border border-gray-300 rounded-md"
                />
              </div>
            </div>
          </div>

          {/* Auto Failover Settings */}
          <div className="bg-white p-6 rounded-lg shadow">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">🔄 Auto Failover Settings</h3>
            
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700">Enable Auto Failover</label>
                  <p className="text-xs text-gray-500">Automatically switch to backup providers when primary fails</p>
                </div>
                <input
                  type="checkbox"
                  checked={config.autoFailoverEnabled}
                  onChange={(e) => setConfig({...config, autoFailoverEnabled: e.target.checked})}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Max Latency Threshold (ms)
                  </label>
                  <input
                    type="number"
                    value={config.maxLatencyThresholdMs}
                    onChange={(e) => setConfig({...config, maxLatencyThresholdMs: parseInt(e.target.value)})}
                    className="w-full p-2 border border-gray-300 rounded-md"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Max Error Rate Threshold (%)
                  </label>
                  <input
                    type="number"
                    step="0.1"
                    min="0"
                    max="100"
                    value={config.maxErrorRateThreshold}
                    onChange={(e) => setConfig({...config, maxErrorRateThreshold: parseFloat(e.target.value)})}
                    className="w-full p-2 border border-gray-300 rounded-md"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Auto Failover Providers
                </label>
                <div className="flex flex-wrap gap-2">
                  {config.autoFailoverProviders.map((provider, index) => (
                    <span key={index} className="px-3 py-1 bg-red-100 text-red-800 rounded-full text-sm">
                      {provider}
                      <button
                        onClick={() => setConfig({
                          ...config, 
                          autoFailoverProviders: config.autoFailoverProviders.filter(p => p !== provider)
                        })}
                        className="ml-2 text-red-600 hover:text-red-800"
                      >
                        ×
                      </button>
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Auto Replication Settings */}
          <div className="bg-white p-6 rounded-lg shadow">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">📋 Auto Replication Settings</h3>
            
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700">Enable Auto Replication</label>
                  <p className="text-xs text-gray-500">Automatically replicate data across multiple providers</p>
                </div>
                <input
                  type="checkbox"
                  checked={config.autoReplicationEnabled}
                  onChange={(e) => setConfig({...config, autoReplicationEnabled: e.target.checked})}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Auto Replication Providers
                </label>
                <div className="flex flex-wrap gap-2">
                  {config.autoReplicationProviders.map((provider, index) => (
                    <span key={index} className="px-3 py-1 bg-green-100 text-green-800 rounded-full text-sm">
                      {provider}
                      <button
                        onClick={() => setConfig({
                          ...config, 
                          autoReplicationProviders: config.autoReplicationProviders.filter(p => p !== provider)
                        })}
                        className="ml-2 text-green-600 hover:text-green-800"
                      >
                        ×
                      </button>
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Auto Load Balancing Settings */}
          <div className="bg-white p-6 rounded-lg shadow">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">⚖️ Auto Load Balancing Settings</h3>
            
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700">Enable Auto Load Balancing</label>
                  <p className="text-xs text-gray-500">Automatically distribute requests across providers</p>
                </div>
                <input
                  type="checkbox"
                  checked={config.autoLoadBalancingEnabled}
                  onChange={(e) => setConfig({...config, autoLoadBalancingEnabled: e.target.checked})}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Load Balancing Providers
                </label>
                <div className="flex flex-wrap gap-2">
                  {config.loadBalancingProviders.map((provider, index) => (
                    <span key={index} className="px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm">
                      {provider}
                      <button
                        onClick={() => setConfig({
                          ...config, 
                          loadBalancingProviders: config.loadBalancingProviders.filter(p => p !== provider)
                        })}
                        className="ml-2 text-blue-600 hover:text-blue-800"
                      >
                        ×
                      </button>
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Load Balancing Weights */}
          <div className="bg-white p-6 rounded-lg shadow">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">🎯 Load Balancing Weights</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Latency Weight (Primary)
                </label>
                <input
                  type="number"
                  step="0.1"
                  min="0"
                  max="1"
                  value={config.latencyWeight}
                  onChange={(e) => setConfig({...config, latencyWeight: parseFloat(e.target.value)})}
                  className="w-full p-2 border border-gray-300 rounded-md"
                />
                <p className="text-xs text-gray-500 mt-1">Primary criteria for provider selection</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Performance Weight
                </label>
                <input
                  type="number"
                  step="0.1"
                  min="0"
                  max="1"
                  value={config.performanceWeight}
                  onChange={(e) => setConfig({...config, performanceWeight: parseFloat(e.target.value)})}
                  className="w-full p-2 border border-gray-300 rounded-md"
                />
                <p className="text-xs text-gray-500 mt-1">Throughput and response time</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Cost Weight
                </label>
                <input
                  type="number"
                  step="0.1"
                  min="0"
                  max="1"
                  value={config.costWeight}
                  onChange={(e) => setConfig({...config, costWeight: parseFloat(e.target.value)})}
                  className="w-full p-2 border border-gray-300 rounded-md"
                />
                <p className="text-xs text-gray-500 mt-1">Cost efficiency consideration</p>
              </div>
            </div>
          </div>

          {/* Geographic Settings */}
          <div className="bg-white p-6 rounded-lg shadow">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">🌍 Geographic Settings</h3>
            
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700">Enable Geographic Routing</label>
                  <p className="text-xs text-gray-500">Route requests to geographically closest providers</p>
                </div>
                <input
                  type="checkbox"
                  checked={config.geographicConfig.isEnabled}
                  onChange={(e) => setConfig({
                    ...config, 
                    geographicConfig: {...config.geographicConfig, isEnabled: e.target.checked}
                  })}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Default Region
                  </label>
                  <input
                    type="text"
                    value={config.geographicConfig.defaultRegion}
                    onChange={(e) => setConfig({
                      ...config, 
                      geographicConfig: {...config.geographicConfig, defaultRegion: e.target.value}
                    })}
                    className="w-full p-2 border border-gray-300 rounded-md"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Max Distance (km)
                  </label>
                  <input
                    type="number"
                    value={config.geographicConfig.maxDistanceKm}
                    onChange={(e) => setConfig({
                      ...config, 
                      geographicConfig: {...config.geographicConfig, maxDistanceKm: parseInt(e.target.value)}
                    })}
                    className="w-full p-2 border border-gray-300 rounded-md"
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Cost Settings */}
          <div className="bg-white p-6 rounded-lg shadow">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">💰 Cost Settings</h3>
            
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700">Enable Cost-Based Routing</label>
                  <p className="text-xs text-gray-500">Consider cost when selecting providers</p>
                </div>
                <input
                  type="checkbox"
                  checked={config.costConfig.isEnabled}
                  onChange={(e) => setConfig({
                    ...config, 
                    costConfig: {...config.costConfig, isEnabled: e.target.checked}
                  })}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Max Cost Per Operation ($)
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    value={config.costConfig.maxCostPerOperation}
                    onChange={(e) => setConfig({
                      ...config, 
                      costConfig: {...config.costConfig, maxCostPerOperation: parseFloat(e.target.value)}
                    })}
                    className="w-full p-2 border border-gray-300 rounded-md"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Currency
                  </label>
                  <select
                    value={config.costConfig.currency}
                    onChange={(e) => setConfig({
                      ...config, 
                      costConfig: {...config.costConfig, currency: e.target.value}
                    })}
                    className="w-full p-2 border border-gray-300 rounded-md"
                  >
                    <option value="USD">USD</option>
                    <option value="EUR">EUR</option>
                    <option value="GBP">GBP</option>
                    <option value="JPY">JPY</option>
                  </select>
                </div>
              </div>
            </div>
          </div>

          {/* Performance Settings */}
          <div className="bg-white p-6 rounded-lg shadow">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">⚡ Performance Settings</h3>
            
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700">Enable Performance Monitoring</label>
                  <p className="text-xs text-gray-500">Monitor and optimize provider performance</p>
                </div>
                <input
                  type="checkbox"
                  checked={config.performanceConfig.isEnabled}
                  onChange={(e) => setConfig({
                    ...config, 
                    performanceConfig: {...config.performanceConfig, isEnabled: e.target.checked}
                  })}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Max Response Time (ms)
                  </label>
                  <input
                    type="number"
                    value={config.performanceConfig.maxResponseTimeMs}
                    onChange={(e) => setConfig({
                      ...config, 
                      performanceConfig: {...config.performanceConfig, maxResponseTimeMs: parseInt(e.target.value)}
                    })}
                    className="w-full p-2 border border-gray-300 rounded-md"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Max Error Rate (%)
                  </label>
                  <input
                    type="number"
                    step="0.1"
                    min="0"
                    max="100"
                    value={config.performanceConfig.maxErrorRate}
                    onChange={(e) => setConfig({
                      ...config, 
                      performanceConfig: {...config.performanceConfig, maxErrorRate: parseFloat(e.target.value)}
                    })}
                    className="w-full p-2 border border-gray-300 rounded-md"
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="bg-white p-6 rounded-lg shadow">
            <div className="flex space-x-4">
              <button
                onClick={() => handleConfigUpdate(config)}
                className="px-6 py-3 bg-blue-600 text-white rounded-md hover:bg-blue-700 font-medium"
              >
                💾 Save Configuration
              </button>
              <button
                onClick={handleResetConfig}
                className="px-6 py-3 bg-gray-600 text-white rounded-md hover:bg-gray-700 font-medium"
              >
                🔄 Reset to Defaults
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );

  const renderAnalytics = () => (
    <div className="space-y-6">
      {analytics && (
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Analytics Report</h3>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {analytics.providerAnalytics.map((provider, index) => (
              <div key={index} className="border border-gray-200 rounded-lg p-4">
                <h4 className="font-semibold text-gray-900">{provider.providerType}</h4>
                <div className="mt-2 space-y-1 text-sm">
                  <p>Requests: {provider.totalRequests.toLocaleString()}</p>
                  <p>Success Rate: {((provider.successfulRequests / provider.totalRequests) * 100).toFixed(1)}%</p>
                  <p>Avg Response: {provider.averageResponseTime.toFixed(1)}ms</p>
                  <p>Uptime: {provider.uptimePercentage.toFixed(1)}%</p>
                  <p>Cost: ${provider.averageCost.toFixed(4)}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );

  const renderRecommendations = () => (
    <div className="space-y-6">
      {recommendations.length > 0 && (
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">AI Optimization Recommendations</h3>
          
          <div className="space-y-4">
            {recommendations.map((rec, index) => (
              <div key={index} className={`p-4 rounded-lg border ${
                rec.priority === Priority.Critical ? 'border-red-300 bg-red-50' :
                rec.priority === Priority.High ? 'border-orange-300 bg-orange-50' :
                rec.priority === Priority.Medium ? 'border-yellow-300 bg-yellow-50' :
                'border-gray-300 bg-gray-50'
              }`}>
                <div className="flex justify-between items-start mb-2">
                  <h4 className="font-semibold">{rec.providerType} - {rec.type}</h4>
                  <span className={`px-2 py-1 rounded text-xs ${
                    rec.priority === Priority.Critical ? 'bg-red-200 text-red-800' :
                    rec.priority === Priority.High ? 'bg-orange-200 text-orange-800' :
                    rec.priority === Priority.Medium ? 'bg-yellow-200 text-yellow-800' :
                    'bg-gray-200 text-gray-800'
                  }`}>
                    {rec.priority}
                  </span>
                </div>
                <p className="text-sm text-gray-600 mb-2">{rec.description}</p>
                <ul className="text-sm text-gray-700">
                  {rec.suggestedActions.map((action, actionIndex) => (
                    <li key={actionIndex} className="list-disc list-inside">• {action}</li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );

  const renderFailoverPredictions = () => (
    <div className="space-y-6">
      {failoverPredictions && (
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Failure Predictions</h3>
          
          <div className={`p-4 rounded-lg border ${
            failoverPredictions.riskLevel === 'Critical' ? 'border-red-300 bg-red-50' :
            failoverPredictions.riskLevel === 'High' ? 'border-orange-300 bg-orange-50' :
            failoverPredictions.riskLevel === 'Medium' ? 'border-yellow-300 bg-yellow-50' :
            'border-green-300 bg-green-50'
          }`}>
            <div className="flex justify-between items-center mb-2">
              <h4 className="font-semibold">Overall Risk Level</h4>
              <span className={`px-2 py-1 rounded text-xs ${
                failoverPredictions.riskLevel === 'Critical' ? 'bg-red-200 text-red-800' :
                failoverPredictions.riskLevel === 'High' ? 'bg-orange-200 text-orange-800' :
                failoverPredictions.riskLevel === 'Medium' ? 'bg-yellow-200 text-yellow-800' :
                'bg-green-200 text-green-800'
              }`}>
                {failoverPredictions.riskLevel}
              </span>
            </div>
          </div>

          <div className="mt-4 space-y-4">
            {failoverPredictions.predictions.map((prediction, index) => (
              <div key={index} className="border border-gray-200 rounded-lg p-4">
                <div className="flex justify-between items-start mb-2">
                  <h4 className="font-semibold">{prediction.providerType}</h4>
                  <span className={`px-2 py-1 rounded text-xs ${
                    prediction.riskLevel === 'Critical' ? 'bg-red-200 text-red-800' :
                    prediction.riskLevel === 'High' ? 'bg-orange-200 text-orange-800' :
                    prediction.riskLevel === 'Medium' ? 'bg-yellow-200 text-yellow-800' :
                    'bg-green-200 text-green-800'
                  }`}>
                    {prediction.riskLevel}
                  </span>
                </div>
                <p className="text-sm text-gray-600 mb-2">
                  Failure Probability: {(prediction.failureProbability * 100).toFixed(1)}%
                </p>
                <p className="text-sm text-gray-600 mb-2">
                  Predicted Failure Time: {new Date(prediction.predictedFailureTime).toLocaleString()}
                </p>
                <p className="text-sm text-gray-600 mb-2">
                  Confidence: {(prediction.confidence * 100).toFixed(1)}%
                </p>
                {prediction.riskFactors.length > 0 && (
                  <div className="mt-2">
                    <p className="text-sm font-medium text-gray-700">Risk Factors:</p>
                    <ul className="text-sm text-gray-600">
                      {prediction.riskFactors.map((factor, factorIndex) => (
                        <li key={factorIndex} className="list-disc list-inside">• {factor}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <p className="text-red-800">{error}</p>
        <button 
          onClick={loadData}
          className="mt-2 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">OASIS HyperDrive</h1>
        <p className="mt-2 text-gray-600">
          Advanced AI-powered auto-failover, load balancing, and performance optimization system
        </p>
      </div>

      <div className="bg-white shadow rounded-lg">
        <div className="border-b border-gray-200">
          <nav className="-mb-px flex space-x-8 px-6">
            {[
              { id: 'dashboard', name: 'Dashboard' },
              { id: 'configuration', name: 'Configuration' },
              { id: 'providers', name: 'Provider Management' },
              { id: 'permissions', name: 'Data Permissions' },
              { id: 'analytics', name: 'Analytics' },
              { id: 'recommendations', name: 'AI Recommendations' },
              { id: 'predictions', name: 'Failure Predictions' }
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`py-4 px-1 border-b-2 font-medium text-sm ${
                  activeTab === tab.id
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                {tab.name}
              </button>
            ))}
          </nav>
        </div>

        <div className="p-6">
          {activeTab === 'dashboard' && renderDashboard()}
          {activeTab === 'configuration' && renderConfiguration()}
          {activeTab === 'providers' && config && (
            <ProviderManager 
              config={config} 
              onConfigUpdate={setConfig}
              subscriptionPlan={subscriptionPlan}
            />
          )}
          {activeTab === 'permissions' && (
            <DataPermissionsManager 
              onPermissionsUpdate={(permissions) => {
                if (config) {
                  const updatedConfig = { ...config, dataPermissions: permissions };
                  setConfig(updatedConfig);
                }
              }}
            />
          )}
          {activeTab === 'analytics' && renderAnalytics()}
          {activeTab === 'recommendations' && renderRecommendations()}
          {activeTab === 'predictions' && renderFailoverPredictions()}
        </div>
      </div>
    </div>
  );
};

export default HyperDrivePage;
