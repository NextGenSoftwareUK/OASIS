import React from "react";

// Reform UK x OASIS One-Pager Component
// Tailwind required. Drop this into a Vite/Next/CRA app with Tailwind configured.

const onePager = {
  title: "OASIS × Reform UK",
  subtitle: "Blockchain for British Sovereignty",
  tagline: "Digital Infrastructure to Deliver Reform UK's Contract with You",
  date: "November 2025",
  
  vision: "Position Reform UK as the pro-innovation, pro-freedom, blockchain-forward party that delivers on its £150 billion savings promise while protecting citizens from CBDC overreach and centralized control.",

  corePledges: [
    {
      number: "1",
      icon: "🛂",
      title: "Smart Immigration",
      reformGoal: "Freeze non-essential immigration, stop the boats",
      oasisSolution: "Sovereign digital identity with biometric blockchain",
      implementation: [
        "Tamper-proof visa records",
        "Automated employer tax (20% NI)",
        "Real-time cross-agency sharing",
        "100-day pilot → nationwide in 12 months"
      ],
      savings: "£5-11bn/year"
    },
    {
      number: "2",
      icon: "🚢",
      title: "Stop the Boats",
      reformGoal: "Zero illegal immigrant resettlement",
      oasisSolution: "Cross-jurisdiction border intelligence",
      implementation: [
        "Immutable detention records",
        "Instant criminal checks",
        "Automated case processing",
        "France coordination via blockchain"
      ],
      savings: "£3-5bn/year"
    },
    {
      number: "3",
      icon: "🏥",
      title: "Zero NHS Waiting Lists",
      reformGoal: "Free at point of delivery, cut back-office waste",
      oasisSolution: "Patient-owned records + AI optimization",
      implementation: [
        "Decentralized patient records",
        "Real-time resource tracking",
        "Blockchain prescriptions",
        "Smart contract tax relief for staff"
      ],
      savings: "£25-42bn/year",
      highlight: "(exceeds £17bn investment)"
    },
    {
      number: "4",
      icon: "💰",
      title: "Good Wages",
      reformGoal: "£20k tax threshold, save workers £1,500/year",
      oasisSolution: "Automated tax via smart contracts",
      implementation: [
        "Instant threshold adjustments",
        "Real-time HMRC processing",
        "Zero admin overhead",
        "Fraud-proof verification"
      ],
      savings: "£70bn tax relief"
    },
    {
      number: "5",
      icon: "⚡",
      title: "Affordable Energy",
      reformGoal: "Scrap Net Zero, save £500/household",
      oasisSolution: "Tokenized energy + P2P trading",
      implementation: [
        "Blockchain energy marketplace",
        "Peer-to-peer trading",
        "Smart meter integration",
        "Transparent pricing"
      ],
      savings: "£30bn/year"
    }
  ],

  savingsBreakdown: [
    { area: "Government Waste", reformTarget: "£50bn", oasisDelivered: "£50-70bn", status: "✅" },
    { area: "BoE QE Interest", reformTarget: "£35bn", oasisDelivered: "£35bn", status: "✅" },
    { area: "Energy (Net Zero)", reformTarget: "£30bn", oasisDelivered: "£30bn", status: "✅" },
    { area: "Immigration", reformTarget: "£5bn", oasisDelivered: "£5-11bn", status: "✅" },
    { area: "Employer Immigration Tax", reformTarget: "£4bn", oasisDelivered: "£4bn", status: "✅" },
    { area: "NHS (net savings)", reformTarget: "—", oasisDelivered: "£25-42bn", status: "🚀" },
  ],

  cbdcOpposition: {
    title: "CBDC Opposition: Reform UK's Winning Issue",
    quote: "Reform UK opposes the Creation of a Central Bank Digital Currency (CBDC). We oppose a cashless society.",
    solutions: [
      "Multi-chain infrastructure (15+ blockchains)",
      "Self-custody wallets (citizen control)",
      "Zero-knowledge proofs (privacy)",
      "Cash-compatible system",
      "Censorship-resistant finance"
    ]
  },

  roadmap: [
    {
      phase: "Phase 1: First 100 Days",
      timeline: "Q1 2025",
      items: [
        "CBDC opposition strategy launch",
        "Government transparency pilot (3 departments)",
        "Digital identity pilot (3 border points)",
        "100 employer immigration tax integration",
        "3 hospital NHS pilot",
        "Demonstrate £500m-£1bn annual savings"
      ]
    },
    {
      phase: "Phase 2: 6-Month Scale-Up",
      timeline: "Q2-Q3 2025",
      items: [
        "All UK border points blockchain-enabled",
        "5 NHS regions with patient records",
        "Major contracts via blockchain procurement",
        "Energy trading pilot operational",
        "Demonstrate £10-30bn annual savings"
      ]
    },
    {
      phase: "Phase 3: Nationwide Deployment",
      timeline: "Year 1-2",
      items: [
        "100% government spending transparent",
        "Nationwide digital identity system",
        "NHS zero waiting lists achieved",
        "Energy tokenization nationwide",
        "£120-170bn annual savings delivered"
      ]
    }
  ],

  roi: {
    pilotBudget: "£10-20m",
    pilotReturn: "£500m-£1bn/year",
    pilotROI: "25-50x",
    fullBudget: "£100-200m",
    fullReturn: "£120-170bn/year",
    fullROI: "600-1,700x",
    payback: "1-3 months"
  },

  whyOASIS: [
    "15+ blockchain integrations (no vendor lock-in)",
    "HyperDrive auto-failover (censorship-resistant)",
    "4+ years production experience (Asset Rail, MetaBricks)",
    "Privacy-first architecture (anti-surveillance)",
    "4x Grant Winner (Solana, Arbitrum, Radix, Thrive)",
    "Superteam UK network access"
  ],

  contact: {
    name: "Max Gershfield",
    role: "Web3 Advisor - Reform UK",
    email: "max.gershfield1@gmail.com",
    phone: "+447572116603",
    twitter: "@maxgershfield"
  }
};

