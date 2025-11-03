/**
 * Type definitions for OASIS Web5 STAR API Client
 */

export interface STARConfig {
  apiUrl: string;
  timeout?: number;
  debug?: boolean;
  autoRetry?: boolean;
  maxRetries?: number;
}

export interface OASISResult<T> {
  isError: boolean;
  message: string;
  result: T | null;
  isSaved?: boolean;
}

export interface STARStatus {
  isIgnited: boolean;
  isLit: boolean;
  uptime?: number;
  version?: string;
  oappsRunning?: number;
  totalOAPPs?: number;
  starnetConnected?: boolean;
  activeNodes?: number;
}

export interface OAPP {
  id: string;
  name: string;
  description: string;
  version: string;
  author: string;
  category: string;
  icon?: string;
  screenshots?: string[];
  isPublished: boolean;
  isInstalled: boolean;
  downloads?: number;
  rating?: number;
  size?: number;
  createdDate?: string;
  updatedDate?: string;
  publishedDate?: string;
  holons?: string[];
  zomes?: string[];
  dependencies?: string[];
  permissions?: string[];
  config?: { [key: string]: any };
}

export interface CreateOAPPRequest {
  name: string;
  description: string;
  category: string;
  version?: string;
  icon?: string;
  templateId?: string;
  config?: { [key: string]: any };
}

export interface Mission {
  id: string;
  title: string;
  description: string;
  chapterId?: string;
  difficulty: 'easy' | 'medium' | 'hard' | 'expert';
  estimatedTime: number;
  karmaReward: number;
  xpReward: number;
  prerequisites?: string[];
  objectives: Objective[];
  questsCount?: number;
  completionPercentage?: number;
  status?: 'locked' | 'available' | 'in-progress' | 'completed';
}

export interface Quest {
  id: string;
  title: string;
  description: string;
  missionId?: string;
  type: 'main' | 'side' | 'daily' | 'event';
  difficulty: 'easy' | 'medium' | 'hard' | 'expert';
  karmaReward: number;
  xpReward: number;
  objectives: Objective[];
  subQuests?: Quest[];
  status?: 'locked' | 'available' | 'in-progress' | 'completed';
  progress?: number;
}

export interface Chapter {
  id: string;
  number: number;
  title: string;
  description: string;
  imageUrl?: string;
  missionsCount: number;
  totalQuestsCount: number;
  totalSubQuestsCount: number;
  difficulty: 'beginner' | 'intermediate' | 'advanced' | 'master';
  status: 'locked' | 'available' | 'in-progress' | 'completed';
  completionPercentage: number;
  estimatedTime: number;
}

export interface Objective {
  id: string;
  description: string;
  type: string;
  target: number;
  current: number;
  completed: boolean;
}

export interface CreateMissionRequest {
  title: string;
  description: string;
  chapterId?: string;
  difficulty: 'easy' | 'medium' | 'hard' | 'expert';
  estimatedTime: number;
  karmaReward: number;
  xpReward: number;
  objectives: Partial<Objective>[];
}

export interface CreateQuestRequest {
  title: string;
  description: string;
  missionId?: string;
  type: 'main' | 'side' | 'daily' | 'event';
  difficulty: 'easy' | 'medium' | 'hard' | 'expert';
  karmaReward: number;
  xpReward: number;
  objectives: Partial<Objective>[];
}

export interface ProgressUpdate {
  objectiveId: string;
  progress: number;
}

export interface Holon {
  id: string;
  name: string;
  description: string;
  version: string;
  category: string;
  type: string;
  author: string;
  imageUrl?: string;
  downloads: number;
  rating: number;
  size: number;
  lastUpdated: string;
  isPublic: boolean;
  isFeatured: boolean;
  tags: string[];
  dataSchema: { [key: string]: any };
  properties: string[];
  methods: string[];
  events: string[];
  documentation?: string;
  repository?: string;
  license?: string;
  price?: number;
  isFree: boolean;
  isInstalled: boolean;
}

export interface Zome {
  id: string;
  name: string;
  description: string;
  version: string;
  category: string;
  type: string;
  language: string;
  framework: string;
  author: string;
  imageUrl?: string;
  downloads: number;
  rating: number;
  size: number;
  lastUpdated: string;
  isPublic: boolean;
  isFeatured: boolean;
  tags: string[];
  functions: string[];
  dependencies: string[];
  apis: string[];
  documentation?: string;
  repository?: string;
  license?: string;
  price?: number;
  isFree: boolean;
  isInstalled: boolean;
}

export interface STARPlugin {
  id: string;
  name: string;
  description: string;
  version: string;
  author: string;
  category: string;
  type: 'core' | 'utility' | 'integration' | 'enhancement';
  imageUrl?: string;
  downloads: number;
  rating: number;
  size: number;
  lastUpdated: string;
  isInstalled: boolean;
  isEnabled: boolean;
  config?: { [key: string]: any };
  dependencies?: string[];
  documentation?: string;
}

export interface OAPPTemplate {
  id: string;
  name: string;
  description: string;
  category: string;
  imageUrl?: string;
  downloads: number;
  rating: number;
  features: string[];
  tech: string[];
  difficulty: 'beginner' | 'intermediate' | 'advanced';
  estimatedTime: string;
  includedHolons?: string[];
  includedZomes?: string[];
}
