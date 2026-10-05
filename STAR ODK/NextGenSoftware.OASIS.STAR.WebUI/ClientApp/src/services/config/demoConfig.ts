/**
 * Demo Mode Configuration
 * Centralized demo mode management
 */

export const isDemoMode = (): boolean => {
  const saved = localStorage.getItem('demoMode');
  return saved ? JSON.parse(saved) : true; // Default to demo mode
};

export const setDemoMode = (enabled: boolean): void => {
  localStorage.setItem('demoMode', JSON.stringify(enabled));
  console.log(`Demo mode ${enabled ? 'enabled' : 'disabled'}`);
};

export const toggleDemoMode = (): boolean => {
  const current = isDemoMode();
  setDemoMode(!current);
  return !current;
};

export const DEMO_CONFIG = {
  DEFAULT_DEMO_MODE: true,
  DEMO_DELAY: 500, // Simulate network delay
  DEMO_SUCCESS_RATE: 0.95, // 95% success rate for demo
} as const;