export default function ReformUKOASISOnePager() {
  const data = onePager;

  const downloadPDF = () => {
    window.print();
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-slate-50 py-8 print:bg-white print:py-0">
      {/* Print Button */}
      <button
        onClick={downloadPDF}
        className="fixed top-4 right-4 z-50 bg-blue-700 hover:bg-blue-800 text-white px-6 py-3 rounded-lg shadow-lg font-semibold transition-all print:hidden"
      >
        Download PDF
      </button>

      <div className="max-w-7xl mx-auto bg-white shadow-2xl print:shadow-none rounded-xl overflow-hidden print:rounded-none">
        
        {/* Header */}
        <div className="bg-gradient-to-r from-blue-900 via-blue-800 to-blue-900 text-white p-8 relative">
          <img
            src="/Logo_of_the_Reform_UK.svg.png"
            alt="Reform UK"
            className="absolute top-6 right-8 h-16 w-auto opacity-90"
          />
          <div className="max-w-4xl">
            <h1 className="text-5xl font-bold mb-2">{data.title}</h1>
            <p className="text-2xl text-blue-200 mb-4">{data.subtitle}</p>
            <p className="text-lg text-blue-100 italic">{data.tagline}</p>
            <p className="text-sm text-blue-300 mt-4">{data.date}</p>
          </div>
        </div>

        {/* Vision */}
        <div className="p-8 bg-gradient-to-r from-blue-50 to-white border-b-4 border-blue-600">
          <h2 className="text-2xl font-bold text-blue-900 mb-3">🇬🇧 Our Vision</h2>
          <p className="text-lg text-slate-700 leading-relaxed italic border-l-4 border-blue-600 pl-4 py-2">
            {data.vision}
          </p>
        </div>

        {/* Core Pledges */}
        <div className="p-8">
          <h2 className="text-3xl font-bold text-slate-800 border-b-2 border-blue-600 pb-2 mb-6">
            Reform UK's 5 Core Pledges + OASIS Solutions
          </h2>
          
          <div className="grid grid-cols-1 gap-6">
            {data.corePledges.map((pledge, i) => (
              <div key={i} className="bg-gradient-to-br from-blue-50 to-white border-2 border-blue-200 rounded-lg p-6 hover:shadow-lg transition-shadow">
                <div className="flex items-start gap-4">
                  <div className="flex-shrink-0">
                    <div className="w-12 h-12 bg-blue-700 text-white rounded-full flex items-center justify-center font-bold text-xl">
                      {pledge.number}
                    </div>
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-3">
                      <span className="text-3xl">{pledge.icon}</span>
                      <h3 className="text-2xl font-bold text-blue-900">{pledge.title}</h3>
                      {pledge.savings && (
                        <span className="ml-auto bg-green-600 text-white px-3 py-1 rounded-full text-sm font-bold whitespace-nowrap">
                          {pledge.savings}
                        </span>
                      )}
                    </div>
                    
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-3">
                      <div>
                        <p className="text-xs font-semibold text-blue-600 uppercase mb-1">Reform UK Goal</p>
                        <p className="text-sm text-slate-700">{pledge.reformGoal}</p>
                      </div>
                      <div>
                        <p className="text-xs font-semibold text-green-600 uppercase mb-1">OASIS Solution</p>
                        <p className="text-sm text-slate-700">{pledge.oasisSolution}</p>
                      </div>
                    </div>
                    
                    <div>
                      <p className="text-xs font-semibold text-slate-600 uppercase mb-2">Implementation</p>
                      <ul className="grid grid-cols-1 md:grid-cols-2 gap-2">
                        {pledge.implementation.map((item, j) => (
                          <li key={j} className="text-xs text-slate-600 flex gap-2">
                            <span className="text-blue-600">✓</span>
                            <span>{item}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                    
                    {pledge.highlight && (
                      <p className="text-xs text-green-600 font-semibold mt-2 italic">{pledge.highlight}</p>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Savings Breakdown */}
        <div className="p-8 bg-slate-50">
          <h2 className="text-3xl font-bold text-slate-800 border-b-2 border-blue-600 pb-2 mb-6">
            £150 Billion Savings Plan: OASIS-Enabled
          </h2>
          
          <div className="bg-white rounded-lg border-2 border-blue-200 overflow-hidden">
            <table className="w-full">
              <thead className="bg-blue-700 text-white">
                <tr>
                  <th className="text-left p-4 font-semibold">Policy Area</th>
                  <th className="text-right p-4 font-semibold">Reform UK Target</th>
                  <th className="text-right p-4 font-semibold">OASIS Delivered</th>
                  <th className="text-center p-4 font-semibold">Status</th>
                </tr>
              </thead>
              <tbody>
                {data.savingsBreakdown.map((item, i) => (
                  <tr key={i} className={i % 2 === 0 ? "bg-blue-50" : "bg-white"}>
                    <td className="p-4 font-medium text-slate-800">{item.area}</td>
                    <td className="p-4 text-right text-slate-700">{item.reformTarget}</td>
                    <td className="p-4 text-right font-bold text-green-600">{item.oasisDelivered}</td>
                    <td className="p-4 text-center text-xl">{item.status}</td>
                  </tr>
                ))}
                <tr className="bg-blue-900 text-white font-bold">
                  <td className="p-4 text-lg">TOTAL</td>
                  <td className="p-4 text-right text-lg">£150bn</td>
                  <td className="p-4 text-right text-lg">£120-170bn</td>
                  <td className="p-4 text-center text-2xl">✅✅✅</td>
                </tr>
              </tbody>
            </table>
          </div>
          
          <p className="text-center text-lg font-bold text-green-600 mt-4">
            Total Verified Impact: £120-170 billion annually
          </p>
        </div>

        {/* CBDC Opposition */}
        <div className="p-8 bg-gradient-to-br from-slate-800 to-slate-700 text-white">
          <h2 className="text-3xl font-bold border-b-2 border-blue-400 pb-2 mb-4">
            🛡️ {data.cbdcOpposition.title}
          </h2>
          <div className="bg-white/10 backdrop-blur-sm border border-white/20 rounded-lg p-6 mb-4">
            <p className="text-lg italic mb-4">"{data.cbdcOpposition.quote}"</p>
            <p className="text-sm text-blue-200">— Reform UK Contract with You</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {data.cbdcOpposition.solutions.map((solution, i) => (
              <div key={i} className="flex gap-2 items-start">
                <span className="text-green-400 text-xl mt-0.5">✓</span>
                <span className="text-white/90">{solution}</span>
              </div>
            ))}
          </div>
          <p className="text-blue-200 font-semibold mt-4 text-center">
            Only major party opposing CBDCs with a real alternative
          </p>
        </div>

        {/* Implementation Roadmap */}
        <div className="p-8">
          <h2 className="text-3xl font-bold text-slate-800 border-b-2 border-blue-600 pb-2 mb-6">
            🚀 Implementation Roadmap
          </h2>
          
          <div className="space-y-6">
            {data.roadmap.map((phase, i) => (
              <div key={i} className="border-l-4 border-blue-600 pl-6">
                <div className="flex items-center gap-3 mb-3">
                  <div className="bg-blue-700 text-white px-3 py-1 rounded-full text-sm font-bold">
                    {phase.timeline}
                  </div>
                  <h3 className="text-xl font-bold text-blue-900">{phase.phase}</h3>
                </div>
                <ul className="grid grid-cols-1 md:grid-cols-2 gap-2">
                  {phase.items.map((item, j) => (
                    <li key={j} className="text-sm text-slate-700 flex gap-2">
                      <span className="text-blue-600 font-bold">→</span>
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>

        {/* ROI */}
        <div className="p-8 bg-gradient-to-r from-green-50 to-blue-50">
          <h2 className="text-3xl font-bold text-slate-800 border-b-2 border-green-600 pb-2 mb-6">
            💼 Investment & ROI
          </h2>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="bg-white border-2 border-green-200 rounded-lg p-6">
              <h3 className="text-xl font-bold text-green-700 mb-4">Pilot Phase (100 Days)</h3>
              <div className="space-y-2">
                <div className="flex justify-between">
                  <span className="text-slate-600">Budget:</span>
                  <span className="font-bold text-slate-800">{data.roi.pilotBudget}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-600">Annual Return:</span>
                  <span className="font-bold text-green-600">{data.roi.pilotReturn}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-600">ROI:</span>
                  <span className="font-bold text-green-700 text-xl">{data.roi.pilotROI}</span>
                </div>
              </div>
            </div>
            
            <div className="bg-white border-2 border-blue-200 rounded-lg p-6">
              <h3 className="text-xl font-bold text-blue-700 mb-4">Full Deployment (1-2 Years)</h3>
              <div className="space-y-2">
                <div className="flex justify-between">
                  <span className="text-slate-600">Budget:</span>
                  <span className="font-bold text-slate-800">{data.roi.fullBudget}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-600">Annual Return:</span>
                  <span className="font-bold text-green-600">{data.roi.fullReturn}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-600">ROI:</span>
                  <span className="font-bold text-green-700 text-2xl">{data.roi.fullROI}</span>
                </div>
              </div>
            </div>
          </div>
          
          <p className="text-center text-lg font-bold text-green-700 mt-6">
            Payback Period: {data.roi.payback}
          </p>
          <p className="text-center text-slate-600 italic mt-2">
            This isn't a cost—it's the most profitable investment Reform UK will ever make.
          </p>
        </div>

        {/* Why OASIS */}
        <div className="p-8 bg-slate-50">
          <h2 className="text-3xl font-bold text-slate-800 border-b-2 border-blue-600 pb-2 mb-6">
            🔧 Why OASIS is Uniquely Qualified
          </h2>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {data.whyOASIS.map((reason, i) => (
              <div key={i} className="flex gap-3 items-start bg-white border border-blue-200 rounded-lg p-4">
                <span className="text-green-600 text-xl font-bold mt-0.5">✓</span>
                <span className="text-slate-700">{reason}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Contact */}
        <div className="bg-gradient-to-r from-blue-900 via-blue-800 to-blue-900 text-white p-8">
          <h2 className="text-2xl font-bold mb-4">📞 Contact</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <p className="text-xl font-bold mb-2">{data.contact.name}</p>
              <p className="text-blue-200 mb-4">{data.contact.role}</p>
              <div className="space-y-1 text-sm">
                <p>Email: {data.contact.email}</p>
                <p>Phone: {data.contact.phone}</p>
                <p>Twitter: {data.contact.twitter}</p>
              </div>
            </div>
            <div className="flex items-center justify-center md:justify-end">
              <div className="text-center">
                <p className="text-2xl font-bold mb-2">Ready to Begin</p>
                <p className="text-blue-200 text-sm">
                  Delivering Reform UK's Contract with You
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="bg-slate-800 text-white p-6 text-center">
          <p className="text-lg font-bold mb-2">
            "Only Reform UK will secure Britain's future as a free, proud and independent sovereign nation."
          </p>
          <p className="text-blue-200 text-sm">
            And only OASIS can provide the technology to make it happen.
          </p>
          <p className="text-xs text-slate-400 mt-4">
            Powered by OASIS Web4 Infrastructure | November 2025
          </p>
        </div>

      </div>
    </div>
  );
}


