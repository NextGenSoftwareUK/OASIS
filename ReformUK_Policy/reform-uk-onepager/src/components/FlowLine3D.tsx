import { useMemo } from "react";
import { Line } from "@react-three/drei";
import * as THREE from "three";

type FlowLineProps = {
  from: [number, number, number];
  to: [number, number, number];
  amount: number;
  isActive: boolean;
  isWasteful?: boolean;
};

export function FlowLine({ from, to, isActive, isWasteful = false }: FlowLineProps) {
  // Create curved path
  const points = useMemo(() => {
    const curve = new THREE.CatmullRomCurve3([
      new THREE.Vector3(...from),
      new THREE.Vector3(
        (from[0] + to[0]) / 2,
        (from[1] + to[1]) / 2 + 2, // Arc height
        (from[2] + to[2]) / 2
      ),
      new THREE.Vector3(...to)
    ]);
    
    return curve.getPoints(50);
  }, [from, to]);
  
  // Color: red for wasteful, green for efficient
  const color = isWasteful ? "#ef4444" : "#22c55e";
  
  if (!isActive) return null;
  
  return (
    <Line
      points={points}
      color={color}
      lineWidth={1}
      transparent
      opacity={0.5}
    />
  );
}
