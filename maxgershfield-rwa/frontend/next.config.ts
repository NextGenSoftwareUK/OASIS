import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  images: {
    remotePatterns: [new URL("https://ipfs.io/**")],
  },
  eslint: {
    ignoreDuringBuilds: true,
  },
};

export default nextConfig;
