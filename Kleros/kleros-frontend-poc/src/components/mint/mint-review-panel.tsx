"use client";

import { useMemo, useState } from "react";
import { AssetDraft } from "@/components/assets/asset-upload-panel";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useOasisApi } from "@/hooks/use-oasis-api";
import React from "react";

const SOLANA_ONCHAIN = { value: 3, name: "SolanaOASIS" } as const;
const MONGO_OFFCHAIN = { value: 23, name: "MongoDBOASIS" } as const;
const EXTERNAL_JSON = { value: 3, name: "ExternalJsonURL" } as const;
const SPL_STANDARD = { value: 2, name: "SPL" } as const;

export type MintReviewPanelProps = {
  assetDraft: AssetDraft;
  onStatusChange?: (state: "idle" | "ready") => void;
  onMintStart?: () => void;
  onMintSuccess?: (result: unknown) => void;
  baseUrl: string;
  token?: string;
  avatarId?: string;
};

export function MintReviewPanel({ assetDraft, onStatusChange, onMintStart, onMintSuccess, baseUrl, token, avatarId }: MintReviewPanelProps) {
  const [waitSeconds, setWaitSeconds] = useState(60);
  const [retrySeconds, setRetrySeconds] = useState(5);
  const [waitTillSent, setWaitTillSent] = useState(true);
  const [storeOnChain, setStoreOnChain] = useState(false);
  const [numberToMint, setNumberToMint] = useState(1);
  const [price, setPrice] = useState(0.02);
  const [minting, setMinting] = useState(false);
  const [mintResult, setMintResult] = useState<unknown>(null);
  const [mintError, setMintError] = useState<string | null>(null);
  const [showSuccessModal, setShowSuccessModal] = useState(false);

  const { call } = useOasisApi({ baseUrl, token });

  const payload = useMemo(() => {
    const basePayload: Record<string, unknown> = {
      Title: assetDraft.title,
      Description: assetDraft.description,
      Symbol: assetDraft.symbol,
      OnChainProvider: SOLANA_ONCHAIN,
      OffChainProvider: MONGO_OFFCHAIN,
      NFTOffChainMetaType: EXTERNAL_JSON,
      NFTStandardType: SPL_STANDARD,
      JSONMetaDataURL: assetDraft.jsonUrl,
      ImageUrl: assetDraft.imageUrl,
      ThumbnailUrl: assetDraft.thumbnailUrl,
      Price: price,
      NumberToMint: numberToMint,
      StoreNFTMetaDataOnChain: storeOnChain,
      MintedByAvatarId: avatarId ?? "89d907a8-5859-4171-b6c5-621bfe96930d",
      SendToAddressAfterMinting: assetDraft.sendToAddress,
      WaitTillNFTSent: waitTillSent,
      WaitForNFTToSendInSeconds: waitSeconds,
      AttemptToSendEveryXSeconds: retrySeconds,
    };

    if (assetDraft.imageData) {
      basePayload.Image = assetDraft.imageData;
    }

    if (assetDraft.thumbnailData) {
      basePayload.Thumbnail = assetDraft.thumbnailData;
    }

    if (!assetDraft.jsonUrl && assetDraft.imageData) {
      basePayload.NFTOffChainMetaType = { value: 1, name: "OASIS" };
    }

    return basePayload;
  }, [assetDraft, avatarId, numberToMint, price, retrySeconds, storeOnChain, waitSeconds, waitTillSent]);

  const mintDisabled = useMemo(() => {
    const hasJson = Boolean(assetDraft.jsonUrl);
    const hasImage = Boolean(assetDraft.imageUrl);
    const hasUploads = Boolean(assetDraft.imageData);
    const hasRecipient = Boolean(assetDraft.sendToAddress);
    const hasTitle = Boolean(assetDraft.title);
    const hasDescription = Boolean(assetDraft.description);
    const hasSymbol = Boolean(assetDraft.symbol);
    return (!(hasJson && hasImage) && !hasUploads) || !hasRecipient || !hasTitle || !hasDescription || !hasSymbol;
  }, [assetDraft]);

  const missingFields = useMemo(() => {
    const items: string[] = [];
    if (!assetDraft.title) items.push("title");
    if (!assetDraft.symbol) items.push("symbol");
    if (!assetDraft.description) items.push("description");
    if (!assetDraft.sendToAddress) items.push("recipient wallet");
    if (!assetDraft.imageData) {
      if (!assetDraft.imageUrl) items.push("image url");
      if (!assetDraft.jsonUrl) items.push("json metadata url");
    }
    return items;
  }, [assetDraft]);

  return (
    <div className="space-y-6">
      <div className="grid gap-3 md:grid-cols-2">
        <InputField label="Price (SOL)" value={price} onChange={(value) => setPrice(value)} />
        <InputField label="Number To Mint" value={numberToMint} onChange={(value) => setNumberToMint(Math.max(1, value))} />
        <Toggle label="Store Metadata On Chain" value={storeOnChain} onChange={setStoreOnChain} />
        <Toggle label="Wait Till NFT Sent" value={waitTillSent} onChange={setWaitTillSent} />
        <InputField label="Wait Seconds" value={waitSeconds} onChange={(value) => setWaitSeconds(Math.max(0, value))} />
        <InputField label="Retry Interval" value={retrySeconds} onChange={(value) => setRetrySeconds(Math.max(1, value))} />
      </div>

      <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-4 text-sm text-[var(--muted)]">
        <p className="text-xs uppercase tracking-[0.4em] text-[var(--accent)]">Summary</p>
        <div className="mt-3 grid gap-2 md:grid-cols-2">
          <SummaryField label="Title" value={assetDraft.title || "—"} />
          <SummaryField label="Symbol" value={assetDraft.symbol || "—"} />
          <SummaryField label="Description" value={assetDraft.description || "—"} multiline />
          <SummaryField label="Recipient Wallet" value={assetDraft.sendToAddress || "—"} />
          <SummaryField label="Metadata URL" value={assetDraft.jsonUrl || "Will be generated"} />
          <SummaryField label="Image Source" value={renderSource(assetDraft.imageUrl, assetDraft.imageData)} />
          <SummaryField label="Thumbnail Source" value={renderSource(assetDraft.thumbnailUrl, assetDraft.thumbnailData)} />
        </div>
      </div>

      <div className="flex flex-col gap-3 border-t border-[var(--color-card-border)]/30 pt-4">
        <p className="text-sm text-[var(--muted)]">
          If you included uploads above, the mint endpoint will push files to Pinata automatically; otherwise ensure the URLs are set.
        </p>
        {mintDisabled && missingFields.length ? (
          <p className="text-xs text-[var(--warning)]">
            Complete before minting: {missingFields.join(", ")}
          </p>
        ) : null}
        <div className="flex flex-wrap gap-3">
          <Button
            variant="primary"
            disabled={mintDisabled || minting}
            onClick={async () => {
              try {
                onMintStart?.();
                onStatusChange?.("idle");
                setMinting(true);
                setMintError(null);
                setMintResult(null);
                const response = await call("/api/nft/mint-nft", {
                  method: "POST",
                  body: JSON.stringify(payload),
                });

                setMintResult(response);
                console.log("[mint] API response:", JSON.stringify(response, null, 2));

                // Check if the response indicates actual success
                const responseObj = response as Record<string, unknown> | null;
                const isSuccess = responseObj?.IsError === false || responseObj?.isError === false;
                const errorMessage = responseObj?.Message || responseObj?.message || responseObj?.ErrorMessage;
                const innerMessages = responseObj?.InnerMessages;

                if (!isSuccess && errorMessage) {
                  console.error("[mint] Error details:", {
                    message: errorMessage,
                    innerMessages,
                    fullResponse: responseObj,
                  });
                  let fullError = String(errorMessage);
                  if (innerMessages) {
                    fullError += `\n\nInner Messages: ${JSON.stringify(innerMessages, null, 2)}`;
                  }
                  throw new Error(fullError);
                }

                // Check if there's an error in nested result
                const result = responseObj?.result as Record<string, unknown> | undefined;
                if (result?.IsError === true || result?.isError === true) {
                  const nestedError = result?.Message || result?.message || result?.ErrorMessage;
                  const nestedInner = result?.InnerMessages;
                  console.error("[mint] Nested error:", {
                    message: nestedError,
                    innerMessages: nestedInner,
                    fullResult: result,
                  });
                  let fullError = String(nestedError || "Minting failed");
                  if (nestedInner) {
                    fullError += `\n\nInner Messages: ${JSON.stringify(nestedInner, null, 2)}`;
                  }
                  throw new Error(fullError);
                }

                onStatusChange?.("ready");
                onMintSuccess?.(response);
                setShowSuccessModal(true);
              } catch (error: unknown) {
                const message = error instanceof Error ? error.message : "Minting failed";
                setMintError(message);
                console.error("[mint] failed:", error);
                console.error("[mint] full error:", JSON.stringify(error, null, 2));
                onStatusChange?.("idle");
              } finally {
                setMinting(false);
              }
            }}
          >
            {minting ? "Minting..." : "Mint via OASIS API"}
          </Button>
        </div>
        {mintError ? <p className="text-xs text-[var(--negative)]">{mintError}</p> : null}
      </div>
      <div className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(4,8,24,0.95)] p-4 text-xs text-[var(--muted)]">
        <pre className="max-h-80 overflow-auto">{JSON.stringify(payload, null, 2)}</pre>
      </div>
      {showSuccessModal ? (
        <MintSuccessModal
          onClose={() => setShowSuccessModal(false)}
          response={mintResult}
        />
      ) : null}
    </div>
  );
}

