import { OASISResult } from '../../types/star';

export interface NodeStatus {
  isRunning: boolean;
  nodeId: string;
  version: string;
  uptime: number;
  lastUpdated: string;
}

export interface NodeInfo {
  nodeId: string;
  name: string;
  version: string;
  platform: string;
  architecture: string;
  uptime: number;
  memoryUsage: number;
  cpuUsage: number;
}

export interface NodeMetrics {
  cpuUsage: number;
  memoryUsage: number;
  diskUsage: number;
  networkIn: number;
  networkOut: number;
  connections: number;
  lastUpdated: string;
}

export interface PeerNode {
  id: string;
  name: string;
  address: string;
  status: 'connected' | 'disconnected' | 'connecting';
  latency: number;
  lastSeen: string;
}

export interface NodeStats {
  totalPeers: number;
  activeConnections: number;
  messagesProcessed: number;
  averageLatency: number;
  uptime: number;
}

export interface NodeConfigRequest {
  config: Record<string, any>;
}

class ONODEService {
  private baseUrl = '/api/v1/onode';

  async getNodeStatus(): Promise<OASISResult<NodeStatus>> {
    const response = await fetch(`${this.baseUrl}/status`);
    return await response.json();
  }

  async getNodeInfo(): Promise<OASISResult<NodeInfo>> {
    const response = await fetch(`${this.baseUrl}/info`);
    return await response.json();
  }

  async getNodeMetrics(): Promise<OASISResult<NodeMetrics>> {
    const response = await fetch(`${this.baseUrl}/metrics`);
    return await response.json();
  }

  async getConnectedPeers(): Promise<OASISResult<PeerNode[]>> {
    const response = await fetch(`${this.baseUrl}/peers`);
    return await response.json();
  }

  async getNodeStats(): Promise<OASISResult<NodeStats>> {
    const response = await fetch(`${this.baseUrl}/stats`);
    return await response.json();
  }

  async getNodeLogs(lines: number = 100): Promise<OASISResult<string[]>> {
    const response = await fetch(`${this.baseUrl}/logs?lines=${lines}`);
    return await response.json();
  }

  async getNodeConfig(): Promise<OASISResult<Record<string, any>>> {
    const response = await fetch(`${this.baseUrl}/config`);
    return await response.json();
  }

  async updateNodeConfig(request: NodeConfigRequest): Promise<OASISResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/config`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });
    return await response.json();
  }

  async startNode(): Promise<OASISResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/start`, {
      method: 'POST',
    });
    return await response.json();
  }

  async stopNode(): Promise<OASISResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/stop`, {
      method: 'POST',
    });
    return await response.json();
  }

  async restartNode(): Promise<OASISResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/restart`, {
      method: 'POST',
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

export const onodeService = new ONODEService();