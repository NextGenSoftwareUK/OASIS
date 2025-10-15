// OASIS HyperDrive Types
export interface HyperDriveConfig {
  isEnabled: boolean;
  defaultStrategy: LoadBalancingStrategy;
  autoFailoverEnabled: boolean;
  autoReplicationEnabled: boolean;
  autoLoadBalancingEnabled: boolean;
  maxRetryAttempts: number;
  requestTimeoutMs: number;
  healthCheckIntervalMs: number;
  maxConcurrentRequests: number;
  performanceWeight: number;
  costWeight: number;
  geographicWeight: number;
  availabilityWeight: number;
  latencyWeight: number;
  throughputWeight: number;
  reliabilityWeight: number;
  maxLatencyThresholdMs: number;
  maxErrorRateThreshold: number;
  minUptimeThreshold: number;
  enabledProviders: string[];
  autoFailoverProviders: string[];
  autoReplicationProviders: string[];
  loadBalancingProviders: string[];
  providerConfigs: Record<string, ProviderConfig>;
  geographicConfig: GeographicConfig;
  costConfig: CostConfig;
  performanceConfig: PerformanceConfig;
  securityConfig: SecurityConfig;
  monitoringConfig: MonitoringConfig;
  
  // Enhanced granular configuration
  replicationRules: ReplicationRules;
  failoverRules: FailoverRules;
  subscriptionConfig: SubscriptionConfig;
  dataPermissions: DataPermissions;
  intelligentMode: IntelligentMode;
}

export interface ProviderConfig {
  providerType: string;
  isEnabled: boolean;
  weight: number;
  timeoutMs: number;
  maxConnections: number;
  errorThreshold: number;
  minUptime: number;
  maxLatencyMs: number;
}

export interface GeographicConfig {
  isEnabled: boolean;
  defaultRegion: string;
  userLocation: string;
  maxDistanceKm: number;
  maxNetworkHops: number;
  maxLatencyMs: number;
  regions: Record<string, GeographicRegion>;
}

export interface GeographicRegion {
  name: string;
  country: string;
  city: string;
  latitude: number;
  longitude: number;
  timeZone: string;
  maxLatencyMs: number;
  maxNetworkHops: number;
}

export interface CostConfig {
  isEnabled: boolean;
  currency: string;
  maxCostPerOperation: number;
  maxStorageCostPerGB: number;
  maxComputeCostPerHour: number;
  maxNetworkCostPerGB: number;
  providerCosts: Record<string, CostAnalysis>;
}

export interface CostAnalysis {
  providerType: string;
  storageCostPerGB: number;
  computeCostPerHour: number;
  networkCostPerGB: number;
  transactionCost: number;
  apiCallCost: number;
  totalCost: number;
  currency: string;
  lastUpdated: string;
  costEfficiencyScore: number;
}

export interface PerformanceConfig {
  isEnabled: boolean;
  maxResponseTimeMs: number;
  maxErrorRate: number;
  minUptime: number;
  minThroughputMbps: number;
  maxConcurrentConnections: number;
  queueDepthThreshold: number;
  maxCpuUsage: number;
  maxMemoryUsage: number;
}

export interface SecurityConfig {
  isEnabled: boolean;
  requireEncryption: boolean;
  requireAuthentication: boolean;
  requireAuthorization: boolean;
  maxRetryAttempts: number;
  sessionTimeoutMs: number;
  maxConcurrentSessions: number;
  allowedIPs: string[];
  blockedIPs: string[];
  securityHeaders: Record<string, string>;
}

export interface MonitoringConfig {
  isEnabled: boolean;
  metricsCollectionIntervalMs: number;
  maxMetricsHistory: number;
  alertThreshold: number;
  enableRealTimeMonitoring: boolean;
  enablePerformanceProfiling: boolean;
  enableCostTracking: boolean;
  enableGeographicTracking: boolean;
  monitoringEndpoints: string[];
  customMetrics: Record<string, string>;
}

export interface HyperDriveStatus {
  isEnabled: boolean;
  autoFailoverEnabled: boolean;
  autoReplicationEnabled: boolean;
  autoLoadBalancingEnabled: boolean;
  defaultStrategy: LoadBalancingStrategy;
  enabledProviders: string[];
  loadBalancingProviders: string[];
  totalProviders: number;
  activeProviders: number;
  lastHealthCheck: string;
}

export interface ProviderPerformanceMetrics {
  providerType: string;
  responseTimeMs: number;
  throughputMbps: number;
  errorRate: number;
  uptimePercentage: number;
  costPerOperation: number;
  activeConnections: number;
  cpuUsage: number;
  memoryUsage: number;
  networkLatency: number;
  lastUpdated: string;
  overallScore: number;
}

