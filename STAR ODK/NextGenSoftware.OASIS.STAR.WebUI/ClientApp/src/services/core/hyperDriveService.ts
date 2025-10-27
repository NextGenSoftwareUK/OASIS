import { BaseService } from '../base/baseService';
import { 
  HyperDriveConfig, 
  HyperDriveStatus, 
  ProviderPerformanceMetrics, 
  AnalyticsReport, 
  DashboardData, 
  PredictiveAnalytics,
  OptimizationRecommendation,
  CostOptimizationRecommendation,
  PerformanceOptimizationRecommendation,
  FailoverPrediction,
  AnalyticsDataPoint,
  PerformanceDataPoint,
  FailureEvent,
  ReplicationRules,
  FailoverRules,
  SubscriptionConfig,
  DataPermissions,
  IntelligentMode,
  ReplicationTrigger,
  FailoverTrigger,
  ProviderReplicationRule,
  ProviderFailoverRule,
  DataTypeReplicationRule,
  ScheduleRule,
  CostOptimizationRule,
  IntelligentSelectionRule,
  EscalationRule,
  AvatarPermissions,
  HolonPermissions,
  ProviderPermissions,
  FieldLevelPermissions,
  AccessControl,
  UsageAlert,
  QuotaNotification,
  ReplicationMode,
  FailoverMode,
  SubscriptionPlanType,
  BillingCycle,
  ConditionType,
  ConditionOperator,
  ActionType,
  SelectionAlgorithm,
  AdaptationSpeed,
  EscalationLevel,
  PermissionLevel,
  AuthorizationLevel,
  EncryptionLevel,
  ThresholdType,
  QuotaType,
  QuotaActionType,
  NotificationChannel,
  ScheduleType,
  TimeUnit,
  DayOfWeek
} from '../../types/hyperDriveTypes';

export class HyperDriveService extends BaseService {
  private baseUrl = '/api/hyperdrive';

  // Configuration Management
  async getConfiguration(): Promise<HyperDriveConfig> {
    return this.httpGet(`${this.baseUrl}/config`);
  }

  async updateConfiguration(config: HyperDriveConfig): Promise<boolean> {
    return this.httpPut(`${this.baseUrl}/config`, config);
  }

