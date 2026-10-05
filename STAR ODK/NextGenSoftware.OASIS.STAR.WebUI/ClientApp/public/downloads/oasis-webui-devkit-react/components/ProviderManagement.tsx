import React, { useState, useEffect } from 'react';
import { useQuery, useMutation } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface ProviderManagementProps {
  theme?: 'light' | 'dark';
  showStatus?: boolean;
  customStyles?: React.CSSProperties;
}

export const ProviderManagement: React.FC<ProviderManagementProps> = ({
  theme = 'dark',
  showStatus = true,
  customStyles = {}
}) => {
  const [selectedProvider, setSelectedProvider] = useState<string>('');
  const client = new OASISClient();

  const { data: providers, isLoading } = useQuery(
    'providers',
    () => client.getAvailableProviders()
  );

  const { data: currentProvider } = useQuery(
    'current-provider',
    () => client.getCurrentProvider()
  );

  const switchProviderMutation = useMutation(
    (provider: string) => client.switchProvider(provider),
    {
      onSuccess: () => {
        // Refresh current provider
      }
    }
  );

  return (
    <div className={`oasis-provider-mgmt oasis-provider-mgmt--${theme}`} style={customStyles}>
      <h3>Storage Provider</h3>
      
      {showStatus && currentProvider && (
        <div className="current-provider">
          <span className="label">Active Provider:</span>
          <span className="value">{currentProvider.name}</span>
          <span className={`status status--${currentProvider.status}`}>
            {currentProvider.status}
          </span>
        </div>
      )}

      <div className="provider-list">
        {isLoading ? (
          <div>Loading providers...</div>
        ) : (
          providers?.map((provider: any) => (
            <div 
              key={provider.id}
              className={`provider-item ${selectedProvider === provider.id ? 'selected' : ''}`}
              onClick={() => setSelectedProvider(provider.id)}
            >
              <div className="provider-icon">
                <img src={provider.icon} alt={provider.name} />
              </div>
              <div className="provider-info">
                <h4>{provider.name}</h4>
                <p>{provider.description}</p>
                <div className="provider-stats">
                  <span>Speed: {provider.speed}</span>
                  <span>Cost: {provider.cost}</span>
                  <span>Reliability: {provider.reliability}%</span>
                </div>
              </div>
              {provider.id === currentProvider?.id && (
                <div className="active-badge">Active</div>
              )}
            </div>
          ))
        )}
      </div>

      {selectedProvider && selectedProvider !== currentProvider?.id && (
        <button
          className="switch-btn"
          onClick={() => switchProviderMutation.mutate(selectedProvider)}
          disabled={switchProviderMutation.isLoading}
        >
          {switchProviderMutation.isLoading ? 'Switching...' : 'Switch Provider'}
        </button>
      )}
    </div>
  );
};

export default ProviderManagement;

