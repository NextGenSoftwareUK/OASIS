import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { OASISAuthProvider } from "../components/oasis-auth-provider";

const inter = Inter({ 
  subsets: ["latin"],
  weight: ["300", "400", "500", "600"],
  display: "swap",
});

export const metadata: Metadata = {
  title: "AssetRail - Smart Contract Generator",
  description: "Generate production-ready smart contracts for Solana, Ethereum, and Radix with template-based or AI-powered generation",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className="dark">
      <body className={inter.className}>
        <OASISAuthProvider>{children}</OASISAuthProvider>
      </body>
    </html>
  );
}
