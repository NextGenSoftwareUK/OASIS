import React from "react";

// Reform UK Web3 Advisor One-Pager
// Tailwind required. Drop this component into a Vite/Next/Cra app with Tailwind configured.

const proposal = {
  name: "Max Gershfield",
  role: "Web3 Advisor - Reform UK | Application",
  avatarUrl: "/MAX_5 square.png",
  reformUKLogo: "/Logo_of_the_Reform_UK.svg.png",
  contact: [
    { label: "+4475721166038", icon: "phone" },
    { label: "max.gershfield1@gmail.com", icon: "mail" },
    { label: "X / TG: @maxgershfield", icon: "map" },
    { label: "github.com/maxgershfield", icon: "map" },
  ],
  brexitStatement:
    "Proud Brexiteer - champion of personal and national sovereignty, highly motivated to make a difference for the UK and with a clear vision to do so.",
  
  valueProposition:
    "Position Reform UK as the pro-innovation, pro-freedom, blockchain-forward party that attracts UK's web3 builders and young digital natives while protecting citizens from CBDC overreach and centralized control.",
  
  whyReformUK: [
    {
      title: "Digital Sovereignty = National Sovereignty",
      icon: "🇬🇧",
      description: "Web3 is the digital extension of Brexit principles. Just as Reform UK champions UK sovereignty from EU control, blockchain enables financial sovereignty from centralized banks and Big Tech. Decentralization isn't just technology—it's ideology that aligns perfectly with Reform's anti-establishment positioning.",
    },
    {
      title: "CBDC Opposition Strategy",
      icon: "🛡️",
      description: "Position Reform UK as defenders against government surveillance money. While other parties embrace CBDCs (Central Bank Digital Currencies), Reform can champion privacy, freedom, and citizen choice. This is a winning issue with both current base AND crypto-native voters.",
    },
    {
      title: "From Red Wall to Digital Wall",
      icon: "🏗️",
      description: "Web3 offers financial inclusion for working-class voters through accessible tools that bypass traditional banking barriers. Tokenization, micro-lending, and DeFi can empower the very voters Reform UK represents—making finance fairer and more democratic.",
    },
    {
      title: "Great British Bitcoin Strategy",
      icon: "🚀",
      description: "Make UK the global crypto hub—beating Singapore, Dubai, and Switzerland. Reform UK can attract billions in investment, thousands of jobs, and position Britain as THE destination for blockchain innovation. Patriotic framing meets economic reality.",
    },
    {
      title: "Reform = Future, Others = Past",
      icon: "⚡",
      description: "Web3 engagement breaks the 'old party' perception and attracts 18-35 demographics currently underserved by UK politics. While Reform's current base skews older, web3 strategy expands reach to tech entrepreneurs, crypto builders, and digital natives—without alienating existing supporters.",
    },
  ],

  pillars: [
    {
      title: "Strategic Advisory",
      icon: "🎯",
      items: [
        "Web3 strategy development aligned with Reform UK's core values",
        "Policy research on digital sovereignty, CBDC alternatives, and financial freedom",
        "Making UK competitive with crypto-friendly jurisdictions (Dubai, Singapore, Switzerland)",
        "Economic innovation: blockchain for small business, tokenized assets, reduced banking barriers",
        "Transparency initiatives: blockchain for government accountability and verifiable records",
      ],
    },
    {
      title: "Technical Leadership",
      icon: "⚡",
      items: [
        "Platform development: Reform UK's own web3 infrastructure and tools",
        "Technical due diligence: evaluating projects, security audits, risk assessment",
        "Multi-chain expertise: UK shouldn't be locked to one ecosystem (Ethereum, Solana, Radix, etc.)",
        "Real-World Asset (RWA) tokenization policy for UK property, bonds, commodities",
        "AI x Blockchain convergence: trustless AI agents, accountability systems",
      ],
    },
    {
      title: "Community & Growth",
      icon: "🌐",
      items: [
        "Foundation relationships: deepening ties with Solana, Ethereum, Radix, Superteam UK",
        "Youth engagement: attracting 18-35 web3-native voters through gamification and token-gated communities",
        "Student groups and grassroots: developing educational programs and onboarding new demographics",
        "Content & social media: web3-focused messaging, policy explainers, and thought leadership",
        "International diplomacy: representing UK interests at blockchain conferences and with pro-crypto governments",
      ],
    },
  ],

  additionalCapabilities: [
    {
      title: "Regulatory Navigation",
      description: "Working with regulators to understand core concerns, safeguard against risks while remaining progressive. Experience with compliant RWA tokenization (Quantum Street/Asset Rail).",
    },
    {
      title: "Fundraising Innovation",
      description: "Exploring web3-native fundraising mechanisms compliant with UK electoral law. Microtransactions, cryptocurrency donations, and transparent donor systems.",
    },
    {
      title: "Media & Misinformation Defense",
      description: "Content authenticity via blockchain, fighting deepfakes with cryptographic verification. Reform UK could pioneer 'verified political content'.",
    },
    {
      title: "Defense & National Security",
      description: "Blockchain for supply chain verification, secure communications, data sovereignty, and reducing reliance on foreign tech infrastructure.",
    },
  ],

  differentiators: [
    "Bridge creative & technical: translate complex tech to politicians AND vice versa",
    "Real production experience: shipped actual blockchain systems (OASIS Web4, MetaBricks, Asset Rail)",
    "Multi-chain expertise: not tribal about any single blockchain (Solana, Ethereum, Radix, Kadena, etc.)",
    "4x Grant winner: trusted by Solana, Arbitrum, Radix, Thrive Protocol foundations",
    "Copywriting background: Uber EMEA, TBWA, JWT - crucial for public-facing policy and messaging",
    "Superteam UK network: immediate access to UK's web3 builders and thought leaders",
    "Technical depth: ERC-8004 trustless agents, cross-chain protocols, RWA tokenization",
    "Proud Brexiteer: champion of personal and national sovereignty, highly motivated to make a difference for the UK and with a clear vision to do so",
  ],

  credentials: [
    "Superteam UK Contributor",
    "4x Web3 Grant Winner (Solana, Arbitrum, Radix, Thrive)",
    "3+ Years Production Blockchain Development",
    "10+ Years Enterprise/Agency Experience",
    "15+ Blockchain Networks Integrated",
  ],
};

