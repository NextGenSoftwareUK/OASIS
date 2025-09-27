import React, { useState, useEffect } from 'react';
import { OAPPKarmaData, AvatarKarmaData } from '../types/star';
import { starService } from '../services/starService';
import { toast } from 'react-hot-toast';

interface KarmaVisualizationProps {
  oappId: string;
  oappName?: string;
  className?: string;
}

const KarmaVisualization: React.FC<KarmaVisualizationProps> = ({ 
  oappId, 
  oappName = 'Unknown OAPP',
  className = '' 
}) => {
  const [karmaData, setKarmaData] = useState<OAPPKarmaData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showDetails, setShowDetails] = useState(false);

  useEffect(() => {
    loadKarmaData();
  }, [oappId]);

  const loadKarmaData = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await starService.getOAPPKarmaData(oappId);
      setKarmaData(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load karma data');
      toast.error('Failed to load karma data');
    } finally {
      setLoading(false);
    }
  };

  const getKarmaColor = (totalKarma: number): string => {
    if (totalKarma <= 0) return '#6B7280'; // Gray
    if (totalKarma < 100) return '#EF4444'; // Red
    if (totalKarma < 1000) return '#F59E0B'; // Yellow
    if (totalKarma < 10000) return '#10B981'; // Green
    if (totalKarma < 100000) return '#3B82F6'; // Blue
    return '#8B5CF6'; // Purple (Legendary)
  };

  const getKarmaGlowIntensity = (totalKarma: number): number => {
    if (totalKarma <= 0) return 0;
    return Math.min(Math.log10(totalKarma + 1) / 5, 1); // Normalize to 0-1
  };

  const formatKarma = (karma: number): string => {
    if (karma >= 1000000) return `${(karma / 1000000).toFixed(1)}M`;
    if (karma >= 1000) return `${(karma / 1000).toFixed(1)}K`;
    return karma.toFixed(0);
  };

  const getKarmaLevelIcon = (level: string): string => {
    switch (level.toLowerCase()) {
      case 'none': return '⚫';
      case 'low': return '🔴';
      case 'medium': return '🟡';
      case 'high': return '🟢';
      case 'very high': return '🔵';
      case 'legendary': return '🟣';
      default: return '⚫';
    }
  };

  if (loading) {
    return (
      <div className={`karma-visualization loading ${className}`}>
        <div className="animate-pulse">
          <div className="h-4 bg-gray-300 rounded w-3/4 mb-2"></div>
          <div className="h-3 bg-gray-300 rounded w-1/2"></div>
        </div>
      </div>
    );
  }

  if (error || !karmaData) {
    return (
      <div className={`karma-visualization error ${className}`}>
        <div className="text-red-500 text-sm">
          {error || 'Failed to load karma data'}
        </div>
      </div>
    );
  }

  const glowIntensity = getKarmaGlowIntensity(karmaData.totalKarma);
  const karmaColor = getKarmaColor(karmaData.totalKarma);

  return (
    <div className={`karma-visualization ${className}`}>
      {/* Main Karma Display */}
      <div 
        className="karma-card relative p-4 rounded-lg border-2 transition-all duration-300 hover:shadow-lg cursor-pointer"
        style={{
          borderColor: karmaColor,
          boxShadow: `0 0 ${20 + glowIntensity * 20}px ${karmaColor}40`,
          background: `linear-gradient(135deg, ${karmaColor}10, ${karmaColor}05)`
        }}
        onClick={() => setShowDetails(!showDetails)}
      >
        {/* Karma Level Badge */}
        <div className="absolute top-2 right-2">
          <span className="text-2xl" title={karmaData.karmaLevel}>
            {getKarmaLevelIcon(karmaData.karmaLevel)}
          </span>
        </div>

        {/* OAPP Name */}
        <h3 className="text-lg font-semibold text-gray-800 mb-2">
          {karmaData.oappName}
        </h3>

        {/* Karma Stats */}
        <div className="grid grid-cols-2 gap-4 mb-3">
          <div className="text-center">
            <div 
              className="text-2xl font-bold"
              style={{ color: karmaColor }}
            >
              {formatKarma(karmaData.totalKarma)}
            </div>
            <div className="text-xs text-gray-600">Total Karma</div>
          </div>
          <div className="text-center">
            <div className="text-2xl font-bold text-gray-800">
              {karmaData.userCount}
            </div>
            <div className="text-xs text-gray-600">Users</div>
          </div>
        </div>

        {/* Average Karma */}
        <div className="text-center mb-3">
          <div className="text-sm text-gray-600">
            Avg: {formatKarma(karmaData.averageKarma)} karma/user
          </div>
        </div>

        {/* Karma Level */}
        <div className="text-center">
          <span 
            className="inline-block px-3 py-1 rounded-full text-xs font-medium"
            style={{ 
              backgroundColor: `${karmaColor}20`,
              color: karmaColor,
              border: `1px solid ${karmaColor}40`
            }}
          >
            {karmaData.karmaLevel} Level
          </span>
        </div>

        {/* Expand/Collapse Indicator */}
        <div className="absolute bottom-2 right-2">
          <span className="text-gray-400 text-sm">
            {showDetails ? '▼' : '▶'}
          </span>
        </div>
      </div>

      {/* Detailed Information */}
      {showDetails && (
        <div className="karma-details mt-4 p-4 bg-gray-50 rounded-lg border">
          <h4 className="font-semibold text-gray-800 mb-3">Karma Details</h4>
          
          {/* Karma Sources */}
          {karmaData.karmaSources.length > 0 && (
            <div className="mb-3">
              <div className="text-sm font-medium text-gray-700 mb-1">Karma Sources:</div>
              <div className="flex flex-wrap gap-1">
                {karmaData.karmaSources.map((source, index) => (
                  <span 
                    key={index}
                    className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded"
                  >
                    {source}
                  </span>
                ))}
              </div>
            </div>
          )}

          {/* Registered Avatars */}
          <div className="mb-3">
            <div className="text-sm font-medium text-gray-700 mb-2">
              Registered Avatars ({karmaData.registeredAvatars.length}):
            </div>
            <div className="max-h-32 overflow-y-auto">
              {karmaData.registeredAvatars.slice(0, 5).map((avatar, index) => (
                <div key={index} className="flex justify-between items-center py-1 text-xs">
                  <span className="text-gray-600">{avatar.avatarName}</span>
                  <span 
                    className="font-medium"
                    style={{ color: karmaColor }}
                  >
                    {formatKarma(avatar.totalKarma)}
                  </span>
                </div>
              ))}
              {karmaData.registeredAvatars.length > 5 && (
                <div className="text-xs text-gray-500 text-center py-1">
                  ... and {karmaData.registeredAvatars.length - 5} more
                </div>
              )}
            </div>
          </div>

          {/* Refresh Button */}
          <button
            onClick={loadKarmaData}
            className="w-full mt-3 px-3 py-2 bg-blue-500 text-white text-sm rounded hover:bg-blue-600 transition-colors"
          >
            Refresh Karma Data
          </button>
        </div>
      )}
    </div>
  );
};

export default KarmaVisualization;
