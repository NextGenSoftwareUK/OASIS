/**
 * Messaging Service
 * Handles direct messaging between avatars
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class MessagingService extends BaseService {
  async getInbox(limit: number = 50, offset: number = 0): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get(`/messaging/messages`, { params: { limit, offset } }),
      [],
      'Inbox (Demo Mode)'
    );
  }

  async getSent(avatarId: string, limit: number = 50): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get(`/messaging/conversation/${avatarId}`, { params: { limit } }),
      [],
      'Sent (Demo Mode)'
    );
  }

  async sendMessage(toAvatarId: string, content: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post(`/messaging/send-message-to-avatar/${toAvatarId}`, content),
      { id: `msg-${Date.now()}`, toAvatarId, content, timestamp: new Date().toISOString() },
      'Message sent (Demo Mode)'
    );
  }
}

export const messagingService = new MessagingService();


