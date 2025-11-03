import React from "react";
import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";

// Tailwind required. Drop this component into a Vite/Next/Cra app with Tailwind configured.
// Print-ready: A4-ish container, white background, soft gray separators, timeline dots.
// Replace "resume" data below or feed it via props/context/API.

const resume = {
  name: "Max Gershfield",
  role: "Creative Technologist",
  avatarUrl:
    "/MAX_5 square.png",
  contact: [
    { label: "+4475721166038", icon: "phone" },
    { label: "max.gershfield1@gmail.com", icon: "mail" },
    { label: "X / TG: @maxgershfield", icon: "map" },
    { label: "github.com/maxgershfield", icon: "map" },
  ],
  about:
    "Creative technologist focused on full stack web3 systems. Combining 10 years of network agency / enterprise experience with 4 years in the trenches of web3 building in Solana, EVM, Radix and Holochain ecos. My ideal role will combine technical challenges with a need for creative thinking, rapid iteration, and working with teams both in-house and client-facing.",
  skills: [
    "React/Next.js/Angular",
    "TypeScript/JavaScript",
    "Web3 & Blockchain",
    "Solana/Ethereum/Radix",
    ".NET",
    "Smart Contracts",
    "Copywriting & Brand Strategy",
    "Technical Writing / Whitepapers",
    "Creative Direction",
  ],
  creativeExperience: [
    {
      title: "Creative Strategist (Consultant)",
      company: "Flight3 - UK's Top Web3 Creative Agency",
      period: "2025",
      summary:
        "Joined as consultant bridging creative and strategic expertise. Created brand strategy for gasp.xyz, gbm.auction, and mugshot.xyz. Developed marketing and ICO content for gvnr.xyz, Ripple, and zebu.live, leveraging copywriting background to communicate complex blockchain concepts.",
    },
    {
      title: "Copywriter - EMEA CRM Team",
      company: "Uber",
      period: "2018 – 2022",
      summary:
        "Lead copywriter for product launches including Uber Lite, Uber Rewards, Uber Premium, and Uber Freight. Built CRM Lifecycles that reached millions of riders + drivers globally. Managed regional copywriters to ensure brand consistency across 54 markets - contributed to Uber's brand bible to build best practices, and worked on global creative playbooks.",
    },
    {
      title: "Copywriter",
      company: "Network Agencies (TBWA, JWT)",
      period: "2013 – 2018",
      summary:
        "Worked in creative teams for leading agencies and freelanced for Luxottica, Adyen, MessageBird. Managed Arnette Sunglasses Instagram (grew from 18K to 150K followers), rebranded Hi-Tec shoes, and built campaigns for Puma, Bridgestone, Adidas, TomTom. Collaborated with Post Malone for Arnette campaign.",
    },
  ],
  creativeEducation: [
    {
      degree: "BA in English Literature (2:1)",
      school: "Bristol University",
      period: "2008 – 2011",
      summary:
        "Graduated with a 2.1, developing a strong creative foundation while also running a successful club night and releasing several electronic music records under the monicker 'JockTalk'.",
    },
  ],
  technicalEducation: [
    {
      degree: "Coding Bootcamp",
      school: "Codam.nl",
      period: "2022",
      summary:
        "Completed one-month intensive program focusing on software development fundamentals, marking the transition into full-stack development and blockchain technologies.",
    },
  ],
  technologyExperience: [
    {
      title: "Full Stack Developer",
      company: "OASIS Web4",
      period: "2022 – Present",
      summary:
        "Contributed to comprehensive Web4 platform with cross-chain interoperability across 15+ networks. Built React 18/Next.js 14 applications with TypeScript, implemented multi-chain wallet management, developed database integrations (PostgreSQL, MongoDB, Neo4j).",
    },
    {
      title: "Full-Stack Developer",
      company: "Metabricks.xyz - Web3 NFT Marketplace",
      period: "2023 – 2024",
      summary:
        "Built comprehensive Web3 application combining NFT minting, wallet management, and cross-chain interoperability. Developed Angular 15 frontend with multi-chain wallet integration, Node.js/Express backend with Stripe payments, Solana NFT minting system with IPFS storage. Achieved production deployment with 432 unique NFTs and complex metadata.",
    },
    {
      title: "Blockchain Developer",
      company: "Quantum Street: Asset Rail",
      period: "2023 – 2024",
      summary:
        "Developed blockchain-based platform for compliant tokenization of real-world assets through Wyoming Trust structures. Built .NET 8 Web API and Next.js frontend supporting multi-blockchain deployment (Ethereum, Solana, Kadena). Implemented automated smart contract generation, cross-chain bridge between Solana and Radix, and RWA marketplace with Docker microservice architecture.",
    },
  ],
  references: [
    {
      name: "Harumi Kobayashi",
      role: "Wardiere Inc. / CEO",
      phone: "123-456-7890",
      email: "hello@reallygreatsite.com",
    },
    {
      name: "Bailey Dupont",
      role: "Wardiere Inc. / CEO",
      phone: "123-456-7890",
      email: "hello@reallygreatsite.com",
    },
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
    case "edu":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M3 8l9-4 9 4-9 4-9-4z" />
          <path d="M7 10v4a5 5 0 0 0 10 0v-4" />
        </svg>
      );
    case "brief":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M10 4h4a2 2 0 0 1 2 2v2h-8V6a2 2 0 0 1 2-2z"/> 
          <rect x="3" y="8" width="18" height="12" rx="2"/> 
        </svg>
      );
    case "ref":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <circle cx="8" cy="8" r="3" />
          <path d="M2 20a6 6 0 0 1 12 0" />
          <rect x="14" y="4" width="8" height="12" rx="2" />
        </svg>
      );
    default:
      return null;
  }
}

