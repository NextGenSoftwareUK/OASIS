"use client";

import { useMemo, useState } from "react";
import { SOLANA_CHAIN } from "@/types/chains";
import { AppLayout } from "@/components/layout/app-layout";
import { WizardShell } from "@/components/wizard/wizard-shell";
import { SolanaConfigStep } from "@/components/wizard/chain-step";
import { CredentialsPanel } from "@/components/auth/credentials-panel";
import { ProviderTogglePanel, ProviderToggle } from "@/components/auth/provider-toggle-panel";
import { AssetUploadPanel, DEFAULT_ASSET_DRAFT, AssetDraft } from "@/components/assets/asset-upload-panel";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { MintReviewPanel } from "@/components/mint/mint-review-panel";
import { useOasisApi } from "@/hooks/use-oasis-api";

const DEVNET_URL = "http://devnet.oasisweb4.one";
const LOCAL_URL = "https://localhost:5004";

const WIZARD_STEPS = [
  {
    id: "solana-config",
    title: "Solana Configuration",
    description: "Select the minting profile you want to use (Metaplex, editions, compression).",
  },
  {
    id: "auth",
    title: "Authenticate & Providers",
    description: "Login with Site Avatar credentials and activate SolanaOASIS + MongoDBOASIS.",
  },
  {
    id: "assets",
    title: "Assets & Metadata",
    description: "Upload artwork, thumbnails, and JSON metadata placeholders.",
  },
  {
    id: "mint",
    title: "Review & Mint",
    description: "Generate the PascalCase payload and fire `/api/nft/mint-nft`.",
  },
];

const CHECKLIST = [
  "Authenticate with metabricks_admin credentials",
  "Register & activate SolanaOASIS provider",
  "Confirm MongoDBOASIS is active for metadata storage",
  "Upload image + JSON URLs and validate IPFS availability",
  "Review payload casing and enum object formats",
];

