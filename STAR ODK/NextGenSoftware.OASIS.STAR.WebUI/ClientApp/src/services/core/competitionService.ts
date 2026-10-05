/**
 * Competition Service
 * Seasons, leagues, ranks, and scores
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class CompetitionService extends BaseService {
  /** Get current season overview */
  async getCurrentSeason(): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get('/star/competition/season/current'),
      { name: 'Season 1', status: 'active', startedOn: new Date().toISOString() },
      'Season info (Demo Mode)'
    );
  }

  /** Get avatar rank */
  async getAvatarRank(avatarId: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.get(`/star/competition/rank/${avatarId}`),
      { rank: 0, score: 0, league: 'Bronze' },
      'Avatar rank (Demo Mode)'
    );
  }

  /** Update avatar score */
  async updateAvatarScore(avatarId: string, scoreDelta: number): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.starApi.post(`/star/competition/score/${avatarId}`, { scoreDelta }),
      { rank: 0, score: scoreDelta, league: 'Bronze' },
      'Score updated (Demo Mode)'
    );
  }
}

export const competitionService = new CompetitionService();


