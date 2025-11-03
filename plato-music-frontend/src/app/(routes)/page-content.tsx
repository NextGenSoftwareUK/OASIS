"use client";

import { useMemo, useState } from "react";
import { AppLayout } from "@/components/layout/app-layout";
import { WizardShell } from "@/components/wizard/wizard-shell";
import { CatalogConfigPanel } from "@/components/music/catalog-config-panel";
import { RoyaltyTemplateSelector } from "@/components/templates/royalty-template-selector";
import { MicroLicenseTemplateSelector } from "@/components/templates/micro-license-template-selector";
import { TemplatePreview } from "@/components/templates/template-preview";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useOasisApi } from "@/hooks/use-oasis-api";
import { MusicTrack, RoyaltySplitTemplate, MicroLicenseTemplate } from "@/types/music";

const DEVNET_URL = "http://devnet.oasisweb4.one";
const LOCAL_URL = "https://localhost:5004";

const WIZARD_STEPS = [
  {
    id: "catalog-config",
    title: "Catalog Configuration",
    description: "Configure your music catalog and track information.",
  },
  {
    id: "templates",
    title: "Royalty & Licensing Templates",
    description: "Set up royalty splits and micro-licensing templates.",
  },
  {
    id: "rights-holders",
    title: "Rights Holders",
    description: "Configure rights holders and wallet addresses.",
  },
  {
    id: "deploy",
    title: "Deploy & Mint",
    description: "Review configuration and deploy smart contracts.",
  },
];

const CHECKLIST = [
  "Configure music catalog and track information",
  "Set up royalty split templates (40/30/30, 50/25/25, etc.)",
  "Configure micro-licensing templates (TikTok $0.50, Instagram $0.75, etc.)",
  "Add rights holders with wallet addresses",
  "Deploy smart contracts and mint music tokens",
];

