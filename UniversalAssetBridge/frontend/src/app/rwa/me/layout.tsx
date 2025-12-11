import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "My RWAs",
};

export default function MeRwaLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <div>{children}</div>;
}