export interface AnalyticsReport {
  generatedAt: string;
  timeRange: TimeRange;
  providerType?: string;
  providerAnalytics: ProviderAnalytics[];
  systemMetrics: SystemMetrics;
  topPerformers: ProviderAnalytics[];
  underperformers: ProviderAnalytics[];
  costOptimization: CostOptimization[];
  reliabilityInsights: ReliabilityInsight[];
}

export interface ProviderAnalytics {
  providerType: string;
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  averageResponseTime: number;
  minResponseTime: number;
  maxResponseTime: number;
  averageThroughput: number;
  totalCost: number;
  averageCost: number;
  errorRate: number;
  uptimePercentage: number;
  peakUsageTime: string;
  geographicDistribution: Record<string, number>;
  costTrend: number;
  performanceTrend: number;
  reliabilityTrend: number;
  anomalies: Anomaly[];
  recommendations: string[];
}

export interface SystemMetrics {
  overallHealth: number;
  averagePerformance: number;
  totalCost: number;
  totalProviders: number;
}

export interface CostOptimization {
  description: string;
  potentialSavings: number;
  actions: string[];
}

export interface ReliabilityInsight {
  description: string;
  reliabilityScore: number;
  recommendations: string[];
}

export interface DashboardData {
  timestamp: string;
  activeProviders: number;
  totalRequests: number;
  systemHealth: number;
  performanceMetrics: PerformanceMetrics;
  costMetrics: CostMetrics;
  geographicMetrics: GeographicMetrics;
  alerts: Alert[];
  trends: Trend[];
}

export interface PerformanceMetrics {
  totalRequests: number;
  successfulRequests: number;
  averageResponseTime: number;
  throughput: number;
}

export interface CostMetrics {
  totalCost: number;
  averageCost: number;
  costPerRequest: number;
}

export interface GeographicMetrics {
  totalRegions: number;
  topRegion: string;
  geographicDistribution: Record<string, number>;
}

export interface Alert {
  providerType: string;
  type: AlertType;
  severity: Severity;
  message: string;
  timestamp: string;
}

export interface Trend {
  providerType: string;
  performanceTrend: number;
  costTrend: number;
  reliabilityTrend: number;
}

export interface PredictiveAnalytics {
  providerType: string;
  forecastDays: number;
  confidence: number;
  message: string;
  predictedPerformance: number;
  predictedCost: number;
  predictedReliability: number;
  riskFactors: string[];
  recommendations: string[];
}

export interface OptimizationRecommendation {
  providerType: string;
  type: OptimizationType;
  priority: Priority;
  description: string;
  suggestedActions: string[];
}

export interface CostOptimizationRecommendation {
  providerType: string;
  currentCost: number;
  costTrend: number;
  potentialSavings: number;
  recommendedActions: string[];
  priority: Priority;
}

export interface PerformanceOptimizationRecommendation {
  providerType: string;
  currentPerformance: number;
  performanceTrend: number;
  errorRate: number;
  recommendedActions: string[];
  priority: Priority;
}

export interface FailoverPrediction {
  timestamp: string;
  predictions: ProviderFailurePrediction[];
  recommendedActions: string[];
  riskLevel: RiskLevel;
}

export interface ProviderFailurePrediction {
  providerType: string;
  riskLevel: RiskLevel;
  failureProbability: number;
  predictedFailureTime: string;
  confidence: number;
  riskFactors: string[];
  recommendedActions: string[];
}

export interface AnalyticsDataPoint {
  timestamp: string;
  providerType: string;
  success: boolean;
  responseTimeMs: number;
  throughputMbps: number;
  cost: number;
  region: string;
  userId: string;
  operation: string;
}

export interface PerformanceDataPoint {
  timestamp: string;
  providerType: string;
  responseTimeMs: number;
  throughputMbps: number;
  errorRate: number;
  uptimePercentage: number;
  costPerOperation: number;
  activeConnections: number;
  cpuUsage: number;
  memoryUsage: number;
  networkLatency: number;
}

export interface FailureEvent {
  timestamp: string;
  providerType: string;
  failureType: FailureType;
  description: string;
  duration: number;
  cause: string;
  resolution: string;
}

export interface Anomaly {
  timestamp: string;
  type: AnomalyType;
  severity: Severity;
  description: string;
}

// Enums
export enum LoadBalancingStrategy {
  Auto = 'Auto',
  RoundRobin = 'RoundRobin',
  WeightedRoundRobin = 'WeightedRoundRobin',
  LeastConnections = 'LeastConnections',
  Geographic = 'Geographic',
  CostBased = 'CostBased',
  Performance = 'Performance'
}

