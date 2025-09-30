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
      let json: any = null;
      try {
        json = text ? JSON.parse(text) : null;
      } catch (error) {
        console.warn("Failed to parse JSON response", error);
      }

      if (!response.ok) {
        throw new Error(json?.message ?? `HTTP ${response.status}`);
      }

      return json;
    },
    [config.baseUrl, config.token]
  );

  return { call };
}
