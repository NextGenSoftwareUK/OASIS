import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { OASISClient } from '@oasis/api-client';

export interface AvatarSSOProps {
  providers?: string[];
  onSuccess?: (avatar: any) => void;
  onError?: (error: Error) => void;
  theme?: 'light' | 'dark';
  position?: 'modal' | 'inline';
  customStyles?: React.CSSProperties;
}

export const AvatarSSO: React.FC<AvatarSSOProps> = ({
  providers = ['holochain', 'ethereum', 'solana', 'polygon'],
  onSuccess,
  onError,
  theme = 'dark',
  position = 'modal',
  customStyles = {}
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState<string | null>(null);

  const handleProviderLogin = async (provider: string) => {
    setLoading(provider);
    try {
      const client = new OASISClient();
      const result = await client.authenticateWithProvider(provider);
      onSuccess?.(result);
      setIsOpen(false);
    } catch (error) {
      onError?.(error as Error);
    } finally {
      setLoading(null);
    }
  };

  return (
    <div className={`oasis-sso oasis-sso--${theme}`} style={customStyles}>
      <button 
        className="oasis-sso__trigger"
        onClick={() => setIsOpen(true)}
      >
        Sign In with OASIS
      </button>

      {isOpen && (
        <motion.div
          className="oasis-sso__modal"
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.9 }}
        >
          <div className="oasis-sso__header">
            <h2>Sign In to OASIS</h2>
            <button onClick={() => setIsOpen(false)}>×</button>
          </div>

          <div className="oasis-sso__providers">
            {providers.map(provider => (
              <button
                key={provider}
                className="oasis-sso__provider-btn"
                onClick={() => handleProviderLogin(provider)}
                disabled={loading === provider}
              >
                {loading === provider ? 'Connecting...' : `Connect with ${provider}`}
              </button>
            ))}
          </div>
        </motion.div>
      )}
    </div>
  );
};

export default AvatarSSO;

