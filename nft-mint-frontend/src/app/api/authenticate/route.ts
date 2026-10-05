import { NextRequest, NextResponse } from "next/server";
import { execFile, ExecFileException } from "child_process";
import { promisify } from "util";

const execFileAsync = promisify(execFile);

const DEFAULT_BASE_URL = "http://devnet.oasisweb4.one";
const CURL_TIMEOUT_SECONDS = 30;
const CURL_CONNECT_TIMEOUT_SECONDS = 10;

function sanitizeBaseUrl(input?: string | null): string {
  if (!input) return DEFAULT_BASE_URL;
  try {
    const url = new URL(input);
    url.pathname = url.pathname.replace(/\/$/, "");
    return url.toString().replace(/\/$/, "");
  } catch {
    return DEFAULT_BASE_URL;
  }
}

function extractToken(raw: string): { token?: string; avatarId?: string } {
  const tokenMatch = raw.match(/"jwtToken"\s*:\s*"([^"]+)"/);
  const avatarIdMatch = raw.match(/"avatarId"\s*:\s*"([^"]+)"/);
  return {
    token: tokenMatch?.[1],
    avatarId: avatarIdMatch?.[1],
  };
}

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { username, password, baseUrl } = body ?? {};

    if (!username || !password) {
      return NextResponse.json(
        { message: "Username and password are required" },
        { status: 400 }
      );
    }

    const targetBase = sanitizeBaseUrl(baseUrl);
    const targetUrl = `${targetBase.replace(/\/$/, "")}/api/avatar/authenticate`;

    const curlArgs = [
      "-s",
      "-k",
      "-X",
      "POST",
      targetUrl,
      "-H",
      "Content-Type: application/json",
      "--max-time",
      String(CURL_TIMEOUT_SECONDS),
      "--connect-timeout",
      String(CURL_CONNECT_TIMEOUT_SECONDS),
      "-d",
      JSON.stringify({ username, password }),
    ];

    let stdout: string | undefined;
    try {
      const result = await execFileAsync("curl", curlArgs, {
        maxBuffer: 10 * 1024 * 1024, // 10 MB
      });
      stdout = result.stdout?.toString();
    } catch (error: unknown) {
      const execError = error as ExecFileException & {
        stdout?: string | Buffer;
        stderr?: string | Buffer;
      };
      const stderr = execError?.stderr?.toString?.();
      if (stderr) {
        console.warn("Auth proxy curl stderr", stderr);
      }
      const candidate = execError?.stdout?.toString?.();
      if (candidate && candidate.trim().length > 0) {
        stdout = candidate;
      } else {
        console.error("Auth proxy curl error", execError?.message ?? error);
        return NextResponse.json(
          { message: "Authentication proxy error" },
          { status: 502 }
        );
      }
    }

    if (!stdout || !stdout.trim()) {
      return NextResponse.json(
        { message: "Empty response from authentication service" },
        { status: 502 }
      );
    }

    let parsed: {
      message?: string;
      result?: {
        jwtToken?: string;
        avatarId?: string;
        avatar?: { id?: string; AvatarId?: string };
        message?: string;
      };
    } | null = null;

    try {
      parsed = JSON.parse(stdout);
    } catch (error) {
      console.warn("Failed to parse auth response JSON, falling back to regex", error);
    }

    const extracted = extractToken(stdout);
    const token = parsed?.result?.jwtToken ?? extracted.token;
    const avatarId =
      parsed?.result?.avatarId ??
      parsed?.result?.avatar?.id ??
      parsed?.result?.avatar?.AvatarId ??
      extracted.avatarId;

    if (!token) {
      const message =
        parsed?.message ??
        parsed?.result?.message ??
        "Authentication succeeded but no token was returned";
      return NextResponse.json({ message }, { status: 502 });
    }

    return NextResponse.json(
      {
        token,
        avatarId: avatarId ?? null,
        message: parsed?.message ?? parsed?.result?.message ?? "Authenticated",
      },
      { status: 200 }
    );
  } catch (error) {
    console.error("Auth proxy failed", error);
    return NextResponse.json({ message: "Authentication proxy error" }, { status: 500 });
  }
}