export default function PageContent() {
  const [activeStep, setActiveStep] = useState<string>(WIZARD_STEPS[0]?.id ?? "catalog-config");
  const [configPreset, setConfigPreset] = useState<string>("Single Track");
  const [musicTrack, setMusicTrack] = useState<MusicTrack>({
    id: "",
    title: "",
    artist: "",
    album: "",
    isrc: "",
    genre: "",
    duration: 0,
    bpm: undefined,
    key: undefined,
    mood: [],
    releaseDate: new Date()
  });
  const [royaltyTemplate, setRoyaltyTemplate] = useState<RoyaltySplitTemplate | null>(null);
  const [microLicenseTemplate, setMicroLicenseTemplate] = useState<MicroLicenseTemplate | null>(null);
  const [statusState, setStatusState] = useState<"idle" | "ready">("idle");
  const [deployReady, setDeployReady] = useState(false);
  const [authToken, setAuthToken] = useState<string | null>(null);
  const [avatarId, setAvatarId] = useState<string | null>(null);
  const [useLocalApi, setUseLocalApi] = useState(false);

  const baseUrl = useLocalApi ? LOCAL_URL : DEVNET_URL;

  const { call } = useOasisApi({ baseUrl, token: authToken ?? undefined });

  const canProceed = useMemo(() => {
    switch (activeStep) {
      case "catalog-config":
        return Boolean(musicTrack.title && musicTrack.artist && musicTrack.genre);
      case "templates":
        return Boolean(royaltyTemplate && microLicenseTemplate);
      case "rights-holders":
        return Boolean(royaltyTemplate?.splits.every(split => split.walletAddress));
      case "deploy":
        return Boolean(musicTrack.title && royaltyTemplate && microLicenseTemplate);
      default:
        return false;
    }
  }, [activeStep, musicTrack.title, musicTrack.artist, musicTrack.genre, royaltyTemplate, microLicenseTemplate]);

  const renderSessionSummary = (
    <div className="flex flex-wrap items-center gap-4 rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(8,12,26,0.7)] px-4 py-3 text-[11px] text-[var(--muted)]">
      <span className="text-[9px] uppercase tracking-[0.4em] text-[var(--muted)]">Session Summary</span>
      <div className="flex items-center gap-3">
        <span className="text-[var(--accent)] text-xs font-semibold">Catalog Type</span>
        <span>{configPreset}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-[var(--accent)] text-xs font-semibold">Track</span>
        <span>{musicTrack.title || "—"}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-[var(--accent)] text-xs font-semibold">Royalty Template</span>
        <span>{royaltyTemplate?.name || "—"}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-[var(--accent)] text-xs font-semibold">Micro-License</span>
        <span>{microLicenseTemplate?.name || "—"}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-[var(--accent)] text-xs font-semibold">Checklist</span>
        <span>{CHECKLIST.length} tasks</span>
      </div>
    </div>
  );

  // Template management functions
  const handleRoyaltyTemplateSelect = (template: RoyaltySplitTemplate) => {
    setRoyaltyTemplate(template);
  };

  const handleMicroLicenseTemplateSelect = (template: MicroLicenseTemplate) => {
    setMicroLicenseTemplate(template);
  };

  return (
    <AppLayout
      sidebar={null}
      footer={
        <div className="flex flex-col items-center gap-3 rounded-2xl border border-[var(--color-card-border)]/40 bg-[rgba(7,10,26,0.75)] p-6 text-center text-sm text-[var(--muted)] md:flex-row md:justify-between md:text-left">
          <div>
            <p className="text-[var(--color-foreground)]">Plato Music Licensing Platform</p>
            <p>OASIS WEB4 Devnet • Smart Contract Deployment</p>
          </div>
          <p className="text-xs uppercase tracking-[0.4em] text-[var(--muted)]">Music Tokenization • Automated Royalty Splits</p>
        </div>
      }
    >
      <section id="wizard" className="space-y-6">
        <div>
          <p className="text-sm uppercase tracking-[0.4em] text-[var(--muted)]">Music Catalog Tokenization</p>
          <div className="flex flex-col gap-4">
            <div className="flex flex-wrap items-center gap-4">
              <h2 className="mt-2 text-3xl font-semibold text-[var(--color-foreground)]">
                Tokenize your music catalog with automated royalty splits
              </h2>
              <span
                className={cn(
                  "mt-2 h-fit rounded-full border px-3 py-1 text-xs uppercase tracking-[0.4em]",
                  statusState === "ready" && deployReady
                    ? "border-[var(--color-positive)]/60 bg-[rgba(20,118,96,0.25)] text-[var(--color-positive)]"
                    : "border-[var(--negative)]/60 bg-[rgba(120,35,50,0.2)] text-[var(--negative)]"
                )}
              >
                {statusState === "ready" && deployReady ? "Ready To Deploy" : "Pending Configuration"}
              </span>
            </div>
            {renderSessionSummary}
          </div>
          <p className="mt-3 max-w-3xl text-sm leading-relaxed text-[var(--muted)]">
            Configure your music catalog, set up automated royalty splits, and enable micro-licensing for platforms like TikTok and Instagram.
            Each track gets its own smart contract with configurable royalty distribution and per-use licensing.
          </p>
        </div>
        <WizardShell
          steps={WIZARD_STEPS}
          activeStep={activeStep}
          onStepChange={setActiveStep}
          footer={
            <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
              <div className="flex flex-wrap gap-2 text-[11px] text-[var(--muted)]">
                <span>Need help? Follow the checklist above.</span>
              </div>
              <div className="flex gap-3">
                <Button
                  variant="secondary"
                  disabled={activeStep === WIZARD_STEPS[0]?.id}
                  onClick={() => {
                    const currentIndex = WIZARD_STEPS.findIndex((step) => step.id === activeStep);
                    if (currentIndex > 0) {
                      setActiveStep(WIZARD_STEPS[currentIndex - 1].id);
                    }
                  }}
                >
                  Previous
                </Button>
                <Button
                  variant="primary"
                  onClick={() => {
                    const currentIndex = WIZARD_STEPS.findIndex((step) => step.id === activeStep);
                    const nextStep = WIZARD_STEPS[currentIndex + 1];
                    if (nextStep) {
                      setActiveStep(nextStep.id);
                    }
                  }}
                  disabled={activeStep === WIZARD_STEPS[WIZARD_STEPS.length - 1]?.id || !canProceed}
                >
                  Next
                </Button>
              </div>
            </div>
          }
        >
          {activeStep === "catalog-config" ? (
            <CatalogConfigPanel 
              selectedOption={configPreset} 
              onSelect={(option) => setConfigPreset(option)}
              track={musicTrack}
              onTrackChange={setMusicTrack}
            />
          ) : null}
          
          {activeStep === "templates" ? (
            <div className="space-y-8">
              <div>
                <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Royalty Split Template</h3>
                <p className="mt-2 text-sm text-[var(--muted)]">
                  Choose a pre-defined royalty split template or create a custom one for your track.
                </p>
                <div className="mt-4">
                  <RoyaltyTemplateSelector
                    selectedTemplateId={royaltyTemplate?.id}
                    onTemplateSelect={handleRoyaltyTemplateSelect}
                    onCreateCustom={() => {
                      // TODO: Implement custom template creation modal
                      console.log("Create custom royalty template");
                    }}
                  />
                </div>
              </div>
              
              <div>
                <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Micro-License Template</h3>
                <p className="mt-2 text-sm text-[var(--muted)]">
                  Configure per-use licensing for platforms like TikTok, Instagram, and YouTube.
                </p>
                <div className="mt-4">
                  <MicroLicenseTemplateSelector
                    selectedTemplateId={microLicenseTemplate?.id}
                    onTemplateSelect={handleMicroLicenseTemplateSelect}
                    onCreateCustom={() => {
                      // TODO: Implement custom template creation modal
                      console.log("Create custom micro-license template");
                    }}
                  />
                </div>
              </div>
            </div>
          ) : null}
          
          {activeStep === "rights-holders" ? (
            <div className="space-y-8">
              <div>
                <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Rights Holders Configuration</h3>
                <p className="mt-2 text-sm text-[var(--muted)]">
                  Configure wallet addresses for each rights holder based on your selected royalty template.
                </p>
                {royaltyTemplate && (
                  <div className="mt-6 rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
                    <h4 className="text-lg font-semibold text-[var(--color-foreground)] mb-4">Configure Wallet Addresses</h4>
                    <div className="space-y-4">
                      {royaltyTemplate.splits.map((split, index) => (
                        <div key={index} className="flex items-center gap-4 p-4 rounded-lg border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)]">
                          <div className="flex-1">
                            <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
                              {split.role} ({split.percentage}%)
                            </label>
                            <input
                              type="text"
                              value={split.walletAddress || ""}
                              onChange={(e) => {
                                // TODO: Update wallet address in template
                                console.log(`Update wallet for ${split.role}:`, e.target.value);
                              }}
                              placeholder="Enter wallet address (0x...)"
                              className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
                            />
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>
          ) : null}
          
          {activeStep === "deploy" ? (
            <div className="space-y-8">
              <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
                <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Deploy Music Licensing Contract</h3>
                <p className="mt-2 text-sm text-[var(--muted)]">
                  Review your configuration and deploy the smart contract with your royalty splits and micro-licensing setup.
                </p>
                
                <div className="mt-6 space-y-6">
                  <TemplatePreview 
                    royaltyTemplate={royaltyTemplate || undefined}
                    microLicenseTemplate={microLicenseTemplate || undefined}
                  />
                  
                  <div className="rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(4,8,24,0.95)] p-4">
                    <h4 className="text-sm font-medium text-[var(--color-foreground)] mb-3">Deployment Summary</h4>
                    <div className="space-y-2 text-sm text-[var(--muted)]">
                      <div className="flex justify-between">
                        <span>Track:</span>
                        <span className="text-[var(--color-foreground)]">{musicTrack.title}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Artist:</span>
                        <span className="text-[var(--color-foreground)]">{musicTrack.artist}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Genre:</span>
                        <span className="text-[var(--color-foreground)]">{musicTrack.genre}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Royalty Template:</span>
                        <span className="text-[var(--color-foreground)]">{royaltyTemplate?.name}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Micro-License Template:</span>
                        <span className="text-[var(--color-foreground)]">{microLicenseTemplate?.name}</span>
                      </div>
                    </div>
                  </div>
                  
                  <div className="flex gap-3">
                    <Button
                      variant="primary"
                      onClick={() => {
                        // TODO: Implement contract deployment
                        console.log("Deploy music licensing contract");
                        setDeployReady(true);
                        setStatusState("ready");
                      }}
                      className="flex-1"
                    >
                      Deploy Contract
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          ) : null}
        </WizardShell>
      </section>
      <Button
        variant="secondary"
        className="text-[10px]"
        onClick={() => {
          setUseLocalApi((prev) => !prev);
          setStatusState("idle");
          setDeployReady(false);
          setAuthToken(null);
          setAvatarId(null);
          setRoyaltyTemplate(null);
          setMicroLicenseTemplate(null);
          setMusicTrack({
            id: "",
            title: "",
            artist: "",
            album: "",
            isrc: "",
            genre: "",
            duration: 0,
            bpm: undefined,
            key: undefined,
            mood: [],
            releaseDate: new Date()
          });
        }}
      >
        {useLocalApi ? "Switch to Devnet" : "Use Local API"}
      </Button>
    </AppLayout>
  );
}