export enum TimeRange {
  LastHour = 'LastHour',
  Last24Hours = 'Last24Hours',
  Last7Days = 'Last7Days',
  Last30Days = 'Last30Days'
}

export enum AlertType {
  HighErrorRate = 'HighErrorRate',
  SlowResponse = 'SlowResponse',
  HighCost = 'HighCost',
  LowUptime = 'LowUptime',
  SecurityBreach = 'SecurityBreach'
}

export enum Severity {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export enum OptimizationType {
  Performance = 'Performance',
  Cost = 'Cost',
  Reliability = 'Reliability',
  Security = 'Security',
  Geographic = 'Geographic'
}

export enum Priority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export enum RiskLevel {
  VeryLow = 'VeryLow',
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical',
  Unknown = 'Unknown'
}

export enum FailureType {
  Timeout = 'Timeout',
  ConnectionError = 'ConnectionError',
  AuthenticationError = 'AuthenticationError',
  RateLimitExceeded = 'RateLimitExceeded',
  ServiceUnavailable = 'ServiceUnavailable',
  DataCorruption = 'DataCorruption',
  PerformanceDegradation = 'PerformanceDegradation',
  SecurityBreach = 'SecurityBreach'
}

export enum AnomalyType {
  Performance = 'Performance',
  Cost = 'Cost',
  Reliability = 'Reliability',
  Security = 'Security'
}

// Enhanced HyperDrive Configuration Types

export interface ReplicationRules {
  mode: ReplicationMode;
  isEnabled: boolean;
  maxReplicationsPerMonth: number;
  costThreshold: number;
  freeProvidersOnly: boolean;
  gasFeeThreshold: number;
  replicationTriggers: ReplicationTrigger[];
  providerRules: ProviderReplicationRule[];
  dataTypeRules: DataTypeReplicationRule[];
  scheduleRules: ScheduleRule[];
  costOptimization: CostOptimizationRule;
  intelligentSelection: IntelligentSelectionRule;
}

export interface FailoverRules {
  mode: FailoverMode;
  isEnabled: boolean;
  maxFailoversPerMonth: number;
  costThreshold: number;
  freeProvidersOnly: boolean;
  gasFeeThreshold: number;
  failoverTriggers: FailoverTrigger[];
  providerRules: ProviderFailoverRule[];
  intelligentSelection: IntelligentSelectionRule;
  escalationRules: EscalationRule[];
}

export interface SubscriptionConfig {
  planType: SubscriptionPlanType;
  maxReplicationsPerMonth: number;
  maxFailoversPerMonth: number;
  maxStorageGB: number;
  payAsYouGoEnabled: boolean;
  costPerReplication: number;
  costPerFailover: number;
  costPerGB: number;
  currency: string;
  billingCycle: BillingCycle;
  usageAlerts: UsageAlert[];
  quotaNotifications: QuotaNotification[];
}

export interface DataPermissions {
  avatarPermissions: AvatarPermissions;
  holonPermissions: HolonPermissions;
  providerPermissions: ProviderPermissions;
  fieldLevelPermissions: FieldLevelPermissions;
  accessControl: AccessControl;
}

export interface IntelligentMode {
  isEnabled: boolean;
  autoOptimization: boolean;
  costAwareness: boolean;
  performanceOptimization: boolean;
  securityOptimization: boolean;
  learningEnabled: boolean;
  adaptationSpeed: AdaptationSpeed;
  optimizationGoals: OptimizationGoal[];
}

export interface ReplicationTrigger {
  id: string;
  name: string;
  condition: ReplicationCondition;
  priority: Priority;
  isEnabled: boolean;
  action: ReplicationAction;
}

export interface ReplicationCondition {
  type: ConditionType;
  operator: ConditionOperator;
  value: any;
  field?: string;
  providerType?: string;
  timeWindow?: TimeWindow;
}

export interface ReplicationAction {
  type: ActionType;
  targetProviders: string[];
  dataTypes: string[];
  permissions: DataPermissions;
  costLimit: number;
  schedule?: Schedule;
}

export interface ProviderReplicationRule {
  providerType: string;
  isEnabled: boolean;
  priority: number;
  costLimit: number;
  gasFeeLimit: number;
  dataTypes: string[];
  permissions: DataPermissions;
  conditions: ReplicationCondition[];
  schedule?: Schedule;
}

export interface DataTypeReplicationRule {
  dataType: string;
  isEnabled: boolean;
  requiredProviders: string[];
  optionalProviders: string[];
  permissions: DataPermissions;
  costLimit: number;
  schedule?: Schedule;
}

export interface ScheduleRule {
  name: string;
  isEnabled: boolean;
  schedule: Schedule;
  dataTypes: string[];
  providers: string[];
  permissions: DataPermissions;
}

export interface CostOptimizationRule {
  isEnabled: boolean;
  maxCostPerReplication: number;
  maxCostPerMonth: number;
  preferredFreeProviders: string[];
  avoidHighGasProviders: boolean;
  gasFeeThreshold: number;
  costAlertThreshold: number;
}

export interface IntelligentSelectionRule {
  isEnabled: boolean;
  algorithm: SelectionAlgorithm;
  weights: SelectionWeights;
  learningEnabled: boolean;
  adaptationSpeed: AdaptationSpeed;
  optimizationGoals: OptimizationGoal[];
}

export interface FailoverTrigger {
  id: string;
  name: string;
  condition: FailoverCondition;
  priority: Priority;
  isEnabled: boolean;
  action: FailoverAction;
}

export interface FailoverCondition {
  type: ConditionType;
  operator: ConditionOperator;
  value: any;
  providerType?: string;
  timeWindow?: TimeWindow;
  threshold?: number;
}

export interface FailoverAction {
  type: ActionType;
  targetProvider: string;
  fallbackProviders: string[];
  costLimit: number;
  schedule?: Schedule;
}

export interface ProviderFailoverRule {
  providerType: string;
  isEnabled: boolean;
  priority: number;
  costLimit: number;
  gasFeeLimit: number;
  conditions: FailoverCondition[];
  fallbackProviders: string[];
}

export interface EscalationRule {
  level: EscalationLevel;
  condition: FailoverCondition;
  action: FailoverAction;
  notification: NotificationRule;
}

export interface AvatarPermissions {
  isEnabled: boolean;
  fields: AvatarFieldPermission[];
  defaultPermission: PermissionLevel;
  providerOverrides: Record<string, AvatarFieldPermission[]>;
}

export interface AvatarFieldPermission {
  fieldName: string;
  permission: PermissionLevel;
  isEncrypted: boolean;
  isRequired: boolean;
  providerTypes: string[];
}

export interface HolonPermissions {
  isEnabled: boolean;
  holonTypes: HolonTypePermission[];
  defaultPermission: PermissionLevel;
  providerOverrides: Record<string, HolonTypePermission[]>;
}

export interface HolonTypePermission {
  holonType: string;
  permission: PermissionLevel;
  isEncrypted: boolean;
  isRequired: boolean;
  providerTypes: string[];
  fields: HolonFieldPermission[];
}

export interface HolonFieldPermission {
  fieldName: string;
  permission: PermissionLevel;
  isEncrypted: boolean;
  isRequired: boolean;
}

export interface ProviderPermissions {
  isEnabled: boolean;
  providers: ProviderPermission[];
}

export interface ProviderPermission {
  providerType: string;
  permission: PermissionLevel;
  allowedDataTypes: string[];
  costLimit: number;
  gasFeeLimit: number;
  schedule?: Schedule;
}

export interface FieldLevelPermissions {
  isEnabled: boolean;
  rules: FieldPermissionRule[];
}

export interface FieldPermissionRule {
  fieldPath: string;
  dataType: string;
  permissions: Record<string, PermissionLevel>;
  encryption: Record<string, boolean>;
  required: Record<string, boolean>;
}

export interface AccessControl {
  isEnabled: boolean;
  authenticationRequired: boolean;
  authorizationLevel: AuthorizationLevel;
  encryptionLevel: EncryptionLevel;
  auditLogging: boolean;
  accessPolicies: AccessPolicy[];
}

export interface AccessPolicy {
  name: string;
  condition: AccessCondition;
  permissions: PermissionLevel;
  providers: string[];
  dataTypes: string[];
}

export interface AccessCondition {
  userRole?: string;
  subscriptionPlan?: string;
  timeWindow?: TimeWindow;
  location?: string;
  deviceType?: string;
}

export interface UsageAlert {
  id: string;
  name: string;
  threshold: number;
  thresholdType: ThresholdType;
  notificationChannels: NotificationChannel[];
  isEnabled: boolean;
}

export interface QuotaNotification {
  id: string;
  name: string;
  quotaType: QuotaType;
  threshold: number;
  notificationChannels: NotificationChannel[];
  actions: QuotaAction[];
  isEnabled: boolean;
}

export interface QuotaAction {
  type: QuotaActionType;
  value: any;
  schedule?: Schedule;
}

export interface NotificationRule {
  channels: NotificationChannel[];
  message: string;
  priority: Priority;
  isEnabled: boolean;
}

export interface Schedule {
  type: ScheduleType;
  interval?: number;
  intervalUnit?: TimeUnit;
  cronExpression?: string;
  timeZone: string;
  startTime?: string;
  endTime?: string;
  daysOfWeek?: DayOfWeek[];
  daysOfMonth?: number[];
}

export interface TimeWindow {
  start: string;
  end: string;
  timeZone: string;
  daysOfWeek?: DayOfWeek[];
}

export interface SelectionWeights {
  cost: number;
  performance: number;
  reliability: number;
  security: number;
  geographic: number;
  availability: number;
}

export interface OptimizationGoal {
  type: OptimizationType;
  weight: number;
  target: number;
  isEnabled: boolean;
}

// Enums for enhanced configuration

export enum ReplicationMode {
  Auto = 'Auto',
  Manual = 'Manual',
  Hybrid = 'Hybrid'
}

export enum FailoverMode {
  Auto = 'Auto',
  Manual = 'Manual',
  Hybrid = 'Hybrid'
}

export enum SubscriptionPlanType {
  Free = 'Free',
  Basic = 'Basic',
  Pro = 'Pro',
  Enterprise = 'Enterprise'
}

export enum BillingCycle {
  Monthly = 'Monthly',
  Quarterly = 'Quarterly',
  Yearly = 'Yearly'
}

export enum ConditionType {
  ResponseTime = 'ResponseTime',
  ErrorRate = 'ErrorRate',
  Cost = 'Cost',
  Availability = 'Availability',
  DataSize = 'DataSize',
  UserAction = 'UserAction',
  Schedule = 'Schedule',
  Quota = 'Quota'
}

export enum ConditionOperator {
  GreaterThan = 'GreaterThan',
  LessThan = 'LessThan',
  Equals = 'Equals',
  NotEquals = 'NotEquals',
  Contains = 'Contains',
  NotContains = 'NotContains',
  In = 'In',
  NotIn = 'NotIn'
}

export enum ActionType {
  Replicate = 'Replicate',
  Failover = 'Failover',
  Notify = 'Notify',
  Block = 'Block',
  Optimize = 'Optimize'
}

export enum SelectionAlgorithm {
  WeightedRoundRobin = 'WeightedRoundRobin',
  LeastConnections = 'LeastConnections',
  Geographic = 'Geographic',
  CostBased = 'CostBased',
  PerformanceBased = 'PerformanceBased',
  Intelligent = 'Intelligent'
}

export enum AdaptationSpeed {
  Slow = 'Slow',
  Medium = 'Medium',
  Fast = 'Fast',
  Instant = 'Instant'
}

export enum EscalationLevel {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export enum PermissionLevel {
  None = 'None',
  Read = 'Read',
  Write = 'Write',
  Admin = 'Admin',
  Owner = 'Owner'
}

export enum AuthorizationLevel {
  Public = 'Public',
  Authenticated = 'Authenticated',
  Authorized = 'Authorized',
  Admin = 'Admin',
  Owner = 'Owner'
}

export enum EncryptionLevel {
  None = 'None',
  Basic = 'Basic',
  Standard = 'Standard',
  High = 'High',
  Military = 'Military'
}

export enum ThresholdType {
  Percentage = 'Percentage',
  Absolute = 'Absolute',
  Count = 'Count',
  Size = 'Size'
}

export enum QuotaType {
  Replications = 'Replications',
  Failovers = 'Failovers',
  Storage = 'Storage',
  Requests = 'Requests',
  Cost = 'Cost'
}

export enum QuotaActionType {
  Block = 'Block',
  Notify = 'Notify',
  Upgrade = 'Upgrade',
  Charge = 'Charge'
}

export enum NotificationChannel {
  Email = 'Email',
  SMS = 'SMS',
  Push = 'Push',
  Dashboard = 'Dashboard',
  Webhook = 'Webhook'
}

export enum ScheduleType {
  Immediate = 'Immediate',
  Interval = 'Interval',
  Cron = 'Cron',
  Once = 'Once'
}

export enum TimeUnit {
  Seconds = 'Seconds',
  Minutes = 'Minutes',
  Hours = 'Hours',
  Days = 'Days',
  Weeks = 'Weeks',
  Months = 'Months'
}

export enum DayOfWeek {
  Monday = 'Monday',
  Tuesday = 'Tuesday',
  Wednesday = 'Wednesday',
  Thursday = 'Thursday',
  Friday = 'Friday',
  Saturday = 'Saturday',
  Sunday = 'Sunday'
}
