import React, { useState, useEffect } from 'react';
import { OAPPKarmaData } from '../types/star';
import { starCoreService, avatarService } from '../services';
import { toast } from 'react-hot-toast';
import KarmaVisualization from './KarmaVisualization';

interface KarmaSearchProps {
  className?: string;
}

interface KarmaSearchFilters {
  minKarma: number;
  maxKarma: number;
  minUsers: number;
  maxUsers: number;
  karmaLevel: string;
}

const KarmaSearch: React.FC<KarmaSearchProps> = ({ className = '' }) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [searchResults, setSearchResults] = useState<OAPPKarmaData[]>([]);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<KarmaSearchFilters>({
    minKarma: 0,
    maxKarma: 1000000,
    minUsers: 0,
    maxUsers: 10000,
    karmaLevel: 'all'
  });
  const [showFilters, setShowFilters] = useState(false);

  const karmaLevels = [
    { value: 'all', label: 'All Levels', icon: '⚫' },
    { value: 'none', label: 'None (0)', icon: '⚫' },
    { value: 'low', label: 'Low (0-100)', icon: '🔴' },
    { value: 'medium', label: 'Medium (100-1K)', icon: '🟡' },
    { value: 'high', label: 'High (1K-10K)', icon: '🟢' },
    { value: 'very high', label: 'Very High (10K-100K)', icon: '🔵' },
    { value: 'legendary', label: 'Legendary (100K+)', icon: '🟣' }
  ];

  const performSearch = async () => {
    if (!searchTerm.trim()) {
      toast.error('Please enter a search term');
      return;
    }

    setLoading(true);
    try {
      // This would be replaced with actual search logic
      // For now, we'll simulate searching for OAPPs
      const mockResults: OAPPKarmaData[] = [
        {
          oappId: 'oapp_1',
          oappName: 'Quest OAPP',
          registeredAvatars: [],
          totalKarma: 15000,
          userCount: 25,
          averageKarma: 600,
          karmaLevel: 'High',
          karmaSources: ['Quest Completion', 'Good Deeds']
        },
        {
          oappId: 'oapp_2',
          oappName: 'Social OAPP',
          registeredAvatars: [],
          totalKarma: 5000,
          userCount: 50,
          averageKarma: 100,
          karmaLevel: 'Medium',
          karmaSources: ['Social Interactions', 'Community Help']
        }
      ];

      // Filter results based on current filters
      const filteredResults = mockResults.filter(result => {
        const matchesKarma = result.totalKarma >= filters.minKarma && result.totalKarma <= filters.maxKarma;
        const matchesUsers = result.userCount >= filters.minUsers && result.userCount <= filters.maxUsers;
        const matchesLevel = filters.karmaLevel === 'all' || result.karmaLevel.toLowerCase() === filters.karmaLevel;
        
        return matchesKarma && matchesUsers && matchesLevel;
      });

      setSearchResults(filteredResults);
      toast.success(`Found ${filteredResults.length} OAPPs`);
    } catch (error) {
      toast.error('Search failed');
      console.error('Search error:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (key: keyof KarmaSearchFilters, value: string | number) => {
    setFilters(prev => ({
      ...prev,
      [key]: value
    }));
  };

  const clearFilters = () => {
    setFilters({
      minKarma: 0,
      maxKarma: 1000000,
      minUsers: 0,
      maxUsers: 10000,
      karmaLevel: 'all'
    });
  };

  const formatKarma = (karma: number): string => {
    if (karma >= 1000000) return `${(karma / 1000000).toFixed(1)}M`;
    if (karma >= 1000) return `${(karma / 1000).toFixed(1)}K`;
    return karma.toFixed(0);
  };

  return (
    <div className={`karma-search ${className}`}>
      {/* Search Header */}
      <div className="bg-white rounded-lg shadow-md p-6 mb-6">
        <h2 className="text-2xl font-bold text-gray-800 mb-4">
          🔍 Karma Search
        </h2>
        
        {/* Search Input */}
        <div className="flex gap-3 mb-4">
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Search OAPPs by name, karma, or users..."
            className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            onKeyPress={(e) => e.key === 'Enter' && performSearch()}
          />
          <button
            onClick={performSearch}
            disabled={loading}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {loading ? 'Searching...' : 'Search'}
          </button>
        </div>

        {/* Filter Toggle */}
        <button
          onClick={() => setShowFilters(!showFilters)}
          className="text-blue-500 hover:text-blue-600 text-sm font-medium"
        >
          {showFilters ? '▼' : '▶'} Advanced Filters
        </button>

        {/* Advanced Filters */}
        {showFilters && (
          <div className="mt-4 p-4 bg-gray-50 rounded-lg border">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              {/* Karma Range */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Min Karma
                </label>
                <input
                  type="number"
                  value={filters.minKarma}
                  onChange={(e) => handleFilterChange('minKarma', parseInt(e.target.value) || 0)}
                  className="w-full px-3 py-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Max Karma
                </label>
                <input
                  type="number"
                  value={filters.maxKarma}
                  onChange={(e) => handleFilterChange('maxKarma', parseInt(e.target.value) || 1000000)}
                  className="w-full px-3 py-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500"
                />
              </div>

              {/* User Count Range */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Min Users
                </label>
                <input
                  type="number"
                  value={filters.minUsers}
                  onChange={(e) => handleFilterChange('minUsers', parseInt(e.target.value) || 0)}
                  className="w-full px-3 py-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Max Users
                </label>
                <input
                  type="number"
                  value={filters.maxUsers}
                  onChange={(e) => handleFilterChange('maxUsers', parseInt(e.target.value) || 10000)}
                  className="w-full px-3 py-2 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>

            {/* Karma Level Filter */}
            <div className="mt-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Karma Level
              </label>
              <div className="flex flex-wrap gap-2">
                {karmaLevels.map(level => (
                  <button
                    key={level.value}
                    onClick={() => handleFilterChange('karmaLevel', level.value)}
                    className={`px-3 py-1 rounded-full text-sm border transition-colors ${
                      filters.karmaLevel === level.value
                        ? 'bg-blue-500 text-white border-blue-500'
                        : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'
                    }`}
                  >
                    <span className="mr-1">{level.icon}</span>
                    {level.label}
                  </button>
                ))}
              </div>
            </div>

            {/* Clear Filters */}
            <div className="mt-4">
              <button
                onClick={clearFilters}
                className="px-4 py-2 text-gray-600 hover:text-gray-800 text-sm"
              >
                Clear All Filters
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Search Results */}
      {searchResults.length > 0 && (
        <div className="bg-white rounded-lg shadow-md p-6">
          <h3 className="text-lg font-semibold text-gray-800 mb-4">
            Search Results ({searchResults.length})
          </h3>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {searchResults.map((result) => (
              <KarmaVisualization
                key={result.oappId}
                oappId={result.oappId}
                oappName={result.oappName}
                className="h-full"
              />
            ))}
          </div>
        </div>
      )}

      {/* No Results */}
      {searchResults.length === 0 && !loading && searchTerm && (
        <div className="bg-white rounded-lg shadow-md p-6 text-center">
          <div className="text-gray-500">
            <div className="text-4xl mb-2">🔍</div>
            <div className="text-lg font-medium mb-1">No OAPPs found</div>
            <div className="text-sm">Try adjusting your search terms or filters</div>
          </div>
        </div>
      )}

      {/* Quick Actions */}
      <div className="mt-6 bg-white rounded-lg shadow-md p-6">
        <h3 className="text-lg font-semibold text-gray-800 mb-4">Quick Actions</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <button
            onClick={() => {
              setSearchTerm('high karma');
              setFilters(prev => ({ ...prev, minKarma: 1000, karmaLevel: 'high' }));
            }}
            className="p-4 bg-green-50 border border-green-200 rounded-lg hover:bg-green-100 transition-colors text-left"
          >
            <div className="text-green-600 font-medium">🟢 High Karma OAPPs</div>
            <div className="text-sm text-green-600">Find OAPPs with 1K+ karma</div>
          </button>
          
          <button
            onClick={() => {
              setSearchTerm('popular');
              setFilters(prev => ({ ...prev, minUsers: 10 }));
            }}
            className="p-4 bg-blue-50 border border-blue-200 rounded-lg hover:bg-blue-100 transition-colors text-left"
          >
            <div className="text-blue-600 font-medium">👥 Popular OAPPs</div>
            <div className="text-sm text-blue-600">Find OAPPs with 10+ users</div>
          </button>
          
          <button
            onClick={() => {
              setSearchTerm('legendary');
              setFilters(prev => ({ ...prev, karmaLevel: 'legendary' }));
            }}
            className="p-4 bg-purple-50 border border-purple-200 rounded-lg hover:bg-purple-100 transition-colors text-left"
          >
            <div className="text-purple-600 font-medium">🟣 Legendary OAPPs</div>
            <div className="text-sm text-purple-600">Find OAPPs with 100K+ karma</div>
          </button>
        </div>
      </div>
    </div>
  );
};

export default KarmaSearch;
