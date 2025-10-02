"use client";

import { useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const PINATA_FILE_ENDPOINT = "/api/pinata/upload-file";
const PINATA_JSON_ENDPOINT = "/api/pinata/upload-json";

export type AssetDraft = {
  title: string;
  description: string;
  symbol: string;
  jsonUrl: string;
  imageUrl: string;
  thumbnailUrl: string;
  sendToAddress: string;
  recipientLabel?: string;
  imageFileName?: string;
  thumbnailFileName?: string;
  imageData?: string;
  thumbnailData?: string;
  imageUploading?: boolean;
  thumbnailUploading?: boolean;
  metadataUploading?: boolean;
};

export const DEFAULT_ASSET_DRAFT: AssetDraft = {
  title: "MetaBrick Test NFT",
  description: "Test NFT minted via devnet.oasisweb4.one",
  symbol: "MBRICK",
  jsonUrl: "",
  imageUrl: "",
  thumbnailUrl: "",
  sendToAddress: "85ArqfA2fy8spGcMGsSW7cbEJAWj26vewmmoG2bwkgT9",
  recipientLabel: "Primary Recipient",
  imageFileName: undefined,
  thumbnailFileName: undefined,
  imageData: undefined,
  thumbnailData: undefined,
};

export type AssetUploadPanelProps = {
  value?: AssetDraft;
  onChange?: (draft: AssetDraft) => void;
  token?: string;
};

export function AssetUploadPanel({ value, onChange, token }: AssetUploadPanelProps) {
  const [draft, setDraft] = useState<AssetDraft>(value ?? DEFAULT_ASSET_DRAFT);

  useEffect(() => {
    if (value) {
      setDraft(value);
    }
  }, [value]);

  const updateDraft = (patch: Partial<AssetDraft>) => {
    const next = { ...draft, ...patch };
    setDraft(next);
    onChange?.(next);
  };


  const previewPayload = useMemo(
    () => ({
      Title: draft.title,
      Description: draft.description,
      Symbol: draft.symbol,
      JSONMetaDataURL: draft.jsonUrl,
      ImageUrl: draft.imageUrl,
      ThumbnailUrl: draft.thumbnailUrl,
      SendToAddressAfterMinting: draft.sendToAddress,
    }),
    [draft]
  );

  const uploadToPinata = async (kind: "image" | "thumbnail", base64: string, fileName?: string, contentType?: string) => {
    if (!base64) return;
    const endpoint = PINATA_FILE_ENDPOINT;

    const setterKey = kind === "image" ? "imageUploading" : "thumbnailUploading";
    updateDraft({ [setterKey]: true } as Partial<AssetDraft>);

    try {
      const response = await fetch(endpoint, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({
          base64,
          fileName,
          contentType,
        }),
      });

      const json: { isError?: boolean; message?: string; result?: string } = await response.json();
      if (!response.ok || json?.isError) {
        throw new Error(json?.message ?? "Failed to upload file to Pinata");
      }

      const url = json?.result ?? "";
      if (kind === "image") {
        updateDraft({ imageUrl: url, imageData: undefined, imageUploading: false });
      } else {
        updateDraft({ thumbnailUrl: url, thumbnailData: undefined, thumbnailUploading: false });
      }
    } catch (error) {
      console.error("Pinata upload failed", error);
      updateDraft({ [setterKey]: false } as Partial<AssetDraft>);
    }
  };

  const uploadMetadataJson = async () => {
    const endpoint = PINATA_JSON_ENDPOINT;
    const metadata = {
      name: draft.title,
      symbol: draft.symbol,
      description: draft.description,
      image: draft.imageUrl,
      external_url: draft.thumbnailUrl || undefined,
      properties: {
        files: draft.imageUrl
          ? [
              {
                uri: draft.imageUrl,
                type: "image/png",
              },
            ]
          : [],
      },
    };

    updateDraft({ metadataUploading: true });

    try {
      const response = await fetch(endpoint, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({
          content: metadata,
          name: draft.title || "Metadata",
        }),
      });

      const json: { isError?: boolean; message?: string; result?: string } = await response.json();
      if (!response.ok || json?.isError) {
        throw new Error(json?.message ?? "Failed to upload metadata to Pinata");
      }

      const url = json?.result ?? "";
      updateDraft({ jsonUrl: url, metadataUploading: false });
    } catch (error) {
      console.error("Metadata upload failed", error);
      updateDraft({ metadataUploading: false });
    }
  };

  const handleFileSelect = async (file: File | null, kind: "image" | "thumbnail") => {
    if (!file) {
      if (kind === "image") {
        updateDraft({ imageFileName: undefined, imageData: undefined, imageUploading: false });
      } else {
        updateDraft({ thumbnailFileName: undefined, thumbnailData: undefined, thumbnailUploading: false });
      }
      return;
    }

    try {
      const base64 = await fileToBase64(file);
      if (kind === "image") {
        updateDraft({ imageFileName: file.name, imageData: base64, imageUploading: true });
      } else {
        updateDraft({ thumbnailFileName: file.name, thumbnailData: base64, thumbnailUploading: true });
      }

      await uploadToPinata(kind, base64, file.name, file.type);
    } catch (error) {
      console.error("Failed to process file", error);
      if (kind === "image") {
        updateDraft({ imageUploading: false });
      } else {
        updateDraft({ thumbnailUploading: false });
      }
    }
  };

  const canGenerateMetadata = Boolean(draft.title && draft.symbol && draft.description && draft.imageUrl);

  return (
    <div className="space-y-8">
      <section className="space-y-4">
        <header className="flex items-center justify-between">
          <div>
            <h4 className="text-lg font-semibold text-[var(--color-foreground)]">Media Uploads</h4>
            <p className="text-sm text-[var(--muted)]">
              Upload artwork and thumbnails. When provided, we will pin them via Pinata during the mint call.
            </p>
          </div>
          <Button
            variant="secondary"
            className="text-xs"
            onClick={() => updateDraft(DEFAULT_ASSET_DRAFT)}
          >
            Reset to Template
          </Button>
        </header>
        <div className="grid gap-4 md:grid-cols-2">
          <UploadTile
            label="Primary Artwork"
            description="PNG, JPG, GIF up to 25 MB"
            fileName={draft.imageFileName}
            hasPayload={Boolean(draft.imageUrl)}
            uploading={draft.imageUploading}
            onSelect={(file) => handleFileSelect(file, "image")}
          />
          <UploadTile
            label="Thumbnail"
            description="Optional preview image"
            fileName={draft.thumbnailFileName}
            hasPayload={Boolean(draft.thumbnailUrl)}
            uploading={draft.thumbnailUploading}
            onSelect={(file) => handleFileSelect(file, "thumbnail")}
          />
        </div>
      </section>

      <section className="grid gap-4 lg:grid-cols-2">
        <div className="space-y-4 rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(7,12,30,0.75)] p-6">
          <h4 className="text-lg font-semibold text-[var(--color-foreground)]">Metadata Fields</h4>
          <div className="grid gap-4 md:grid-cols-2">
            <Field
              label="Title"
              value={draft.title}
              onChange={(value) => updateDraft({ title: value })}
              placeholder="MetaBrick Test NFT"
            />
            <Field
              label="Symbol"
              value={draft.symbol}
              onChange={(value) => updateDraft({ symbol: value })}
              placeholder="MBRICK"
            />
          </div>
          <Field
            label="Description"
            value={draft.description}
            onChange={(value) => updateDraft({ description: value })}
            placeholder="Test NFT minted via devnet.oasisweb4.one"
            multiline
          />
          <div className="grid gap-4 md:grid-cols-2">
            <Field
              label="JSON Metadata URL"
              value={draft.jsonUrl}
              onChange={(value) => updateDraft({ jsonUrl: value })}
              placeholder="https://gateway.pinata.cloud/ipfs/..."
            />
            <Field
              label="Send To Address"
              value={draft.sendToAddress}
              onChange={(value) => updateDraft({ sendToAddress: value })}
              placeholder="85ArqfA2..."
            />
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <Field
              label="Image URL"
              value={draft.imageUrl}
              onChange={(value) => updateDraft({ imageUrl: value })}
              placeholder="https://gateway.pinata.cloud/ipfs/..."
            />
            <Field
              label="Thumbnail URL"
              value={draft.thumbnailUrl}
              onChange={(value) => updateDraft({ thumbnailUrl: value })}
              placeholder="https://gateway.pinata.cloud/ipfs/..."
            />
          </div>
        </div>
        <div className="space-y-4 rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,14,34,0.8)] p-6">
          <h4 className="text-lg font-semibold text-[var(--color-foreground)]">Media Preview</h4>
          <p className="text-sm text-[var(--muted)]">
            Provide either hosted URLs or upload files above so the mint endpoint can push them to Pinata automatically.
          </p>
          <div className="flex h-48 items-center justify-center rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.6)] text-[var(--muted)]">
            {draft.imageUploading
              ? "Uploading to Pinata..."
              : draft.imageUrl
              ? "Image uploaded via Pinata"
              : "No image selected"}
          </div>
          <div className="grid grid-cols-2 gap-3 text-xs text-[var(--muted)]">
            <div>
              <p className="font-semibold text-[var(--color-foreground)]">JSON</p>
              <p className="break-all text-[11px] opacity-80">{draft.jsonUrl || "Will be generated via Pinata"}</p>
            </div>
            <div>
              <p className="font-semibold text-[var(--color-foreground)]">Recipient</p>
              <p className="break-all text-[11px] opacity-80">{draft.sendToAddress}</p>
            </div>
          </div>
          <Button
            variant="secondary"
            disabled={!canGenerateMetadata || draft.metadataUploading}
            onClick={uploadMetadataJson}
          >
            {draft.metadataUploading ? "Uploading metadata..." : "Generate & Pin Metadata"}
          </Button>
        </div>
      </section>

      <section className="rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h4 className="text-lg font-semibold text-[var(--color-foreground)]">Request Snapshot</h4>
            <p className="text-sm text-[var(--muted)]">
              Reference scaffold before final mint. The review step will add provider enums and mint options automatically.
            </p>
          </div>
        </div>
        <pre className="mt-4 max-h-80 overflow-auto rounded-xl bg-[rgba(4,8,24,0.95)] p-4 text-xs text-[var(--muted)]">
{JSON.stringify(previewPayload, null, 2)}
        </pre>
      </section>
    </div>
  );
}

