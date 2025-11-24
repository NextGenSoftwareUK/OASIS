import React, { useState, useEffect } from 'react';
import { Badge } from '@/components/ui/badge';
import { Wifi, WifiOff, AlertCircle } from 'lucide-react';
import { config } from '@/lib/config';

interface APIStatusProps {
  className?: string;
}

export const APIStatus: React.FC<APIStatusProps> = ({ className }) => {
  const [status, setStatus] = useState<'checking' | 'connected' | 'disconnected'>('checking');
  const [lastCheck, setLastCheck] = useState<Date>(new Date());

  const checkAPIStatus = async () => {
    try {
      const response = await fetch(`${config.api.baseUrl}/api/wallet/clear_cache`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });
      
      if (response.ok) {
        setStatus('connected');
      } else {
        setStatus('disconnected');
      }
    } catch (error) {
      setStatus('disconnected');
    }
    
    setLastCheck(new Date());
  };

  useEffect(() => {
    checkAPIStatus();
    
    // Check every 30 seconds
    const interval = setInterval(checkAPIStatus, 30000);
    return () => clearInterval(interval);
  }, []);

  const getStatusIcon = () => {
    switch (status) {
      case 'connected':
        return <Wifi className="h-3 w-3" />;
      case 'disconnected':
        return <WifiOff className="h-3 w-3" />;
      case 'checking':
        return <AlertCircle className="h-3 w-3" />;
    }
  };

  const getStatusColor = () => {
    switch (status) {
      case 'connected':
        return 'bg-green-500 text-white';
      case 'disconnected':
        return 'bg-red-500 text-white';
      case 'checking':
        return 'bg-yellow-500 text-white';
    }
  };

  const getStatusText = () => {
    switch (status) {
      case 'connected':
        return 'API Connected';
      case 'disconnected':
        return 'API Disconnected';
      case 'checking':
        return 'Checking API...';
    }
  };

  return (
    <Badge 
      variant="outline" 
      className={`flex items-center space-x-1 ${getStatusColor()} ${className}`}
      onClick={checkAPIStatus}
    >
      {getStatusIcon()}
      <span className="text-xs">{getStatusText()}</span>
    </Badge>
  );
}; 