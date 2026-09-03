import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Self-contained .next/standalone build for the Docker image - but Vercel's own build
  // pipeline conflicts with it (it expects trace files standalone mode restructures), so
  // skip it there. Vercel sets process.env.VERCEL during its build.
  output: process.env.VERCEL ? undefined : "standalone",
};

export default nextConfig;
