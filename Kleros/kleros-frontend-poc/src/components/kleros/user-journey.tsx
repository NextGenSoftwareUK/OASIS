'use client';

import { useState } from 'react';
import { 
  ShoppingCart,
  AlertTriangle,
  FileText,
  Users,
  Vote,
  Gavel,
  CheckCircle2,
  ArrowRight,
  ArrowDown,
  Repeat,
  Sparkles
} from 'lucide-react';

type JourneyStep = 1 | 2 | 3 | 4 | 5 | 6 | 7;

export function UserJourney() {
  const [activeStep, setActiveStep] = useState<JourneyStep>(1);

  const steps = [
    {
      step: 1,
      title: 'Problem Occurs',
      location: 'Partner dApp (e.g., OpenSea)',
      actor: 'User (Buyer/Seller)',
      description: 'A dispute arises - NFT not as described, payment not received, etc.',
      icon: AlertTriangle,
      color: 'red',
      example: 'Bob bought an NFT from Alice for 1 ETH. The NFT metadata doesn\'t match what was advertised.',
      codeSnippet: null
    },
    {
      step: 2,
      title: 'Create Dispute',
      location: 'Partner dApp UI → Kleros Smart Contract',
      actor: 'User clicks "File Dispute" button',
      description: 'User files dispute through partner\'s interface. Transaction sent to Kleros arbitrator contract with subcourt selection via extraData.',
      icon: Gavel,
      color: 'orange',
      example: 'Bob clicks "Dispute Transaction" on OpenSea. Selects "NFT Court". Pays arbitration fee (0.1 ETH on Ethereum, or 100 MATIC on Polygon ~$50).',
      codeSnippet: `// On OpenSea's smart contract (ERC-792 standard)
function raiseDispute(uint saleID) external payable {
  // Encode extraData: subcourt ID + min jurors
  bytes memory extraData = abi.encodePacked(
    uint96(5),  // Subcourt ID: NFT Court
    uint(3)     // Minimum 3 jurors
  );
  
  uint disputeID = arbitrator.createDispute{value: msg.value}(
    2,          // Ruling options: 0=refuse, 1=buyer, 2=seller
    extraData
  );
  
  sales[saleID].disputeID = disputeID;
  emit DisputeCreated(disputeID, saleID);
}

// On OpenSea's frontend (TypeScript)
await escrowContract.raiseDispute(saleID, {
  value: ethers.utils.parseEther("0.1") // Arbitration fee
});
// Returns: Dispute ID #12345, assigned to NFT Court`
    },
    {
      step: 3,
      title: 'Evidence Submission',
      location: 'Partner dApp → IPFS → Kleros Contract',
      actor: 'Both Parties + Public',
      description: 'Evidence period (usually 3-7 days). Both parties upload evidence to IPFS (via Pinata/Infura) and submit hashes on-chain via ERC-1497 Evidence event.',
      icon: FileText,
      color: 'blue',
      example: 'Alice uploads: Original NFT listing (IPFS), creation proof (IPFS). Bob uploads: Screenshots (IPFS), chat logs (IPFS), expert analysis (IPFS).',
      codeSnippet: `// STEP 1: Upload evidence to IPFS (using PinataOASIS for reliability)
const pinata = new PinataClient(apiKey);

const files = [
  { name: 'screenshot.png', data: screenshotBlob },
  { name: 'chat-log.pdf', data: chatLogBlob },
  { name: 'expert-report.pdf', data: reportBlob }
];

const ipfsResult = await pinata.pinFileToIPFS(files, {
  metadata: {
    disputeID: 12345,
    party: 'buyer',
    timestamp: Date.now()
  },
  pinataOptions: {
    cidVersion: 1,
    wrapWithDirectory: true
  }
});
// Returns: ipfs://QmBobsEvidence123...

// STEP 2: Submit IPFS hash to Kleros contract (ERC-1497)
const tx = await metaEvidence.submitEvidence(
  12345,  // dispute ID
  ipfsResult.IpfsHash
);
// Emits: Evidence(arbitrator, 12345, msg.sender, "ipfs://Qm...")

// IPFS ensures evidence is permanent and accessible to jurors
// PinataOASIS guarantees pinning (no disappearing evidence)`
    },
    {
      step: 4,
      title: 'Juror Selection',
      location: 'Kleros Smart Contract (Automatic)',
      actor: 'Kleros Protocol',
      description: 'Kleros randomly selects jurors from PNK stakers. Uses VRF (Verifiable Random Function) for fairness.',
      icon: Users,
      color: 'purple',
      example: 'System draws 3 jurors from "NFT" court. Jurors: @juror1, @juror2, @juror3',
      codeSnippet: null
    },
    {
      step: 5,
      title: 'Jurors Vote',
      location: 'court.kleros.io (Kleros Court dApp)',
      actor: 'Jurors',
      description: 'Jurors review evidence on court.kleros.io and vote. Voting period typically 3-5 days.',
      icon: Vote,
      color: 'cyan',
      example: 'Jurors review evidence. Vote: Juror1 → Buyer wins, Juror2 → Buyer wins, Juror3 → Buyer wins',
      codeSnippet: `// Jurors vote on court.kleros.io
// Options: 
// 0 = Refuse to arbitrate
// 1 = Buyer wins (refund)
// 2 = Seller wins (release payment)

// Majority wins (2 out of 3 voted "1")`
    },
    {
      step: 6,
      title: 'Ruling Announced',
      location: 'Kleros Smart Contract',
      actor: 'Kleros Protocol',
      description: 'Ruling revealed after voting period. Appeals possible within appeal period (typically 3 days).',
      icon: Gavel,
      color: 'green',
      example: 'Ruling: Option 1 (Buyer wins - refund). Appeal period: 3 days. Appeal cost: 0.2 ETH',
      codeSnippet: `// Check ruling
const ruling = await klerosArbitrator.currentRuling(12345);
// Result: 1 (Buyer wins)

const isFinal = await checkAppealPeriod(12345);
// false = appeals still possible
// true = ruling is final`
    },
    {
      step: 7,
      title: 'Execute Ruling',
      location: 'Partner dApp Smart Contract',
      actor: 'Partner\'s Escrow Contract',
      description: 'Once ruling is final, partner\'s smart contract executes it automatically (refund, release funds, etc.)',
      icon: CheckCircle2,
      color: 'emerald',
      example: 'OpenSea escrow contract releases 1 ETH back to Bob. NFT returned to Alice.',
      codeSnippet: `// OpenSea's escrow contract
function executeRuling(uint256 disputeID) {
  uint ruling = arbitrator.currentRuling(disputeID);
  
  if (ruling == 1) {
    // Buyer wins - refund
    buyer.transfer(1 ether);
    nft.transfer(seller);
  } else if (ruling == 2) {
    // Seller wins - release payment
    seller.transfer(1 ether);
    nft.transfer(buyer);
  }
}`
    },
  ];

  const currentStepData = steps.find(s => s.step === activeStep);

  const getStepColor = (step: number) => {
    const colorMap: Record<number, string> = {
      1: 'red',
      2: 'orange',
      3: 'blue',
      4: 'purple',
      5: 'cyan',
      6: 'green',
      7: 'emerald'
    };
    return colorMap[step] || 'gray';
  };

  return (
    <div className="space-y-8">
      {/* Title */}
      <div className="text-center">
        <h2 className="text-4xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 via-purple-400 to-pink-400 mb-3">
          The Kleros User Journey
        </h2>
        <p className="text-lg text-cyan-100/80 max-w-3xl mx-auto">
          Understanding <strong className="text-purple-400">where</strong> the Kleros process actually happens
        </p>
      </div>

      {/* Journey Timeline */}
      <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg shadow-[0_0_30px_rgba(138,43,226,0.3)] border border-purple-500/30 p-8">
        <h3 className="text-xl font-semibold text-purple-300 mb-6 flex items-center gap-2">
          <Sparkles className="w-5 h-5" />
          7-Step Dispute Resolution Process
        </h3>

        {/* Step Navigation */}
        <div className="grid grid-cols-7 gap-2 mb-8">
          {steps.map((step) => {
            const Icon = step.icon;
            const isActive = activeStep === step.step;
            const isPast = activeStep > step.step;
            const color = getStepColor(step.step);
            
            return (
              <button
                key={step.step}
                onClick={() => setActiveStep(step.step as JourneyStep)}
                className={`p-3 rounded-lg border-2 transition-all ${
                  isActive 
                    ? `border-${color}-500 bg-${color}-500/20 shadow-[0_0_20px_rgba(138,43,226,0.4)]`
                    : isPast
                    ? `border-${color}-500/50 bg-${color}-500/10`
                    : 'border-slate-700 bg-slate-900/40'
                }`}
              >
                <Icon className={`w-5 h-5 mx-auto mb-1 ${
                  isActive ? `text-${color}-400 drop-shadow-[0_0_10px_rgba(138,43,226,0.8)]` : isPast ? `text-${color}-400/70` : 'text-slate-500'
                }`} />
                <div className={`text-xs font-medium ${
                  isActive ? `text-${color}-300` : isPast ? `text-${color}-300/70` : 'text-slate-500'
                }`}>
                  Step {step.step}
                </div>
              </button>
            );
          })}
        </div>

        {/* Step Details */}
        {currentStepData && (
          <div className="space-y-6">
            {/* Header */}
            <div className={`bg-gradient-to-r from-${currentStepData.color}-900/40 to-${currentStepData.color}-800/20 rounded-lg p-6 border border-${currentStepData.color}-500/40`}>
              <div className="flex items-start gap-4">
                <div className={`p-3 bg-${currentStepData.color}-500/20 rounded-lg border border-${currentStepData.color}-500/40`}>
                  {(() => {
                    const Icon = currentStepData.icon;
                    return <Icon className={`w-8 h-8 text-${currentStepData.color}-400 drop-shadow-[0_0_10px_rgba(138,43,226,0.8)]`} />;
                  })()}
                </div>
                <div className="flex-1">
                  <h4 className={`text-2xl font-bold text-${currentStepData.color}-300 mb-2`}>
                    {currentStepData.title}
                  </h4>
                  <div className="space-y-2 text-sm">
                    <div className="flex items-center gap-2">
                      <span className="text-slate-400">Location:</span>
                      <code className={`text-${currentStepData.color}-300 bg-slate-900/60 px-2 py-1 rounded`}>
                        {currentStepData.location}
                      </code>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-slate-400">Actor:</span>
                      <span className="text-slate-200 font-medium">{currentStepData.actor}</span>
                    </div>
                  </div>
                </div>
              </div>
              <p className="text-slate-300 mt-4">
                {currentStepData.description}
              </p>
            </div>

            {/* Example */}
            <div className="bg-slate-900/60 backdrop-blur-sm rounded-lg p-5 border border-cyan-500/20">
              <div className="text-sm font-medium text-cyan-300 mb-2 flex items-center gap-2">
                <Sparkles className="w-4 h-4" />
                Real-World Example:
              </div>
              <p className="text-slate-300 text-sm italic">
                "{currentStepData.example}"
              </p>
            </div>

            {/* Code Snippet (if available) */}
            {currentStepData.codeSnippet && (
              <div className="bg-slate-950 rounded-lg p-4 border border-purple-500/30 shadow-[inset_0_0_20px_rgba(138,43,226,0.1)]">
                <div className="text-sm font-medium text-purple-300 mb-2 flex items-center gap-2">
                  <FileText className="w-4 h-4" />
                  Code (Standard Web3):
                </div>
                <pre className="text-xs text-emerald-400 font-mono overflow-x-auto">
                  {currentStepData.codeSnippet}
                </pre>
              </div>
            )}

            {/* Navigation */}
            <div className="flex justify-between items-center pt-4">
              <button
                onClick={() => activeStep > 1 && setActiveStep((activeStep - 1) as JourneyStep)}
                disabled={activeStep === 1}
                className="px-4 py-2 text-sm font-medium text-slate-300 bg-slate-700/60 border border-slate-600/50 rounded-lg hover:bg-slate-700 hover:border-purple-500/30 disabled:opacity-30 disabled:cursor-not-allowed transition-all"
              >
                Previous Step
              </button>
              <div className="text-sm text-slate-400">
                Step {activeStep} of 7
              </div>
              <button
                onClick={() => activeStep < 7 && setActiveStep((activeStep + 1) as JourneyStep)}
                disabled={activeStep === 7}
                className={`px-4 py-2 text-sm font-medium text-white bg-gradient-to-r from-purple-600 to-pink-600 rounded-lg hover:from-purple-500 hover:to-pink-500 disabled:opacity-30 disabled:cursor-not-allowed transition-all shadow-[0_0_15px_rgba(168,85,247,0.4)]`}
              >
                Next Step
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Key Locations */}
      <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg shadow-[0_0_30px_rgba(0,255,255,0.2)] border border-cyan-500/30 p-6">
        <h3 className="text-xl font-semibold text-cyan-300 mb-4 flex items-center gap-2">
          <ShoppingCart className="w-5 h-5" />
          Where Does the Kleros Process Occur?
        </h3>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* Partner dApp */}
          <div className="bg-slate-900/60 backdrop-blur-sm rounded-lg p-5 border border-emerald-500/30">
            <div className="text-lg font-bold text-emerald-300 mb-3">
              1. Partner dApp
            </div>
            <div className="text-sm text-slate-300 mb-4">
              (e.g., OpenSea, Uniswap)
            </div>
            <ul className="text-xs text-slate-400 space-y-2">
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-emerald-400 flex-shrink-0 mt-0.5" />
                <span>User files dispute</span>
              </li>
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-emerald-400 flex-shrink-0 mt-0.5" />
                <span>Submit evidence (upload to IPFS)</span>
              </li>
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-emerald-400 flex-shrink-0 mt-0.5" />
                <span>See ruling notification</span>
              </li>
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-emerald-400 flex-shrink-0 mt-0.5" />
                <span>Automatic execution (refund/release)</span>
              </li>
            </ul>
          </div>

          {/* Kleros Court */}
          <div className="bg-slate-900/60 backdrop-blur-sm rounded-lg p-5 border border-purple-500/30">
            <div className="text-lg font-bold text-purple-300 mb-3">
              2. Kleros Court
            </div>
            <div className="text-sm text-slate-300 mb-4">
              (court.kleros.io)
            </div>
            <ul className="text-xs text-slate-400 space-y-2">
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-purple-400 flex-shrink-0 mt-0.5" />
                <span>Jurors review evidence</span>
              </li>
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-purple-400 flex-shrink-0 mt-0.5" />
                <span>Jurors cast votes</span>
              </li>
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-purple-400 flex-shrink-0 mt-0.5" />
                <span>Ruling revealed</span>
              </li>
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-purple-400 flex-shrink-0 mt-0.5" />
                <span>Jurors rewarded/penalized</span>
              </li>
            </ul>
          </div>

          {/* Smart Contracts */}
          <div className="bg-slate-900/60 backdrop-blur-sm rounded-lg p-5 border border-cyan-500/30">
            <div className="text-lg font-bold text-cyan-300 mb-3">
              3. Smart Contracts
            </div>
            <div className="text-sm text-slate-300 mb-4">
              (On-chain, automatic)
            </div>
            <ul className="text-xs text-slate-400 space-y-2">
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-cyan-400 flex-shrink-0 mt-0.5" />
                <span>Kleros arbitrator contract</span>
              </li>
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-cyan-400 flex-shrink-0 mt-0.5" />
                <span>Partner's escrow contract</span>
              </li>
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-cyan-400 flex-shrink-0 mt-0.5" />
                <span>Automatic execution of ruling</span>
              </li>
              <li className="flex items-start gap-2">
                <ArrowRight className="w-3 h-3 text-cyan-400 flex-shrink-0 mt-0.5" />
                <span>Funds transferred per ruling</span>
              </li>
            </ul>
          </div>
        </div>
      </div>

      {/* Timeline Visual */}
      <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg shadow-[0_0_30px_rgba(236,72,153,0.2)] border border-pink-500/30 p-6">
        <h3 className="text-xl font-semibold text-pink-300 mb-6 flex items-center gap-2">
          <Repeat className="w-5 h-5" />
          Typical Timeline
        </h3>

        <div className="space-y-4">
          <div className="flex items-center gap-4">
            <div className="w-32 text-sm text-slate-400">Day 0</div>
            <div className="flex-1">
              <div className="bg-red-500/20 border border-red-500/40 rounded p-3">
                <div className="text-sm font-medium text-red-300">Dispute Created</div>
                <div className="text-xs text-slate-400 mt-1">Transaction sent to Kleros contract</div>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-32 text-sm text-slate-400">Days 0-7</div>
            <div className="flex-1">
              <div className="bg-blue-500/20 border border-blue-500/40 rounded p-3">
                <div className="text-sm font-medium text-blue-300">Evidence Period</div>
                <div className="text-xs text-slate-400 mt-1">Both parties upload evidence to IPFS</div>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-32 text-sm text-slate-400">Day 7</div>
            <div className="flex-1">
              <div className="bg-purple-500/20 border border-purple-500/40 rounded p-3">
                <div className="text-sm font-medium text-purple-300">Jurors Selected</div>
                <div className="text-xs text-slate-400 mt-1">Random selection from PNK stakers</div>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-32 text-sm text-slate-400">Days 7-12</div>
            <div className="flex-1">
              <div className="bg-cyan-500/20 border border-cyan-500/40 rounded p-3">
                <div className="text-sm font-medium text-cyan-300">Voting Period</div>
                <div className="text-xs text-slate-400 mt-1">Jurors review evidence on court.kleros.io and vote</div>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-32 text-sm text-slate-400">Day 12</div>
            <div className="flex-1">
              <div className="bg-green-500/20 border border-green-500/40 rounded p-3">
                <div className="text-sm font-medium text-green-300">Ruling Announced</div>
                <div className="text-xs text-slate-400 mt-1">Votes tallied, majority wins</div>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-32 text-sm text-slate-400">Days 12-15</div>
            <div className="flex-1">
              <div className="bg-orange-500/20 border border-orange-500/40 rounded p-3">
                <div className="text-sm font-medium text-orange-300">Appeal Period (Optional)</div>
                <div className="text-xs text-slate-400 mt-1">Either party can appeal if they pay the fee</div>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-32 text-sm text-slate-400">Day 15+</div>
            <div className="flex-1">
              <div className="bg-emerald-500/20 border border-emerald-500/40 rounded p-3">
                <div className="text-sm font-medium text-emerald-300">Ruling Executed</div>
                <div className="text-xs text-slate-400 mt-1">Partner's smart contract releases funds per ruling</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Key Insight */}
      <div className="bg-gradient-to-br from-purple-900/40 to-pink-900/30 rounded-lg border border-purple-500/40 p-6 backdrop-blur-sm shadow-[0_0_30px_rgba(168,85,247,0.2)]">
        <div className="flex items-start gap-4">
          <Sparkles className="w-6 h-6 text-pink-400 flex-shrink-0 drop-shadow-[0_0_10px_rgba(236,72,153,0.8)]" />
          <div>
            <h3 className="font-semibold text-pink-300 mb-2">Key Insight: Three Separate Interfaces</h3>
            <div className="space-y-2 text-sm text-slate-300">
              <p>
                <strong className="text-emerald-400">1. Users</strong> interact with the <strong>partner's dApp</strong> (OpenSea, Uniswap) - they file disputes and see results there.
              </p>
              <p>
                <strong className="text-purple-400">2. Jurors</strong> interact with <strong>court.kleros.io</strong> - they review evidence and vote there.
              </p>
              <p>
                <strong className="text-cyan-400">3. Smart contracts</strong> execute automatically - rulings are enforced on-chain without manual intervention.
              </p>
              <p className="mt-4 p-3 bg-slate-900/40 rounded border border-purple-500/20">
                <strong className="text-pink-400">Users never visit court.kleros.io</strong> - they stay on the partner dApp (like OpenSea) the whole time. The partner dApp handles the UI, Kleros handles the arbitration logic in the background.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

