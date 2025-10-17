import React from "react";

// Universal Asset Token (UAT) One-Pager
// Print-ready: A4-ish container, professional styling

const uatData = {
  title: "Universal Asset Token (UAT)",
  subtitle: "Cross-Chain Real World Asset Tokenization Standard",
  version: "v1.0 | October 17, 2025",
  tagline: "A modular, extensible token standard for fractional ownership of real-world assets across Solana, Ethereum, and Radix blockchains.",
  
  contact: [
    { label: "AssetRail", icon: "brief" },
    { label: "@maxgershfield", icon: "mail" },
  ],
  
  problemStatement: {
    title: "The Problem",
    points: [
      "Inconsistent metadata across blockchains (Solana, Ethereum, Radix)",
      "Limited extensibility for different asset types (real estate, art, IP, commodities)",
      "No standardized trust/legal wrapper integration",
      "Fragmented yield distribution tracking",
      "Compliance gaps for regulatory requirements (KYC, accreditation, jurisdiction)"
    ]
  },
  
  solution: {
    title: "The Solution",
    description: "A modular, extensible metadata standard that:",
    features: [
      "Works across all supported blockchains",
      "Supports multiple asset classes",
      "Embeds trust/legal structures (Wyoming Statutory Trusts)",
      "Tracks yield and distributions",
      "Enables compliance and regulatory integration",
      "Compatible with existing NFT standards (ERC-721, ERC-1155, Metaplex, Radix)"
    ]
  },
  
  coreModules: [
    {
      name: "Core Metadata",
      icon: "database",
      description: "Required fields including token type, asset class, issuer info, blockchain details, and media assets. Standard UAT-1.0 schema with versioning."
    },
    {
      name: "Asset Details",
      icon: "building",
      description: "Physical property characteristics, valuation (professional + AI), ownership history, condition reports, and comparable sales data."
    },
    {
      name: "Trust Structure",
      icon: "shield",
      description: "Wyoming Statutory Trust integration with settlor, trustee, and beneficiary roles. Governance rules, voting thresholds, and duration terms."
    },
    {
      name: "Yield Distribution",
      icon: "chart",
      description: "Income sources, expense tracking, net income calculations, distribution schedules, waterfall logic, and projected returns (5-10 year horizon)."
    },
    {
      name: "Legal Documents",
      icon: "document",
      description: "IPFS-stored trust agreements, title deeds, appraisals, insurance policies, PPMs, and subscription agreements with cryptographic hashes."
    },
    {
      name: "Compliance",
      icon: "check-circle",
      description: "KYC/AML integration, accreditation verification, transfer restrictions, whitelist/blacklist, tax reporting (Schedule K-1), and GDPR/CCPA compliance."
    },
    {
      name: "Insurance",
      icon: "umbrella",
      description: "Property insurance, liability coverage, title insurance, umbrella policies, and claims history tracking."
    },
    {
      name: "Valuation",
      icon: "trending",
      description: "Professional appraisals, AI-powered valuations with confidence scores, comparable properties, market trends, and price-per-token calculations."
    },
    {
      name: "Governance (Optional)",
      icon: "users",
      description: "On-chain voting rights, proposal types (property sale, renovation, refinancing), quorum requirements, and decision thresholds."
    }
  ],
  
  blockchainSupport: [
    {
      chain: "Solana",
      standard: "SPL Token + Metaplex",
      features: "Anchor program, module flags, compliance checks on mint/transfer"
    },
    {
      chain: "Ethereum",
      standard: "ERC-721 / ERC-1155",
      features: "Compliance mappings, whitelist/accredited addresses, transfer restrictions"
    },
    {
      chain: "Radix",
      standard: "Component/Blueprint",
      features: "Native fungible tokens, compliance hashmaps, vault management"
    }
  ],
  
  keyBenefits: [
    {
      title: "Multi-Chain Native",
      description: "Deploy once, work everywhere. Consistent metadata across Solana, Ethereum, and Radix."
    },
    {
      title: "Regulatory Ready",
      description: "Built-in compliance for Reg D 506(c), accredited investor verification, and transfer restrictions."
    },
    {
      title: "Institutional Grade",
      description: "Wyoming Trust structures, professional appraisals, insurance tracking, and audit trails."
    },
    {
      title: "Yield Transparent",
      description: "Automated income tracking, expense management, and distribution waterfalls with 10-year projections."
    },
    {
      title: "Modular Architecture",
      description: "Enable only the modules you need. Extend with custom fields without breaking compatibility."
    },
    {
      title: "Future-Proof",
      description: "Versioned schema, extensible design, and IPFS/Arweave storage for immutability."
    }
  ],
  
  useCases: [
    "Fractional Real Estate - $1.89M Beverly Hills property tokenized into 3,500 tokens at $540 each",
    "Art & Collectibles - High-value artwork with provenance tracking and insurance",
    "Commercial Property - Income-generating assets with quarterly distributions",
    "Commodities - Gold, silver, rare earth elements with physical storage verification",
    "Intellectual Property - Music catalogs, patents, licensing rights with royalty streams"
  ],
  
  technicalStack: {
    storage: "IPFS (Pinata) primary, Arweave backup, minimal on-chain",
    metadata: "JSON with UAT-1.0 schema, content-addressed via CID",
    deployment: "AssetRail Smart Contract Generator with template system"
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
    case "map":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M9 20l-6-2V4l6 2 6-2 6 2v14l-6-2-6 2z" />
          <path d="M9 6v14M15 4v14" />
        </svg>
      );
    case "database":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <ellipse cx="12" cy="5" rx="9" ry="3" />
          <path d="M3 5v14c0 1.66 4.03 3 9 3s9-1.34 9-3V5" />
          <path d="M3 12c0 1.66 4.03 3 9 3s9-1.34 9-3" />
        </svg>
      );
    case "building":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <rect x="4" y="2" width="16" height="20" rx="2" />
          <path d="M9 22v-4h6v4M8 6h.01M16 6h.01M8 10h.01M16 10h.01M8 14h.01M16 14h.01M12 6h.01M12 10h.01M12 14h.01" />
        </svg>
      );
    case "shield":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M12 2L3 7v5c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V7l-9-5z" />
        </svg>
      );
    case "chart":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M3 3v18h18" />
          <path d="M18 17V9M13 17V5M8 17v-3" />
        </svg>
      );
    case "document":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6z" />
          <path d="M14 2v6h6M16 13H8M16 17H8M10 9H8" />
        </svg>
      );
    case "check-circle":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <circle cx="12" cy="12" r="10" />
          <path d="M9 12l2 2 4-4" />
        </svg>
      );
    case "umbrella":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M23 12a11.05 11.05 0 0 0-22 0zm-5 7a3 3 0 0 1-6 0v-7" />
        </svg>
      );
    case "trending":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M23 6l-9.5 9.5-5-5L1 18" />
          <path d="M17 6h6v6" />
        </svg>
      );
    case "users":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
          <circle cx="9" cy="7" r="4" />
          <path d="M23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" />
        </svg>
      );
    default:
      return null;
  }
}

