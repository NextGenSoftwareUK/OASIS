'use client';

import { useState } from 'react';
import { 
  Code, 
  CheckCircle2,
  XCircle,
  Terminal,
  FileCode,
  Zap,
  Package,
  Users
} from 'lucide-react';

type IntegrationStep = 'docs' | 'install' | 'code' | 'test' | 'complete';

export function PartnerIntegrationView() {
  const [currentStep, setCurrentStep] = useState<IntegrationStep>('docs');
  const [selectedPartner, setSelectedPartner] = useState('uniswap');
  const [selectedChain, setSelectedChain] = useState('polygon');
  const [selectedSubcourt, setSelectedSubcourt] = useState(5); // NFT default

  const partners = [
    { id: 'uniswap', name: 'Uniswap', useCase: 'OTC Escrow', icon: '🦄', subcourt: 0 },
    { id: 'opensea', name: 'OpenSea', useCase: 'NFT Disputes', icon: '🌊', subcourt: 5 },
    { id: 'magiceden', name: 'Magic Eden', useCase: 'Marketplace Disputes', icon: '✨', subcourt: 5 },
  ];

  const chains = [
    { id: 'ethereum', name: 'Ethereum', contractAddress: '0x988b3a538b618c7a603e1c11ab82cd16dbe28069' },
    { id: 'polygon', name: 'Polygon', contractAddress: '0x9C1dA9A04925bDfDedf0f6421bC7EEa8305F9002' },
    { id: 'arbitrum', name: 'Arbitrum', contractAddress: '0x[ARBITRUM_ADDRESS]' },
  ];

  const subcourts = [
    { id: 0, name: 'General Court', description: 'Any type of dispute', color: 'slate' },
    { id: 1, name: 'Blockchain', description: 'Technical blockchain disputes', color: 'blue' },
    { id: 3, name: 'English Language', description: 'Translation, content quality', color: 'green' },
    { id: 4, name: 'Marketing Services', description: 'Marketing deliverables', color: 'orange' },
    { id: 5, name: 'NFT', description: 'NFT authenticity, quality', color: 'purple' },
  ];

  const getContractAddress = () => {
    return chains.find(c => c.id === selectedChain)?.contractAddress || '';
  };

  const getCodeExample = () => {
    const subcourt = subcourts.find(s => s.id === selectedSubcourt);
    return `// ${selectedPartner.toUpperCase()} - ERC-792 Integration (Official Standard)
// NO OASIS KNOWLEDGE REQUIRED - Just standard Web3 + Kleros docs

// SOLIDITY: Implement IArbitrable interface in your contract
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface IArbitrator {
    function createDispute(uint _choices, bytes calldata _extraData) 
        external payable returns (uint disputeID);
    function arbitrationCost(bytes calldata _extraData) 
        external view returns (uint cost);
    function currentRuling(uint _disputeID) 
        external view returns (uint ruling);
}

interface IArbitrable {
    function rule(uint _disputeID, uint _ruling) external;
}

contract ${selectedPartner.charAt(0).toUpperCase() + selectedPartner.slice(1)}Escrow is IArbitrable {
    IArbitrator public arbitrator = IArbitrator(${getContractAddress()});
    
    // Create dispute with ERC-792
    function raiseDispute(uint saleID) external payable {
        // extraData encodes: subcourt ID + min jurors
        bytes memory extraData = abi.encodePacked(
            uint96(${selectedSubcourt}),  // Subcourt: ${subcourt?.name}
            uint(3)        // Min jurors: 3
        );
        
        uint disputeID = arbitrator.createDispute{value: msg.value}(
            2,             // Ruling options: 0=refuse, 1=buyer wins, 2=seller wins
            extraData
        );
        
        sales[saleID].disputeID = disputeID;
        emit DisputeCreated(disputeID, saleID);
    }
    
    // Kleros calls this when ruling is final
    function rule(uint _disputeID, uint _ruling) external override {
        require(msg.sender == address(arbitrator), "Only arbitrator");
        
        uint saleID = disputes[_disputeID].saleID;
        
        if (_ruling == 1) {
            // Buyer wins - refund
            payable(sales[saleID].buyer).transfer(sales[saleID].amount);
            nft.transferFrom(address(this), sales[saleID].seller, sales[saleID].tokenID);
        } else if (_ruling == 2) {
            // Seller wins - release payment
            payable(sales[saleID].seller).transfer(sales[saleID].amount);
            nft.transferFrom(address(this), sales[saleID].buyer, sales[saleID].tokenID);
        }
        
        emit RulingExecuted(_disputeID, _ruling);
    }
}

// FRONTEND: Call from JavaScript/TypeScript
import { ethers } from 'ethers';

const contract = new ethers.Contract(escrowAddress, abi, signer);

// File dispute
await contract.raiseDispute(saleID, {
  value: ethers.utils.parseEther("0.1") // Arbitration fee
});

// Check ruling (after jurors vote)
const ruling = await arbitrator.currentRuling(disputeID);
// Returns: 0 (refuse), 1 (buyer), 2 (seller)`;
  };

  const steps: { id: IntegrationStep; label: string; icon: any }[] = [
    { id: 'docs', label: '1. Read Docs', icon: FileCode },
    { id: 'install', label: '2. Install SDK', icon: Package },
    { id: 'code', label: '3. Write Code', icon: Code },
    { id: 'test', label: '4. Test', icon: Zap },
    { id: 'complete', label: '5. Deploy', icon: CheckCircle2 },
  ];

  const currentStepIndex = steps.findIndex(s => s.id === currentStep);

  return (
    <div className="space-y-8 relative">
      {/* Title */}
      <div>
        <h2 className="text-4xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-emerald-400 via-cyan-400 to-blue-400 mb-3">
          Partner Integration: Standard Web3
        </h2>
        <p className="text-lg text-emerald-200/70">
          Integrate Kleros using tools you already know - <strong className="text-cyan-400">no OASIS required</strong>
        </p>
      </div>

      {/* Select Partner & Chain & Subcourt */}
      <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg shadow-[0_0_30px_rgba(16,185,129,0.2)] border border-emerald-500/30 p-6">
        <h3 className="text-xl font-semibold text-emerald-300 mb-4 flex items-center gap-2">
          <Users className="w-5 h-5" />
          Example Partner Integration
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* Select Partner */}
          <div>
            <label className="block text-sm font-medium text-cyan-300 mb-2">
              Partner (Example):
            </label>
            <select
              value={selectedPartner}
              onChange={(e) => {
                const partner = partners.find(p => p.id === e.target.value);
                setSelectedPartner(e.target.value);
                if (partner) setSelectedSubcourt(partner.subcourt);
              }}
              className="w-full px-4 py-2 bg-slate-900/60 border border-cyan-500/30 text-slate-200 rounded-lg focus:ring-2 focus:ring-cyan-500/50 focus:border-cyan-500 backdrop-blur-sm shadow-[0_0_10px_rgba(0,255,255,0.1)]"
            >
              {partners.map(p => (
                <option key={p.id} value={p.id} className="bg-slate-900">
                  {p.icon} {p.name} - {p.useCase}
                </option>
              ))}
            </select>
          </div>

          {/* Select Chain */}
          <div>
            <label className="block text-sm font-medium text-purple-300 mb-2">
              Blockchain:
            </label>
            <select
              value={selectedChain}
              onChange={(e) => setSelectedChain(e.target.value)}
              className="w-full px-4 py-2 bg-slate-900/60 border border-purple-500/30 text-slate-200 rounded-lg focus:ring-2 focus:ring-purple-500/50 focus:border-purple-500 backdrop-blur-sm shadow-[0_0_10px_rgba(168,85,247,0.1)]"
            >
              {chains.map(c => (
                <option key={c.id} value={c.id} className="bg-slate-900">
                  {c.name}
                </option>
              ))}
            </select>
          </div>

          {/* Select Subcourt */}
          <div>
            <label className="block text-sm font-medium text-pink-300 mb-2">
              Subcourt (Specialized):
            </label>
            <select
              value={selectedSubcourt}
              onChange={(e) => setSelectedSubcourt(Number(e.target.value))}
              className="w-full px-4 py-2 bg-slate-900/60 border border-pink-500/30 text-slate-200 rounded-lg focus:ring-2 focus:ring-pink-500/50 focus:border-pink-500 backdrop-blur-sm shadow-[0_0_10px_rgba(236,72,153,0.1)]"
            >
              {subcourts.map(s => (
                <option key={s.id} value={s.id} className="bg-slate-900">
                  {s.name} - {s.description}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Subcourt Info */}
        <div className="mt-4 p-3 bg-blue-900/20 border border-blue-500/30 rounded-lg">
          <div className="text-xs text-blue-200">
            <strong>Subcourts</strong>: Specialized courts with expert jurors.  
            NFT Court has jurors who stake PNK specifically for NFT disputes.  
            Choosing the right subcourt improves ruling quality.
          </div>
        </div>
      </div>

      {/* Integration Steps */}
      <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg shadow-[0_0_30px_rgba(0,255,255,0.2)] border border-cyan-500/30 p-6">
        <h3 className="text-xl font-semibold text-cyan-300 mb-4 flex items-center gap-2">
          <Zap className="w-5 h-5 drop-shadow-[0_0_10px_rgba(0,255,255,0.8)]" />
          5-Step Integration Process
        </h3>

        {/* Step Progress */}
        <div className="flex items-center justify-between mb-6 gap-1">
          {steps.map((step, idx) => {
            const Icon = step.icon;
            const isActive = currentStep === step.id;
            const isComplete = currentStepIndex > idx;
            
            return (
              <button
                key={step.id}
                onClick={() => setCurrentStep(step.id as IntegrationStep)}
                className={`flex-1 p-3 rounded-lg border-2 transition-all ${
                  isActive 
                    ? 'border-cyan-500 bg-cyan-500/20 shadow-[0_0_20px_rgba(0,255,255,0.4)]' 
                    : isComplete
                    ? 'border-emerald-500/50 bg-emerald-500/10'
                    : 'border-slate-700 bg-slate-900/40'
                }`}
              >
                <Icon className={`w-5 h-5 mx-auto mb-1 ${
                  isActive 
                    ? 'text-cyan-400 drop-shadow-[0_0_10px_rgba(0,255,255,0.8)]' 
                    : isComplete
                    ? 'text-emerald-400'
                    : 'text-slate-500'
                }`} />
                <div className={`text-xs font-medium ${
                  isActive 
                    ? 'text-cyan-300' 
                    : isComplete
                    ? 'text-emerald-300'
                    : 'text-slate-500'
                }`}>
                  {step.label}
                </div>
              </button>
            );
          })}
        </div>

        {/* Step Content */}
        <div className="bg-slate-900/60 backdrop-blur-sm rounded-lg p-6 border border-purple-500/20">
          {currentStep === 'docs' && (
            <div>
              <h4 className="font-semibold text-cyan-300 mb-3 flex items-center gap-2">
                <FileCode className="w-5 h-5" />
                Read Kleros Documentation
              </h4>
              <p className="text-sm text-slate-300 mb-4">
                Standard documentation - just like Uniswap, Aave, or any other protocol
              </p>
              <div className="bg-slate-950/60 rounded-lg p-4 border border-cyan-500/30 shadow-[inset_0_0_20px_rgba(0,255,255,0.1)]">
                <code className="text-sm text-cyan-400 font-mono">
                  https://docs.kleros.io/integrations/{selectedChain}
                </code>
              </div>
              <div className="mt-4 space-y-2 text-sm text-slate-300">
                <div className="flex items-start gap-2">
                  <CheckCircle2 className="w-4 h-4 text-emerald-400 flex-shrink-0 mt-0.5" />
                  <span>Contract addresses for each chain</span>
                </div>
                <div className="flex items-start gap-2">
                  <CheckCircle2 className="w-4 h-4 text-cyan-400 flex-shrink-0 mt-0.5" />
                  <span>ABI (Application Binary Interface)</span>
                </div>
                <div className="flex items-start gap-2">
                  <CheckCircle2 className="w-4 h-4 text-purple-400 flex-shrink-0 mt-0.5" />
                  <span>Integration examples</span>
                </div>
              </div>
            </div>
          )}

          {currentStep === 'install' && (
            <div>
              <h4 className="font-semibold text-purple-300 mb-3 flex items-center gap-2">
                <Package className="w-5 h-5" />
                Install SDK (npm)
              </h4>
              <div className="bg-slate-950 rounded-lg p-4 mb-4 border border-emerald-500/30 shadow-[inset_0_0_20px_rgba(16,185,129,0.1)]">
                <code className="text-emerald-400 text-sm font-mono">
                  npm install @kleros/sdk-{selectedChain}
                </code>
              </div>
              <p className="text-sm text-slate-300 mb-3">
                Or use vanilla Ethers.js/Web3.js - your choice!
              </p>
              <div className="bg-slate-950 rounded-lg p-4 border border-cyan-500/30 shadow-[inset_0_0_20px_rgba(0,255,255,0.1)]">
                <code className="text-cyan-400 text-sm font-mono">
                  npm install ethers
                </code>
              </div>
              <div className="mt-4 space-y-2 text-sm text-slate-300">
                <div className="flex items-start gap-2">
                  <XCircle className="w-4 h-4 text-red-400 flex-shrink-0 mt-0.5" />
                  <span>No OASIS installation needed</span>
                </div>
                <div className="flex items-start gap-2">
                  <XCircle className="w-4 h-4 text-red-400 flex-shrink-0 mt-0.5" />
                  <span>No custom tooling required</span>
                </div>
                <div className="flex items-start gap-2">
                  <CheckCircle2 className="w-4 h-4 text-emerald-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(16,185,129,0.6)]" />
                  <span>Use your existing Web3 stack</span>
                </div>
              </div>
            </div>
          )}

          {currentStep === 'code' && (
            <div>
              <h4 className="font-semibold text-pink-300 mb-3 flex items-center gap-2">
                <Code className="w-5 h-5" />
                Write Integration Code
              </h4>
              <div className="bg-slate-950 rounded-lg p-4 overflow-x-auto border border-pink-500/30 shadow-[inset_0_0_20px_rgba(236,72,153,0.1)]">
                <pre className="text-xs text-emerald-400 font-mono whitespace-pre">
                  {getCodeExample()}
                </pre>
              </div>
              <div className="mt-4 bg-blue-900/30 border border-blue-500/30 rounded-lg p-4 backdrop-blur-sm shadow-[0_0_15px_rgba(59,130,246,0.2)]">
                <div className="flex items-start gap-2">
                  <Terminal className="w-5 h-5 text-blue-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_10px_rgba(59,130,246,0.8)]" />
                  <div className="text-sm text-blue-200">
                    <strong className="text-cyan-400">Notice:</strong> This is 100% standard Web3 code. No OASIS imports, 
                    no custom APIs. Partners never need to know OASIS exists!
                  </div>
                </div>
              </div>
            </div>
          )}

          {currentStep === 'test' && (
            <div>
              <h4 className="font-semibold text-cyan-300 mb-3 flex items-center gap-2">
                <Zap className="w-5 h-5" />
                Test Integration
              </h4>
              <div className="space-y-4">
                <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-4 border border-cyan-500/30">
                  <div className="flex items-center justify-between mb-2">
                    <span className="font-medium text-slate-200">1. Create Test Dispute</span>
                    <CheckCircle2 className="w-5 h-5 text-emerald-400 drop-shadow-[0_0_10px_rgba(16,185,129,0.8)]" />
                  </div>
                  <code className="text-xs text-cyan-300 block bg-slate-950/60 p-2 rounded border border-cyan-500/20">
                    Dispute ID: 12345 | Status: WaitingForEvidence
                  </code>
                </div>

                <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-4 border border-purple-500/30">
                  <div className="flex items-center justify-between mb-2">
                    <span className="font-medium text-slate-200">2. Submit Evidence</span>
                    <CheckCircle2 className="w-5 h-5 text-emerald-400 drop-shadow-[0_0_10px_rgba(16,185,129,0.8)]" />
                  </div>
                  <code className="text-xs text-purple-300 block bg-slate-950/60 p-2 rounded border border-purple-500/20">
                    Evidence URI: ipfs://QmTest123... | Tx: 0xabc...
                  </code>
                </div>

                <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-4 border border-pink-500/30">
                  <div className="flex items-center justify-between mb-2">
                    <span className="font-medium text-slate-200">3. Get Ruling</span>
                    <CheckCircle2 className="w-5 h-5 text-emerald-400 drop-shadow-[0_0_10px_rgba(16,185,129,0.8)]" />
                  </div>
                  <code className="text-xs text-pink-300 block bg-slate-950/60 p-2 rounded border border-pink-500/20">
                    Ruling: 2 (Seller wins) | Final: true
                  </code>
                </div>
              </div>
            </div>
          )}

          {currentStep === 'complete' && (
            <div>
              <h4 className="font-semibold text-emerald-300 mb-3 flex items-center gap-2">
                <CheckCircle2 className="w-6 h-6 drop-shadow-[0_0_10px_rgba(16,185,129,0.8)]" />
                Integration Complete!
              </h4>
              <div className="bg-emerald-900/30 border border-emerald-500/40 rounded-lg p-6 backdrop-blur-sm shadow-[0_0_20px_rgba(16,185,129,0.3)]">
                <p className="text-sm text-emerald-200 mb-4">
                  Your integration is ready. Deploy to production using standard Web3 practices.
                </p>
                <div className="space-y-2 text-sm text-slate-300">
                  <div className="flex items-start gap-2">
                    <CheckCircle2 className="w-4 h-4 text-cyan-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(0,255,255,0.6)]" />
                    <span>Standard Web3 integration - no custom tools</span>
                  </div>
                  <div className="flex items-start gap-2">
                    <CheckCircle2 className="w-4 h-4 text-purple-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(168,85,247,0.6)]" />
                    <span>Works with your existing stack (React, Vue, vanilla JS)</span>
                  </div>
                  <div className="flex items-start gap-2">
                    <CheckCircle2 className="w-4 h-4 text-pink-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(236,72,153,0.6)]" />
                    <span>No vendor lock-in - just smart contract calls</span>
                  </div>
                  <div className="flex items-start gap-2">
                    <CheckCircle2 className="w-4 h-4 text-emerald-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(16,185,129,0.6)]" />
                    <span>Support via normal Kleros channels (docs, Discord, forum)</span>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Navigation */}
        <div className="flex justify-between mt-6">
          <button
            onClick={() => {
              const stepIds: IntegrationStep[] = ['docs', 'install', 'code', 'test', 'complete'];
              const currentIdx = stepIds.indexOf(currentStep);
              if (currentIdx > 0) setCurrentStep(stepIds[currentIdx - 1]);
            }}
            disabled={currentStep === 'docs'}
            className="px-4 py-2 text-sm font-medium text-slate-300 bg-slate-700/60 border border-slate-600/50 rounded-lg hover:bg-slate-700 hover:border-purple-500/30 disabled:opacity-30 disabled:cursor-not-allowed transition-all"
          >
            Previous
          </button>
          <button
            onClick={() => {
              const stepIds: IntegrationStep[] = ['docs', 'install', 'code', 'test', 'complete'];
              const currentIdx = stepIds.indexOf(currentStep);
              if (currentIdx < stepIds.length - 1) setCurrentStep(stepIds[currentIdx + 1]);
            }}
            disabled={currentStep === 'complete'}
            className="px-4 py-2 text-sm font-medium text-white bg-gradient-to-r from-emerald-600 to-cyan-600 rounded-lg hover:from-emerald-500 hover:to-cyan-500 disabled:opacity-30 disabled:cursor-not-allowed transition-all shadow-[0_0_15px_rgba(16,185,129,0.4)] hover:shadow-[0_0_25px_rgba(16,185,129,0.6)]"
          >
            Next
          </button>
        </div>
      </div>

      {/* Key Points */}
      <div className="relative bg-gradient-to-br from-emerald-900/40 to-cyan-900/30 rounded-lg border border-emerald-500/40 p-6 backdrop-blur-sm shadow-[0_0_30px_rgba(16,185,129,0.2)]">
        <div className="absolute inset-0 bg-gradient-to-r from-emerald-500/5 to-cyan-500/5 rounded-lg blur-xl" />
        
        <div className="relative">
          <h3 className="font-semibold text-emerald-300 mb-4 flex items-center gap-2">
            <Zap className="w-5 h-5" />
            What Partners Don't Need to Know
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <h4 className="text-sm font-medium text-red-300 mb-2 flex items-center gap-2">
                <XCircle className="w-4 h-4" />
                Never Touch:
              </h4>
              <ul className="space-y-1 text-sm text-slate-300">
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-red-400 rounded-full" />
                  <span>OASIS platform</span>
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-red-400 rounded-full" />
                  <span>AssetRail SC-Gen</span>
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-red-400 rounded-full" />
                  <span>Multi-chain deployment tools</span>
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-red-400 rounded-full" />
                  <span>Kleros team's internal tooling</span>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="text-sm font-medium text-emerald-300 mb-2 flex items-center gap-2">
                <CheckCircle2 className="w-4 h-4" />
                Just Use:
              </h4>
              <ul className="space-y-1 text-sm text-slate-300">
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-cyan-400 rounded-full shadow-[0_0_4px_rgba(0,255,255,0.8)]" />
                  <span>Ethers.js / Web3.js</span>
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-purple-400 rounded-full shadow-[0_0_4px_rgba(168,85,247,0.8)]" />
                  <span>Standard smart contract calls</span>
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-pink-400 rounded-full shadow-[0_0_4px_rgba(236,72,153,0.8)]" />
                  <span>Kleros documentation</span>
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-emerald-400 rounded-full shadow-[0_0_4px_rgba(16,185,129,0.8)]" />
                  <span>Their existing Web3 stack</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
