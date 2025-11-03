import { useState, useEffect, useCallback } from 'react';
import { useQuery, useQueryClient } from 'react-query';
import { starCoreService } from '../services';
import { signalRService } from '../services/signalRService';
import { ConnectionStatus, STARStatus } from '../types/star';

// Helper function to check demo mode
const isDemoMode = () => {
  const saved = localStorage.getItem('demoMode');
  return saved ? JSON.parse(saved) : true;
};

export const useSTARConnection = () => {
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>({
    isConnected: false,
    status: 'disconnected',
  });

  const queryClient = useQueryClient();

  // Check STAR status - DISABLED due to backend inconsistency
  // The backend reports "ignited successfully" but then immediately reports isIgnited: false
  // This causes the UI to flicker between connected/disconnected states
  // TODO: Re-enable when backend is fixed to be consistent
  const { data: starStatus, isLoading, error } = useQuery<STARStatus>(
    'starStatus',
    starCoreService.getSTARStatus,
    {
      refetchInterval: false, // Disabled due to backend inconsistency
      refetchOnWindowFocus: false, // Disabled due to backend inconsistency
      refetchIntervalInBackground: false, // Disabled due to backend inconsistency
      enabled: false, // Completely disabled due to backend inconsistency
      onSuccess: (data) => {
        console.log('STAR Status Update from API:', data); // Debug logging
        // Commented out due to backend inconsistency - it overrides manual status updates
        // setConnectionStatus(prev => ({
        //   ...prev,
        //   isConnected: data.isIgnited,
        //   status: data.isIgnited ? 'connected' : 'disconnected',
        //   lastConnected: data.isIgnited ? new Date() : prev.lastConnected,
        // }));
      },
      onError: (error) => {
        console.error('STAR Status Error:', error); // Debug logging
        setConnectionStatus(prev => ({
          ...prev,
          isConnected: false,
          status: 'error',
          error: error instanceof Error ? error.message : 'Unknown error',
        }));
      },
    }
  );

  // SignalR connection management - only in live mode
  useEffect(() => {
    if (isDemoMode()) {
      return;
    }

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
      if (!isDemoMode()) {
        signalRService.stop();
      }
    };
  }, [queryClient]);

  const igniteSTAR = useCallback(async () => {
    try {
      setConnectionStatus(prev => ({ ...prev, status: 'connecting' }));
      const result = await starCoreService.igniteSTAR();
      
      if (result.isError || result.result === false) {
        // API failed or backend returned false result - update status to error
        setConnectionStatus(prev => ({
          ...prev,
          isConnected: false,
          status: 'error',
          error: result.message || 'Failed to ignite STAR',
        }));
        throw new Error(result.message || 'Failed to ignite STAR');
      }

      // Only update to connected if API call succeeded AND result is true
      setConnectionStatus(prev => ({
        ...prev,
        isConnected: true,
        status: 'connected',
        lastConnected: new Date(),
      }));

      // Invalidate queries only in live mode
      if (!isDemoMode()) {
        queryClient.invalidateQueries('starStatus');
      }
      
      return result;
    } catch (error) {
      setConnectionStatus(prev => ({
        ...prev,
        isConnected: false,
        status: 'error',
        error: error instanceof Error ? error.message : 'Failed to ignite STAR',
      }));
      throw error;
    }
  }, [queryClient]);

  const extinguishStar = useCallback(async () => {
    try {
      setConnectionStatus(prev => ({ ...prev, status: 'connecting' }));
      const result = await starCoreService.extinguishStar();
      
      if (result.isError) {
        // API failed - update status to error
        setConnectionStatus(prev => ({
          ...prev,
          isConnected: false,
          status: 'error',
          error: result.message || 'Failed to extinguish STAR',
        }));
        throw new Error(result.message || 'Failed to extinguish STAR');
      }

      // Only update to disconnected if API call succeeded
      setConnectionStatus(prev => ({
        ...prev,
        isConnected: false,
        status: 'disconnected',
      }));

      // Invalidate queries only in live mode
      if (!isDemoMode()) {
        queryClient.invalidateQueries('starStatus');
      }
      return result;
    } catch (error) {
      setConnectionStatus(prev => ({
        ...prev,
        isConnected: false,
        status: 'error',
        error: error instanceof Error ? error.message : 'Failed to extinguish STAR',
      }));
      throw error;
    }
  }, [queryClient]);

  const reconnect = useCallback(async () => {
    try {
      setConnectionStatus(prev => ({ ...prev, status: 'connecting' }));
      
      if (isDemoMode()) {
        // Demo mode - just call ignite under the hood
        await igniteSTAR();
      } else {
        // Live mode - attempt actual reconnection
        if (signalRService) {
          try {
            await signalRService.start();
            queryClient.invalidateQueries('starStatus');
          } catch (signalRError) {
            // SignalR might already be running - that's okay
            console.log('SignalR start failed (likely already running):', signalRError);
          }
        }
      }
    } catch (error) {
      setConnectionStatus(prev => ({
        ...prev,
        status: 'error',
        error: error instanceof Error ? error.message : 'Failed to reconnect',
      }));
    }
  }, [queryClient]);


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
