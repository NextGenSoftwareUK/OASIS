import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "RWA",
};

export default function RwaLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <div>{children}</div>;
}