function ModuleCard({ name, icon, description }: { name: string; icon: string; description: string }) {
  return (
    <div className="bg-slate-800/50 border border-cyan-500/20 rounded-lg p-4 hover:shadow-lg hover:shadow-cyan-500/10 hover:border-cyan-500/40 transition-all backdrop-blur-sm">
      <div>
        <div className="flex items-center gap-2 mb-3">
          <Icon name={icon} className="w-4 h-4 text-cyan-400 flex-shrink-0" />
          <h4 className="font-semibold text-cyan-100 text-sm inline-block pb-2 border-b-2 border-cyan-400">{name}</h4>
        </div>
        <p className="text-xs text-cyan-200/70 leading-relaxed">{description}</p>
      </div>
    </div>
  );
}

function BenefitItem({ title, description }: { title: string; description: string }) {
  return (
    <div className="flex items-start gap-3 mb-3">
      <span className="text-cyan-400 font-bold text-lg mt-0.5">•</span>
      <div>
        <h4 className="font-semibold text-cyan-100 text-sm">{title}</h4>
        <p className="text-xs text-cyan-200/70 mt-0.5">{description}</p>
      </div>
    </div>
  );
}

export default function UATOnePager() {
  const uat = uatData;
  
  const exportPDF = () => {
    // Use browser's native print function which fully supports modern CSS
    window.print();
  };
  
  return (
    <div className="min-h-screen w-full py-8 print:py-0">
      {/* Export Button */}
      <button
        onClick={exportPDF}
        className="fixed top-4 right-4 z-50 bg-gradient-to-r from-cyan-500 to-blue-500 hover:from-cyan-600 hover:to-blue-600 text-white px-6 py-3 rounded-lg shadow-lg font-semibold transition-all print:hidden backdrop-blur-sm"
      >
        Download PDF One-Pager
      </button>
      
      <div id="uat-content" className="mx-auto max-w-6xl bg-slate-900/80 backdrop-blur-md shadow-xl print:shadow-none rounded-2xl overflow-hidden print:rounded-none print:max-w-none border border-cyan-500/20">
        {/* Header */}
        <div className="bg-gradient-to-br from-slate-900 via-blue-900/50 to-slate-900 text-cyan-50 p-10 relative overflow-hidden border-b border-cyan-500/20">
          <div className="absolute top-0 right-0 w-96 h-96 bg-cyan-400/5 rounded-full -mr-48 -mt-48" />
          <div className="absolute bottom-0 left-0 w-64 h-64 bg-blue-500/10 rounded-full -ml-32 -mb-32" />
          <div className="relative z-10">
            <h1 className="text-4xl font-bold leading-tight">{uat.title}</h1>
            <p className="text-xl text-white/90 mt-2">{uat.subtitle}</p>
            <p className="text-sm text-white/60 mt-3">{uat.version}</p>
            <p className="text-sm text-white/80 mt-6 leading-relaxed max-w-3xl">{uat.tagline}</p>
            
            <div className="flex flex-wrap gap-4 mt-6 text-xs">
              {uat.contact.map((c, i) => (
                <div key={i} className="flex items-center gap-2 bg-cyan-500/10 px-3 py-2 rounded-lg backdrop-blur-sm border border-cyan-500/20">
                  <Icon name={c.icon} className="w-3 h-3" />
                  <span>{c.label}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Main Content */}
        <div className="p-10">
          {/* Problem & Solution Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8 mb-10">
            {/* Problem */}
            <div>
              <h2 className="text-2xl font-bold text-cyan-100 mb-4">The Problem</h2>
              <ul className="space-y-2">
                {uat.problemStatement.points.map((point, i) => (
                  <li key={i} className="text-sm text-cyan-100/80 flex items-start gap-2">
                    <span className="text-rose-400 mt-1">•</span>
                    <span>{point}</span>
                  </li>
                ))}
              </ul>
            </div>
            
            {/* Solution */}
            <div>
              <h2 className="text-2xl font-bold text-cyan-100 mb-4">The Solution</h2>
              <p className="text-sm text-cyan-100/80 mb-3">{uat.solution.description}</p>
              <ul className="space-y-2">
                {uat.solution.features.map((feature, i) => (
                  <li key={i} className="text-sm text-cyan-100/80 flex items-start gap-2">
                    <span className="text-cyan-400 mt-1">•</span>
                    <span>{feature}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          {/* Core Modules */}
          <div className="mb-10">
            <h2 className="text-2xl font-bold text-cyan-100 mb-6">Core Modules (9 Total)</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {uat.coreModules.map((module, i) => (
                <ModuleCard key={i} {...module} />
              ))}
            </div>
          </div>

          {/* Blockchain Support */}
          <div className="mb-10">
            <h2 className="text-2xl font-bold text-cyan-100 mb-6">Multi-Chain Support</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {uat.blockchainSupport.map((chain, i) => (
                <div key={i} className="border-l-4 border-cyan-400 pl-4 bg-slate-800/30 p-4 rounded-r-lg">
                  <h3 className="font-bold text-cyan-100">{chain.chain}</h3>
                  <p className="text-xs text-cyan-300/70 mt-1">{chain.standard}</p>
                  <p className="text-xs text-cyan-200/70 mt-2">{chain.features}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Key Benefits */}
          <div className="mb-10">
            <h2 className="text-2xl font-bold text-cyan-100 mb-6">Key Benefits</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {uat.keyBenefits.map((benefit, i) => (
                <BenefitItem key={i} {...benefit} />
              ))}
            </div>
          </div>

          {/* Use Cases */}
          <div className="mb-10">
            <h2 className="text-2xl font-bold text-cyan-100 mb-6">Real-World Use Cases</h2>
            <div className="space-y-3">
              {uat.useCases.map((useCase, i) => (
                <div key={i} className="bg-gradient-to-r from-slate-800/60 to-blue-900/40 border border-cyan-500/20 rounded-lg p-4 backdrop-blur-sm">
                  <p className="text-sm text-cyan-100/90">{useCase}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Technical Stack */}
          <div className="bg-slate-800/40 border border-cyan-500/20 rounded-xl p-6 backdrop-blur-sm">
            <h2 className="text-xl font-bold text-cyan-100 mb-4">Technical Architecture</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 text-sm">
              <div>
                <h4 className="font-semibold text-cyan-200 mb-2">Storage</h4>
                <p className="text-xs text-cyan-200/70">{uat.technicalStack.storage}</p>
              </div>
              <div>
                <h4 className="font-semibold text-cyan-200 mb-2">Metadata Format</h4>
                <p className="text-xs text-cyan-200/70">{uat.technicalStack.metadata}</p>
              </div>
              <div>
                <h4 className="font-semibold text-cyan-200 mb-2">Deployment</h4>
                <p className="text-xs text-cyan-200/70">{uat.technicalStack.deployment}</p>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="bg-gradient-to-r from-slate-900 to-blue-900/50 border-t border-cyan-500/20 px-10 py-6">
          <div className="flex flex-col md:flex-row justify-between items-center gap-4">
            <p className="text-sm text-cyan-200/80">
              <span className="font-semibold text-cyan-100">AssetRail</span> • Universal Asset Token Specification v1.0
            </p>
            <div className="flex gap-4 text-xs text-cyan-300/70">
              <span>@maxgershfield</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

