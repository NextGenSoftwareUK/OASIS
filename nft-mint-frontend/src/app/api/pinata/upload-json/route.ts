import { NextRequest, NextResponse } from "next/server";

const PINATA_JWT = process.env.PINATA_JWT;
const PINATA_API_KEY = process.env.PINATA_API_KEY;
const PINATA_SECRET_KEY = process.env.PINATA_API_SECRET;
const PINATA_GATEWAY = process.env.PINATA_GATEWAY ?? "https://gateway.pinata.cloud";

function getAuthHeaders() {
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
      return NextResponse.json({ message: text || "Pinata JSON upload failed" }, { status: response.status });
    }

    const json = text ? JSON.parse(text) : {};
    const hash = json?.IpfsHash;
    if (!hash) {
      return NextResponse.json({ message: "Pinata response missing IpfsHash" }, { status: 502 });
    }

    return NextResponse.json({
      hash,
      url: `${PINATA_GATEWAY.replace(/\/$/, "")}/ipfs/${hash}`,
    });
  } catch (error) {
    console.error("Pinata JSON upload failed", error);
    const message = error instanceof Error ? error.message : "Pinata JSON upload failed";
    return NextResponse.json({ message }, { status: 500 });
  }
}

