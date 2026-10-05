import React, { useState, useEffect } from 'react';
import { 
  HyperDriveConfig, 
  ReplicationRules, 
  FailoverRules,
  ProviderReplicationRule,
  ProviderFailoverRule,
  SubscriptionConfig,
  SubscriptionPlanType
} from '../../types/hyperDriveTypes';
import { hyperDriveService } from '../../services/core/hyperDriveService';

interface ProviderManagerProps {
  config: HyperDriveConfig;
  onConfigUpdate: (config: HyperDriveConfig) => void;
  subscriptionPlan: SubscriptionPlanType;
}

interface Provider {
  id: string;
  name: string;
  type: string;
  category: 'blockchain' | 'storage' | 'network' | 'cloud' | 'core';
  isFree: boolean;
  gasFeeEstimate?: number;
  costPerOperation?: number;
  isEnabled: boolean;
  icon: string;
  description: string;
}

const ProviderManager: React.FC<ProviderManagerProps> = ({ 
  config, 
  onConfigUpdate, 
  subscriptionPlan 
}) => {
  const [availableProviders, setAvailableProviders] = useState<Provider[]>([]);
  const [failoverProviders, setFailoverProviders] = useState<Provider[]>([]);
  const [replicationProviders, setReplicationProviders] = useState<Provider[]>([]);
  const [loadBalancingProviders, setLoadBalancingProviders] = useState<Provider[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Available providers with enhanced information
  const allProviders: Provider[] = [
    // Blockchain Providers
    { id: 'ethereum', name: 'Ethereum', type: 'EthereumOASIS', category: 'blockchain', isFree: false, gasFeeEstimate: 0.05, costPerOperation: 0.02, isEnabled: true, icon: '🔗', description: 'Ethereum blockchain with smart contracts' },
    { id: 'polygon', name: 'Polygon', type: 'PolygonOASIS', category: 'blockchain', isFree: false, gasFeeEstimate: 0.001, costPerOperation: 0.001, isEnabled: true, icon: '💜', description: 'Polygon L2 with low gas fees' },
    { id: 'avalanche', name: 'Avalanche', type: 'AvalancheOASIS', category: 'blockchain', isFree: false, gasFeeEstimate: 0.01, costPerOperation: 0.005, isEnabled: true, icon: '❄️', description: 'Avalanche C-Chain with fast finality' },
    { id: 'bsc', name: 'BNB Chain', type: 'BNBChainOASIS', category: 'blockchain', isFree: false, gasFeeEstimate: 0.002, costPerOperation: 0.001, isEnabled: true, icon: '🟡', description: 'BNB Smart Chain with low fees' },
    { id: 'fantom', name: 'Fantom', type: 'FantomOASIS', category: 'blockchain', isFree: false, gasFeeEstimate: 0.001, costPerOperation: 0.0005, isEnabled: true, icon: '👻', description: 'Fantom Opera with fast transactions' },
    { id: 'tron', name: 'TRON', type: 'TRONOASIS', category: 'blockchain', isFree: false, gasFeeEstimate: 0.1, costPerOperation: 0.05, isEnabled: true, icon: '🔴', description: 'TRON network with high throughput' },
    { id: 'telos', name: 'Telos', type: 'TelosOASIS', category: 'blockchain', isFree: false, gasFeeEstimate: 0.001, costPerOperation: 0.0001, isEnabled: true, icon: '🟢', description: 'Telos EVM with carbon neutral' },
    { id: 'arbitrum', name: 'Arbitrum', type: 'ArbitrumOASIS', category: 'blockchain', isFree: false, gasFeeEstimate: 0.01, costPerOperation: 0.005, isEnabled: true, icon: '🔵', description: 'Arbitrum L2 with Ethereum compatibility' },
    
    // Storage Providers
    { id: 'mongodb', name: 'MongoDB', type: 'MongoOASIS', category: 'storage', isFree: true, costPerOperation: 0, isEnabled: true, icon: '🍃', description: 'MongoDB database storage' },
    { id: 'sqlite', name: 'SQLite', type: 'SQLLiteDBOASIS', category: 'storage', isFree: true, costPerOperation: 0, isEnabled: true, icon: '🗄️', description: 'SQLite local database' },
    { id: 'ipfs', name: 'IPFS', type: 'IPFSOASIS', category: 'storage', isFree: true, costPerOperation: 0, isEnabled: true, icon: '🌐', description: 'InterPlanetary File System' },
    { id: 'pinata', name: 'Pinata', type: 'PinataOASIS', category: 'storage', isFree: false, costPerOperation: 0.01, isEnabled: true, icon: '📌', description: 'Pinata IPFS pinning service' },
    { id: 'azure', name: 'Azure CosmosDB', type: 'AzureCosmosDBOASIS', category: 'cloud', isFree: false, costPerOperation: 0.02, isEnabled: true, icon: '☁️', description: 'Azure CosmosDB cloud database' },
    
    // Network Providers
    { id: 'seeds', name: 'SEEDS', type: 'SEEDSOASIS', category: 'network', isFree: true, costPerOperation: 0, isEnabled: true, icon: '🌱', description: 'SEEDS regenerative economy' },
    { id: 'scuttlebutt', name: 'Scuttlebutt', type: 'ScuttlebuttOASIS', category: 'network', isFree: true, costPerOperation: 0, isEnabled: true, icon: '🚤', description: 'Scuttlebutt peer-to-peer network' },
    { id: 'threefold', name: 'ThreeFold', type: 'ThreeFoldOASIS', category: 'network', isFree: true, costPerOperation: 0, isEnabled: true, icon: '🔺', description: 'ThreeFold decentralized internet' },
    { id: 'holo', name: 'Holo', type: 'HoloOASIS', category: 'network', isFree: true, costPerOperation: 0, isEnabled: true, icon: '🔮', description: 'Holo distributed hosting' },
    { id: 'plan', name: 'PLAN', type: 'PLANOASIS', category: 'network', isFree: true, costPerOperation: 0, isEnabled: true, icon: '📋', description: 'PLAN protocol network' },
    { id: 'elrond', name: 'Elrond', type: 'ElrondOASIS', category: 'blockchain', isFree: false, gasFeeEstimate: 0.001, costPerOperation: 0.0001, isEnabled: true, icon: '⚡', description: 'Elrond high-performance blockchain' },
    
    // Core Providers
    { id: 'solid', name: 'SOLID', type: 'SOLIDOASIS', category: 'core', isFree: true, costPerOperation: 0, isEnabled: true, icon: '🔷', description: 'SOLID decentralized data' },
    { id: 'blockstack', name: 'Blockstack', type: 'BlockStackOASIS', category: 'core', isFree: true, costPerOperation: 0, isEnabled: true, icon: '🧱', description: 'Blockstack decentralized identity' },
    { id: 'chainlink', name: 'Chainlink', type: 'ChainLinkOASIS', category: 'core', isFree: false, costPerOperation: 0.1, isEnabled: true, icon: '🔗', description: 'Chainlink oracle network' },
    { id: 'web3core', name: 'Web3Core', type: 'Web3CoreOASIS', category: 'core', isFree: true, costPerOperation: 0, isEnabled: true, icon: '🌍', description: 'Web3Core infrastructure' }
  ];

  useEffect(() => {
    loadProviderData();
  }, []);

  const loadProviderData = async () => {
    setLoading(true);
    try {
      // Load current provider configurations
      const [failoverRules, replicationRules] = await Promise.all([
        hyperDriveService.getFailoverRules(),
        hyperDriveService.getReplicationRules()
      ]);

      // Set up provider lists based on current configuration
      const failoverList = allProviders.filter(p => 
        config.autoFailoverProviders.includes(p.type)
      );
      const replicationList = allProviders.filter(p => 
        config.autoReplicationProviders.includes(p.type)
      );
      const loadBalancingList = allProviders.filter(p => 
        config.loadBalancingProviders.includes(p.type)
      );
      const availableList = allProviders.filter(p => 
        !config.autoFailoverProviders.includes(p.type) &&
        !config.autoReplicationProviders.includes(p.type) &&
        !config.loadBalancingProviders.includes(p.type)
      );

      setFailoverProviders(failoverList);
      setReplicationProviders(replicationList);
      setLoadBalancingProviders(loadBalancingList);
      setAvailableProviders(availableList);
    } catch (err) {
      setError('Failed to load provider data');
      console.error('Error loading provider data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleDragStart = (e: React.DragEvent, provider: Provider, sourceList: string) => {
    e.dataTransfer.setData('application/json', JSON.stringify({ provider, sourceList }));
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
  };

  const handleDrop = async (e: React.DragEvent, targetList: string) => {
    e.preventDefault();
    const data = JSON.parse(e.dataTransfer.getData('application/json'));
    const { provider, sourceList } = data;

    if (sourceList === targetList) return;

    try {
      // Update provider lists
      const newAvailable = [...availableProviders];
      const newFailover = [...failoverProviders];
      const newReplication = [...replicationProviders];
      const newLoadBalancing = [...loadBalancingProviders];

      // Remove from source list
      if (sourceList === 'available') {
        setAvailableProviders(prev => prev.filter(p => p.id !== provider.id));
      } else if (sourceList === 'failover') {
        setFailoverProviders(prev => prev.filter(p => p.id !== provider.id));
      } else if (sourceList === 'replication') {
        setReplicationProviders(prev => prev.filter(p => p.id !== provider.id));
      } else if (sourceList === 'loadbalancing') {
        setLoadBalancingProviders(prev => prev.filter(p => p.id !== provider.id));
      }

      // Add to target list
      if (targetList === 'available') {
        setAvailableProviders(prev => [...prev, provider]);
      } else if (targetList === 'failover') {
        setFailoverProviders(prev => [...prev, provider]);
      } else if (targetList === 'replication') {
        setReplicationProviders(prev => [...prev, provider]);
      } else if (targetList === 'loadbalancing') {
        setLoadBalancingProviders(prev => [...prev, provider]);
      }

      // Update configuration
      const updatedConfig = { ...config };
      
      if (targetList === 'failover') {
        updatedConfig.autoFailoverProviders = [...updatedConfig.autoFailoverProviders, provider.type];
      } else if (targetList === 'replication') {
        updatedConfig.autoReplicationProviders = [...updatedConfig.autoReplicationProviders, provider.type];
      } else if (targetList === 'loadbalancing') {
        updatedConfig.loadBalancingProviders = [...updatedConfig.loadBalancingProviders, provider.type];
      }

      if (sourceList === 'failover') {
        updatedConfig.autoFailoverProviders = updatedConfig.autoFailoverProviders.filter(p => p !== provider.type);
      } else if (sourceList === 'replication') {
        updatedConfig.autoReplicationProviders = updatedConfig.autoReplicationProviders.filter(p => p !== provider.type);
      } else if (sourceList === 'loadbalancing') {
        updatedConfig.loadBalancingProviders = updatedConfig.loadBalancingProviders.filter(p => p !== provider.type);
      }

      onConfigUpdate(updatedConfig);
    } catch (err) {
      setError('Failed to update provider configuration');
      console.error('Error updating provider configuration:', err);
    }
  };

  const renderProviderCard = (provider: Provider, listType: string) => (
    <div
      key={provider.id}
      draggable
      onDragStart={(e) => handleDragStart(e, provider, listType)}
      className={`p-3 rounded-lg border-2 border-dashed cursor-move transition-all duration-200 hover:shadow-md ${
        provider.isFree 
          ? 'border-green-300 bg-green-50 hover:border-green-400' 
          : 'border-blue-300 bg-blue-50 hover:border-blue-400'
      }`}
    >
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <span className="text-2xl">{provider.icon}</span>
          <div>
            <h4 className="font-semibold text-gray-900">{provider.name}</h4>
            <p className="text-sm text-gray-600">{provider.description}</p>
          </div>
        </div>
        <div className="flex flex-col items-end space-y-1">
          {provider.isFree ? (
            <span className="px-2 py-1 bg-green-100 text-green-800 text-xs rounded-full">
              FREE
            </span>
          ) : (
            <span className="px-2 py-1 bg-orange-100 text-orange-800 text-xs rounded-full">
              ${provider.costPerOperation?.toFixed(4)}/op
            </span>
          )}
          <span className={`px-2 py-1 text-xs rounded-full ${
            provider.category === 'blockchain' ? 'bg-purple-100 text-purple-800' :
            provider.category === 'storage' ? 'bg-blue-100 text-blue-800' :
            provider.category === 'network' ? 'bg-green-100 text-green-800' :
            provider.category === 'cloud' ? 'bg-gray-100 text-gray-800' :
            'bg-yellow-100 text-yellow-800'
          }`}>
            {provider.category.toUpperCase()}
          </span>
        </div>
      </div>
    </div>
  );

  const renderProviderList = (providers: Provider[], title: string, listType: string, color: string) => (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200">
      <div className={`p-4 border-b border-gray-200 bg-${color}-50`}>
        <h3 className={`text-lg font-semibold text-${color}-900`}>{title}</h3>
        <p className="text-sm text-gray-600 mt-1">
          {providers.length} provider{providers.length !== 1 ? 's' : ''} configured
        </p>
      </div>
      <div 
        className="p-4 min-h-[200px]"
        onDragOver={handleDragOver}
        onDrop={(e) => handleDrop(e, listType)}
      >
        {providers.length === 0 ? (
          <div className="flex items-center justify-center h-full text-gray-500">
            <div className="text-center">
              <div className="text-4xl mb-2">📦</div>
              <p>Drag providers here</p>
            </div>
          </div>
        ) : (
          <div className="space-y-3">
            {providers.map(provider => renderProviderCard(provider, listType))}
          </div>
        )}
      </div>
    </div>
  );

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <p className="text-red-800">{error}</p>
        <button 
          onClick={loadProviderData}
          className="mt-2 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h2 className="text-xl font-semibold text-blue-900 mb-2">🎯 Provider Management</h2>
        <p className="text-blue-800">
          Drag and drop providers between lists to configure Auto Failover, Auto Replication, and Auto Load Balancing.
          <br />
          <strong>Free providers</strong> are recommended for {subscriptionPlan} plans to avoid additional costs.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Available Providers */}
        {renderProviderList(
          availableProviders, 
          "📦 Available Providers", 
          "available", 
          "gray"
        )}

        {/* Auto Failover Providers */}
        {renderProviderList(
          failoverProviders, 
          "🔄 Auto Failover Providers", 
          "failover", 
          "red"
        )}

        {/* Auto Replication Providers */}
        {renderProviderList(
          replicationProviders, 
          "📋 Auto Replication Providers", 
          "replication", 
          "green"
        )}

        {/* Auto Load Balancing Providers */}
        {renderProviderList(
          loadBalancingProviders, 
          "⚖️ Auto Load Balancing Providers", 
          "loadbalancing", 
          "blue"
        )}
      </div>

      {/* Provider Statistics */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">📊 Provider Statistics</h3>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div className="text-center">
            <div className="text-2xl font-bold text-gray-900">{availableProviders.length}</div>
            <div className="text-sm text-gray-600">Available</div>
          </div>
          <div className="text-center">
            <div className="text-2xl font-bold text-red-600">{failoverProviders.length}</div>
            <div className="text-sm text-gray-600">Failover</div>
          </div>
          <div className="text-center">
            <div className="text-2xl font-bold text-green-600">{replicationProviders.length}</div>
            <div className="text-sm text-gray-600">Replication</div>
          </div>
          <div className="text-center">
            <div className="text-2xl font-bold text-blue-600">{loadBalancingProviders.length}</div>
            <div className="text-sm text-gray-600">Load Balancing</div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProviderManager;
