"use client";

import { useCallback } from "react";

export type OasisConfig = {
  baseUrl: string;
  token?: string;
};

export function useOasisApi(config: OasisConfig) {
  const call = useCallback(
    async (path: string, options: RequestInit = {}) => {
      const headers = new Headers(options.headers);
      headers.set("Content-Type", "application/json");
      if (config.token) {
        headers.set("Authorization", `Bearer ${config.token}`);
      }

      const response = await fetch(`${config.baseUrl}${path}`, {
        ...options,
        headers,
      });

      const text = await response.text();
      let json: unknown = null;
      try {
        json = text ? JSON.parse(text) : null;
      } catch (error) {
        console.warn("Failed to parse JSON response", error);
      }

      if (!response.ok) {
        console.error("[oasis-api] request failed", {
          url: `${config.baseUrl}${path}`,
          path,
          status: response.status,
          statusText: response.statusText,
          body: text,
          headers: Object.fromEntries(response.headers.entries()),
        });
        const message =
          typeof json === "object" && json !== null && "message" in json && typeof (json as { message: unknown }).message === "string"
            ? (json as { message: string }).message
            : `HTTP ${response.status}: ${response.statusText}`;
        throw new Error(message);
      }

      if (json === null) {
        return null;
      }
      return json as Record<string, unknown>;
    },
    [config.baseUrl, config.token]
  );

  return { call };
}
