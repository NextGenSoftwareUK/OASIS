import { OASISResult } from '../types';

export interface NetworkStatus {
  isRunning: boolean;
  connectedNodesCount: number;
  networkId: string;
  lastUpdated: string;
}

export interface NetworkNode {
  id: string;
  name: string;
  address: string;
  status: 'connected' | 'disconnected' | 'connecting';
  latency: number;
  lastSeen: string;
}

export interface NetworkStats {
  totalNodes: number;
  activeConnections: number;
  messagesPerSecond: number;
  averageLatency: number;
  networkHealth: number;
}

export interface ConnectNodeRequest {
  nodeId: string;
  nodeAddress: string;
}

export interface DisconnectNodeRequest {
  nodeId: string;
}

export interface BroadcastMessageRequest {
  message: string;
  messageType: string;
}

class ONETService {
  private baseUrl = '/api/v1/onet';

  async getNetworkStatus(): Promise<OASISResult<NetworkStatus>> {
    const response = await fetch(`${this.baseUrl}/network/status`);
    return await response.json();
  }

  async getConnectedNodes(): Promise<OASISResult<NetworkNode[]>> {
    const response = await fetch(`${this.baseUrl}/network/nodes`);
    return await response.json();
  }

  async getNetworkStats(): Promise<OASISResult<NetworkStats>> {
    const response = await fetch(`${this.baseUrl}/network/stats`);
    return await response.json();
  }

  async getNetworkTopology(): Promise<OASISResult<any>> {
    const response = await fetch(`${this.baseUrl}/network/topology`);
    return await response.json();
  }

  async startNetwork(): Promise<OASISResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/network/start`, {
      method: 'POST',
    });
    return await response.json();
  }

  async stopNetwork(): Promise<OASISResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/network/stop`, {
      method: 'POST',
    });
    return await response.json();
  }

  async connectToNode(request: ConnectNodeRequest): Promise<OASISResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/network/connect`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });
    return await response.json();
  }

  async disconnectFromNode(request: DisconnectNodeRequest): Promise<OASISResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/network/disconnect`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });
    return await response.json();
  }

  async broadcastMessage(request: BroadcastMessageRequest): Promise<OASISResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/network/broadcast`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });
    return await response.json();
  }

  async getOASISDNA(): Promise<OASISResult<any>> {
    const response = await fetch(`${this.baseUrl}/oasisdna`);
    return await response.json();
  }

  async updateOASISDNA(oasisdna: any): Promise<OASISResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/oasisdna`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(oasisdna),
    });
    return await response.json();
  }
}

export const onetService = new ONETService();