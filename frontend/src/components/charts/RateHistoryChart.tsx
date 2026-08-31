"use client";

import { useEffect, useState } from "react";
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { ratesApi } from "@/lib/api/rates";
import { ApiError } from "@/lib/api/client";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { ErrorBanner } from "@/components/common/ErrorBanner";

interface RateHistoryChartProps {
  baseCurrency: string;
  quoteCurrency: string;
  refreshKey: number;
}

interface ChartPoint {
  date: string;
  rate: number;
}

export function RateHistoryChart({ baseCurrency, quoteCurrency, refreshKey }: RateHistoryChartProps) {
  const [points, setPoints] = useState<ChartPoint[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setIsLoading(true);
      setError(null);
      try {
        const to = new Date();
        const from = new Date();
        from.setDate(from.getDate() - 30);
        const history = await ratesApi.history(baseCurrency, quoteCurrency, from, to);
        if (!cancelled) {
          setPoints(
            history.map((snapshot) => ({
              date: new Date(snapshot.sourceTimestamp).toLocaleDateString(),
              rate: snapshot.rate,
            })),
          );
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : "Failed to load rate history.");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    load();
    return () => {
      cancelled = true;
    };
  }, [baseCurrency, quoteCurrency, refreshKey]);

  if (isLoading) {
    return <LoadingSpinner label="Loading rate history..." />;
  }

  if (error) {
    return <ErrorBanner message={error} />;
  }

  if (points.length === 0) {
    return <p className="empty-state">No stored history yet for {baseCurrency} → {quoteCurrency}. Refresh rates a few times to build a chart.</p>;
  }

  return (
    <div style={{ width: "100%", height: 220 }}>
      <ResponsiveContainer>
        <LineChart data={points} margin={{ top: 5, right: 12, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" opacity={0.3} />
          <XAxis dataKey="date" fontSize={11} tickMargin={8} />
          <YAxis domain={["auto", "auto"]} fontSize={11} width={60} />
          <Tooltip />
          <Line type="monotone" dataKey="rate" stroke="#2563eb" strokeWidth={2} dot={false} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
