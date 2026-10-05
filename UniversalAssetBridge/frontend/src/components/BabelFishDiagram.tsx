"use client";

import React, { useState } from 'react';

interface DiagramProps {
  isActive?: boolean;
  transactionStatus?: 'idle' | 'processing' | 'complete';
}

export default function BabelFishDiagram({ isActive = false, transactionStatus = 'idle' }: DiagramProps) {
  const [hoveredPart, setHoveredPart] = useState<string | null>(null);

  const parts = {
    telepathicExcretor: {
      name: "Telepathic Excretor",
      description: "User Intent Detection - Understands what you want to swap",
      color: "#FFD700",
      status: transactionStatus !== 'idle'
    },
    energyFilter: {
      name: "Energy Absorption Filter",
      description: "Gas Fee Optimizer - Finds the cheapest route across chains",
      color: "#FFD700",
      status: transactionStatus !== 'idle'
    },
    consciousSensors: {
      name: "Conscious Frequency Sensors",
      description: "Real-time Price Monitoring - Watches exchange rates constantly",
      color: "#FF4444",
      status: transactionStatus === 'processing' || transactionStatus === 'complete'
    },
    unconsciousSensors: {
      name: "Unconscious Frequency Sensors",
      description: "Background Sync System - Keeps all chains synchronized",
      color: "#FF4444",
      status: transactionStatus === 'complete'
    },
    digestiveChord: {
      name: "Digestive Nerve Chord",
      description: "Transaction Pipeline - Processes your swap through the system",
      color: "#00FFFF",
      status: transactionStatus === 'processing'
    },
    brain: {
      name: "Quantum Brain",
      description: "Smart Router - Decides optimal path for your transaction",
      color: "#FFFFFF",
      status: transactionStatus !== 'idle'
    },
    gasBladder: {
      name: "Gas Bladder",
      description: "Fee Reserve - Manages transaction costs efficiently",
      color: "#00FFFF",
      status: transactionStatus !== 'idle'
    },
    heart: {
      name: "Atomic Heart",
      description: "Swap Executor - Ensures all-or-nothing transactions",
      color: "#FFFFFF",
      status: transactionStatus === 'processing'
    }
  };

  return (
    <div className="relative w-full max-w-4xl mx-auto p-8">
      {/* Title */}
      <h2 className="text-4xl font-bold mb-2" style={{ color: '#00FFFF' }}>
        BABEL FISH PROTOCOL
      </h2>
      <p className="text-sm text-gray-400 mb-8">
        Universal Translation for Blockchain Languages
      </p>

      {/* SVG Diagram */}
      <svg
        viewBox="0 0 800 400"
        className="w-full h-auto"
        style={{ background: '#000000' }}
      >
        {/* Fish Outline */}
        <path
          d="M 50 200 Q 100 150, 200 160 L 350 160 Q 450 160, 500 180 L 600 180 Q 650 185, 680 200 L 750 200 L 780 220 L 780 180 L 750 200 L 680 200 Q 650 215, 600 220 L 500 220 Q 450 240, 350 240 L 200 240 Q 100 250, 50 200 Z"
          fill="none"
          stroke="#FFD700"
          strokeWidth="3"
          className={isActive ? 'animate-pulse' : ''}
        />

        {/* Telepathic Excretor (top front) */}
        <g 
          onMouseEnter={() => setHoveredPart('telepathicExcretor')}
          onMouseLeave={() => setHoveredPart(null)}
          className="cursor-pointer"
        >
          <ellipse cx="180" cy="175" rx="25" ry="20" fill={parts.telepathicExcretor.color} opacity="0.7">
            {parts.telepathicExcretor.status && (
              <animate attributeName="opacity" values="0.5;1;0.5" dur="2s" repeatCount="indefinite" />
            )}
          </ellipse>
          <line x1="180" y1="155" x2="180" y2="120" stroke="#00FF66" strokeWidth="1" />
          <text x="185" y="115" fill="#00FF66" fontSize="11">Telepathic Excretor</text>
        </g>

        {/* Energy Absorption Filter (top back) */}
        <g 
          onMouseEnter={() => setHoveredPart('energyFilter')}
          onMouseLeave={() => setHoveredPart(null)}
          className="cursor-pointer"
        >
          <path d="M 400 165 L 420 170 L 410 175 L 430 180 L 420 185 L 440 190 L 430 195 L 450 200 L 400 200 Z" 
                fill={parts.energyFilter.color} opacity="0.6">
            {parts.energyFilter.status && (
              <animate attributeName="opacity" values="0.4;0.8;0.4" dur="1.5s" repeatCount="indefinite" />
            )}
          </path>
          <line x1="425" y1="165" x2="425" y2="130" stroke="#00FF66" strokeWidth="1" />
          <text x="320" y="125" fill="#00FF66" fontSize="11">Energy Absorption Filter</text>
        </g>

        {/* Brain */}
        <g 
          onMouseEnter={() => setHoveredPart('brain')}
          onMouseLeave={() => setHoveredPart(null)}
          className="cursor-pointer"
        >
          <ellipse cx="250" cy="190" rx="35" ry="25" fill={parts.brain.color} opacity="0.4">
            {parts.brain.status && (
              <animate attributeName="opacity" values="0.3;0.6;0.3" dur="1s" repeatCount="indefinite" />
            )}
          </ellipse>
          <path d="M 230 185 Q 240 180, 250 185 Q 260 180, 270 185" stroke="#000" strokeWidth="1" fill="none" />
          <line x1="250" y1="165" x2="250" y2="145" stroke="#00FF66" strokeWidth="1" />
          <text x="220" y="140" fill="#00FF66" fontSize="11">Quantum Brain</text>
        </g>

        {/* Digestive Nerve Chord */}
        <g 
          onMouseEnter={() => setHoveredPart('digestiveChord')}
          onMouseLeave={() => setHoveredPart(null)}
          className="cursor-pointer"
        >
          <path d="M 200 170 Q 300 168, 400 170 Q 500 172, 580 175" 
                stroke={parts.digestiveChord.color} 
                strokeWidth="4" 
                fill="none"
                opacity="0.6">
            {parts.digestiveChord.status && (
              <animate attributeName="stroke-dasharray" values="0,1000;1000,0" dur="3s" repeatCount="indefinite" />
            )}
          </path>
          <line x1="390" y1="170" x2="390" y2="140" stroke="#00FF66" strokeWidth="1" />
          <text x="315" y="135" fill="#00FF66" fontSize="11">Digestive Nerve Chord</text>
        </g>

        {/* Gas Bladder */}
        <g 
          onMouseEnter={() => setHoveredPart('gasBladder')}
          onMouseLeave={() => setHoveredPart(null)}
          className="cursor-pointer"
        >
          <ellipse cx="480" cy="195" rx="60" ry="25" fill={parts.gasBladder.color} opacity="0.3">
            {parts.gasBladder.status && (
              <animate attributeName="ry" values="25;28;25" dur="2s" repeatCount="indefinite" />
            )}
          </ellipse>
          <line x1="520" y1="195" x2="560" y2="195" stroke="#00FF66" strokeWidth="1" />
          <text x="565" y="198" fill="#00FF66" fontSize="11">Gas Bladder</text>
        </g>

        {/* Heart */}
        <g 
          onMouseEnter={() => setHoveredPart('heart')}
          onMouseLeave={() => setHoveredPart(null)}
          className="cursor-pointer"
        >
          <path d="M 330 210 L 340 220 L 350 210 L 350 220 L 340 230 L 330 220 Z" 
                fill={parts.heart.color} 
                opacity="0.5">
            {parts.heart.status && (
              <animate attributeName="opacity" values="0.3;0.8;0.3" dur="0.8s" repeatCount="indefinite" />
            )}
          </path>
          <line x1="340" y1="230" x2="340" y2="260" stroke="#00FF66" strokeWidth="1" />
          <text x="305" y="275" fill="#00FF66" fontSize="11">Atomic Heart</text>
        </g>

        {/* Conscious Frequency Sensors */}
        <g 
          onMouseEnter={() => setHoveredPart('consciousSensors')}
          onMouseLeave={() => setHoveredPart(null)}
          className="cursor-pointer"
        >
          {[0, 1, 2, 3, 4].map((i) => (
            <rect 
              key={`conscious-${i}`}
              x={380 + i * 15} 
              y="210" 
              width="8" 
              height="25" 
              fill={parts.consciousSensors.color} 
              opacity="0.6"
            >
              {parts.consciousSensors.status && (
                <animate 
                  attributeName="height" 
                  values="25;30;25" 
                  dur={`${1 + i * 0.2}s`}
                  repeatCount="indefinite" 
                />
              )}
            </rect>
          ))}
          <line x1="420" y1="235" x2="420" y2="265" stroke="#00FF66" strokeWidth="1" />
          <text x="320" y="280" fill="#00FF66" fontSize="11">Conscious Frequency Sensors</text>
        </g>

        {/* Unconscious Frequency Sensors (tail) */}
        <g 
          onMouseEnter={() => setHoveredPart('unconsciousSensors')}
          onMouseLeave={() => setHoveredPart(null)}
          className="cursor-pointer"
        >
          {[0, 1, 2, 3, 4, 5, 6, 7, 8].map((i) => {
            const angle = (i - 4) * 12;
            const length = 30 + Math.abs(i - 4) * 3;
            return (
              <line
                key={`unconscious-${i}`}
                x1="680"
                y1="200"
                x2={680 + length * Math.cos(angle * Math.PI / 180)}
                y2={200 + length * Math.sin(angle * Math.PI / 180)}
                stroke={parts.unconsciousSensors.color}
                strokeWidth="2"
                opacity="0.7"
              >
                {parts.unconsciousSensors.status && (
                  <animate 
                    attributeName="opacity" 
                    values="0.3;0.9;0.3" 
                    dur={`${1.5 + i * 0.1}s`}
                    repeatCount="indefinite" 
                  />
                )}
              </line>
            );
          })}
          <line x1="710" y1="230" x2="730" y2="250" stroke="#00FF66" strokeWidth="1" />
          <text x="580" y="290" fill="#00FF66" fontSize="11">Unconscious Frequency Sensors</text>
        </g>

        {/* Eye */}
        <g>
          <circle cx="210" cy="190" r="15" fill="#FFD700" opacity="0.8" />
          <circle cx="210" cy="190" r="8" fill="#FFFFFF" opacity="0.9" />
          <circle cx="212" cy="188" r="4" fill="#000000" />
        </g>

        {/* Mouth/Sensor */}
        <g>
          <path d="M 80 200 Q 100 195, 120 200 Q 100 205, 80 200" 
                stroke="#00FFFF" 
                strokeWidth="2" 
                fill="none" />
          <line x1="100" y1="200" x2="60" y2="220" stroke="#00FF66" strokeWidth="1" />
          <text x="10" y="230" fill="#00FF66" fontSize="11">Signal Sensor</text>
        </g>

        {/* Version number */}
        <text x="700" y="380" fill="#00FF66" fontSize="12" opacity="0.5">
          v4.2.0
        </text>
      </svg>

      {/* Info Panel */}
      {hoveredPart && (
        <div className="mt-6 p-4 rounded-lg border" style={{ 
          background: 'rgba(0, 255, 102, 0.1)', 
          borderColor: '#00FF66' 
        }}>
          <h3 className="text-xl font-bold mb-2" style={{ color: '#00FF66' }}>
            {parts[hoveredPart as keyof typeof parts].name}
          </h3>
          <p className="text-gray-300">
            {parts[hoveredPart as keyof typeof parts].description}
          </p>
        </div>
      )}

      {/* Status Indicator */}
      {transactionStatus !== 'idle' && (
        <div className="mt-6 p-4 rounded-lg bg-gray-900 border border-cyan-500">
          <h3 className="text-lg font-bold mb-3 text-cyan-400">
            Translation Status
          </h3>
          <div className="space-y-2">
            {Object.entries(parts).map(([key, part]) => (
              <div key={key} className="flex items-center gap-3">
                <div className={`w-2 h-2 rounded-full ${
                  part.status ? 'bg-green-400 animate-pulse' : 'bg-gray-600'
                }`} />
                <span className={part.status ? 'text-green-400' : 'text-gray-500'}>
                  {part.name}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Legend */}
      <div className="mt-6 grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded" style={{ background: '#FFD700' }} />
          <span className="text-gray-300">Processing</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded" style={{ background: '#00FFFF' }} />
          <span className="text-gray-300">Data Flow</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded" style={{ background: '#FF4444' }} />
          <span className="text-gray-300">Sensors</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded" style={{ background: '#00FF66' }} />
          <span className="text-gray-300">Active</span>
        </div>
      </div>
    </div>
  );
}

