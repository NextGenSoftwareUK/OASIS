import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Sign In",
};

export default function ProfileLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <>{children}</>;
}
