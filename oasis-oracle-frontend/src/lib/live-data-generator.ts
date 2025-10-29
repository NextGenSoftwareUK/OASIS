/**
 * Live data generator for realistic capital flow updates
 */

import { blockchain3DNodes, capitalFlows3D } from "./visualization-data";
import type { ChainNode3D, CapitalFlow3D } from "./visualization-data";

export class LiveDataGenerator {
  private updateInterval: NodeJS.Timeout | null = null;
  private listeners: Set<() => void> = new Set();
  
  // Store current state
  public nodes: ChainNode3D[] = [...blockchain3DNodes];
  public flows: CapitalFlow3D[] = [...capitalFlows3D];
  
  start(intervalMs: number = 3000) {
    if (this.updateInterval) return; // Already started
    
    console.log("🚀 Live data generator started (updates every", intervalMs, "ms)");
    
    this.updateInterval = setInterval(() => {
      this.generateUpdate();
      this.notifyListeners();
    }, intervalMs);
  }
  
  stop() {
    if (this.updateInterval) {
      clearInterval(this.updateInterval);
      this.updateInterval = null;
      console.log("⏸️ Live data generator stopped");
    }
  }
  
  subscribe(callback: () => void) {
    this.listeners.add(callback);
    return () => this.listeners.delete(callback); // Unsubscribe function
  }
  
  private notifyListeners() {
    this.listeners.forEach(callback => callback());
  }
  
  private generateUpdate() {
    console.log("📊 Generating live data update...");
    
    // Update TVL for each node (±5% fluctuation)
    this.nodes = this.nodes.map(node => ({
      ...node,
      tvl: node.tvl * (1 + (Math.random() - 0.5) * 0.1), // ±5%
      tps: node.tps * (1 + (Math.random() - 0.5) * 0.2), // ±10%
    }));
    
    // Update capital flows (±10% fluctuation)
    this.flows = this.flows.map(flow => ({
      ...flow,
      amount: flow.amount * (1 + (Math.random() - 0.5) * 0.2), // ±10%
      // Randomly toggle some flows on/off
      isActive: Math.random() > 0.1 ? flow.isActive : !flow.isActive,
    }));
    
    // Occasionally add a new flow
    if (Math.random() > 0.8 && this.flows.length < 15) {
      const randomFrom = this.nodes[Math.floor(Math.random() * this.nodes.length)];
      const randomTo = this.nodes[Math.floor(Math.random() * this.nodes.length)];
      
      if (randomFrom.id !== randomTo.id) {
        this.flows.push({
          from: randomFrom.name,
          to: randomTo.name,
          amount: Math.random() * 1_000_000_000,
          isActive: true,
        });
        console.log("➕ New flow added:", randomFrom.name, "→", randomTo.name);
      }
    }
    
    // Occasionally remove a flow
    if (Math.random() > 0.9 && this.flows.length > 5) {
      this.flows.pop();
      console.log("➖ Flow removed");
    }
    
    console.log("✅ Live data updated:", {
      nodes: this.nodes.length,
      flows: this.flows.length,
      activeFlows: this.flows.filter(f => f.isActive).length,
      totalTVL: this.nodes.reduce((sum, n) => sum + n.tvl, 0),
    });
  }
}

// Singleton instance
export const liveDataGenerator = new LiveDataGenerator();

