import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ChevronDown, ChevronUp, RefreshCw, ExternalLink } from 'lucide-react';
import { config } from '@/lib/config';
import { oasisWalletAPI } from '@/lib/api';

interface DebugPanelProps {
  className?: string;
}

export const DebugPanel: React.FC<DebugPanelProps> = ({ className }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [apiStatus, setApiStatus] = useState<string>('Unknown');
  const [lastTest, setLastTest] = useState<Date | null>(null);

  const testAPI = async () => {
    setApiStatus('Testing...');
    try {
      const result = await oasisWalletAPI.clearCache();
      setApiStatus(result.isError ? 'Error' : 'Success');
      setLastTest(new Date());
    } catch (error) {
      setApiStatus('Failed');
      setLastTest(new Date());
    }
  };

  return (
    <Card className={`cosmic-card ${className}`}>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm cosmic-text">Debug Panel</CardTitle>
          <div className="flex items-center space-x-2">
            <Badge variant={apiStatus === 'Success' ? 'default' : 'destructive'} className="bg-white/10 border-white/20 text-white">
              {apiStatus}
            </Badge>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setIsExpanded(!isExpanded)}
              className="text-white hover:bg-white/10"
            >
              {isExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
            </Button>
          </div>
        </div>
      </CardHeader>
      
      {isExpanded && (
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <label className="font-medium text-gray-300">API URL:</label>
              <p className="text-gray-400 break-all">{config.api.baseUrl}</p>
            </div>
            <div>
              <label className="font-medium text-gray-300">API Version:</label>
              <p className="text-gray-400">{config.api.version}</p>
            </div>
            <div>
              <label className="font-medium text-gray-300">Demo User ID:</label>
              <p className="text-gray-400 break-all">{config.demo.userId}</p>
            </div>
            <div>
              <label className="font-medium text-gray-300">Last Test:</label>
              <p className="text-gray-400">
                {lastTest ? lastTest.toLocaleTimeString() : 'Never'}
              </p>
            </div>
          </div>
          
          <div className="flex space-x-2">
            <Button size="sm" onClick={testAPI} className="cosmic-button">
              <RefreshCw className="h-4 w-4 mr-2" />
              Test API
            </Button>
            <Button 
              size="sm" 
              variant="outline"
              onClick={() => window.open(`${config.api.baseUrl}/api/wallet/clear_cache`, '_blank')}
              className="border-white/20 text-white hover:bg-white/10"
            >
              <ExternalLink className="h-4 w-4 mr-2" />
              Open API
            </Button>
          </div>
          
          <div className="text-xs text-gray-400">
            <p>Check the browser console for detailed API request logs.</p>
          </div>
        </CardContent>
      )}
    </Card>
  );
}; 