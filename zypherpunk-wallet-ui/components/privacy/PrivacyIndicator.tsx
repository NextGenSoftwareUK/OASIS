'use client';

import React from 'react';
import { Shield, Lock, Eye, AlertTriangle } from 'lucide-react';
import { cn } from '@/lib/utils';

export type PrivacyLevel = 'low' | 'medium' | 'high' | 'maximum';
export type PrivacyType = 'shielded' | 'viewing-key' | 'private' | 'warning';

interface PrivacyIndicatorProps {
  level?: PrivacyLevel;
  type?: PrivacyType;
  size?: 'sm' | 'md' | 'lg';
  animated?: boolean;
  className?: string;
}

const privacyLevelColors = {
  low: 'text-privacy-low',
  medium: 'text-privacy-medium',
  high: 'text-privacy-high',
  maximum: 'text-privacy-maximum',
};

const privacyTypeIcons = {
  shielded: Shield,
  'viewing-key': Eye,
  private: Lock,
  warning: AlertTriangle,
};

const sizeClasses = {
  sm: 'w-3 h-3',
  md: 'w-4 h-4',
  lg: 'w-6 h-6',
};

export const PrivacyIndicator: React.FC<PrivacyIndicatorProps> = ({
  level = 'medium',
  type = 'shielded',
  size = 'md',
  animated = false,
  className,
}) => {
  const Icon = privacyTypeIcons[type];
  const colorClass = privacyLevelColors[level];
  const sizeClass = sizeClasses[size];

  return (
    <div className={cn('inline-flex items-center', className)}>
      <Icon
        className={cn(
          sizeClass,
          colorClass,
          animated && 'animate-shield-pulse'
        )}
      />
    </div>
  );
};

interface PrivacyBadgeProps {
  level: PrivacyLevel;
  text?: string;
  className?: string;
}

export const PrivacyBadge: React.FC<PrivacyBadgeProps> = ({
  level,
  text,
  className,
}) => {
  const levelText = text || level.charAt(0).toUpperCase() + level.slice(1);
  
  const badgeColors = {
    low: 'bg-privacy-low/20 text-privacy-low border-privacy-low/50',
    medium: 'bg-privacy-medium/20 text-privacy-medium border-privacy-medium/50',
    high: 'bg-privacy-high/20 text-privacy-high border-privacy-high/50',
    maximum: 'bg-privacy-maximum/20 text-privacy-maximum border-privacy-maximum/50',
  };

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium border',
        badgeColors[level],
        className
      )}
    >
      <PrivacyIndicator level={level} size="sm" animated />
      {levelText}
    </span>
  );
};

