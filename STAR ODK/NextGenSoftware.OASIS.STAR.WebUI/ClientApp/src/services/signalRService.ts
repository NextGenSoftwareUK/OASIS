import * as signalR from '@microsoft/signalr';
import { toast } from 'react-hot-toast';
import { ENV } from '../config/env';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private isConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectInterval = 5000;

  constructor() {
    // Don't auto-initialize - wait for explicit start() call
    // this.initializeConnection();
  }

  private initializeConnection() {
    const hubUrl = `${ENV.HUB_URL.replace(/\/$/, '')}/starhub`;
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers() {
    if (!this.connection) return;

    // Connection events
    this.connection.onclose((error) => {
      console.log('SignalR connection closed:', error);
      this.isConnected = false;
      this.handleReconnection();
    });

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting:', error);
      this.isConnected = false;
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
      this.isConnected = true;
      this.reconnectAttempts = 0;
      toast.success('Reconnected to STAR');
    });

    // STAR events
    this.connection.on('STARIgnited', (data) => {
      console.log('STAR Ignited:', data);
      toast.success('STAR has been ignited!');
      this.emit('starIgnited', data);
    });

    this.connection.on('STARExtinguished', (data) => {
      console.log('STAR Extinguished:', data);
      toast.success('STAR has been extinguished');
      this.emit('starExtinguished', data);
    });

    this.connection.on('STARStatusUpdate', (status) => {
      console.log('STAR Status Update:', status);
      this.emit('starStatusUpdate', status);
    });

    // Avatar events
    this.connection.on('AvatarBeamedIn', (avatar) => {
      console.log('Avatar Beamed In:', avatar);
      toast.success(`Welcome, ${avatar.username}!`);
      this.emit('avatarBeamedIn', avatar);
    });

    this.connection.on('AvatarCreated', (avatar) => {
      console.log('Avatar Created:', avatar);
      toast.success(`Avatar ${avatar.username} created successfully!`);
      this.emit('avatarCreated', avatar);
    });

    this.connection.on('AvatarSaved', (avatar) => {
      console.log('Avatar Saved:', avatar);
      this.emit('avatarSaved', avatar);
    });

    this.connection.on('AvatarDeleted', (avatarId) => {
      console.log('Avatar Deleted:', avatarId);
      toast.success('Avatar deleted');
      this.emit('avatarDeleted', avatarId);
    });

    // Karma events
    this.connection.on('KarmaAdded', (data) => {
      console.log('Karma Added:', data);
      toast.success(`+${data.karma} karma added!`);
      this.emit('karmaAdded', data);
    });

    this.connection.on('KarmaRemoved', (data) => {
      console.log('Karma Removed:', data);
      toast.success(`${data.karma} karma removed`);
      this.emit('karmaRemoved', data);
    });

    this.connection.on('KarmaSet', (data) => {
      console.log('Karma Set:', data);
      toast.success(`Karma set to ${data.karma}`);
      this.emit('karmaSet', data);
    });

    // Progress events
    this.connection.on('ProgressUpdate', (data) => {
      console.log('Progress Update:', data);
      this.emit('progressUpdate', data);
    });

    // Error events
    this.connection.on('Error', (error) => {
      console.error('STAR Error:', error);
      toast.error(`STAR Error: ${error}`);
      this.emit('error', error);
    });

    this.connection.on('Success', (message) => {
      console.log('STAR Success:', message);
      toast.success(message);
      this.emit('success', message);
    });

    // User events
    this.connection.on('UserConnected', (connectionId) => {
      console.log('User Connected:', connectionId);
      this.emit('userConnected', connectionId);
    });

    this.connection.on('UserDisconnected', (connectionId) => {
      console.log('User Disconnected:', connectionId);
      this.emit('userDisconnected', connectionId);
    });

    // Generic message events
    this.connection.on('ReceiveMessage', (user, message) => {
      console.log('Message received:', user, message);
      this.emit('messageReceived', { user, message });
    });
  }

  private handleReconnection() {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      console.log(`Attempting to reconnect (${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);
      
      setTimeout(() => {
        this.start();
      }, this.reconnectInterval);
    } else {
      console.error('Max reconnection attempts reached');
      toast.error('Connection lost. Please refresh the page.');
    }
  }

  public async start(): Promise<void> {
    if (!this.connection) {
      this.initializeConnection();
    }

    try {
      if (this.connection) {
        await this.connection.start();
        this.isConnected = true;
        this.reconnectAttempts = 0;
        console.log('SignalR connection started');
      }
      toast.success('Connected to STAR');
    } catch (error) {
      console.error('Error starting SignalR connection:', error);
      this.isConnected = false;
      this.handleReconnection();
    }
  }

  public async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.isConnected = false;
      console.log('SignalR connection stopped');
    }
  }

  public async joinGroup(groupName: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('JoinGroup', groupName);
    }
  }

  public async leaveGroup(groupName: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('LeaveGroup', groupName);
    }
  }

  public async sendMessage(user: string, message: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('SendMessage', user, message);
    }
  }

  public async sendSTARStatus(status: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('SendSTARStatus', status);
    }
  }

  public async sendProgressUpdate(operation: string, progress: number, message: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('SendProgressUpdate', operation, progress, message);
    }
  }

  public async sendError(error: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('SendError', error);
    }
  }

  public async sendSuccess(message: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('SendSuccess', message);
    }
  }

  public getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state || signalR.HubConnectionState.Disconnected;
  }

  public isConnectionActive(): boolean {
    return this.isConnected && this.connection?.state === signalR.HubConnectionState.Connected;
  }

  // Event emitter functionality
  private eventListeners: { [key: string]: Function[] } = {};

  public on(event: string, callback: Function): void {
    if (!this.eventListeners[event]) {
      this.eventListeners[event] = [];
    }
    this.eventListeners[event].push(callback);
  }

  public off(event: string, callback: Function): void {
    if (this.eventListeners[event]) {
      this.eventListeners[event] = this.eventListeners[event].filter(cb => cb !== callback);
    }
  }

  private emit(event: string, data?: any): void {
    if (this.eventListeners[event]) {
      this.eventListeners[event].forEach(callback => callback(data));
    }
  }
}

// Create singleton instance
export const signalRService = new SignalRService();
export default signalRService;
