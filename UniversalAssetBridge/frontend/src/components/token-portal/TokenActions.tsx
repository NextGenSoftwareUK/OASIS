"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Send, Download, Zap } from "lucide-react";

interface TokenActionsProps {
  selectedChain: string;
  setSelectedChain: (chain: string) => void;
  balance: number;
}

const chains = [
  "Solana", "Ethereum", "Polygon", "Base", "Arbitrum", 
  "Optimism", "BNB Chain", "Avalanche", "Fantom", "Radix"
];

export default function TokenActions({ selectedChain, setSelectedChain, balance }: TokenActionsProps) {
  const [activeTab, setActiveTab] = useState<"send" | "receive" | "stake">("send");
  const [amount, setAmount] = useState("");
  const [recipient, setRecipient] = useState("");

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-sm uppercase tracking-[0.3em]" style={{color: 'var(--oasis-muted)'}}>
          Token Actions
        </h3>
        
        <div className="flex gap-2">
          <button
            onClick={() => setActiveTab("send")}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm transition-all ${
              activeTab === "send" ? "bg-[var(--oasis-accent)] text-white" : "bg-muted text-[var(--oasis-muted)]"
            }`}
          >
            <Send size={16} />
            Send
          </button>
          <button
            onClick={() => setActiveTab("receive")}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm transition-all ${
              activeTab === "receive" ? "bg-[var(--oasis-accent)] text-white" : "bg-muted text-[var(--oasis-muted)]"
            }`}
          >
            <Download size={16} />
            Receive
          </button>
          <button
            onClick={() => setActiveTab("stake")}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm transition-all ${
              activeTab === "stake" ? "bg-[var(--oasis-accent)] text-white" : "bg-muted text-[var(--oasis-muted)]"
            }`}
          >
            <Zap size={16} />
            Stake
          </button>
        </div>
      </div>

      {activeTab === "send" && (
        <div className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm" style={{color: 'var(--oasis-muted)'}}>
              Spend from which chain?
            </label>
            <select
              value={selectedChain}
              onChange={(e) => setSelectedChain(e.target.value)}
              className="w-full rounded-lg border bg-muted px-4 py-3 text-sm"
              style={{
                borderColor: 'var(--oasis-card-border)',
                color: 'var(--oasis-foreground)'
              }}
            >
              {chains.map((chain) => (
                <option key={chain} value={chain}>
                  {chain}
                </option>
              ))}
            </select>
            <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
              This only chooses where to execute the transaction. Your balance updates everywhere.
            </p>
          </div>

          <div className="space-y-2">
            <label className="text-sm" style={{color: 'var(--oasis-muted)'}}>
              Amount (DPT)
            </label>
            <Input
              type="number"
              placeholder="0.00"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              className="bg-muted"
            />
            <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
              Available: {balance.toLocaleString('en-US')} DPT
            </p>
          </div>

          <div className="space-y-2">
            <label className="text-sm" style={{color: 'var(--oasis-muted)'}}>
              Recipient Address
            </label>
            <Input
              type="text"
              placeholder="0x... or wallet.sol"
              value={recipient}
              onChange={(e) => setRecipient(e.target.value)}
              className="bg-muted"
            />
          </div>

          <Button 
            className="w-full" 
            size="lg"
            style={{
              background: 'var(--oasis-accent)',
              color: 'white'
            }}
          >
            Send DPT
          </Button>
        </div>
      )}

      {activeTab === "receive" && (
        <div className="space-y-4">
          <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
            Your DPT address is the same as your wallet address on any chain. Select a chain to view:
          </p>

          <div className="space-y-2">
            <label className="text-sm" style={{color: 'var(--oasis-muted)'}}>
              Show address for:
            </label>
            <select
              value={selectedChain}
              onChange={(e) => setSelectedChain(e.target.value)}
              className="w-full rounded-lg border bg-muted px-4 py-3 text-sm"
              style={{
                borderColor: 'var(--oasis-card-border)',
                color: 'var(--oasis-foreground)'
              }}
            >
              {chains.map((chain) => (
                <option key={chain} value={chain}>
                  {chain}
                </option>
              ))}
            </select>
          </div>

          <div className="rounded-lg border p-4" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(15,118,110,0.1)'
          }}>
            <p className="text-xs mb-2" style={{color: 'var(--oasis-muted)'}}>
              Your {selectedChain} Address:
            </p>
            <p className="font-mono text-sm break-all" style={{color: 'var(--oasis-foreground)'}}>
              0x1234...5678
            </p>
          </div>

          <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
            DPT sent to this address will appear on ALL chains automatically.
          </p>
        </div>
      )}

      {activeTab === "stake" && (
        <div className="space-y-4">
          <div className="rounded-lg border p-6" style={{
            borderColor: 'var(--oasis-accent)',
            background: 'rgba(15,118,110,0.1)'
          }}>
            <div className="flex items-center justify-between mb-4">
              <span className="text-sm" style={{color: 'var(--oasis-muted)'}}>Current APY</span>
              <span className="text-2xl font-bold" style={{color: 'var(--oasis-accent)'}}>42%</span>
            </div>
            
            <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
              Stake your DPT to earn rewards. Staked tokens remain accessible on all chains.
            </p>
          </div>

          <div className="space-y-2">
            <label className="text-sm" style={{color: 'var(--oasis-muted)'}}>
              Amount to Stake (DPT)
            </label>
            <Input
              type="number"
              placeholder="0.00"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              className="bg-muted"
            />
          </div>

          <Button 
            className="w-full" 
            size="lg"
            style={{
              background: 'var(--oasis-accent)',
              color: 'white'
            }}
          >
            Stake DPT
          </Button>

          <p className="text-xs text-center" style={{color: 'var(--oasis-muted)'}}>
            Rewards are distributed across all chains simultaneously
          </p>
        </div>
      )}
    </div>
  );
}

