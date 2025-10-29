"use client";

import { useRef } from "react";
import { useFrame } from "@react-three/fiber";
import { Line } from "@react-three/drei";
import * as THREE from "three";

type CapitalFlowLineProps = {
  from: [number, number, number];
  to: [number, number, number];
  amount: number;
  isActive: boolean;
  showFlows: boolean;
};

export function CapitalFlowLine({ from, to, amount, isActive, showFlows }: CapitalFlowLineProps) {
  const lineRef = useRef<any>(null);
  
  // Animated dashed line (flows)
  useFrame((state) => {
    if (lineRef.current && isActive && showFlows) {
      // Animate dash offset to create flowing effect
      if (lineRef.current.material) {
        lineRef.current.material.dashOffset -= 0.02;
      }
    }
  });
  
  if (!showFlows) return null;
  
  // Line thickness based on capital amount
  const thickness = Math.log10(amount / 10_000_000);
  
  return (
    <Line
      ref={lineRef}
      points={[from, to]}
      color={isActive ? "#22d3ee" : "#64748b"}
      lineWidth={thickness}
      dashed={isActive}
      dashScale={50}
      dashSize={2}
      gapSize={1}
      transparent
      opacity={isActive ? 0.6 : 0.2}
    />
  );
}

