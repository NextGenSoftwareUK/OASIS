/**
 * Video/Calls Service
 * Start/stop calls and retrieve session details
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class VideoService extends BaseService {
  /** Start a video call */
  async startCall(participantIds: string[], callName?: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post(`/video/start-video-call`, participantIds, { params: { callName } }),
      { sessionId: 'demo-session', startedAt: new Date().toISOString(), callName: callName || 'Demo Call' },
      'Video call started (Demo Mode)'
    );
  }

  /** End a video call */
  async endCall(callId: string): Promise<OASISResult<boolean>> {
    return this.handleBooleanRequest(
      () => this.web4Api.post(`/video/end-call/${callId}`),
      true,
      'Video call ended (Demo Mode)'
    );
  }

  /** Get active calls for an avatar */
  async getActiveCalls(avatarId: string): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => Promise.resolve({ data: { result: [] } } as any),
      [],
      'Active calls (Demo Mode)'
    );
  }
}

export const videoService = new VideoService();


