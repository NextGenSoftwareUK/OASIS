import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "My wallets",
};

export default function ProfileLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <>{children}</>;
}
