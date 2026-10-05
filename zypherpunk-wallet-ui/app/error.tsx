'use client';

import { useEffect } from 'react';

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error('Application error:', error);
  }, [error]);

  return (
    <div className="flex items-center justify-center min-h-screen bg-black text-white">
      <div className="text-center">
        <h2 className="text-2xl font-bold mb-4">Something went wrong!</h2>
        <p className="text-gray-400 mb-4">{error.message}</p>
        <button
          onClick={reset}
          className="px-4 py-2 bg-white text-black rounded-lg hover:bg-gray-200"
        >
          Try again
        </button>
      </div>
    </div>
  );
}

