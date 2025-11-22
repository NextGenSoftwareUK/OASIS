'use client';

import React from 'react';
import { Shield, Lock, Key, CheckCircle2, AlertCircle, LockKeyhole, Fingerprint, EyeOff } from 'lucide-react';
import { Card } from '@/components/ui/card';
import { useAvatarStore } from '@/lib/avatarStore';

interface SecurityFeature {
  id: string;
  name: string;
  description: string;
  status: 'active' | 'inactive' | 'unknown';
  icon: React.ReactNode;
  details: string[];
}

export const SecurityFeaturesDisplay: React.FC = () => {
  const { token, avatar } = useAvatarStore();
  const isAuthenticated = !!token && !!avatar;

  const securityFeatures: SecurityFeature[] = [
    {
      id: 'jwt-auth',
      name: 'JWT Authentication',
      description: 'All wallet operations require valid JWT tokens',
      status: isAuthenticated ? 'active' : 'inactive',
      icon: <Fingerprint className="w-5 h-5" />,
      details: [
        'Token-based authentication',
        'Automatic token validation',
        'Zero clock skew (no grace period)',
        'Avatar context injection'
      ]
    },
    {
      id: 'aes-256',
      name: 'AES-256 Encryption',
      description: 'Private keys encrypted using Rijndael AES-256',
      status: 'active', // Always active on backend
      icon: <Lock className="w-5 h-5" />,
      details: [
        'Rijndael AES-256 encryption',
        'Configurable encryption key',
        'Encrypted storage in database',
        'Secret recovery phrase encryption'
      ]
    },
    {
      id: 'selective-decrypt',
      name: 'Selective Decryption',
      description: 'Private keys only decrypted when explicitly requested',
      status: 'active', // Always active on backend
      icon: <EyeOff className="w-5 h-5" />,
      details: [
        'Opt-in decryption only',
        'Default encrypted storage',
        'Granular control per request',
        'Security by default'
      ]
    },
    {
      id: 'secure-keys',
      name: 'Secure Key Generation',
      description: 'Cryptographically secure random key generation',
      status: 'active', // Always active on backend
      icon: <Key className="w-5 h-5" />,
      details: [
        'Cryptographically secure randomness',
        'Wallet Import Format (WIF)',
        'Provider-specific prefixes',
        'Secure public/private key pairs'
      ]
    },
    {
      id: 'authorization',
      name: 'Endpoint Authorization',
      description: 'All wallet endpoints protected with [Authorize]',
      status: isAuthenticated ? 'active' : 'inactive',
      icon: <Shield className="w-5 h-5" />,
      details: [
        'All endpoints require authorization',
        'Avatar-based access control',
        'Consistent security model',
        '401 Unauthorized for invalid tokens'
      ]
    },
    {
      id: 'avatar-context',
      name: 'Avatar Context Security',
      description: 'Authenticated avatar automatically attached to requests',
      status: isAuthenticated ? 'active' : 'inactive',
      icon: <LockKeyhole className="w-5 h-5" />,
      details: [
        'Automatic avatar injection',
        'Request-scoped context',
        'Type-safe avatar access',
        'User isolation'
      ]
    }
  ];

  const activeFeatures = securityFeatures.filter(f => f.status === 'active');
  const inactiveFeatures = securityFeatures.filter(f => f.status === 'inactive');

  return (
    <div className="space-y-6">
      {/* Summary Card */}
      <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-xl font-semibold text-zypherpunk-text mb-1 flex items-center space-x-2">
              <Shield className="w-6 h-6 text-zypherpunk-primary" />
              <span>Security Status</span>
            </h3>
            <p className="text-sm text-zypherpunk-text-muted">
              OASIS Wallet API Security Features
            </p>
          </div>
          <div className="text-right">
            <p className="text-3xl font-bold text-zypherpunk-primary">
              {activeFeatures.length}/{securityFeatures.length}
            </p>
            <p className="text-xs text-zypherpunk-text-muted">Active</p>
          </div>
        </div>
      </Card>

      {/* Active Features List */}
      <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
        <h3 className="text-lg font-semibold text-zypherpunk-text mb-4 flex items-center space-x-2">
          <CheckCircle2 className="w-5 h-5 text-zypherpunk-primary" />
          <span>Active Security Features ({activeFeatures.length})</span>
        </h3>
        <div className="space-y-3">
          {activeFeatures.map((feature) => (
            <div
              key={feature.id}
              className="flex items-start space-x-3 p-3 rounded-lg border border-zypherpunk-primary/30 bg-zypherpunk-primary/10"
            >
              <div className="text-zypherpunk-primary mt-0.5">
                {feature.icon}
              </div>
              <div className="flex-1">
                <div className="flex items-center space-x-2">
                  <h4 className="font-semibold text-zypherpunk-text">{feature.name}</h4>
                  <CheckCircle2 className="w-4 h-4 text-zypherpunk-primary" />
                </div>
                <p className="text-sm text-zypherpunk-text-muted mt-1">{feature.description}</p>
                <div className="mt-2 flex flex-wrap gap-2">
                  {feature.details.map((detail, idx) => (
                    <span
                      key={idx}
                      className="text-xs px-2 py-1 rounded bg-zypherpunk-bg border border-zypherpunk-primary/20 text-zypherpunk-text-muted"
                    >
                      {detail}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Inactive Features List (if any) */}
      {inactiveFeatures.length > 0 && (
        <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
          <h3 className="text-lg font-semibold text-zypherpunk-text mb-4 flex items-center space-x-2">
            <AlertCircle className="w-5 h-5 text-zypherpunk-text-muted" />
            <span>Requires Authentication ({inactiveFeatures.length})</span>
          </h3>
          <div className="space-y-3">
            {inactiveFeatures.map((feature) => (
              <div
                key={feature.id}
                className="flex items-start space-x-3 p-3 rounded-lg border border-zypherpunk-border bg-zypherpunk-bg/50"
              >
                <div className="text-zypherpunk-text-muted mt-0.5">
                  {feature.icon}
                </div>
                <div className="flex-1">
                  <div className="flex items-center space-x-2">
                    <h4 className="font-semibold text-zypherpunk-text-muted">{feature.name}</h4>
                    <AlertCircle className="w-4 h-4 text-zypherpunk-text-muted" />
                  </div>
                  <p className="text-sm text-zypherpunk-text-muted mt-1">{feature.description}</p>
                  <p className="text-xs text-zypherpunk-warning mt-2">
                    ⚠️ Sign in to activate this feature
                  </p>
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Security Architecture */}
      <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
        <h3 className="text-lg font-semibold text-zypherpunk-text mb-4">Security Architecture</h3>
        <div className="bg-zypherpunk-primary/10 border border-zypherpunk-primary/30 rounded-lg p-4 space-y-2">
          <div className="flex items-start space-x-2 text-sm text-zypherpunk-text-muted">
            <Shield className="w-4 h-4 text-zypherpunk-primary mt-0.5 flex-shrink-0" />
            <div>
              <strong className="text-zypherpunk-text">Defense in Depth</strong> - Multiple security layers provide additional protection
            </div>
          </div>
          <div className="flex items-start space-x-2 text-sm text-zypherpunk-text-muted">
            <Lock className="w-4 h-4 text-zypherpunk-primary mt-0.5 flex-shrink-0" />
            <div>
              <strong className="text-zypherpunk-text">Principle of Least Privilege</strong> - Keys only decrypted when needed
            </div>
          </div>
          <div className="flex items-start space-x-2 text-sm text-zypherpunk-text-muted">
            <Key className="w-4 h-4 text-zypherpunk-primary mt-0.5 flex-shrink-0" />
            <div>
              <strong className="text-zypherpunk-text">Secure by Default</strong> - Encryption enabled by default
            </div>
          </div>
          <div className="flex items-start space-x-2 text-sm text-zypherpunk-text-muted">
            <LockKeyhole className="w-4 h-4 text-zypherpunk-primary mt-0.5 flex-shrink-0" />
            <div>
              <strong className="text-zypherpunk-text">Future-Proof</strong> - Quantum encryption planned
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
};

