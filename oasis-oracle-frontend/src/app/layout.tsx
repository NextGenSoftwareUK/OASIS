import type { Metadata } from "next";
import "./globals.css";

// Using system fonts for now - can add custom fonts later
const fontConfig = {
  variable: "--font-geist-sans",
};

export const metadata: Metadata = {
  title: "OASIS Oracle Dashboard | Multi-Chain Data Aggregation",
  description: "Real-time cross-chain oracle network monitoring, price feeds, transaction verification, and analytics across 20+ blockchains",
  keywords: ["oracle", "cross-chain", "blockchain", "defi", "price feeds", "verification"],
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="antialiased">
        {children}
      </body>
    </html>
  );
}