export default function PageContent() {
  const [activeStep, setActiveStep] = useState<string>(WIZARD_STEPS[0]?.id ?? "solana-config");
  const [configPreset, setConfigPreset] = useState<string>("Metaplex Standard");
  const [providerStates, setProviderStates] = useState<ProviderToggle[]>([
    {
      id: "solana",
      label: "SolanaOASIS",
      description: "Handles on-chain mint + transfer across Solana devnet",
      registerEndpoint: "/api/provider/register-provider-type/SolanaOASIS",
      activateEndpoint: "/api/provider/activate-provider/SolanaOASIS",
      state: "idle",
    },
    {
      id: "mongo",
      label: "MongoDBOASIS",
      description: "Stores off-chain metadata JSON for NFTs",
      registerEndpoint: "/api/provider/register-provider-type/MongoDBOASIS",
      activateEndpoint: "/api/provider/activate-provider/MongoDBOASIS",
      state: "idle",
    },
  ]);
  const [statusState, setStatusState] = useState<"idle" | "ready">("idle");
  const [assetDraft, setAssetDraft] = useState<AssetDraft>(DEFAULT_ASSET_DRAFT);
  const [mintReady, setMintReady] = useState(false);
  const [authToken, setAuthToken] = useState<string | null>(null);
  const [avatarId, setAvatarId] = useState<string | null>(null);
  const [providerLoading, setProviderLoading] = useState<string[]>([]);
  const [useLocalApi, setUseLocalApi] = useState(false);

  const baseUrl = useLocalApi ? LOCAL_URL : DEVNET_URL;

  const { call } = useOasisApi({ baseUrl, token: authToken ?? undefined });

  const providerActive = useMemo(() => providerStates.every((provider) => provider.state === "active"), [providerStates]);

  const canProceed = useMemo(() => {
    switch (activeStep) {
      case "solana-config":
        return true;
      case "auth":
        return Boolean(authToken && providerActive);
      case "assets":
        return Boolean(assetDraft.imageUrl && assetDraft.jsonUrl && assetDraft.sendToAddress);
      default:
        return false;
    }
  }, [activeStep, authToken, providerActive, assetDraft.imageUrl, assetDraft.jsonUrl, assetDraft.sendToAddress]);

  const renderSessionSummary = (
    <div className="flex flex-wrap items-center gap-4 rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(8,12,26,0.7)] px-4 py-3 text-[11px] text-[var(--muted)]">
      <span className="text-[9px] uppercase tracking-[0.4em] text-[var(--muted)]">Session Summary</span>
      <div className="flex items-center gap-3">
        <span className="text-[var(--accent)] text-xs font-semibold">Profile</span>
        <span>{configPreset}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-[var(--accent)] text-xs font-semibold">On-chain</span>
        <span>{`${SOLANA_CHAIN.providerMapping.onChain.name} (${SOLANA_CHAIN.providerMapping.onChain.value})`}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-[var(--accent)] text-xs font-semibold">Off-chain</span>
        <span>{`${SOLANA_CHAIN.providerMapping.offChain.name} (${SOLANA_CHAIN.providerMapping.offChain.value})`}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-[var(--accent)] text-xs font-semibold">Checklist</span>
        <span>{CHECKLIST.length} tasks</span>
      </div>
    </div>
  );

  const updateProviderState = (id: string, state: ProviderToggle["state"]) => {
    setProviderStates((prev) =>
      prev.map((p) => {
        if (p.id !== id) return p;
        return { ...p, state };
      })
    );
  };

  return (
    <AppLayout
      sidebar={null}
      footer={
        <div className="flex flex-col items-center gap-3 rounded-2xl border border-[var(--color-card-border)]/40 bg-[rgba(7,10,26,0.75)] p-6 text-center text-sm text-[var(--muted)] md:flex-row md:justify-between md:text-left">
          <div>
            <p className="text-[var(--color-foreground)]">OASIS WEB4 Devnet</p>
            <p>Swagger docs: http://oasisweb4.one/swagger</p>
          </div>
          <p className="text-xs uppercase tracking-[0.4em] text-[var(--muted)]">Solana Track • MetaBricks</p>
        </div>
      }
    >
      <section id="wizard" className="space-y-6">
        <div>
          <p className="text-sm uppercase tracking-[0.4em] text-[var(--muted)]">Solana Configuration</p>
          <div className="flex flex-col gap-4">
            <div className="flex flex-wrap items-center gap-4">
              <h2 className="mt-2 text-3xl font-semibold text-[var(--color-foreground)]">
                Configure and mint NFTs via unified OASIS providers
              </h2>
              <span
                className={cn(
                  "mt-2 h-fit rounded-full border px-3 py-1 text-xs uppercase tracking-[0.4em]",
                  statusState === "ready" && mintReady
                    ? "border-[var(--color-positive)]/60 bg-[rgba(20,118,96,0.25)] text-[var(--color-positive)]"
                    : "border-[var(--negative)]/60 bg-[rgba(120,35,50,0.2)] text-[var(--negative)]"
                )}
              >
                {statusState === "ready" && mintReady ? "Ready To Mint" : "Pending Configuration"}
              </span>
            </div>
            {renderSessionSummary}
          </div>
          <p className="mt-3 max-w-3xl text-sm leading-relaxed text-[var(--muted)]">
            Choose the mint type, validate provider readiness, and assemble the exact payload required by `/api/nft/mint-nft`.
            Each configuration ensures compliance with Metaplex and OASIS contract expectations.
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
          {activeStep === "solana-config" ? (
            <SolanaConfigStep selectedOption={configPreset} onSelect={(option) => setConfigPreset(option)} />
          ) : null}
          {activeStep === "auth" ? (
            <div className="space-y-8">
              <div>
                <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Authenticate Site Avatar</h3>
                <p className="mt-2 text-sm text-[var(--muted)]">
                  Enter the Site Avatar credentials to obtain a JWT. The token is required for every subsequent provider call.
                </p>
                <CredentialsPanel
                  baseUrl={baseUrl}
                  onAuthenticate={(creds) => {
                    console.log("Authenticate", creds);
                  }}
                  onAcquireAvatar={() => {
                    window.open("https://metabricks.xyz", "_blank", "noopener,noreferrer");
                  }}
                  onToken={(token) => {
                    setAuthToken(token);
                  }}
                  onAuthenticated={({ token, avatarId }) => {
                    setAuthToken(token);
                    if (avatarId) setAvatarId(avatarId);
                  }}
                  onSuccess={() => {
                    const currentIndex = WIZARD_STEPS.findIndex((step) => step.id === "auth");
                    const nextStep = WIZARD_STEPS[currentIndex + 1];
                    if (nextStep) {
                      setActiveStep(nextStep.id);
                    }
                  }}
                />
              </div>
              <div>
                <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Register & Activate Providers</h3>
                <p className="mt-2 text-sm text-[var(--muted)]">
                  Flip the toggles to enable SolanaOASIS and MongoDBOASIS. Both must show Active before minting.
                </p>
                <ProviderTogglePanel
                  providers={providerStates}
                  loadingIds={providerLoading}
                  onRegister={async (provider) => {
                    if (!authToken) {
                      console.warn("Authenticate before registering providers");
                      throw new Error("You must authenticate first");
                    }
                    if (provider.state !== "idle") return true;
                    setProviderLoading((prev) => [...prev, provider.id]);
                    try {
                      await call(provider.registerEndpoint, { method: "POST" });
                      updateProviderState(provider.id, "registered");
                      return true;
                    } catch (error) {
                      console.error("Register provider failed", error);
                      throw error instanceof Error ? error : new Error("Register provider failed");
                    } finally {
                      setProviderLoading((prev) => prev.filter((id) => id !== provider.id));
                    }
                  }}
                  onActivate={async (provider) => {
                    if (!authToken) {
                      console.warn("Authenticate before activating providers");
                      throw new Error("You must authenticate first");
                    }
                    if (provider.state !== "registered") return true;
                    setProviderLoading((prev) => [...prev, provider.id]);
                    try {
                      await call(provider.activateEndpoint, { method: "POST" });
                      updateProviderState(provider.id, "active");
                      return true;
                    } catch (error) {
                      console.error("Activate provider failed", error);
                      throw error instanceof Error ? error : new Error("Activate provider failed");
                    } finally {
                      setProviderLoading((prev) => prev.filter((id) => id !== provider.id));
                    }
                  }}
                  onError={(provider, mode, error) => {
                    alert(`${provider.label} ${mode} failed: ${error.message}`);
                  }}
                />
              </div>
            </div>
          ) : null}
          {activeStep === "assets" ? (
            <AssetUploadPanel
              value={assetDraft}
              onChange={setAssetDraft}
              token={authToken ?? undefined}
              onNext={() => setActiveStep("mint")}
            />
          ) : null}
          {activeStep === "mint" ? (
            <div className="space-y-8">
              <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
                <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Mint Configuration</h3>
                <p className="mt-2 text-sm text-[var(--muted)]">
                  Review the payload assembled from previous steps. Adjust mint timing options and submit to `/api/nft/mint-nft`.
                </p>
                <MintReviewPanel
                  assetDraft={assetDraft}
                  avatarId={avatarId ?? undefined}
                  onStatusChange={(state) => {
                    setMintReady(state === "ready");
                  }}
                  onMintStart={() => {
                    setStatusState("idle");
                  }}
                  onMintSuccess={() => {
                    setStatusState("ready");
                  }}
                  baseUrl={baseUrl}
                  token={authToken ?? undefined}
                />
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
          setProviderStates((prev) => prev.map((p) => ({ ...p, state: "idle" })));
          setStatusState("idle");
          setAuthToken(null);
          setAvatarId(null);
        }}
      >
        {useLocalApi ? "Switch to Devnet" : "Use Local API"}
      </Button>
    </AppLayout>
  );
}
