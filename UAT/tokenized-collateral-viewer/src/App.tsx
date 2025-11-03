// OASIS Tokenized Collateral Solution One-Pager
// Print-ready: A4-ish container, professional styling

const collateralData = {
  title: "Unlocking Real-Time Collateral Mobility",
  subtitle: "$100-150B Opportunity | OASIS WEB4 - Hyperdrive",
  version: "v1.0 | October 24, 2025",
  tagline: "Real-time ownership tracking, instant settlement, and cross-chain collateral optimization for institutional finance.",
  
  contact: [
    { label: "OASIS Platform", icon: "brief" },
    { label: "max@oasisweb4.com / david@oasisweb4.com", icon: "mail" },
    { label: "@maxgershfield", icon: "pen" },
  ],
  
  problemStatement: {
    eyebrow: "Problem",
    title: "Current collateral management locks capital unnecessarily",
    points: [
      {
        title: "Fragmented Visibility",
        description: "Each bank has separate ledger, no real-time 'who owns what, when'",
        impact: "Takes 2-3 days to reconcile positions across systems",
        diagram: `        💰 $100M Bond
              ↙  ↓  ↘
    
    Bank A      Bank B      Bank C
    "Mine"      "Mine"      "Mine"
    
       ?    Who owns it?    ?
    
    → Mon-Fri to reconcile`
      },
      {
        title: "Low Reusability & Settlement Delays",
        description: "Settlement delays (T+2, T+5) lock assets 50-70% of the time, preventing reuse",
        impact: "Need 2-3x more collateral. Cost: $1M locked capital per day per $100M collateral",
        diagram: `$100M Treasury Bond:

Day 1-4: Locked in Repo A
Day 5: Settlement delay
Day 6: Finally available

= 83% idle time`
      },
      {
        title: "Manual Reconciliation",
        description: "Error-prone, expensive process for every transaction",
        impact: "Cost: $500-2,000 per transaction + high error rate",
        diagram: `Transaction executed
→ Bank A DB ≠ Bank B DB
→ Manual reconciliation
→ Takes 2 days, errors common

$500-2,000 per transaction`
      },
      {
        title: "Zero Interoperability",
        description: "Bilateral agreements only, can't optimize across platforms",
        impact: "Each chain/system operates in isolation",
        diagram: `Ethereum | JP Morgan | Bank Core

Each system isolated
No cross-chain transfers
No unified view

= Capital trapped in silos`
      }
    ]
  },
  
  solution: {
    eyebrow: "Solution",
    title: "Real-time ownership tracking, instant settlement, and cross-chain optimization",
    subtitle: "Seven unique capabilities unlocking $100-150B in trapped capital",
    hyperdriveDiagram: `HOW OASIS HYPERDRIVE WORKS:

                    Application Request
                          ↓
                ┌─────────────────────┐
                │   HYPERDRIVE CORE   │
                │  • Parallel Query   │
                │  • Auto-Failover    │
                │  • Aggregation      │
                └─────────┬───────────┘
                          ↓
         ┌───────────────────────────────┐
         │   Queries ALL providers       │
         │   SIMULTANEOUSLY (<1s)        │
         └───────────────┬───────────────┘
                         ↓
┌──────────────────────────────────────────────────────────────────────────────────────┐
│                         BLOCKCHAIN PROVIDERS (22+)                                  │
│  Ethereum │ Solana │ Polygon │ Arbitrum │ Base │ Rootstock │ Avalanche │ Fantom   │
│  Cosmos │ Polkadot │ TRON │ Elrond │ Hashgraph │ EOSIO │ Telos │ SEEDS │ TON      │
│  Stellar │ Cardano │ NEAR │ Aptos │ BNB Chain │ Optimism │ Bitcoin                 │
└──────────────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────────────┐
│              DATABASE │ STORAGE │ LEGACY │ ORACLE │ COMPLIANCE │ SOCIAL            │
│  MongoDB │ Neo4j │ SQLite │ SQL Server │ Oracle │ Azure Cosmos │ Google Cloud     │
│  IPFS │ Pinata │ Arweave │ Azure Storage │ AWS S3 │ LocalFile                    │
│  Bank Core │ SWIFT │ FedWire │ Chainlink │ Band Protocol │ Bloomberg API       │
│  Chainalysis │ Elliptic │ TRM Labs │ KYC/AML │ Holochain │ ThreeFold │ SOLID   │
│  ActivityPub │ Scuttlebutt │ Urbit │ PLAN │ HoloWeb                                │
└──────────────────────────────────────────────────────────────────────────────────────┘
                         ↓
                ┌─────────────────────┐
                │  Unified Response   │
                │  (<1 second)        │
                │  • Ownership        │
                │  • Real-time Value  │
                │  • Compliance       │
                └─────────────────────┘

        50+ Providers → Parallel Query → Instant Unified View → T+0 Settlement`,
    capabilities: [
      {
        name: "Real-Time Ownership Tracking",
        icon: "eye",
        description: "HyperDrive aggregates all providers (blockchain + databases + legacy) in <1 second. Instant answer to 'who owns what, when' across ALL systems."
      },
      {
        name: "Instant Settlement (T+0)",
        icon: "lightning",
        description: "Smart contracts execute transfers in 3 minutes vs 2-3 days. Collateral immediately available for reuse = 10-20x daily velocity."
      },
      {
        name: "Cross-Chain Optimization",
        icon: "network",
        description: "50+ blockchain integrations. OASIS intelligently routes collateral to cheapest/fastest chain based on requirements. No manual bridging."
      },
      {
        name: "Automated Compliance",
        icon: "shield",
        description: "Avatar API embeds KYC/AML at identity level. Real-time screening on every transfer. Cost: $0.01-0.50 vs $500-2,000 traditional."
      },
      {
        name: "Real-Time Valuation",
        icon: "trending",
        description: "Multi-oracle integration updates all collateral values instantly. Margin calls detected and resolved in seconds, preventing cascades."
      },
      {
        name: "Legacy Integration",
        icon: "link",
        description: "Bridges blockchain to SWIFT, FedWire, core banking. JP Morgan Onyx ↔ Ethereum ↔ FedWire all synchronized. No system replacement needed."
      },
      {
        name: "Immutable Audit",
        icon: "lock",
        description: "Every transaction recorded on blockchain + IPFS + databases. Court-admissible, tamper-proof. Zero disputes about 'who had priority'."
      }
    ]
  },
  
  scenarioComparison: {
    title: "Real-World Scenario: Bank Faces Margin Call",
    traditional: {
      title: "Without Hyperdrive (March 2023 Crisis)",
      timeline: [
        { time: "11:00 AM", event: "Market drops 5%", status: "warning" },
        { time: "2:00 PM", event: "Counterparty demands $500M", status: "alert" },
        { time: "3:00 PM", event: "Bank scrambling for collateral", status: "alert" },
        { time: "4:00 PM", event: "Collateral found but pledged elsewhere", status: "error" },
        { time: "5:00 PM", event: "Must sell into falling market → Cascade → Failure", status: "error" }
      ]
    },
    oasis: {
      title: "With Hyperdrive",
      timeline: [
        { time: "11:00 AM", event: "Market drops 5%", status: "info" },
        { time: "11:01 AM", event: "Hyperdrive alerts: '$500M needed' + shows available collateral", status: "info" },
        { time: "11:02 AM", event: "Bank posts collateral from optimized pool", status: "success" },
        { time: "11:05 AM", event: "Margin satisfied. Crisis averted.", status: "success" }
      ]
    }
  },
  
  dailyOperations: {
    title: "Bank's Day with Hyperdrive: 3x Collateral Efficiency",
    operations: [
      { time: "8:00 AM", action: "Repo executed", detail: "$500M Treasuries pledged", duration: "3 minutes" },
      { time: "11:00 AM", action: "Volatility spike", detail: "Hyperdrive detects, posts additional collateral automatically", duration: "1 minute" },
      { time: "2:00 PM", action: "First repo matures", detail: "Collateral auto-returned, immediately available", duration: "Instant" },
      { time: "2:15 PM", action: "Same collateral, new swap", detail: "No settlement delay, instant reuse", duration: "3 minutes" },
      { time: "4:00 PM", action: "Third use today", detail: "Same $500M used 3x vs 1x traditional", duration: "3 minutes" }
    ]
  },
  
  keyNumbers: {
    title: "The Numbers",
    individual: {
      title: "Individual Bank Benefits",
      before: [
        "Collateral needed: $1.5B",
        "Settlement: 2-3 days",
        "Reuse velocity: 1x per week",
        "Operational costs: $10M/year",
        "Capital efficiency: 50%"
      ],
      after: [
        "Collateral needed: $750M",
        "Settlement: Instant (T+0)",
        "Reuse velocity: 10-20x per day",
        "Operational costs: $1M/year (90% ↓)",
        "Capital efficiency: 90%"
      ],
      unlocked: "$750M per bank"
    },
    industry: {
      title: "Industry-Wide Impact",
      metrics: [
        "Total locked collateral: $1-1.5 trillion",
        "Hyperdrive efficiency: 30-50% improvement",
        "Capital unlocked: $100-150 billion",
        "Additional lending: +$10-15B annual interest",
        "Trading opportunities: +$5-10B annual profit",
        "Reduced funding costs: +$20-30B annual savings",
        "Total annual value: $35-55 billion"
      ]
    }
  },
  
  technicalCapabilities: [
    {
      name: "HyperDrive Multi-Provider Aggregation",
      description: "Queries Ethereum + MongoDB + IPFS + Bank Core + Oracles simultaneously. Returns unified view in <1 second."
    },
    {
      name: "50+ Blockchain Integrations",
      description: "EthereumOASIS, SolanaOASIS, PolygonOASIS, ArbitrumOASIS, BaseOASIS, and 45+ more. Cross-chain atomic transfers."
    },
    {
      name: "Avatar API Identity Layer",
      description: "Universal KYC/AML embedded. One verification serves all platforms. Real-time sanctions screening via Chainalysis/Elliptic."
    },
    {
      name: "Wyoming Trust Framework",
      description: "Production-ready legal structures. Smart contracts + legal agreements cryptographically linked (Ricardian contracts)."
    },
    {
      name: "Multi-Oracle Valuation",
      description: "Chainlink + Band Protocol + Bloomberg API + Reuters. Consensus valuation updates every block for real-time risk."
    },
    {
      name: "Legacy System Bridges",
      description: "SWIFT MT599 messages, FedWire integration, core banking APIs (Temenos, FIS). Blockchain + traditional finance synchronized."
    }
  ],
  
  comparison: {
    title: "Why Hyperdrive vs. Competitors?",
    competitors: [
      {
        feature: "Real-time ownership across all systems",
        oasis: true,
        blockchain: false,
        tradfi: false,
        details: "HyperDrive only"
      },
      {
        feature: "T+0 settlement",
        oasis: true,
        blockchain: true,
        tradfi: false,
        details: "Smart contracts"
      },
      {
        feature: "Cross-chain optimization",
        oasis: true,
        blockchain: false,
        tradfi: false,
        details: "50+ chains"
      },
      {
        feature: "Legacy integration (SWIFT, FedWire)",
        oasis: true,
        blockchain: false,
        tradfi: true,
        details: "Universal bridge"
      },
      {
        feature: "Embedded compliance",
        oasis: true,
        blockchain: false,
        tradfi: false,
        details: "Avatar API"
      },
      {
        feature: "Real-time valuation",
        oasis: true,
        blockchain: false,
        tradfi: false,
        details: "Multi-oracle"
      },
      {
        feature: "Cost per transaction",
        oasis: "$0.01-0.50",
        blockchain: "$5-50",
        tradfi: "$500-2,000",
        details: "99% reduction"
      }
    ]
  },
  
  codebaseEvidence: [
    "/NextGenSoftware.OASIS.API.Core/Managers/ProviderManager/ - HyperDrive auto-failover",
    "/NextGenSoftware.OASIS.API.Core/Avatar/ - Universal identity with compliance",
    "50+ provider directories - Full Web2/Web3 interoperability",
    "Production deployments on Solana, Ethereum, Arbitrum, Polygon mainnet"
  ],
  
  conclusion: {
    title: "Bottom Line",
    points: [
      "Hyperdrive is the only platform combining all 7 capabilities",
      "$100-150 billion capital unlocked industry-wide",
      "99% cost reduction + 99% time reduction per transaction",
      "Prevents SVB-type collapses through real-time risk management",
      "Production-ready: Deployed on Solana, Ethereum, Arbitrum, Polygon"
    ]
  }
};

