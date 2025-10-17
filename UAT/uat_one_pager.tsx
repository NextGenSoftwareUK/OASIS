import React from "react";
import { jsPDF } from "jspdf";

// Universal Asset Token (UAT) One-Pager
// Print-ready: A4-ish container, professional styling

const uatData = {
  title: "Universal Asset Token (UAT)",
  subtitle: "Cross-Chain Real World Asset Tokenization Standard",
  version: "v1.0 | October 17, 2025",
  tagline: "A modular, extensible token standard for fractional ownership of real-world assets across Ethereum, Solana, and Radix blockchains.",
  
  contact: [
    { label: "AssetRail - Quantum Securities Platform", icon: "brief" },
    { label: "dev@quantumsecurities.com", icon: "mail" },
    { label: "docs.quantumsecurities.com/uat", icon: "map" },
  ],
  
  problemStatement: {
    title: "The Problem",
    points: [
      "Inconsistent metadata across blockchains (Ethereum, Solana, Radix)",
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
      icon: "🏗️",
      description: "Required fields including token type, asset class, issuer info, blockchain details, and media assets. Standard UAT-1.0 schema with versioning."
    },
    {
      name: "Asset Details",
      icon: "🏢",
      description: "Physical property characteristics, valuation (professional + AI), ownership history, condition reports, and comparable sales data."
    },
    {
      name: "Trust Structure",
      icon: "⚖️",
      description: "Wyoming Statutory Trust integration with settlor, trustee, and beneficiary roles. Governance rules, voting thresholds, and duration terms."
    },
    {
      name: "Yield Distribution",
      icon: "💰",
      description: "Income sources, expense tracking, net income calculations, distribution schedules, waterfall logic, and projected returns (5-10 year horizon)."
    },
    {
      name: "Legal Documents",
      icon: "📄",
      description: "IPFS-stored trust agreements, title deeds, appraisals, insurance policies, PPMs, and subscription agreements with cryptographic hashes."
    },
    {
      name: "Compliance",
      icon: "✅",
      description: "KYC/AML integration, accreditation verification, transfer restrictions, whitelist/blacklist, tax reporting (Schedule K-1), and GDPR/CCPA compliance."
    },
    {
      name: "Insurance",
      icon: "🛡️",
      description: "Property insurance, liability coverage, title insurance, umbrella policies, and claims history tracking."
    },
    {
      name: "Valuation",
      icon: "📊",
      description: "Professional appraisals, AI-powered valuations with confidence scores, comparable properties, market trends, and price-per-token calculations."
    },
    {
      name: "Governance (Optional)",
      icon: "🗳️",
      description: "On-chain voting rights, proposal types (property sale, renovation, refinancing), quorum requirements, and decision thresholds."
    }
  ],
  
  blockchainSupport: [
    {
      chain: "Ethereum",
      standard: "ERC-721 / ERC-1155",
      features: "Compliance mappings, whitelist/accredited addresses, transfer restrictions"
    },
    {
      chain: "Solana",
      standard: "SPL Token + Metaplex",
      features: "Anchor program, module flags, compliance checks on mint/transfer"
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
      description: "Deploy once, work everywhere. Consistent metadata across Ethereum, Solana, and Radix."
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
    "🏠 Fractional Real Estate - $1.89M Beverly Hills property tokenized into 3,500 tokens at $540 each",
    "🎨 Art & Collectibles - High-value artwork with provenance tracking and insurance",
    "🏭 Commercial Property - Income-generating assets with quarterly distributions",
    "💎 Commodities - Gold, silver, rare earth elements with physical storage verification",
    "🎼 Intellectual Property - Music catalogs, patents, licensing rights with royalty streams"
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
    default:
      return null;
  }
}

function ModuleCard({ name, icon, description }: { name: string; icon: string; description: string }) {
  return (
    <div className="bg-gradient-to-br from-slate-50 to-white border border-slate-200 rounded-lg p-4 hover:shadow-md transition-shadow">
      <div className="flex items-start gap-3">
        <span className="text-2xl">{icon}</span>
        <div>
          <h4 className="font-semibold text-slate-800 text-sm">{name}</h4>
          <p className="text-xs text-slate-600 mt-1 leading-relaxed">{description}</p>
        </div>
      </div>
    </div>
  );
}

function BenefitItem({ title, description }: { title: string; description: string }) {
  return (
    <div className="flex items-start gap-3 mb-3">
      <span className="text-emerald-600 font-bold text-lg mt-0.5">✓</span>
      <div>
        <h4 className="font-semibold text-slate-800 text-sm">{title}</h4>
        <p className="text-xs text-slate-600 mt-0.5">{description}</p>
      </div>
    </div>
  );
}

export default function UATOnePager() {
  const uat = uatData;
  
  const exportPDF = () => {
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4',
      compress: true
    });
    let yPos = 15;
    
    // Header
    doc.setFontSize(20);
    doc.setFont('helvetica', 'bold');
    doc.text(uat.title, 105, yPos, { align: 'center' });
    yPos += 7;
    
    doc.setFontSize(12);
    doc.setFont('helvetica', 'normal');
    doc.text(uat.subtitle, 105, yPos, { align: 'center' });
    yPos += 5;
    
    doc.setFontSize(9);
    doc.text(uat.version, 105, yPos, { align: 'center' });
    yPos += 8;
    
    // Tagline
    doc.setFontSize(10);
    const taglineLines = doc.splitTextToSize(uat.tagline, 180);
    doc.text(taglineLines, 105, yPos, { align: 'center' });
    yPos += taglineLines.length * 4.5 + 5;
    
    // Problem Statement
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('THE PROBLEM', 15, yPos);
    yPos += 5;
    
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    uat.problemStatement.points.forEach((point) => {
      const lines = doc.splitTextToSize(`• ${point}`, 180);
      doc.text(lines, 15, yPos);
      yPos += lines.length * 4;
    });
    yPos += 3;
    
    // Solution
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('THE SOLUTION', 15, yPos);
    yPos += 5;
    
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    uat.solution.features.forEach((feature) => {
      const lines = doc.splitTextToSize(`✓ ${feature}`, 180);
      doc.text(lines, 15, yPos);
      yPos += lines.length * 4;
    });
    yPos += 3;
    
    // Core Modules
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('CORE MODULES (9 TOTAL)', 15, yPos);
    yPos += 5;
    
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    uat.coreModules.slice(0, 6).forEach((module) => {
      doc.setFont('helvetica', 'bold');
      doc.text(`${module.icon} ${module.name}`, 15, yPos);
      yPos += 4;
      doc.setFont('helvetica', 'normal');
      const descLines = doc.splitTextToSize(module.description, 180);
      doc.text(descLines, 15, yPos);
      yPos += descLines.length * 3.5 + 2;
    });
    
    // New Page
    doc.addPage();
    yPos = 15;
    
    // Continue Modules
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('CORE MODULES (CONTINUED)', 15, yPos);
    yPos += 5;
    
    doc.setFontSize(9);
    uat.coreModules.slice(6).forEach((module) => {
      doc.setFont('helvetica', 'bold');
      doc.text(`${module.icon} ${module.name}`, 15, yPos);
      yPos += 4;
      doc.setFont('helvetica', 'normal');
      const descLines = doc.splitTextToSize(module.description, 180);
      doc.text(descLines, 15, yPos);
      yPos += descLines.length * 3.5 + 2;
    });
    yPos += 2;
    
    // Blockchain Support
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('MULTI-CHAIN SUPPORT', 15, yPos);
    yPos += 5;
    
    doc.setFontSize(9);
    uat.blockchainSupport.forEach((chain) => {
      doc.setFont('helvetica', 'bold');
      doc.text(`${chain.chain} (${chain.standard})`, 15, yPos);
      yPos += 4;
      doc.setFont('helvetica', 'normal');
      const featLines = doc.splitTextToSize(chain.features, 180);
      doc.text(featLines, 15, yPos);
      yPos += featLines.length * 3.5 + 2;
    });
    yPos += 2;
    
    // Key Benefits
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('KEY BENEFITS', 15, yPos);
    yPos += 5;
    
    doc.setFontSize(9);
    uat.keyBenefits.forEach((benefit) => {
      doc.setFont('helvetica', 'bold');
      doc.text(`✓ ${benefit.title}`, 15, yPos);
      yPos += 4;
      doc.setFont('helvetica', 'normal');
      const descLines = doc.splitTextToSize(benefit.description, 180);
      doc.text(descLines, 15, yPos);
      yPos += descLines.length * 3.5 + 2;
    });
    yPos += 2;
    
    // Use Cases
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('USE CASES', 15, yPos);
    yPos += 5;
    
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    uat.useCases.forEach((useCase) => {
      const lines = doc.splitTextToSize(useCase, 180);
      doc.text(lines, 15, yPos);
      yPos += lines.length * 4;
    });
    yPos += 3;
    
    // Contact
    doc.setFontSize(10);
    doc.setFont('helvetica', 'bold');
    doc.text('CONTACT & DOCUMENTATION', 15, yPos);
    yPos += 5;
    
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    uat.contact.forEach((c) => {
      doc.text(c.label, 15, yPos);
      yPos += 4;
    });
    
    doc.save('Universal_Asset_Token_OnePager.pdf');
  };
  
  return (
    <div className="min-h-screen w-full bg-gradient-to-br from-slate-50 to-slate-100 py-8 print:bg-white print:py-0">
      {/* Export Button */}
      <button
        onClick={exportPDF}
        className="fixed top-4 right-4 z-50 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-700 hover:to-teal-700 text-white px-6 py-3 rounded-lg shadow-lg font-semibold transition-all print:hidden"
      >
        📄 Download PDF One-Pager
      </button>
      
      <div className="mx-auto max-w-6xl bg-white shadow-xl print:shadow-none rounded-2xl overflow-hidden print:rounded-none print:max-w-none">
        {/* Header */}
        <div className="bg-gradient-to-br from-slate-800 via-slate-700 to-slate-600 text-white p-10 relative overflow-hidden">
          <div className="absolute top-0 right-0 w-96 h-96 bg-white/5 rounded-full -mr-48 -mt-48" />
          <div className="absolute bottom-0 left-0 w-64 h-64 bg-emerald-500/10 rounded-full -ml-32 -mb-32" />
          <div className="relative z-10">
            <h1 className="text-4xl font-bold leading-tight">{uat.title}</h1>
            <p className="text-xl text-white/90 mt-2">{uat.subtitle}</p>
            <p className="text-sm text-white/60 mt-3">{uat.version}</p>
            <p className="text-sm text-white/80 mt-6 leading-relaxed max-w-3xl">{uat.tagline}</p>
            
            <div className="flex flex-wrap gap-4 mt-6 text-xs">
              {uat.contact.map((c, i) => (
                <div key={i} className="flex items-center gap-2 bg-white/10 px-3 py-2 rounded-lg backdrop-blur-sm">
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
              <h2 className="text-2xl font-bold text-slate-800 mb-4 flex items-center gap-2">
                <span className="text-rose-600">⚠️</span> The Problem
              </h2>
              <ul className="space-y-2">
                {uat.problemStatement.points.map((point, i) => (
                  <li key={i} className="text-sm text-slate-700 flex items-start gap-2">
                    <span className="text-rose-500 mt-1">•</span>
                    <span>{point}</span>
                  </li>
                ))}
              </ul>
            </div>
            
            {/* Solution */}
            <div>
              <h2 className="text-2xl font-bold text-slate-800 mb-4 flex items-center gap-2">
                <span className="text-emerald-600">✓</span> The Solution
              </h2>
              <p className="text-sm text-slate-700 mb-3">{uat.solution.description}</p>
              <ul className="space-y-2">
                {uat.solution.features.map((feature, i) => (
                  <li key={i} className="text-sm text-slate-700 flex items-start gap-2">
                    <span className="text-emerald-600 mt-1">✓</span>
                    <span>{feature}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          {/* Core Modules */}
          <div className="mb-10">
            <h2 className="text-2xl font-bold text-slate-800 mb-6">Core Modules (9 Total)</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {uat.coreModules.map((module, i) => (
                <ModuleCard key={i} {...module} />
              ))}
            </div>
          </div>

          {/* Blockchain Support */}
          <div className="mb-10">
            <h2 className="text-2xl font-bold text-slate-800 mb-6">Multi-Chain Support</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {uat.blockchainSupport.map((chain, i) => (
                <div key={i} className="border-l-4 border-emerald-500 pl-4">
                  <h3 className="font-bold text-slate-800">{chain.chain}</h3>
                  <p className="text-xs text-slate-600 mt-1">{chain.standard}</p>
                  <p className="text-xs text-slate-700 mt-2">{chain.features}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Key Benefits */}
          <div className="mb-10">
            <h2 className="text-2xl font-bold text-slate-800 mb-6">Key Benefits</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {uat.keyBenefits.map((benefit, i) => (
                <BenefitItem key={i} {...benefit} />
              ))}
            </div>
          </div>

          {/* Use Cases */}
          <div className="mb-10">
            <h2 className="text-2xl font-bold text-slate-800 mb-6">Real-World Use Cases</h2>
            <div className="space-y-3">
              {uat.useCases.map((useCase, i) => (
                <div key={i} className="bg-gradient-to-r from-emerald-50 to-teal-50 border border-emerald-200 rounded-lg p-4">
                  <p className="text-sm text-slate-800">{useCase}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Technical Stack */}
          <div className="bg-slate-50 border border-slate-200 rounded-xl p-6">
            <h2 className="text-xl font-bold text-slate-800 mb-4">Technical Architecture</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 text-sm">
              <div>
                <h4 className="font-semibold text-slate-700 mb-2">Storage</h4>
                <p className="text-xs text-slate-600">{uat.technicalStack.storage}</p>
              </div>
              <div>
                <h4 className="font-semibold text-slate-700 mb-2">Metadata Format</h4>
                <p className="text-xs text-slate-600">{uat.technicalStack.metadata}</p>
              </div>
              <div>
                <h4 className="font-semibold text-slate-700 mb-2">Deployment</h4>
                <p className="text-xs text-slate-600">{uat.technicalStack.deployment}</p>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="bg-gradient-to-r from-slate-100 to-slate-50 border-t border-slate-200 px-10 py-6">
          <div className="flex flex-col md:flex-row justify-between items-center gap-4">
            <p className="text-sm text-slate-600">
              <span className="font-semibold">AssetRail Quantum Securities Platform</span> • Universal Asset Token Specification v1.0
            </p>
            <div className="flex gap-4 text-xs text-slate-500">
              <span>docs.quantumsecurities.com/uat</span>
              <span>•</span>
              <span>dev@quantumsecurities.com</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

