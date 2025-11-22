'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    router.push('/wallet');
  }, [router]);

  return (
    <div className="flex items-center justify-center min-h-screen bg-zypherpunk-bg">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-zypherpunk-primary"></div>
    </div>
  );
}