function Icon({ name, className = "w-4 h-4" }: { name: string; className?: string }) {
  const common = "inline-block align-[-0.125em]";
  switch (name) {
    case "brief":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M10 4h4a2 2 0 0 1 2 2v2h-8V6a2 2 0 0 1 2-2z"/> 
          <rect x="3" y="8" width="18" height="12" rx="2"/> 
        </svg>
      );
    case "mail":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M3 5.75A1.75 1.75 0 0 1 4.75 4h14.5A1.75 1.75 0 0 1 21 5.75v12.5A1.75 1.75 0 0 1 19.25 20H4.75A1.75 1.75 0 0 1 3 18.25V5.75z" />
          <path d="M4 6l8 6 8-6" />
        </svg>
      );
    case "eye":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
          <circle cx="12" cy="12" r="3" />
        </svg>
      );
    case "lightning":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M13 2L3 14h8l-1 8 10-12h-8l1-8z" />
        </svg>
      );
    case "network":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <circle cx="12" cy="12" r="2" />
          <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83" />
        </svg>
      );
    case "shield":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M12 2L3 7v5c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V7l-9-5z" />
        </svg>
      );
    case "trending":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M23 6l-9.5 9.5-5-5L1 18" />
          <path d="M17 6h6v6" />
        </svg>
      );
    case "link":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
          <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" />
        </svg>
      );
    case "lock":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
          <path d="M7 11V7a5 5 0 0 1 10 0v4" />
        </svg>
      );
    case "check":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M20 6L9 17l-5-5" />
        </svg>
      );
    case "x":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M18 6L6 18M6 6l12 12" />
        </svg>
      );
    case "pen":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M12 20h9" />
          <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z" />
        </svg>
      );
    default:
      return null;
  }
}

