"use client";

import React from 'react';

interface MostlyHarmlessRatingProps {
  rating?: 1 | 2 | 3 | 4 | 5;
  showDescription?: boolean;
}

export default function MostlyHarmlessRating({ 
  rating = 5, 
  showDescription = true 
}: MostlyHarmlessRatingProps) {
  
  const descriptions = {
    5: "Perfectly safe. The Guide recommends this swap without reservation.",
    4: "Very safe. Minor risks, but mostly harmless.",
    3: "Generally safe. Standard blockchain risks apply.",
    2: "Some risk. Proceed with caution.",
    1: "Risky. The Guide suggests reconsidering."
  };

  return (
    <div className="p-4 rounded-lg border" style={{
      background: 'rgba(0, 255, 102, 0.05)',
      borderColor: rating >= 4 ? 'rgba(0, 255, 102, 0.3)' : 'rgba(255, 215, 0, 0.3)'
    }}>
      <div className="flex items-center justify-between mb-2">
        <span className="text-sm font-semibold text-gray-400">Safety Rating:</span>
        <div className="flex items-center gap-1">
          {[1, 2, 3, 4, 5].map((star) => (
            <span 
              key={star}
              className="text-lg"
              style={{ color: star <= rating ? '#FFD700' : '#333' }}
            >
              ★
            </span>
          ))}
        </div>
      </div>
      
      <div className="flex items-center gap-2 mb-2">
        <span className="text-lg font-bold" style={{ color: '#00FF66' }}>
          Mostly Harmless
        </span>
        {rating === 5 && (
          <span className="text-xs px-2 py-1 rounded" style={{ 
            background: 'rgba(0, 255, 102, 0.2)',
            color: '#00FF66'
          }}>
            VERIFIED
          </span>
        )}
      </div>

      {showDescription && (
        <p className="text-sm text-gray-400 leading-relaxed">
          {descriptions[rating]}
        </p>
      )}

      <div className="mt-3 pt-3 border-t border-gray-700/50">
        <div className="grid grid-cols-2 gap-2 text-xs">
          <div className="flex items-center gap-2">
            <span className="text-green-400">✓</span>
            <span className="text-gray-500">No bridges</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-green-400">✓</span>
            <span className="text-gray-500">Atomic swap</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-green-400">✓</span>
            <span className="text-gray-500">Auto-rollback</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-green-400">✓</span>
            <span className="text-gray-500">Verified</span>
          </div>
        </div>
      </div>
    </div>
  );
}

