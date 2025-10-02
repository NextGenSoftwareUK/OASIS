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
    const { base64, fileName, contentType } = body ?? {};

    if (!base64) {
      return NextResponse.json({ message: "base64 field is required" }, { status: 400 });
    }

    const matches = /^data:(?<type>[^;]+);base64,(?<data>.+)$/u.exec(base64);
    const buffer = Buffer.from(matches?.groups?.data ?? base64, "base64");
    const isNodeRuntime = typeof Blob === "undefined";
    const inferredType = matches?.groups?.type ?? contentType ?? "application/octet-stream";
    const safeName = fileName ?? `upload-${Date.now()}`;

    const form = new FormData();
    if (isNodeRuntime) {
      // Running in Node (Next.js route) – use buffer
      form.append("file", buffer, safeName);
    } else {
      const blob = new Blob([buffer], { type: inferredType });
      form.append("file", blob, safeName);
    }
    form.append("pinataMetadata", JSON.stringify({ name: safeName }));
    form.append("pinataOptions", JSON.stringify({ cidVersion: 1 }));

    const response = await fetch("https://api.pinata.cloud/pinning/pinFileToIPFS", {
      method: "POST",
      body: form,
      headers: {
        ...(getAuthHeaders()),
      },
    });

    const text = await response.text();
    if (!response.ok) {
      return NextResponse.json({ message: text || "Pinata upload failed" }, { status: response.status });
    }

    const json = text ? JSON.parse(text) : {};
    const hash = json?.IpfsHash;
    if (!hash) {
      return NextResponse.json({ message: "Pinata response missing IpfsHash" }, { status: 502 });
    }

    const url = `${PINATA_GATEWAY.replace(/\/$/, "")}/ipfs/${hash}`;
    return NextResponse.json({
      hash,
      url,
      result: url,
    });
  } catch (error) {
    console.error("Pinata upload failed", error);
    const message = error instanceof Error ? error.message : "Pinata upload failed";
    return NextResponse.json({ message }, { status: 500 });
  }
}

