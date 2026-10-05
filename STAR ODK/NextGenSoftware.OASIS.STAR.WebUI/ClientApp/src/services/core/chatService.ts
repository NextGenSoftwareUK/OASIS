/**
 * Chat Service
 * Group/channel chat operations
 */

import { BaseService } from '../base/baseService';
import { OASISResult } from '../../types/star';

class ChatService extends BaseService {
  async getChannels(): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get('/chat/history/demo'),
      [],
      'Channels (Demo Mode)'
    );
  }

  async getChannelMessages(channelId: string, limit: number = 100): Promise<OASISResult<any[]>> {
    return this.handleArrayRequest(
      () => this.web4Api.get(`/chat/history/${channelId}`, { params: { limit } }),
      [],
      'Channel messages (Demo Mode)'
    );
  }

  async sendChannelMessage(channelId: string, content: string): Promise<OASISResult<any>> {
    return this.handleRequest(
      () => this.web4Api.post(`/chat/send-message/${channelId}`, content),
      { id: `chat-${Date.now()}`, content, timestamp: new Date().toISOString() },
      'Message sent (Demo Mode)'
    );
  }
}

export const chatService = new ChatService();


