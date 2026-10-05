import React, { createContext, useContext, useState, ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from 'react-query';
import { OASISClient } from '@oasis/api-client';

interface OASISConfig {
  apiEndpoint: string;
  defaultProvider?: string;
  theme?: any;
  enableAnalytics?: boolean;
  debug?: boolean;
  [key: string]: any;
}

interface OASISContextType {
  client: OASISClient;
  config: OASISConfig;
  updateConfig: (newConfig: Partial<OASISConfig>) => void;
}

const OASISContext = createContext<OASISContextType | undefined>(undefined);

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
});

export interface OASISProviderProps {
  config: OASISConfig;
  children: ReactNode;
}

export const OASISProvider: React.FC<OASISProviderProps> = ({ config, children }) => {
  const [currentConfig, setCurrentConfig] = useState<OASISConfig>(config);
  const [client] = useState(() => new OASISClient(config));

  const updateConfig = (newConfig: Partial<OASISConfig>) => {
    setCurrentConfig(prev => ({ ...prev, ...newConfig }));
  };

  const value = {
    client,
    config: currentConfig,
    updateConfig,
  };

  return (
    <QueryClientProvider client={queryClient}>
      <OASISContext.Provider value={value}>
        {children}
      </OASISContext.Provider>
    </QueryClientProvider>
  );
};

export const useOASIS = (): OASISContextType => {
  const context = useContext(OASISContext);
  if (!context) {
    throw new Error('useOASIS must be used within an OASISProvider');
  }
  return context;
};

export default OASISProvider;