function Icon({ name, className = "w-4 h-4" }: { name: string; className?: string }) {
  const common = "inline-block align-[-0.125em]";
  switch (name) {
    case "phone":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M2.5 4.5c0-1.1.9-2 2-2h2.1c.9 0 1.7.6 1.9 1.5l.8 3.2c.2.9-.2 1.8-1 2.3l-1.2.8c1.5 3 3.9 5.4 6.9 6.9l.8-1.2c.5-.8 1.4-1.2 2.3-1l3.2.8c.9.2 1.5 1 1.5 1.9V19.5c0 1.1-.9 2-2 2h-1c-9.4 0-17-7.6-17-17v0z" />
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

function SectionHeading({ children }: { children: React.ReactNode }) {
  return (
    <h2 className="text-2xl font-bold tracking-tight text-slate-800 border-b-2 border-blue-600 pb-2 mb-4">
      {children}
    </h2>
  );
}

function PillarCard({ title, icon, items }: { title: string; icon: string; items: string[] }) {
  return (
    <div className="bg-gradient-to-br from-blue-50 to-white border border-blue-200 rounded-lg p-6 shadow-sm hover:shadow-md transition-shadow">
      <div className="flex items-center gap-3 mb-4">
        <span className="text-3xl">{icon}</span>
        <h3 className="text-xl font-bold text-blue-900">{title}</h3>
      </div>
      <ul className="space-y-2">
        {items.map((item, i) => (
          <li key={i} className="text-sm text-slate-700 flex gap-2">
            <span className="text-blue-600 font-bold mt-0.5">•</span>
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default function ReformUKProposal() {
  const p = proposal;

  const downloadPDF = () => {
    // Use browser's native print function which fully supports modern CSS
    window.print();
  };

    // Header - Name and Title
    doc.setFontSize(26);
    doc.setFont("helvetica", "bold");
    doc.setTextColor(0, 51, 153);
    doc.text(p.name, 105, yPos, { align: "center" });
    yPos += 10;

    doc.setFontSize(14);
    doc.setFont("helvetica", "bold");
    doc.setTextColor(0, 51, 153);
    doc.text(p.role, 105, yPos, { align: "center" });
    yPos += 8;

    // Credentials line
    doc.setFontSize(9);
    doc.setFont("helvetica", "normal");
    doc.setTextColor(60, 60, 60);
    doc.text(p.credentials.join(" | "), 105, yPos, { align: "center" });
    yPos += 8;

    // Contact
    doc.setFontSize(9);
    doc.setTextColor(0, 0, 0);
    const contactLine = p.contact.map(c => c.label).join(" | ");
    doc.text(contactLine, 105, yPos, { align: "center" });
    yPos += 10;

    // Separator line
    doc.setDrawColor(0, 51, 153);
    doc.setLineWidth(0.5);
    doc.line(15, yPos, 195, yPos);
    yPos += 8;

    // Value Proposition
    doc.setFontSize(13);
    doc.setFont("helvetica", "bold");
    doc.setTextColor(0, 51, 153);
    doc.text("VALUE PROPOSITION", 15, yPos);
    yPos += 7;

    doc.setFontSize(10);
    doc.setFont("helvetica", "italic");
    doc.setTextColor(0, 0, 0);
    const vpLines = doc.splitTextToSize(p.valueProposition, 180);
    doc.text(vpLines, 15, yPos);
    yPos += vpLines.length * 5 + 10;

    // Three Pillars
    doc.setFontSize(13);
    doc.setFont("helvetica", "bold");
    doc.setTextColor(0, 51, 153);
    doc.text("THREE PILLARS OF ADVISORY", 15, yPos);
    yPos += 7;

    p.pillars.forEach((pillar) => {
      if (yPos > 250) {
        doc.addPage();
        yPos = 15;
      }

      doc.setFontSize(11);
      doc.setFont("helvetica", "bold");
      doc.setTextColor(0, 102, 204);
      doc.text(`${pillar.icon} ${pillar.title}`, 15, yPos);
      yPos += 6;

      doc.setFontSize(9);
      doc.setFont("helvetica", "normal");
      doc.setTextColor(0, 0, 0);
      pillar.items.forEach((item) => {
        const itemLines = doc.splitTextToSize(`  • ${item}`, 175);
        doc.text(itemLines, 18, yPos);
        yPos += itemLines.length * 4 + 1;
      });
      yPos += 4;
    });

    // Additional Capabilities
    if (yPos > 220) {
      doc.addPage();
      yPos = 15;
    }

    doc.setFontSize(13);
    doc.setFont("helvetica", "bold");
    doc.setTextColor(0, 51, 153);
    doc.text("ADDITIONAL CAPABILITIES", 15, yPos);
    yPos += 7;

    p.additionalCapabilities.forEach((cap) => {
      if (yPos > 260) {
        doc.addPage();
        yPos = 15;
      }

      doc.setFontSize(10);
      doc.setFont("helvetica", "bold");
      doc.setTextColor(0, 102, 204);
      doc.text(cap.title, 15, yPos);
      yPos += 5;

      doc.setFontSize(9);
      doc.setFont("helvetica", "normal");
      doc.setTextColor(0, 0, 0);
      const descLines = doc.splitTextToSize(cap.description, 180);
      doc.text(descLines, 15, yPos);
      yPos += descLines.length * 4 + 5;
    });

    // Why Reform UK x Web3
    if (yPos > 200) {
      doc.addPage();
      yPos = 15;
    }

    doc.setFontSize(13);
    doc.setFont("helvetica", "bold");
    doc.setTextColor(0, 51, 153);
    doc.text("WHY REFORM UK x WEB3 MAKES SENSE", 15, yPos);
    yPos += 7;

    p.whyReformUK.forEach((reason) => {
      if (yPos > 250) {
        doc.addPage();
        yPos = 15;
      }

      doc.setFontSize(10);
      doc.setFont("helvetica", "bold");
      doc.setTextColor(0, 102, 204);
      doc.text(`${reason.icon} ${reason.title}`, 15, yPos);
      yPos += 5;

      doc.setFontSize(9);
      doc.setFont("helvetica", "normal");
      doc.setTextColor(0, 0, 0);
      const descLines = doc.splitTextToSize(reason.description, 180);
      doc.text(descLines, 15, yPos);
      yPos += descLines.length * 4 + 5;
    });

    // Differentiators
    if (yPos > 200) {
      doc.addPage();
      yPos = 15;
    }

    doc.setFontSize(13);
    doc.setFont("helvetica", "bold");
    doc.setTextColor(0, 51, 153);
    doc.text("MY UNIQUE DIFFERENTIATORS", 15, yPos);
    yPos += 7;

    doc.setFontSize(9);
    doc.setFont("helvetica", "normal");
    doc.setTextColor(0, 0, 0);
    p.differentiators.forEach((diff) => {
      if (yPos > 270) {
        doc.addPage();
        yPos = 15;
      }
      const diffLines = doc.splitTextToSize(`  ✓  ${diff}`, 175);
      doc.text(diffLines, 15, yPos);
      yPos += diffLines.length * 4 + 2;
    });

    // Footer
    yPos += 5;
    if (yPos > 270) {
      doc.addPage();
      yPos = 15;
    }
    
    doc.setDrawColor(0, 51, 153);
    doc.setLineWidth(0.5);
    doc.line(15, yPos, 195, yPos);
    yPos += 5;

    doc.setFontSize(9);
    doc.setFont("helvetica", "italic");
    doc.setTextColor(60, 60, 60);
    doc.text("Portfolio: api.assetrail.xyz | metabricks.xyz | maxgershfield.co.uk", 105, yPos, { align: "center" });

    // Open preview in new window instead of downloading
    const pdfBlob = doc.output('blob');
    const pdfUrl = URL.createObjectURL(pdfBlob);
    window.open(pdfUrl, '_blank');
  };

  const downloadPDF = () => {
    const doc = new jsPDF({
      orientation: "portrait",
      unit: "mm",
      format: "a4",
      compress: true,
    });
    let yPos = 20;

    // (Same PDF generation code as exportPDF - I'll reuse the function)
    // Create a helper function to generate the PDF
    const generatePDFContent = (doc: any) => {
      let yPos = 20;

      // Header - Name and Title
      doc.setFontSize(26);
      doc.setFont("helvetica", "bold");
      doc.setTextColor(0, 51, 153);
      doc.text(p.name, 105, yPos, { align: "center" });
      yPos += 10;

      doc.setFontSize(14);
      doc.setFont("helvetica", "bold");
      doc.setTextColor(0, 51, 153);
      doc.text(p.role, 105, yPos, { align: "center" });
      yPos += 8;

      // Credentials line
      doc.setFontSize(9);
      doc.setFont("helvetica", "normal");
      doc.setTextColor(60, 60, 60);
      doc.text(p.credentials.join(" | "), 105, yPos, { align: "center" });
      yPos += 8;

      // Contact
      doc.setFontSize(9);
      doc.setTextColor(0, 0, 0);
      const contactLine = p.contact.map((c: any) => c.label).join(" | ");
      doc.text(contactLine, 105, yPos, { align: "center" });
      yPos += 10;

      // Separator line
      doc.setDrawColor(0, 51, 153);
      doc.setLineWidth(0.5);
      doc.line(15, yPos, 195, yPos);
      yPos += 8;

      // Value Proposition
      doc.setFontSize(13);
      doc.setFont("helvetica", "bold");
      doc.setTextColor(0, 51, 153);
      doc.text("VALUE PROPOSITION", 15, yPos);
      yPos += 7;

      doc.setFontSize(10);
      doc.setFont("helvetica", "italic");
      doc.setTextColor(0, 0, 0);
      const vpLines = doc.splitTextToSize(p.valueProposition, 180);
      doc.text(vpLines, 15, yPos);
      yPos += vpLines.length * 5 + 10;

      // Three Pillars
      doc.setFontSize(13);
      doc.setFont("helvetica", "bold");
      doc.setTextColor(0, 51, 153);
      doc.text("THREE PILLARS OF ADVISORY", 15, yPos);
      yPos += 7;

      p.pillars.forEach((pillar: any) => {
        if (yPos > 250) {
          doc.addPage();
          yPos = 15;
        }

        doc.setFontSize(11);
        doc.setFont("helvetica", "bold");
        doc.setTextColor(0, 102, 204);
        doc.text(`${pillar.icon} ${pillar.title}`, 15, yPos);
        yPos += 6;

        doc.setFontSize(9);
        doc.setFont("helvetica", "normal");
        doc.setTextColor(0, 0, 0);
        pillar.items.forEach((item: any) => {
          const itemLines = doc.splitTextToSize(`  • ${item}`, 175);
          doc.text(itemLines, 18, yPos);
          yPos += itemLines.length * 4 + 1;
        });
        yPos += 4;
      });

      // Additional Capabilities
      if (yPos > 220) {
        doc.addPage();
        yPos = 15;
      }

      doc.setFontSize(13);
      doc.setFont("helvetica", "bold");
      doc.setTextColor(0, 51, 153);
      doc.text("ADDITIONAL CAPABILITIES", 15, yPos);
      yPos += 7;

      p.additionalCapabilities.forEach((cap: any) => {
        if (yPos > 260) {
          doc.addPage();
          yPos = 15;
        }

        doc.setFontSize(10);
        doc.setFont("helvetica", "bold");
        doc.setTextColor(0, 102, 204);
        doc.text(cap.title, 15, yPos);
        yPos += 5;

        doc.setFontSize(9);
        doc.setFont("helvetica", "normal");
        doc.setTextColor(0, 0, 0);
        const descLines = doc.splitTextToSize(cap.description, 180);
        doc.text(descLines, 15, yPos);
        yPos += descLines.length * 4 + 5;
      });

      // Why Reform UK x Web3
      if (yPos > 200) {
        doc.addPage();
        yPos = 15;
      }

      doc.setFontSize(13);
      doc.setFont("helvetica", "bold");
      doc.setTextColor(0, 51, 153);
      doc.text("WHY REFORM UK x WEB3 MAKES SENSE", 15, yPos);
      yPos += 7;

      p.whyReformUK.forEach((reason: any) => {
        if (yPos > 250) {
          doc.addPage();
          yPos = 15;
        }

        doc.setFontSize(10);
        doc.setFont("helvetica", "bold");
        doc.setTextColor(0, 102, 204);
        doc.text(`${reason.icon} ${reason.title}`, 15, yPos);
        yPos += 5;

        doc.setFontSize(9);
        doc.setFont("helvetica", "normal");
        doc.setTextColor(0, 0, 0);
        const descLines = doc.splitTextToSize(reason.description, 180);
        doc.text(descLines, 15, yPos);
        yPos += descLines.length * 4 + 5;
      });

      // Differentiators
      if (yPos > 200) {
        doc.addPage();
        yPos = 15;
      }

      doc.setFontSize(13);
      doc.setFont("helvetica", "bold");
      doc.setTextColor(0, 51, 153);
      doc.text("MY UNIQUE DIFFERENTIATORS", 15, yPos);
      yPos += 7;

      doc.setFontSize(9);
      doc.setFont("helvetica", "normal");
      doc.setTextColor(0, 0, 0);
      p.differentiators.forEach((diff: any) => {
        if (yPos > 270) {
          doc.addPage();
          yPos = 15;
        }
        const diffLines = doc.splitTextToSize(`  ✓  ${diff}`, 175);
        doc.text(diffLines, 15, yPos);
        yPos += diffLines.length * 4 + 2;
      });

      // Footer
      yPos += 5;
      if (yPos > 270) {
        doc.addPage();
        yPos = 15;
      }
      
      doc.setDrawColor(0, 51, 153);
      doc.setLineWidth(0.5);
      doc.line(15, yPos, 195, yPos);
      yPos += 5;

      doc.setFontSize(9);
      doc.setFont("helvetica", "italic");
      doc.setTextColor(60, 60, 60);
      doc.text("Portfolio: api.assetrail.xyz | metabricks.xyz | maxgershfield.co.uk", 105, yPos, { align: "center" });
    };

    generatePDFContent(doc);
    doc.save("Max_Gershfield_Reform_UK_Web3_Advisor.pdf");
  };

  return (
    <div className="min-h-screen w-full bg-gradient-to-br from-slate-50 via-blue-50 to-slate-50 py-8 print:bg-white print:py-0">
      {/* Export Button */}
      <button
        onClick={downloadPDF}
        className="fixed top-4 right-4 z-50 bg-blue-700 hover:bg-blue-800 text-white px-6 py-3 rounded-lg shadow-lg font-semibold transition-all print:hidden"
      >
        Download PDF One-Pager
      </button>

      <div className="mx-auto max-w-6xl bg-white shadow-xl print:shadow-none rounded-xl overflow-hidden print:rounded-none print:max-w-none">
        {/* Header */}
        <div className="bg-gradient-to-r from-blue-900 via-blue-800 to-blue-900 text-white p-8 relative">
          <img
            src={p.reformUKLogo}
            alt="Reform UK"
            className="absolute top-8 right-8 h-16 w-auto opacity-90"
          />
          <div className="flex flex-col md:flex-row items-center md:items-start gap-6">
            <img
              src={p.avatarUrl}
              alt={p.name}
              className="h-32 w-32 rounded-full object-cover ring-4 ring-white/40 shadow-lg"
            />
            <div className="flex-1 text-center md:text-left">
              <h1 className="text-4xl font-bold mb-2">{p.name}</h1>
              <p className="text-2xl text-blue-200 mb-4">{p.role}</p>
              <div className="flex flex-wrap gap-4 justify-center md:justify-start text-sm">
                {p.contact.map((c, i) => (
                  <div key={i} className="flex items-center gap-2 bg-white/10 px-3 py-1 rounded-full">
                    <Icon name={c.icon} className="w-4 h-4" />
                    <span>{c.label}</span>
                  </div>
                ))}
              </div>
              <div className="mt-4 flex flex-wrap gap-2 justify-center md:justify-start">
                {p.credentials.map((cred, i) => (
                  <span key={i} className="text-xs bg-blue-700 px-3 py-1 rounded-full">
                    {cred}
                  </span>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* Value Proposition */}
        <div className="p-8 bg-gradient-to-r from-blue-50 to-white">
          <SectionHeading>Value Proposition</SectionHeading>
          <p className="text-lg text-slate-700 leading-relaxed italic border-l-4 border-blue-600 pl-4 py-2">
            {p.valueProposition}
          </p>
        </div>

        {/* Three Pillars */}
        <div className="p-8">
          <SectionHeading>Three Pillars of Advisory</SectionHeading>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {p.pillars.map((pillar, i) => (
              <PillarCard key={i} title={pillar.title} icon={pillar.icon} items={pillar.items} />
            ))}
          </div>
        </div>

        {/* Additional Capabilities */}
        <div className="p-8 bg-slate-50">
          <SectionHeading>Additional Capabilities</SectionHeading>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {p.additionalCapabilities.map((cap, i) => (
              <div key={i} className="bg-white border border-slate-200 rounded-lg p-5 shadow-sm">
                <h3 className="text-lg font-bold text-blue-900 mb-2">{cap.title}</h3>
                <p className="text-sm text-slate-700 leading-relaxed">{cap.description}</p>
              </div>
            ))}
          </div>
        </div>

        {/* Why Reform UK x Web3 */}
        <div className="p-8 bg-gradient-to-br from-slate-800 to-slate-700 text-white">
          <h2 className="text-2xl font-bold tracking-tight border-b-2 border-blue-400 pb-2 mb-6">
            Why Reform UK × Web3 Makes Sense
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {p.whyReformUK.map((reason, i) => (
              <div key={i} className="bg-white/10 backdrop-blur-sm border border-white/20 rounded-lg p-5 hover:bg-white/15 transition-all">
                <div className="flex items-center gap-3 mb-3">
                  <span className="text-3xl">{reason.icon}</span>
                  <h3 className="text-lg font-bold text-blue-200">{reason.title}</h3>
                </div>
                <p className="text-sm text-white/90 leading-relaxed">{reason.description}</p>
              </div>
            ))}
          </div>
        </div>

        {/* Differentiators */}
        <div className="p-8">
          <SectionHeading>My Unique Differentiators</SectionHeading>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {p.differentiators.map((diff, i) => (
              <div key={i} className="flex gap-3 items-start">
                <span className="text-green-600 text-xl font-bold mt-0.5">✓</span>
                <p className="text-sm text-slate-700">{diff}</p>
              </div>
            ))}
          </div>
        </div>

        {/* Footer */}
        <div className="bg-gradient-to-r from-slate-800 to-slate-700 text-white p-6 text-center">
          <p className="text-sm">
            Ready to position Reform UK as the UK's blockchain-forward, freedom-tech party
          </p>
          <p className="text-xs text-slate-300 mt-2">
            Portfolio: api.assetrail.xyz | metabricks.xyz | maxgershfield.co.uk
          </p>
        </div>
      </div>
    </div>
  );
}