function Toggle({ label, value, onChange }: { label: string; value: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="flex items-center justify-between rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] px-3 py-2 text-xs text-[var(--muted)]">
      <span className="text-[var(--color-foreground)]">{label}</span>
      <Button variant="toggle" data-state={value ? "active" : undefined} onClick={() => onChange(!value)} className="px-3 py-1">
        {value ? "Enabled" : "Disabled"}
      </Button>
    </label>
  );
}

function InputField({ label, value, onChange }: { label: string; value: number; onChange: (value: number) => void }) {
  return (
    <label className="flex flex-col gap-2 text-xs text-[var(--muted)]">
      <span className="text-[var(--color-foreground)]">{label}</span>
      <input
        type="number"
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
        className="rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
      />
    </label>
  );
}

function SummaryField({ label, value, multiline }: { label: string; value: string; multiline?: boolean }) {
  return (
    <div className="rounded-lg border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] px-3 py-2">
      <p className="text-[10px] uppercase tracking-[0.35em] text-[var(--muted)]">{label}</p>
      <p className={cn("text-xs text-[var(--color-foreground)]", multiline ? "mt-2" : "mt-1")}>{value}</p>
    </div>
  );
}

function renderSource(url?: string, data?: string) {
  if (data) return "Upload (Pinata)";
  return url || "—";
}

function MintSuccessModal({ onClose }: { onClose: () => void; response: unknown }) {
  React.useEffect(() => {
    const handler = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [onClose]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm">
      <div className="w-full max-w-md rounded-3xl border border-[var(--color-card-border)]/60 bg-[rgba(6,11,26,0.95)] p-8 text-center">
        <h3 className="text-2xl font-semibold text-[var(--color-foreground)]">Mint successful!</h3>
        <p className="mt-3 text-sm text-[var(--muted)]">Check wallet for NFT</p>
        <div className="mt-6 flex flex-col gap-3">
          <Button variant="primary" onClick={onClose}>
            Close
          </Button>
        </div>
      </div>
    </div>
  );
}