function UploadTile({
  label,
  description,
  fileName,
  hasPayload,
  uploading,
  onSelect,
}: {
  label: string;
  description: string;
  fileName?: string;
  hasPayload?: boolean;
  uploading?: boolean;
  onSelect: (file: File | null) => void;
}) {
  const inputId = `${label.toLowerCase().replace(/\s+/g, "-")}-upload`;
  return (
    <label
      htmlFor={inputId}
      className="flex h-full cursor-pointer flex-col justify-between rounded-2xl border border-dashed border-[var(--color-card-border)]/60 bg-[rgba(6,10,24,0.7)] p-6 hover:border-[var(--accent)]/60"
    >
      <div>
        <p className="text-sm font-semibold text-[var(--color-foreground)]">{label}</p>
        <p className="mt-1 text-xs text-[var(--muted)]">{description}</p>
      </div>
      <div className="mt-6 rounded-xl bg-[rgba(4,8,20,0.8)] px-4 py-3 text-xs text-[var(--muted)]">
        {fileName ? <span>{fileName}</span> : <span>No file selected</span>}
      </div>
      {uploading ? (
        <span className="mt-3 text-[10px] uppercase tracking-[0.35em] text-[var(--accent)]">Uploading...</span>
      ) : hasPayload ? (
        <span className="mt-3 text-[10px] uppercase tracking-[0.35em] text-[var(--accent)]">Ready</span>
      ) : null}
      <input
        id={inputId}
        type="file"
        className="hidden"
        onChange={(event: React.ChangeEvent<HTMLInputElement>) => onSelect(event.target.files?.[0] ?? null)}
      />
    </label>
  );
}

function Field({
  label,
  value,
  onChange,
  placeholder,
  multiline,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  multiline?: boolean;
  type?: string;
}) {
  const InputComponent = multiline ? "textarea" : "input";
  return (
    <label className="flex flex-col gap-2 text-sm text-[var(--muted)]">
      <span className="text-xs uppercase tracking-[0.35em] text-[var(--muted)]">{label}</span>
      <InputComponent
        className={cn(
          "w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none",
          multiline ? "min-h-[96px]" : undefined
        )}
        value={value}
        placeholder={placeholder}
        onChange={(event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => onChange(event.target.value)}
        {...(multiline ? { rows: 4 } : { type })}
      />
    </label>
  );
}

function fileToBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      const base64 = result.split(",")[1];
      resolve(base64);
    };
    reader.onerror = () => reject(reader.error ?? new Error("Unable to read file"));
    reader.readAsDataURL(file);
  });
}
