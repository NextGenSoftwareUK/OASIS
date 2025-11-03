"use client";

import { useState, useEffect } from "react";

export default function TestAnimationPage() {
  const [rotation, setRotation] = useState(0);
  const [count, setCount] = useState(0);
  
  // Simple React animation
  useEffect(() => {
    const interval = setInterval(() => {
      setRotation(prev => (prev + 2) % 360);
      setCount(prev => prev + 1);
    }, 16); // ~60fps
    
    return () => clearInterval(interval);
  }, []);
  
  return (
    <div className="w-full h-screen bg-black text-white flex flex-col items-center justify-center">
      <h1 className="text-4xl font-bold mb-8">Animation Test</h1>
      
      {/* CSS Animation Test */}
      <div className="mb-12">
        <h2 className="text-xl mb-4 text-cyan-400">Test 1: CSS Animation</h2>
        <div className="w-24 h-24 bg-red-500 animate-spin"></div>
        <p className="mt-2 text-sm">Should be spinning (CSS)</p>
      </div>
      
      {/* React State Animation Test */}
      <div className="mb-12">
        <h2 className="text-xl mb-4 text-cyan-400">Test 2: React Animation</h2>
        <div 
          className="w-24 h-24 bg-blue-500"
          style={{ transform: `rotate(${rotation}deg)` }}
        ></div>
        <p className="mt-2 text-sm">Should be spinning (React state)</p>
        <p className="text-xs text-gray-400">Rotation: {rotation.toFixed(0)}° | Updates: {count}</p>
      </div>
      
      {/* SVG Animation Test */}
      <div>
        <h2 className="text-xl mb-4 text-cyan-400">Test 3: SVG Animation</h2>
        <svg width="100" height="100">
          <circle cx="50" cy="50" r="40" fill="none" stroke="#22d3ee" strokeWidth="4">
            <animate attributeName="r" values="30;45;30" dur="2s" repeatCount="indefinite" />
          </circle>
        </svg>
        <p className="mt-2 text-sm">Should be pulsing (SVG)</p>
      </div>
      
      <div className="mt-12 p-4 border border-cyan-400/30 rounded-lg max-w-lg">
        <h3 className="text-lg font-bold mb-2 text-cyan-400">Diagnosis:</h3>
        <ul className="text-sm space-y-1">
          <li>✅ All 3 spinning = Animations work</li>
          <li>❌ None spinning = Browser issue</li>
          <li>⚠️ Mixed results = Specific tech broken</li>
        </ul>
      </div>
    </div>
  );
}