  async validateConfiguration(): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/config/validate`);
  }

  async resetConfiguration(): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/config/reset`);
  }

  // Mode
  async getMode(): Promise<string> {
    return this.httpGet(`${this.baseUrl}/mode`);
  }

  async setMode(mode: string): Promise<boolean> {
    return this.httpPut(`${this.baseUrl}/mode`, mode);
  }

  // Performance Metrics
  async getMetrics(): Promise<Record<string, ProviderPerformanceMetrics>> {
    return this.httpGet(`${this.baseUrl}/metrics`);
  }

  async getProviderMetrics(providerType: string): Promise<ProviderPerformanceMetrics> {
    return this.httpGet(`${this.baseUrl}/metrics/${providerType}`);
  }

  async getConnectionCounts(): Promise<Record<string, number>> {
    return this.httpGet(`${this.baseUrl}/connections`);
  }

  async getBestProvider(strategy?: string): Promise<string> {
    const params = strategy ? `?strategy=${strategy}` : '';
    return this.httpGet(`${this.baseUrl}/best-provider${params}`);
  }

  // Analytics
  async getAnalyticsReport(providerType?: string, timeRange?: string): Promise<AnalyticsReport> {
    const params = new URLSearchParams();
    if (providerType) params.append('providerType', providerType);
    if (timeRange) params.append('timeRange', timeRange);
    return this.httpGet(`${this.baseUrl}/analytics/report?${params.toString()}`);
  }

  async getDashboardData(): Promise<DashboardData> {
    return this.httpGet(`${this.baseUrl}/dashboard`);
  }

  async getPredictiveAnalytics(providerType: string, forecastDays: number = 7): Promise<PredictiveAnalytics> {
    return this.httpGet(`${this.baseUrl}/analytics/predictive/${providerType}?forecastDays=${forecastDays}`);
  }

  // AI Optimization
  async getAIRecommendations(): Promise<OptimizationRecommendation[]> {
    return this.httpGet(`${this.baseUrl}/ai/recommendations`);
  }

  async getCostOptimizationRecommendations(): Promise<CostOptimizationRecommendation[]> {
    return this.httpGet(`${this.baseUrl}/analytics/cost-optimization`);
  }

  async getPerformanceOptimizationRecommendations(): Promise<PerformanceOptimizationRecommendation[]> {
    return this.httpGet(`${this.baseUrl}/analytics/performance-optimization`);
  }

  // Failure Prediction
  async getFailurePredictions(): Promise<FailoverPrediction> {
    return this.httpGet(`${this.baseUrl}/failover/predictions`);
  }

  async initiatePreventiveFailover(highRiskProviders: string[]): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/failover/preventive`, highRiskProviders);
  }

  // Data Recording
  async recordAnalyticsData(data: AnalyticsDataPoint): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/analytics/record`, data);
  }

  async recordPerformanceData(data: PerformanceDataPoint): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/ai/record-performance`, data);
  }

  async recordFailureEvent(event: FailureEvent): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/failover/record-failure`, event);
  }

  // Status
  async getStatus(): Promise<HyperDriveStatus> {
    return this.httpGet(`${this.baseUrl}/status`);
  }

  // Request Recording
  async recordRequest(providerType: string, success: boolean, responseTimeMs: number, cost: number): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/record-request`, {
      providerType,
      success,
      responseTimeMs,
      cost
    });
  }

  async recordConnection(providerType: string, isConnecting: boolean): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/record-connection`, {
      providerType,
      isConnecting
    });
  }

  // Geographic and Cost Updates
  async updateGeographicInfo(providerType: string, geoInfo: any): Promise<boolean> {
    return this.httpPut(`${this.baseUrl}/geographic/${providerType}`, geoInfo);
  }

  async updateCostAnalysis(providerType: string, costAnalysis: any): Promise<boolean> {
    return this.httpPut(`${this.baseUrl}/cost/${providerType}`, costAnalysis);
  }

  // Metrics Reset
  async resetProviderMetrics(providerType: string): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/metrics/${providerType}/reset`);
  }

  async resetAllMetrics(): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/metrics/reset-all`);
  }

  // Enhanced Replication Management
  async getReplicationRules(): Promise<ReplicationRules> {
    return this.httpGet(`${this.baseUrl}/replication/rules`);
  }

  async updateReplicationRules(rules: ReplicationRules): Promise<boolean> {
    return this.httpPut(`${this.baseUrl}/replication/rules`, rules);
  }

  async createReplicationTrigger(trigger: ReplicationTrigger): Promise<ReplicationTrigger> {
    return this.httpPost(`${this.baseUrl}/replication/triggers`, trigger);
  }

  async updateReplicationTrigger(id: string, trigger: ReplicationTrigger): Promise<ReplicationTrigger> {
    return this.httpPut(`${this.baseUrl}/replication/triggers/${id}`, trigger);
  }

  async deleteReplicationTrigger(id: string): Promise<boolean> {
    return this.httpDelete(`${this.baseUrl}/replication/triggers/${id}`);
  }

  async getProviderReplicationRules(): Promise<ProviderReplicationRule[]> {
    return this.httpGet(`${this.baseUrl}/replication/provider-rules`);
  }

  async updateProviderReplicationRule(rule: ProviderReplicationRule): Promise<ProviderReplicationRule> {
    return this.httpPut(`${this.baseUrl}/replication/provider-rules`, rule);
  }

  async getDataTypeReplicationRules(): Promise<DataTypeReplicationRule[]> {
    return this.httpGet(`${this.baseUrl}/replication/data-type-rules`);
  }

  async updateDataTypeReplicationRule(rule: DataTypeReplicationRule): Promise<DataTypeReplicationRule> {
    return this.httpPut(`${this.baseUrl}/replication/data-type-rules`, rule);
  }

  async getScheduleRules(): Promise<ScheduleRule[]> {
    return this.httpGet(`${this.baseUrl}/replication/schedule-rules`);
  }

  async updateScheduleRule(rule: ScheduleRule): Promise<ScheduleRule> {
    return this.httpPut(`${this.baseUrl}/replication/schedule-rules`, rule);
  }

  async getCostOptimizationRule(): Promise<CostOptimizationRule> {
    return this.httpGet(`${this.baseUrl}/replication/cost-optimization`);
  }

  async updateCostOptimizationRule(rule: CostOptimizationRule): Promise<CostOptimizationRule> {
    return this.httpPut(`${this.baseUrl}/replication/cost-optimization`, rule);
  }

  // Enhanced Failover Management
  async getFailoverRules(): Promise<FailoverRules> {
    return this.httpGet(`${this.baseUrl}/failover/rules`);
  }

  async updateFailoverRules(rules: FailoverRules): Promise<boolean> {
    return this.httpPut(`${this.baseUrl}/failover/rules`, rules);
  }

  async createFailoverTrigger(trigger: FailoverTrigger): Promise<FailoverTrigger> {
    return this.httpPost(`${this.baseUrl}/failover/triggers`, trigger);
  }

  async updateFailoverTrigger(id: string, trigger: FailoverTrigger): Promise<FailoverTrigger> {
    return this.httpPut(`${this.baseUrl}/failover/triggers/${id}`, trigger);
  }

  async deleteFailoverTrigger(id: string): Promise<boolean> {
    return this.httpDelete(`${this.baseUrl}/failover/triggers/${id}`);
  }

  async getProviderFailoverRules(): Promise<ProviderFailoverRule[]> {
    return this.httpGet(`${this.baseUrl}/failover/provider-rules`);
  }

  async updateProviderFailoverRule(rule: ProviderFailoverRule): Promise<ProviderFailoverRule> {
    return this.httpPut(`${this.baseUrl}/failover/provider-rules`, rule);
  }

  async getEscalationRules(): Promise<EscalationRule[]> {
    return this.httpGet(`${this.baseUrl}/failover/escalation-rules`);
  }

  async updateEscalationRule(rule: EscalationRule): Promise<EscalationRule> {
    return this.httpPut(`${this.baseUrl}/failover/escalation-rules`, rule);
  }

  // Subscription Management
  async getSubscriptionConfig(): Promise<SubscriptionConfig> {
    return this.httpGet(`${this.baseUrl}/subscription/config`);
  }

  async updateSubscriptionConfig(config: SubscriptionConfig): Promise<boolean> {
    return this.httpPut(`${this.baseUrl}/subscription/config`, config);
  }

  async getUsageAlerts(): Promise<UsageAlert[]> {
    return this.httpGet(`${this.baseUrl}/subscription/usage-alerts`);
  }

  async createUsageAlert(alert: UsageAlert): Promise<UsageAlert> {
    return this.httpPost(`${this.baseUrl}/subscription/usage-alerts`, alert);
  }

  async updateUsageAlert(id: string, alert: UsageAlert): Promise<UsageAlert> {
    return this.httpPut(`${this.baseUrl}/subscription/usage-alerts/${id}`, alert);
  }

  async deleteUsageAlert(id: string): Promise<boolean> {
    return this.httpDelete(`${this.baseUrl}/subscription/usage-alerts/${id}`);
  }

  async getQuotaNotifications(): Promise<QuotaNotification[]> {
    return this.httpGet(`${this.baseUrl}/subscription/quota-notifications`);
  }

  async createQuotaNotification(notification: QuotaNotification): Promise<QuotaNotification> {
    return this.httpPost(`${this.baseUrl}/subscription/quota-notifications`, notification);
  }

  async updateQuotaNotification(id: string, notification: QuotaNotification): Promise<QuotaNotification> {
    return this.httpPut(`${this.baseUrl}/subscription/quota-notifications/${id}`, notification);
  }

  async deleteQuotaNotification(id: string): Promise<boolean> {
    return this.httpDelete(`${this.baseUrl}/subscription/quota-notifications/${id}`);
  }

  // Data Permissions Management
  async getDataPermissions(): Promise<DataPermissions> {
    return this.httpGet(`${this.baseUrl}/data-permissions`);
  }

  async updateDataPermissions(permissions: DataPermissions): Promise<boolean> {
    return this.httpPut(`${this.baseUrl}/data-permissions`, permissions);
  }

  async getAvatarPermissions(): Promise<AvatarPermissions> {
    return this.httpGet(`${this.baseUrl}/data-permissions/avatar`);
  }

  async updateAvatarPermissions(permissions: AvatarPermissions): Promise<AvatarPermissions> {
    return this.httpPut(`${this.baseUrl}/data-permissions/avatar`, permissions);
  }

  async getHolonPermissions(): Promise<HolonPermissions> {
    return this.httpGet(`${this.baseUrl}/data-permissions/holon`);
  }

  async updateHolonPermissions(permissions: HolonPermissions): Promise<HolonPermissions> {
    return this.httpPut(`${this.baseUrl}/data-permissions/holon`, permissions);
  }

  async getProviderPermissions(): Promise<ProviderPermissions> {
    return this.httpGet(`${this.baseUrl}/data-permissions/provider`);
  }

  async updateProviderPermissions(permissions: ProviderPermissions): Promise<ProviderPermissions> {
    return this.httpPut(`${this.baseUrl}/data-permissions/provider`, permissions);
  }

  async getFieldLevelPermissions(): Promise<FieldLevelPermissions> {
    return this.httpGet(`${this.baseUrl}/data-permissions/field-level`);
  }

  async updateFieldLevelPermissions(permissions: FieldLevelPermissions): Promise<FieldLevelPermissions> {
    return this.httpPut(`${this.baseUrl}/data-permissions/field-level`, permissions);
  }

  async getAccessControl(): Promise<AccessControl> {
    return this.httpGet(`${this.baseUrl}/data-permissions/access-control`);
  }

  async updateAccessControl(accessControl: AccessControl): Promise<AccessControl> {
    return this.httpPut(`${this.baseUrl}/data-permissions/access-control`, accessControl);
  }

  // Intelligent Mode Management
  async getIntelligentMode(): Promise<IntelligentMode> {
    return this.httpGet(`${this.baseUrl}/intelligent-mode`);
  }

  async updateIntelligentMode(mode: IntelligentMode): Promise<boolean> {
    return this.httpPut(`${this.baseUrl}/intelligent-mode`, mode);
  }

  async enableIntelligentMode(): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/intelligent-mode/enable`);
  }

  async disableIntelligentMode(): Promise<boolean> {
    return this.httpPost(`${this.baseUrl}/intelligent-mode/disable`);
  }

  // Cost Management
  async getCurrentCosts(): Promise<Record<string, number>> {
    return this.httpGet(`${this.baseUrl}/costs/current`);
  }

  async getCostHistory(timeRange: string): Promise<Record<string, any[]>> {
    return this.httpGet(`${this.baseUrl}/costs/history?timeRange=${timeRange}`);
  }

  async getCostProjections(): Promise<Record<string, number>> {
    return this.httpGet(`${this.baseUrl}/costs/projections`);
  }

  async setCostLimits(limits: Record<string, number>): Promise<boolean> {
    return this.httpPut(`${this.baseUrl}/costs/limits`, limits);
  }

  // Quota Management
  async getCurrentUsage(): Promise<Record<string, number>> {
    return this.httpGet(`${this.baseUrl}/quota/usage`);
  }

  async getQuotaLimits(): Promise<Record<string, number>> {
    return this.httpGet(`${this.baseUrl}/quota/limits`);
  }

  async checkQuotaStatus(): Promise<Record<string, any>> {
    return this.httpGet(`${this.baseUrl}/quota/status`);
  }

  // Provider Cost Analysis
  async getProviderCosts(): Promise<Record<string, any>> {
    return this.httpGet(`${this.baseUrl}/providers/costs`);
  }

  async getGasFeeEstimates(): Promise<Record<string, number>> {
    return this.httpGet(`${this.baseUrl}/providers/gas-fees`);
  }

  async getFreeProviders(): Promise<string[]> {
    return this.httpGet(`${this.baseUrl}/providers/free`);
  }

  async getLowCostProviders(): Promise<string[]> {
    return this.httpGet(`${this.baseUrl}/providers/low-cost`);
  }

  // Advanced Analytics
  async getReplicationAnalytics(): Promise<Record<string, any>> {
    return this.httpGet(`${this.baseUrl}/analytics/replication`);
  }

  async getFailoverAnalytics(): Promise<Record<string, any>> {
    return this.httpGet(`${this.baseUrl}/analytics/failover`);
  }

  async getCostAnalytics(): Promise<Record<string, any>> {
    return this.httpGet(`${this.baseUrl}/analytics/cost`);
  }

  async getPerformanceAnalytics(): Promise<Record<string, any>> {
    return this.httpGet(`${this.baseUrl}/analytics/performance`);
  }

  // Smart Recommendations
  async getSmartRecommendations(): Promise<Record<string, any>> {
    return this.httpGet(`${this.baseUrl}/recommendations/smart`);
  }

  async getSmartCostOptimizationRecommendations(): Promise<Record<string, any>> {
    return this.httpGet(`${this.baseUrl}/recommendations/cost-optimization`);
  }

  async getSmartPerformanceRecommendations(): Promise<Record<string, any>> {
    return this.httpGet(`${this.baseUrl}/recommendations/performance`);
  }

  async getSmartSecurityRecommendations(): Promise<Record<string, any>> {
    return this.httpGet(`${this.baseUrl}/recommendations/security`);
  }
}

export const hyperDriveService = new HyperDriveService();
