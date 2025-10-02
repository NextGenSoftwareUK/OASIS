import { createContext, useContext, ReactNode } from 'react';
import axios from 'axios';

interface OASISContextType {
  client: any;
  isAuthenticated: boolean;
}

const OASISContext = createContext<OASISContextType | undefined>(undefined);

interface OASISProviderProps {
  children: ReactNode;
  apiUrl: string;
  apiKey?: string;
}

export function OASISProvider({ children, apiUrl, apiKey }: OASISProviderProps) {
  const client = axios.create({
    baseURL: apiUrl,
    headers: apiKey ? { 'X-API-Key': apiKey } : {}
  });

  return (
    <OASISContext.Provider value={{ client, isAuthenticated: false }}>
      {children}
    </OASISContext.Provider>
  );
}

export function useOASIS() {
  const context = useContext(OASISContext);
  if (!context) {
    throw new Error('useOASIS must be used within OASISProvider');
  }
  return context;
}



