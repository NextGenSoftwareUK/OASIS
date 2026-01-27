import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Create RWA",
};

export default function CreateRwaLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <div>{children}</div>;
}
