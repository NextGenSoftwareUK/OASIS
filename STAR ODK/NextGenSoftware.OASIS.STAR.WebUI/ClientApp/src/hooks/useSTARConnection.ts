import { useState, useEffect, useCallback } from 'react';
import { useQuery, useQueryClient } from 'react-query';
import { starService } from '../services/starService';
import { signalRService } from '../services/signalRService';
import { ConnectionStatus, STARStatus } from '../types/star';

export const useSTARConnection = () => {
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>({
    isConnected: false,
    status: 'disconnected',
  });

  const queryClient = useQueryClient();

  // Check STAR status - TEMPORARILY DISABLED for demo mode
  // TODO: Re-enable when live API is working
  const { data: starStatus, isLoading, error } = useQuery<STARStatus>(
    'starStatus',
    starService.getSTARStatus,
    {
      // refetchInterval: 2000, // Check every 2 seconds for more responsive updates
      // refetchOnWindowFocus: true, // Check when user returns to tab
      // refetchIntervalInBackground: true, // Continue checking even when tab is not active
      enabled: false, // TEMPORARILY DISABLED - only manual control for demo
      // onSuccess: (data) => {
      //   console.log('STAR Status Update from API:', data); // Debug logging
      //   setConnectionStatus(prev => ({
      //     ...prev,
      //     isConnected: data.isIgnited,
      //     status: data.isIgnited ? 'connected' : 'disconnected',
      //     lastConnected: data.isIgnited ? new Date() : prev.lastConnected,
      //   }));
      // },
      // onError: (error) => {
      //   console.error('STAR Status Error:', error); // Debug logging
      //   setConnectionStatus(prev => ({
      //     ...prev,
      //     isConnected: false,
      //     status: 'error',
      //     error: error instanceof Error ? error.message : 'Unknown error',
      //   }));
      // },
    }
  );

  // SignalR connection management
  useEffect(() => {
    const initializeSignalR = async () => {
      try {
        await signalRService.start();
        
        // Set up event listeners
        signalRService.on('starIgnited', () => {
          queryClient.invalidateQueries('starStatus');
        });

        signalRService.on('starExtinguished', () => {
          queryClient.invalidateQueries('starStatus');
        });

        signalRService.on('starStatusUpdate', (status: any) => {
          queryClient.invalidateQueries('starStatus');
        });

        signalRService.on('error', (error: any) => {
          setConnectionStatus(prev => ({
            ...prev,
            status: 'error',
            error: error,
          }));
        });

      } catch (error) {
        console.error('Failed to initialize SignalR:', error);
        setConnectionStatus(prev => ({
          ...prev,
          status: 'error',
          error: error instanceof Error ? error.message : 'Failed to connect',
        }));
      }
    };

    initializeSignalR();

    return () => {
      signalRService.stop();
    };
  }, [queryClient]);

  const igniteSTAR = useCallback(async () => {
    try {
      setConnectionStatus(prev => ({ ...prev, status: 'connecting' }));
      const result = await starService.igniteSTAR();
      
      if (result.isError) {
        throw new Error(result.message || 'Failed to ignite STAR');
      }

      // Immediately update connection status to connected
      console.log('STAR Ignited - updating status to connected');
      setConnectionStatus(prev => ({
        ...prev,
        isConnected: true,
        status: 'connected',
        lastConnected: new Date(),
      }));

      // TODO: Re-enable when live API is working
      // queryClient.invalidateQueries('starStatus');
      
      return result;
    } catch (error) {
      setConnectionStatus(prev => ({
        ...prev,
        status: 'error',
        error: error instanceof Error ? error.message : 'Failed to ignite STAR',
      }));
      throw error;
    }
  }, [queryClient]);

  const extinguishStar = useCallback(async () => {
    try {
      setConnectionStatus(prev => ({ ...prev, status: 'connecting' }));
      const result = await starService.extinguishStar();
      
      if (result.isError) {
        throw new Error(result.message || 'Failed to extinguish STAR');
      }

      // Immediately update connection status to disconnected
      setConnectionStatus(prev => ({
        ...prev,
        isConnected: false,
        status: 'disconnected',
      }));

      // TODO: Re-enable when live API is working
      // queryClient.invalidateQueries('starStatus');
      return result;
    } catch (error) {
      setConnectionStatus(prev => ({
        ...prev,
        status: 'error',
        error: error instanceof Error ? error.message : 'Failed to extinguish STAR',
      }));
      throw error;
    }
  }, [queryClient]);

  const reconnect = useCallback(async () => {
    try {
      setConnectionStatus(prev => ({ ...prev, status: 'connecting' }));
      await signalRService.start();
      queryClient.invalidateQueries('starStatus');
    } catch (error) {
      setConnectionStatus(prev => ({
        ...prev,
        status: 'error',
        error: error instanceof Error ? error.message : 'Failed to reconnect',
      }));
    }
  }, [queryClient]);

  // Debug logging
  console.log('useSTARConnection - Current status:', {
    isConnected: connectionStatus.isConnected,
    status: connectionStatus.status,
    isLoading,
    error: error instanceof Error ? error.message : error
  });

  return {
    isConnected: connectionStatus.isConnected,
    connectionStatus: connectionStatus.status,
    isLoading,
    error,
    starStatus,
    igniteSTAR,
    extinguishStar,
    reconnect,
    signalRService,
  };
};
