import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Currency Watchlist & Alert Service",
  description: "Track currency pairs, refresh live rates, and manage threshold alerts.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
