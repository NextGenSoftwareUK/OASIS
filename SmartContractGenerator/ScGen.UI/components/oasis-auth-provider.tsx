'use client';

import { useEffect } from 'react';

export function OASISAuthProvider({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    // Initialize OASIS auth listener
    if (typeof window !== 'undefined') {
      import('../lib/oasis-auth').then(module => {
        module.initOASISAuth();
      });
    }
  }, []);

  return <>{children}</>;
}