function SectionHeading({ icon, children }: { icon: string; children: React.ReactNode }) {
  return (
    <div className="flex items-center gap-2 text-gray-800">
      <Icon name={icon} className="w-5 h-5" />
      <h2 className="text-xl font-semibold tracking-tight">{children}</h2>
    </div>
  );
}

function TimelineItem({ title, subtitle, period, summary }: { title: string; subtitle: string; period: string; summary: string }) {
  return (
    <div className="relative pl-6 pb-5">
      <span className="absolute left-0 top-1.5 h-4 w-4 rounded-full border-2 border-sky-500 bg-white" />
      <div className="flex items-baseline justify-between gap-4">
        <div>
          <p className="font-semibold text-gray-900">{title}</p>
          <p className="text-sm italic text-gray-600">{subtitle}</p>
        </div>
        <span className="text-sm text-gray-500 whitespace-nowrap">{period}</span>
      </div>
      <p className="mt-2 text-sm leading-relaxed text-gray-700">{summary}</p>
    </div>
  );
}

export default function Resume() {
  const r = resume;
  
  const exportPDF = () => {
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4',
      compress: true
    });
    let yPos = 15;
    
    // Header - Name and Title
    doc.setFontSize(24);
    doc.setFont('helvetica', 'bold');
    doc.text(r.name, 105, yPos, { align: 'center' });
    yPos += 8;
    
    doc.setFontSize(14);
    doc.setFont('helvetica', 'normal');
    doc.text(r.role, 105, yPos, { align: 'center' });
    yPos += 6;
    
    doc.setFontSize(10);
    doc.text('Interested in: RWAs | Interoperability | Metaverse', 105, yPos, { align: 'center' });
    yPos += 10;
    
    // Contact Info
    doc.setFontSize(10);
    r.contact.forEach((c) => {
      doc.text(c.label, 105, yPos, { align: 'center' });
      yPos += 5;
    });
    yPos += 3;
    
    // Achievements
    doc.setFont('helvetica', 'normal');
    doc.text('Superteam UK contributor', 105, yPos, { align: 'center' });
    yPos += 5;
    doc.text('4x Grant winner (Solana, Arbitrum, Radix, Thrive)', 105, yPos, { align: 'center' });
    yPos += 5;
    
    // Work Examples
    doc.setFontSize(9);
    doc.text('Work examples: api.assetrail.xyz | metabricks.xyz | maxgershfield.co.uk', 105, yPos, { align: 'center' });
    yPos += 8;
    
    // About
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('ABOUT', 15, yPos);
    yPos += 6;
    
    doc.setFontSize(10);
    doc.setFont('helvetica', 'normal');
    const aboutLines = doc.splitTextToSize(r.about, 180);
    doc.text(aboutLines, 15, yPos);
    yPos += aboutLines.length * 5 + 5;
    
    // Skills
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('SKILLS', 15, yPos);
    yPos += 6;
    
    doc.setFontSize(10);
    doc.setFont('helvetica', 'normal');
    doc.text(r.skills.join(' • '), 15, yPos, { maxWidth: 180 });
    yPos += 8;
    
    // Creative Experience
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('CREATIVE EXPERIENCE', 15, yPos);
    yPos += 6;
    
    r.creativeExperience.forEach((exp) => {
      doc.setFontSize(11);
      doc.setFont('helvetica', 'bold');
      doc.text(exp.company, 15, yPos);
      yPos += 5;
      
      doc.setFontSize(10);
      doc.setFont('helvetica', 'italic');
      doc.text(`${exp.title} | ${exp.period}`, 15, yPos);
      yPos += 5;
      
      doc.setFont('helvetica', 'normal');
      const summaryLines = doc.splitTextToSize(exp.summary, 180);
      doc.text(summaryLines, 15, yPos);
      yPos += summaryLines.length * 4 + 4;
    });
    
    // Technology Experience
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('TECHNOLOGY EXPERIENCE', 15, yPos);
    yPos += 6;
    
    r.technologyExperience.forEach((exp) => {
      if (yPos > 270) {
        doc.addPage();
        yPos = 15;
      }
      
      doc.setFontSize(11);
      doc.setFont('helvetica', 'bold');
      doc.text(exp.company, 15, yPos);
      yPos += 5;
      
      doc.setFontSize(10);
      doc.setFont('helvetica', 'italic');
      doc.text(`${exp.title} | ${exp.period}`, 15, yPos);
      yPos += 5;
      
      doc.setFont('helvetica', 'normal');
      const summaryLines = doc.splitTextToSize(exp.summary, 180);
      doc.text(summaryLines, 15, yPos);
      yPos += summaryLines.length * 4 + 4;
    });
    
    // Education
    if (yPos > 240) {
      doc.addPage();
      yPos = 15;
    }
    
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('EDUCATION', 15, yPos);
    yPos += 6;
    
    [...r.creativeEducation, ...r.technicalEducation].forEach((edu) => {
      doc.setFontSize(11);
      doc.setFont('helvetica', 'bold');
      doc.text(`${edu.degree} - ${edu.school}`, 15, yPos);
      yPos += 5;
      
      doc.setFontSize(10);
      doc.setFont('helvetica', 'italic');
      doc.text(edu.period, 15, yPos);
      yPos += 5;
      
      doc.setFont('helvetica', 'normal');
      const summaryLines = doc.splitTextToSize(edu.summary, 180);
      doc.text(summaryLines, 15, yPos);
      yPos += summaryLines.length * 4 + 5;
    });
    
    doc.save('Max_Gershfield_CV.pdf');
  };
  
  return (
    <div className="min-h-screen w-full bg-gray-100 py-6 print:bg-white print:py-0">
      {/* Export Button */}
      <button
        onClick={exportPDF}
        className="fixed top-4 right-4 z-50 bg-blue-600 hover:bg-blue-700 text-white px-6 py-3 rounded-lg shadow-lg font-semibold transition-all print:hidden"
      >
        📄 Download ATS-Friendly PDF
      </button>
      
      <div id="resume-content" className="mx-auto max-w-5xl bg-white shadow-sm print:shadow-none rounded-xl overflow-hidden print:rounded-none print:max-w-none">
        {/* Header band with avatar */}
        <div className="grid grid-cols-12">
          <div className="col-span-12 md:col-span-4 bg-gradient-to-br from-slate-700 to-slate-500 text-white p-8 relative flex flex-col justify-between">
            {/* angled accent */}
            <div className="absolute right-0 top-0 h-24 w-24 bg-white/10 clip-triangle" />
            <div className="flex flex-col items-center md:items-start gap-4">
              <img src={r.avatarUrl} alt={r.name} className="h-28 w-28 rounded-full object-cover ring-4 ring-white/40" />
              <div>
                <h1 className="text-3xl font-semibold leading-tight">{r.name}</h1>
                <p className="text-white/80 mt-1">{r.role}</p>
                <p className="text-white/60 text-sm mt-2">Interested in: RWAs | Interoperability | Metaverse</p>
              </div>
            </div>
            <div className="text-white/60 text-xs mt-6">
              <p className="text-white/70 text-xs mb-2">Superteam UK contributor</p>
              <p className="text-white/70 text-xs mb-3">4x Grant winner (Solana, Arbitrum, Radix, Thrive)</p>
              <p className="font-medium mb-1">Work examples</p>
              <p>api.assetrail.xyz</p>
              <p>metabricks.xyz</p>
              <p>maxgershfield.co.uk</p>
            </div>
          </div>
          <div className="col-span-12 md:col-span-8 p-8 md:pl-10">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              {/* Contact */}
              <div>
                <h3 className="text-lg font-semibold text-slate-800">Contact</h3>
                <div className="mt-3 space-y-2 text-sm text-slate-700">
                  {r.contact.map((c, i) => (
                    <div key={i} className="flex items-start gap-2">
                      <Icon name={c.icon} />
                      <span>{c.label}</span>
                    </div>
                  ))}
                </div>
              </div>
              {/* About */}
              <div>
                <h3 className="text-lg font-semibold text-slate-800">About Me</h3>
                <p className="mt-3 text-sm text-slate-700 leading-relaxed">{r.about}</p>
              </div>
            </div>
            {/* Skills */}
            <div className="mt-8">
              <h3 className="text-lg font-semibold text-slate-800">Skills</h3>
              <ul className="mt-3 grid grid-cols-1 sm:grid-cols-2 gap-y-2 text-sm text-slate-700">
                {r.skills.map((s, i) => (
                  <li key={i} className="relative pl-5">
                    <span className="absolute left-0 top-2 h-1.5 w-1.5 rounded-full bg-slate-500" />
                    {s}
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>

        {/* Main body */}
        <div className="grid grid-cols-12 p-8 gap-10">
          {/* Left rail for Creative Experience + Education */}
          <div className="col-span-12 md:col-span-6">
            {/* Creative Experience */}
            <div className="flex items-center justify-between border-b border-slate-200 pb-3 mb-6">
              <SectionHeading icon="brief">Creative Experience</SectionHeading>
            </div>
            <div className="relative pl-4 mb-10">
              <div className="absolute left-1 top-0 bottom-0 w-px bg-sky-200" />
              {r.creativeExperience.map((x, i) => (
                <TimelineItem
                  key={i}
                  title={x.company}
                  subtitle={x.title}
                  period={x.period}
                  summary={x.summary}
                />
              ))}
            </div>

            {/* Education */}
            <div className="flex items-center justify-between border-b border-slate-200 pb-3 mb-6 mt-8">
              <SectionHeading icon="edu">Education</SectionHeading>
            </div>
            <div className="relative pl-4">
              <div className="absolute left-1 top-0 bottom-0 w-px bg-sky-200" />
              {r.creativeEducation.map((e, i) => (
                <TimelineItem
                  key={i}
                  title={e.degree}
                  subtitle={e.school}
                  period={e.period}
                  summary={e.summary}
                />
              ))}
            </div>
          </div>

          {/* Right rail for Technology Experience */}
          <div className="col-span-12 md:col-span-6">
            <div className="flex items-center justify-between border-b border-slate-200 pb-3 mb-6">
              <SectionHeading icon="brief">Technology Experience</SectionHeading>
            </div>
            <div className="relative pl-4">
              <div className="absolute left-1 top-0 bottom-0 w-px bg-sky-200" />
              {r.technologyExperience.map((x, i) => (
                <TimelineItem
                  key={i}
                  title={x.company}
                  subtitle={x.title}
                  period={x.period}
                  summary={x.summary}
                />
              ))}
            </div>
            
            {/* Technical Education */}
            <div className="flex items-center justify-between border-b border-slate-200 pb-3 mb-6 mt-3">
              <SectionHeading icon="edu">Technical Education</SectionHeading>
            </div>
            <div className="relative pl-4">
              <div className="absolute left-1 top-0 bottom-0 w-px bg-sky-200" />
              {r.technicalEducation.map((e, i) => (
                <TimelineItem
                  key={i}
                  title={e.degree}
                  subtitle={e.school}
                  period={e.period}
                  summary={e.summary}
                />
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Print helpers */}
      <style>{`
        .clip-triangle { clip-path: polygon(100% 0, 0 0, 100% 100%); }
      `}</style>
    </div>
  );
}

