import type { Metadata } from "next";
import { DM_Mono, DM_Sans } from "next/font/google";
import "./globals.css";
import TanstackProvider from "@/providers/TanstackProvider";
import TooltipProv from "@/providers/TooltipProvider";
import NextTopLoader from "nextjs-toploader";

const dmSans = DM_Sans({
  weight: ["400", "600", "700", "900"],
  subsets: ["latin"],
});

const dmMono = DM_Mono({
  weight: ["400"],
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Quantum Street Bridge",
  description:
    "Quantum Street Bridge is a decentralized bridge that allows users to swap between different cryptocurrencies.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body
        className={`${dmMono.className} ${dmSans.className} antialiased min-h-screen bg-background-website`}
      >
        <NextTopLoader color="var(--secondary)" height={2} />
        <TanstackProvider>
          <TooltipProv>
            <main className="main w-full h-full">{children}</main>
          </TooltipProv>
        </TanstackProvider>
      </body>
    </html>
  );
}
