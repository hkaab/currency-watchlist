import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Produces a self-contained .next/standalone build for a small production Docker image.
  output: "standalone",
};

export default nextConfig;
