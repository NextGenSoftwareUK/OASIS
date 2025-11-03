/**
 * Stats Service
 * Retrieves stats from STAR/Web API with demo-mode fallback
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class StatsService extends BaseService {
  /** Get Karma stats for an avatar */
  async getKarmaStats(avatarId: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get(`/stats/karma-stats/${avatarId}`),
      {
        totalKarma: 0,
        karmaTransactions: 0,
        lastKarmaChange: new Date().toISOString(),
        lastKarmaAmount: 0,
        lastKarmaSource: 'None',
      },
      'Karma stats (Demo Mode)'
    );
  }

  /** Get Chat stats for an avatar */
  async getChatStats(avatarId: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get(`/stats/chat-stats/${avatarId}`),
      {
        totalMessages: 0,
        directMessages: 0,
        groupMessages: 0,
        lastMessageAt: null,
      },
      'Chat stats (Demo Mode)'
    );
  }

  /** Get Quest stats for an avatar */
  async getQuestStats(avatarId: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get(`/stats/quest-stats/${avatarId}`),
      {
        totalQuests: 0,
        completedQuests: 0,
        activeQuests: 0,
        totalRewards: 0,
      },
      'Quest stats (Demo Mode)'
    );
  }

  /** Get comprehensive stats for current avatar */
  async getCurrentAvatarStats(): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get('/stats/get-stats-for-current-logged-in-avatar'),
      {},
      'Avatar stats (Demo Mode)'
    );
  }

  /** Get leaderboard stats for an avatar */
  async getLeaderboardStats(avatarId: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get(`/stats/leaderboard-stats/${avatarId}`),
      { rank: 0, score: 0, league: 'Bronze' },
      'Leaderboard stats (Demo Mode)'
    );
  }

  /** Get system-wide stats */
  async getSystemStats(): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.get('/stats/system-stats'),
      {},
      'System stats (Demo Mode)'
    );
  }
}

export const statsService = new StatsService();