function CapabilityCard({ name, icon, description }: { name: string; icon: string; description: string }) {
  return (
    <div className="bg-gradient-to-br from-slate-800/70 to-blue-900/30 border border-cyan-500/30 rounded-lg p-4 print:p-3 hover:shadow-xl hover:shadow-cyan-500/20 hover:border-cyan-400/50 transition-all backdrop-blur-sm">
      <div className="flex items-start gap-2 mb-2">
        <div className="bg-cyan-500/20 p-1.5 rounded-lg">
          <Icon name={icon} className="w-4 h-4 text-cyan-400 flex-shrink-0" />
        </div>
        <h4 className="font-bold text-cyan-100 text-sm print:text-xs leading-tight flex-1">{name}</h4>
      </div>
      <p className="text-xs print:text-[10px] text-cyan-200/80 leading-relaxed">{description}</p>
    </div>
  );
}

function TimelineEvent({ time, event, status, duration }: { time: string; event: string; status: string; duration?: string }) {
  const statusColors = {
    info: "border-blue-400 bg-blue-500/10",
    success: "border-green-400 bg-green-500/10",
    warning: "border-yellow-400 bg-yellow-500/10",
    alert: "border-amber-400 bg-amber-500/10",
    error: "border-blue-400 bg-blue-500/10"
  };
  
  return (
    <div className={`border-l-4 ${statusColors[status as keyof typeof statusColors]} pl-3 py-2 print:py-1.5 mb-2 print:mb-1.5 rounded-r-lg`}>
      <div className="flex items-start gap-2">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-0.5">
            <span className="font-mono text-[10px] text-cyan-300">{time}</span>
            {duration && <span className="text-[10px] text-cyan-400/60">({duration})</span>}
          </div>
          <p className="text-xs print:text-[10px] text-cyan-100">{event}</p>
        </div>
      </div>
    </div>
  );
}

