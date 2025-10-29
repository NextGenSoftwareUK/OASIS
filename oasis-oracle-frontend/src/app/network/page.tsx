"use client";

import { useState } from "react";
import { OracleLayout } from "@/components/layout/oracle-layout";
import { Blockchain3DScene } from "@/components/visualization/blockchain-3d-scene";
import { ChainDetailsOverlay } from "@/components/visualization/chain-details-overlay";
import { StatsOverlay } from "@/components/visualization/stats-overlay";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { formatCurrency } from "@/lib/utils";
import { Eye, EyeOff, Play, Pause, Maximize2 } from "lucide-react";
import { blockchain3DNodes, capitalFlows3D } from "@/lib/visualization-data";
import type { ChainNode3D } from "@/lib/visualization-data";

export default function NetworkPage() {
  const [selectedChain, setSelectedChain] = useState<ChainNode3D | null>(null);
  const [showLabels, setShowLabels] = useState(true);
  const [showFlows, setShowFlows] = useState(true);
  const [autoRotate, setAutoRotate] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  
  // Calculate stats
  const totalTVL = blockchain3DNodes.reduce((sum, node) => sum + node.tvl, 0);
  const activeChains = blockchain3DNodes.filter(n => n.health === "healthy").length;
  const activeFlows = capitalFlows3D.filter(f => f.isActive).length;
  
  const handleNodeClick = (node: ChainNode3D) => {
    setSelectedChain(node);
  };
  
  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen();
      setIsFullscreen(true);
    } else {
      document.exitFullscreen();
      setIsFullscreen(false);
    }
  };
  
  return (
    <OracleLayout>
      <div className="space-y-6">
        {/* Hero Section */}
        <div className="flex items-start justify-between">
          <div className="space-y-3">
            <h1 className="text-4xl font-bold tracking-tight text-[var(--color-foreground)]">
              Oracle Network Visualization
            </h1>
            <p className="text-lg text-[var(--muted)] max-w-3xl">
              Real-time 3D visualization of the OASIS oracle network. Watch capital flow across 
              20+ blockchains with live transaction monitoring and multi-oracle consensus.
            </p>
          </div>
          
          {/* View Controls */}
          <div className="flex gap-2">
            <Button
              variant={showLabels ? "primary" : "secondary"}
              onClick={() => setShowLabels(!showLabels)}
              className="flex items-center gap-2 text-sm"
            >
              {showLabels ? <Eye className="h-4 w-4" /> : <EyeOff className="h-4 w-4" />}
              Labels
            </Button>
            <Button
              variant={showFlows ? "primary" : "secondary"}
              onClick={() => setShowFlows(!showFlows)}
              className="text-sm"
            >
              Flows
            </Button>
            <Button
              variant={autoRotate ? "primary" : "secondary"}
              onClick={() => setAutoRotate(!autoRotate)}
              className="flex items-center gap-2 text-sm"
            >
              {autoRotate ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
              Rotate
            </Button>
            <Button
              variant="secondary"
              onClick={toggleFullscreen}
              className="text-sm"
            >
              <Maximize2 className="h-4 w-4" />
            </Button>
          </div>
        </div>
        
        {/* 3D Visualization */}
        <div className="relative h-[700px] w-full rounded-2xl overflow-hidden border border-[var(--color-card-border)]/50 shadow-[0_20px_50px_rgba(13,148,136,0.25)]">
          <Blockchain3DScene
            showLabels={showLabels}
            showFlows={showFlows}
            autoRotate={autoRotate}
            onNodeClick={handleNodeClick}
          />
          
          {/* Stats Overlay */}
          <StatsOverlay
            totalTVL={totalTVL}
            activeChains={activeChains}
            totalChains={blockchain3DNodes.length}
            activeFlows={activeFlows}
            consensus={98.5}
          />
          
          {/* Chain Details Overlay */}
          <ChainDetailsOverlay
            chain={selectedChain}
            onClose={() => setSelectedChain(null)}
          />
        </div>
        
        {/* Visualization Guide */}
        <Card title="Visualization Guide" description="How to interact with the 3D network" variant="glass">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <GuideItem
              icon="●"
              color="text-[var(--accent)]"
              label="Blockchain Node"
              description="Size represents TVL. Click to view details. Glow indicates health status."
            />
            <GuideItem
              icon="━━━"
              color="text-[var(--accent)]"
              label="Oracle Connection"
              description="Lines connect blockchains in the oracle network. Thickness shows connection strength."
            />
            <GuideItem
              icon="→→→"
              color="text-[var(--accent)]"
              label="Capital Flow"
              description="Animated particles show real-time capital movements between chains."
            />
            <GuideItem
              icon="🖱️"
              color="text-[var(--muted)]"
              label="Drag to Rotate"
              description="Click and drag to rotate the view. Scroll to zoom in/out."
            />
            <GuideItem
              icon="☀️"
              color="text-[var(--accent)]"
              label="Oracle Core"
              description="Central hub represents the OASIS oracle system coordinating all chains."
            />
            <GuideItem
              icon="⭐"
              color="text-[var(--muted)]"
              label="Starfield"
              description="Background stars create depth. The network operates in a decentralized universe."
            />
          </div>
        </Card>
        
        {/* Impact Statement */}
        <div className="rounded-2xl border border-[var(--accent)]/30 bg-[rgba(34,211,238,0.05)] p-8 text-center">
          <h2 className="text-2xl font-bold text-[var(--color-foreground)] mb-3">
            Real-Time Cross-Chain Monitoring
          </h2>
          <p className="text-lg text-[var(--muted)] max-w-3xl mx-auto">
            This 3D visualization represents <span className="text-[var(--accent)] font-semibold">{formatCurrency(totalTVL)}</span> in 
            real-time capital across <span className="text-[var(--accent)] font-semibold">{blockchain3DNodes.length} blockchains</span>. 
            Every sphere, line, and particle is backed by multi-oracle verification, solving the 
            <span className="text-[var(--accent)] font-semibold"> $100-150 billion "who owns what, when" problem</span>.
          </p>
        </div>
      </div>
    </OracleLayout>
  );
}

function GuideItem({
  icon,
  color,
  label,
  description,
}: {
  icon: string;
  color: string;
  label: string;
  description: string;
}) {
  return (
    <div className="flex gap-3">
      <div className={`text-2xl ${color} mt-1`}>{icon}</div>
      <div>
        <h4 className="font-semibold text-[var(--color-foreground)] mb-1">{label}</h4>
        <p className="text-sm text-[var(--muted)] leading-relaxed">{description}</p>
      </div>
    </div>
  );
}

