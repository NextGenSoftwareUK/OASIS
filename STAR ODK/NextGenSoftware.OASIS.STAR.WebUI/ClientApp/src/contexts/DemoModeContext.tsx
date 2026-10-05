import React, { createContext, useContext, useState, useEffect } from 'react';

interface DemoModeContextType {
  isDemoMode: boolean;
  setDemoMode: (demoMode: boolean) => void;
  toggleDemoMode: () => void;
}

const DemoModeContext = createContext<DemoModeContextType | undefined>(undefined);

export const DemoModeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isDemoMode, setIsDemoMode] = useState<boolean>(() => {
    // Load from localStorage or default to true
    const saved = localStorage.getItem('demoMode');
    return saved ? JSON.parse(saved) : true;
  });

  const setDemoMode = (demoMode: boolean) => {
    setIsDemoMode(demoMode);
    localStorage.setItem('demoMode', JSON.stringify(demoMode));
  };

  const toggleDemoMode = () => {
    setDemoMode(!isDemoMode);
  };

  return (
    <DemoModeContext.Provider value={{ isDemoMode, setDemoMode, toggleDemoMode }}>
      {children}
    </DemoModeContext.Provider>
  );
};

export const useDemoMode = () => {
  const context = useContext(DemoModeContext);
  if (context === undefined) {
    throw new Error('useDemoMode must be used within a DemoModeProvider');
  }
  return context;
};
