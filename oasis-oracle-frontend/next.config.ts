import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  reactStrictMode: true,
  
  // Enable experimental features
  experimental: {
    // turbo: true, // Turbopack is enabled via CLI flag
  },

  // Image domains for chain logos
  images: {
    domains: [
      'raw.githubusercontent.com',
      'cryptologos.cc',
    ],
  },

  // Environment variables
  env: {
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:3000',
    NEXT_PUBLIC_WS_URL: process.env.NEXT_PUBLIC_WS_URL || 'ws://localhost:3000',
  },
  
  // Allow production builds to complete with linter warnings
  eslint: {
    ignoreDuringBuilds: true,
  },
};

export default nextConfig;