export default function TokenizedCollateralOnePager() {
  const data = collateralData;
  
  const exportPDF = () => {
    window.print();
  };
  
  return (
    <div className="min-h-screen w-full py-8 print:py-0" style={{
      background: 'radial-gradient(circle at 20% 20%, rgba(34, 211, 238, 0.1), transparent 45%), radial-gradient(circle at 80% 10%, rgba(129, 140, 248, 0.12), transparent 40%), radial-gradient(circle at 50% 80%, rgba(56, 189, 248, 0.08), transparent 50%), linear-gradient(180deg, rgba(2, 6, 23, 0.95) 0%, rgba(5, 5, 16, 0.95) 100%)',
      backgroundAttachment: 'fixed'
    }}>
      {/* Export Button */}
      <button
        onClick={exportPDF}
        className="fixed top-4 right-4 z-50 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-600 hover:to-blue-700 text-white px-6 py-3 rounded-lg shadow-2xl font-semibold transition-all print:hidden backdrop-blur-sm border border-cyan-400/30"
      >
        Download PDF
      </button>
      
      <div id="collateral-content" className="mx-auto max-w-6xl bg-[rgba(15,23,42,0.6)] backdrop-blur-xl shadow-2xl print:shadow-none rounded-2xl overflow-hidden print:rounded-none print:max-w-none border-2 border-[rgba(56,189,248,0.2)]">
        {/* Header */}
        <div className="bg-gradient-to-br from-[#050510] via-[#0b1120] to-[#050510] text-[#e2f4ff] p-8 print:p-6 relative overflow-hidden border-b-2 border-[rgba(56,189,248,0.2)]">
          <div className="absolute top-0 right-0 w-[600px] h-[600px] bg-cyan-400/5 rounded-full -mr-64 -mt-64" />
          <div className="absolute bottom-0 left-0 w-96 h-96 bg-blue-500/10 rounded-full -ml-48 -mb-48" />
          <div className="absolute top-1/2 left-1/2 w-64 h-64 bg-purple-500/5 rounded-full -ml-32 -mt-32" />
          
          <div className="relative z-10">
            <div className="inline-block bg-[rgba(45,212,191,0.12)] border border-[rgba(56,189,248,0.2)] px-4 py-1 rounded-full text-xs font-semibold text-[#22d3ee] mb-4">
              THE $100-150B OPPORTUNITY
            </div>
            <h1 className="text-4xl print:text-3xl font-bold leading-tight bg-gradient-to-r from-[#22d3ee] via-[#bfdbfe] to-[#3b82f6] bg-clip-text text-transparent">
              {data.title}
            </h1>
            <p className="text-xl print:text-lg text-[#e2f4ff] mt-2 font-semibold">{data.subtitle}</p>
            <p className="text-xs text-[rgba(148,163,184,0.75)] mt-2">{data.version}</p>
            <p className="text-sm print:text-xs text-[#e2f4ff] mt-4 leading-relaxed max-w-4xl border-l-4 border-[#22d3ee] pl-4 bg-[rgba(45,212,191,0.12)] py-2 rounded-r-lg">
              {data.tagline}
            </p>
            
            <div className="flex flex-wrap gap-3 mt-4 text-xs">
              {data.contact.map((c, i) => (
                <div key={i} className="flex items-center gap-2 bg-gradient-to-r from-cyan-500/20 to-blue-500/20 px-4 py-2 rounded-lg backdrop-blur-sm border border-cyan-400/30">
                  <Icon name={c.icon} className="w-4 h-4" />
                  <span className="font-medium">{c.label}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Main Content */}
        <div className="p-8 print:p-6">
          {/* Problem Statement */}
          <div className="mb-6 print:mb-4">
            <div className="bg-gradient-to-r from-blue-900/30 to-cyan-900/30 border-2 border-blue-500/30 rounded-xl p-6 print:p-4">
              <div className="mb-3">
                <div className="text-xs font-semibold text-blue-300 mb-1">{data.problemStatement.eyebrow}</div>
                <h2 className="text-2xl print:text-xl font-bold text-blue-200">{data.problemStatement.title}</h2>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 print:grid-cols-2 gap-4 print:gap-3">
                {data.problemStatement.points.map((point, i) => (
                  <div key={i} className="bg-slate-900/60 border border-blue-500/20 rounded-lg p-4 print:p-3 hover:border-blue-400/40 transition-all flex flex-col">
                    <h4 className="font-bold text-blue-200 text-xl print:text-sm mb-2">{point.title}</h4>
                    <p className="text-base print:text-xs text-blue-100/80 mb-3 leading-relaxed">{point.description}</p>
                    {point.diagram && (
                      <div className="bg-slate-950/60 border border-blue-400/10 rounded p-3 print:p-2 mb-2 font-mono text-sm print:text-[10px] text-blue-200/80 whitespace-pre-line text-center min-h-[140px] print:min-h-0 flex items-center justify-center">
                        {point.diagram}
                      </div>
                    )}
                    <div className="bg-blue-500/10 border border-blue-400/20 rounded px-3 py-2 print:px-2 print:py-1.5 mt-auto">
                      <p className="text-sm print:text-[10px] text-blue-200/90 font-medium">{point.impact}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Solution */}
          <div className="mb-6 print:mb-4">
            <div className="bg-gradient-to-r from-cyan-900/30 to-blue-900/30 border-2 border-cyan-500/30 rounded-xl p-6 print:p-4">
              <div className="mb-4">
                <div className="text-xs font-semibold text-cyan-300 uppercase tracking-wider mb-1">{data.solution.eyebrow}</div>
                <h2 className="text-2xl print:text-xl font-bold text-cyan-200 mb-2">{data.solution.title}</h2>
                <p className="text-sm print:text-xs text-cyan-300/80 font-medium">{data.solution.subtitle}</p>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 print:gap-3">
                {data.solution.capabilities.map((cap, i) => (
                  <CapabilityCard key={i} {...cap} />
                ))}
              </div>
            </div>
          </div>

          {/* Scenario Comparison */}
          <div className="mb-6 print:mb-4">
            <h2 className="text-2xl print:text-xl font-bold text-cyan-100 mb-4 print:mb-3 text-center">{data.scenarioComparison.title}</h2>
            
            <div className="grid grid-cols-1 lg:grid-cols-2 print:grid-cols-2 gap-4 print:gap-3">
              {/* Traditional */}
              <div className="bg-gradient-to-br from-blue-900/20 to-slate-800/50 border-2 border-blue-500/30 rounded-xl p-4 print:p-3">
                <h3 className="text-lg print:text-base font-bold text-blue-200 mb-4 print:mb-3">{data.scenarioComparison.traditional.title}</h3>
                {data.scenarioComparison.traditional.timeline.map((item, i) => (
                  <TimelineEvent key={i} {...item} />
                ))}
              </div>
              
              {/* With Hyperdrive */}
              <div className="bg-gradient-to-br from-green-900/20 to-slate-800/50 border-2 border-green-500/30 rounded-xl p-4 print:p-3">
                <h3 className="text-lg print:text-base font-bold text-green-200 mb-4 print:mb-3">{data.scenarioComparison.oasis.title}</h3>
                {data.scenarioComparison.oasis.timeline.map((item, i) => (
                  <TimelineEvent key={i} {...item} />
                ))}
              </div>
            </div>
          </div>

          {/* Daily Operations */}
          <div className="mb-6 print:mb-4 bg-gradient-to-br from-blue-900/20 to-slate-800/50 border-2 border-blue-500/30 rounded-xl p-4 print:p-3">
            <h3 className="text-xl print:text-lg font-bold text-blue-200 mb-4 print:mb-3">{data.dailyOperations.title}</h3>
            <div className="space-y-2 print:space-y-1.5">
              {data.dailyOperations.operations.map((op, i) => (
                <TimelineEvent key={i} time={op.time} event={`${op.action}: ${op.detail}`} status="info" duration={op.duration} />
              ))}
            </div>
            <div className="mt-3 bg-blue-500/10 border border-blue-400/30 rounded-lg p-3 print:p-2">
              <p className="text-xs print:text-[10px] text-blue-100 font-semibold">
                Result: Same $500M collateral used 3 times in one day vs once traditionally = 3x efficiency improvement
              </p>
            </div>
          </div>

          {/* Key Numbers */}
          <div className="mb-6 print:mb-4">
            <h2 className="text-2xl print:text-xl font-bold text-cyan-100 mb-4 print:mb-3 text-center">{data.keyNumbers.title}</h2>
            
            <div className="grid grid-cols-1 lg:grid-cols-2 print:grid-cols-2 gap-4 print:gap-3">
              {/* Individual Bank */}
              <div className="bg-gradient-to-br from-purple-900/20 to-slate-800/50 border-2 border-purple-500/30 rounded-xl p-4 print:p-3">
                <h3 className="text-lg print:text-base font-bold text-purple-200 mb-3 print:mb-2">{data.keyNumbers.individual.title}</h3>
                
                <div className="mb-3 print:mb-2">
                  <h4 className="text-xs font-semibold text-purple-300 mb-2 print:mb-1.5">Before Hyperdrive:</h4>
                  <ul className="space-y-1.5 print:space-y-1">
                    {data.keyNumbers.individual.before.map((item, i) => (
                      <li key={i} className="text-xs print:text-[10px] text-purple-100/80 flex items-start gap-2">
                        <Icon name="x" className="w-3 h-3 text-blue-400 mt-0.5 flex-shrink-0" />
                        <span>{item}</span>
                      </li>
                    ))}
                  </ul>
                </div>
                
                <div className="mb-3 print:mb-2">
                  <h4 className="text-xs font-semibold text-purple-300 mb-2 print:mb-1.5">With Hyperdrive:</h4>
                  <ul className="space-y-1.5 print:space-y-1">
                    {data.keyNumbers.individual.after.map((item, i) => (
                      <li key={i} className="text-xs print:text-[10px] text-purple-100/80 flex items-start gap-2">
                        <Icon name="check" className="w-3 h-3 text-green-400 mt-0.5 flex-shrink-0" />
                        <span>{item}</span>
                      </li>
                    ))}
                  </ul>
                </div>
                
                <div className="bg-purple-500/20 border border-purple-400/40 rounded-lg p-3 print:p-2">
                  <p className="text-base print:text-sm font-bold text-purple-100">
                    Capital Unlocked: {data.keyNumbers.individual.unlocked}
                  </p>
            </div>
          </div>

              {/* Industry-Wide */}
              <div className="bg-gradient-to-br from-green-900/20 to-slate-800/50 border-2 border-green-500/30 rounded-xl p-4 print:p-3">
                <h3 className="text-lg print:text-base font-bold text-green-200 mb-3 print:mb-2">{data.keyNumbers.industry.title}</h3>
                <ul className="space-y-1.5 print:space-y-1">
                  {data.keyNumbers.industry.metrics.map((metric, i) => (
                    <li key={i} className="text-xs print:text-[10px] text-green-100/90 flex items-start gap-2 bg-green-500/10 border border-green-400/20 rounded-lg p-2 print:p-1.5">
                      <span className="text-green-400 text-sm">•</span>
                      <span className="font-medium">{metric}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>

          {/* Technical Capabilities */}
          <div className="mb-6 print:mb-4">
            <h2 className="text-xl print:text-lg font-bold text-cyan-100 mb-3 print:mb-2">Technical Capabilities (Codebase-Verified)</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3 print:gap-2">
              {data.technicalCapabilities.map((cap, i) => (
                <div key={i} className="bg-slate-800/60 border border-cyan-500/20 rounded-lg p-3 print:p-2 hover:border-cyan-400/40 transition-all">
                  <h4 className="font-semibold text-cyan-200 text-xs mb-1">{cap.name}</h4>
                  <p className="text-[10px] text-cyan-200/70">{cap.description}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Comparison Table */}
          <div className="mb-6 print:mb-4">
            <h2 className="text-xl print:text-lg font-bold text-cyan-100 mb-3 print:mb-2">{data.comparison.title}</h2>
            <div className="overflow-x-auto">
              <table className="w-full text-xs print:text-[10px]">
                <thead>
                  <tr className="border-b-2 border-cyan-500/30">
                    <th className="text-left p-2 text-cyan-200">Feature</th>
                    <th className="text-center p-2 text-cyan-200">OASIS</th>
                    <th className="text-center p-2 text-cyan-200">Blockchain</th>
                    <th className="text-center p-2 text-cyan-200">TradFi</th>
                  </tr>
                </thead>
                <tbody>
                  {data.comparison.competitors.map((row, i) => (
                    <tr key={i} className="border-b border-cyan-500/10 hover:bg-slate-800/50">
                      <td className="p-2 text-cyan-100/90">{row.feature}</td>
                      <td className="p-2 text-center">
                        {typeof row.oasis === 'boolean' ? (
                          row.oasis ? <Icon name="check" className="w-5 h-5 text-green-400 mx-auto" /> : <Icon name="x" className="w-5 h-5 text-blue-400 mx-auto" />
                        ) : (
                          <span className="text-cyan-100 font-semibold">{row.oasis}</span>
                        )}
                      </td>
                      <td className="p-2 text-center">
                        {typeof row.blockchain === 'boolean' ? (
                          row.blockchain ? <Icon name="check" className="w-5 h-5 text-green-400 mx-auto" /> : <Icon name="x" className="w-5 h-5 text-blue-400 mx-auto" />
                        ) : (
                          <span className="text-cyan-100/70">{row.blockchain}</span>
                        )}
                      </td>
                      <td className="p-2 text-center">
                        {typeof row.tradfi === 'boolean' ? (
                          row.tradfi ? <Icon name="check" className="w-5 h-5 text-green-400 mx-auto" /> : <Icon name="x" className="w-5 h-5 text-blue-400 mx-auto" />
                        ) : (
                          <span className="text-cyan-100/70">{row.tradfi}</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="mt-3 bg-cyan-500/10 border border-cyan-400/30 rounded-lg p-3 print:p-2">
              <p className="text-xs print:text-[10px] text-cyan-100 font-semibold text-center">
                Hyperdrive is the only platform combining all 7 capabilities
              </p>
            </div>
              </div>

          {/* Codebase Evidence */}
          <div className="mb-6 print:mb-4 bg-slate-800/60 border-2 border-cyan-500/30 rounded-xl p-4 print:p-3">
            <h3 className="text-lg print:text-base font-bold text-cyan-100 mb-3 print:mb-2">Codebase Evidence (Production-Ready)</h3>
            <ul className="space-y-1.5 print:space-y-1">
              {data.codebaseEvidence.map((evidence, i) => (
                <li key={i} className="text-xs print:text-[10px] text-cyan-200/80 font-mono flex items-start gap-2">
                  <span className="text-cyan-400">✓</span>
                  <span>{evidence}</span>
                </li>
              ))}
            </ul>
              </div>

          {/* Conclusion */}
          <div className="bg-gradient-to-br from-cyan-900/30 to-blue-900/30 border-2 border-cyan-400/40 rounded-xl p-4 print:p-3">
            <h2 className="text-2xl print:text-xl font-bold text-cyan-100 mb-4 print:mb-3 text-center">{data.conclusion.title}</h2>
            <div className="space-y-2 print:space-y-1.5">
              {data.conclusion.points.map((point, i) => (
                <div key={i} className="flex items-start gap-2 bg-slate-900/50 border border-cyan-500/20 rounded-lg p-3 print:p-2">
                  <span className="text-cyan-400 text-lg">✓</span>
                  <p className="text-sm print:text-xs text-cyan-100/90 font-semibold">{point}</p>
              </div>
              ))}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="bg-gradient-to-r from-slate-900 via-blue-900/60 to-slate-900 border-t-2 border-cyan-500/30 px-8 py-4 print:px-6 print:py-3">
          <div className="flex flex-col md:flex-row justify-between items-center gap-3">
            <div>
              <p className="text-base print:text-sm text-cyan-100 font-bold">
                OASIS Platform
              </p>
              <p className="text-xs print:text-[10px] text-cyan-200/70">
                Universal Financial Infrastructure for Web4/Web5
              </p>
            </div>
            <div className="text-right">
              <p className="text-xs print:text-[10px] text-cyan-300/80">Tokenized Collateral Solution v1.0</p>
              <p className="text-[10px] text-cyan-300/60 mt-0.5">October 24, 2025</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
