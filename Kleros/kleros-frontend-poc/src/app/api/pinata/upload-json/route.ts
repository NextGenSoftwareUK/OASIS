import { NextRequest, NextResponse } from "next/server";

const PINATA_JWT = process.env.PINATA_JWT;
const PINATA_API_KEY = process.env.PINATA_API_KEY ?? process.env.PINATA_PUBLIC_KEY ?? process.env.NEXT_PUBLIC_PINATA_API_KEY;
const PINATA_SECRET_KEY = process.env.PINATA_API_SECRET ?? process.env.PINATA_SECRET_KEY ?? process.env.NEXT_PUBLIC_PINATA_API_SECRET;
const PINATA_GATEWAY = process.env.PINATA_GATEWAY ?? "https://gateway.pinata.cloud";

function getAuthHeaders(): Record<string, string> {
  if (PINATA_JWT) {
    return { Authorization: `Bearer ${PINATA_JWT}` };
  }

  if (PINATA_API_KEY && PINATA_SECRET_KEY) {
    return {
      pinata_api_key: PINATA_API_KEY,
      pinata_secret_api_key: PINATA_SECRET_KEY,
    };
  }

  throw new Error("Pinata credentials not configured");
}

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { content, name } = body ?? {};
    if (process.env.NODE_ENV !== "production") {
      console.log("[pinata-json] request", { hasContent: Boolean(content), name });
    }

    if (!content) {
      return NextResponse.json({ message: "content field is required" }, { status: 400 });
    }

    const response = await fetch("https://api.pinata.cloud/pinning/pinJSONToIPFS", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...(getAuthHeaders()),
      },
      body: JSON.stringify({
        pinataContent: content,
        pinataMetadata: name ? { name } : undefined,
      }),
    });

    const text = await response.text();
    if (!response.ok) {
      if (process.env.NODE_ENV !== "production") {
        console.error("[pinata-json] upload failed", response.status, text);
      }
      return NextResponse.json({ message: text || "Pinata JSON upload failed" }, { status: response.status });
    }

    const json = text ? JSON.parse(text) : {};
    const hash = json?.IpfsHash;
    if (!hash) {
      if (process.env.NODE_ENV !== "production") {
        console.error("[pinata-json] missing hash", json);
      }
      return NextResponse.json({ message: "Pinata response missing IpfsHash" }, { status: 502 });
    }

    const url = `${PINATA_GATEWAY.replace(/\/$/, "")}/ipfs/${hash}`;
    if (process.env.NODE_ENV !== "production") {
      console.log("[pinata-json] success", url);
    }
    return NextResponse.json({
      hash,
      url,
      result: url,
    });
  } catch (error) {
    console.error("Pinata JSON upload failed", error);
    const message = error instanceof Error ? error.message : "Pinata JSON upload failed";
    return NextResponse.json({ message }, { status: 500 });
  }
}

