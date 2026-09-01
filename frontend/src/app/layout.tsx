import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";

const inter = Inter({
  variable: "--font-sans",
  subsets: ["latin"],
  display: "swap",
});

export const metadata: Metadata = {
  title: "Currency Watchlist & Alert Service",
  description: "Track currency pairs, refresh live rates, and manage threshold alerts.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={inter.variable}>
      <body>
        <header className="topbar">
          <div className="topbar-inner">
            <span className="brand-mark" aria-hidden="true">CW</span>
            <span className="brand-name">Currency Watchlist</span>
          </div>
        </header>
        {children}
      </body>
    </html>
  );
}
