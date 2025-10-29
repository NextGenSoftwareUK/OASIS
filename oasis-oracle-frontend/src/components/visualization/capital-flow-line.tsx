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
  
  console.log("📏 CapitalFlowLine render:", { from, to, amount, isActive, showFlows });
  
  // Animated dashed line (flows)
  useFrame((state) => {
    if (lineRef.current && isActive && showFlows) {
      // Animate dash offset to create flowing effect
      if (lineRef.current.material) {
        lineRef.current.material.dashOffset -= 0.02;
      }
    }
  });
  
  if (!showFlows) {
    console.log("📏 CapitalFlowLine hidden (showFlows=false)");
    return null;
  }
  
  // Line thickness based on capital amount (increased for visibility)
  const thickness = Math.max(2, Math.log10(amount / 10_000_000) * 2);
  
  return (
    <Line
      ref={lineRef}
      points={[from, to]}
      color={isActive ? "#22d3ee" : "#64748b"}
      lineWidth={thickness}
      dashed={isActive}
      dashScale={30}
      dashSize={3}
      gapSize={2}
      transparent
      opacity={isActive ? 0.8 : 0.3}
    />
  );
}

