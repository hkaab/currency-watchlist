"use client";

import { useState } from "react";
import { ApiError } from "@/lib/api/client";
import type { AlertEvaluationResult, AlertRuleResponse } from "@/lib/types";

interface AlertListProps {
  alerts: AlertRuleResponse[];
  onEvaluate: (alertId: number) => Promise<AlertEvaluationResult>;
}

export function AlertList({ alerts, onEvaluate }: AlertListProps) {
  const [evaluatingId, setEvaluatingId] = useState<number | null>(null);
  const [results, setResults] = useState<Record<number, AlertEvaluationResult | string>>({});

  if (alerts.length === 0) {
    return <p className="empty-state">No alert rules yet. Create one above.</p>;
  }

  async function handleEvaluate(alertId: number) {
    setEvaluatingId(alertId);
    try {
      const result = await onEvaluate(alertId);
      setResults((prev) => ({ ...prev, [alertId]: result }));
    } catch (err) {
      setResults((prev) => ({
        ...prev,
        [alertId]: err instanceof ApiError ? err.message : "Failed to evaluate alert.",
      }));
    } finally {
      setEvaluatingId(null);
    }
  }

  return (
    <ul className="list">
      {alerts.map((alert) => {
        const result = results[alert.id];
        return (
          <li key={alert.id} className="list-row" style={{ flexDirection: "column", alignItems: "stretch" }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: "0.75rem" }}>
              <div className="list-row-main">
                <span>
                  {alert.baseCurrency} → {alert.quoteCurrency}{" "}
                  <span className={`badge ${alert.condition.toLowerCase()}`}>
                    {alert.condition} {alert.threshold}
                  </span>
                </span>
                <span className="muted">{alert.isActive ? "Active" : "Inactive"}</span>
              </div>
              <button
                type="button"
                className="secondary"
                onClick={() => handleEvaluate(alert.id)}
                disabled={evaluatingId === alert.id}
              >
                {evaluatingId === alert.id ? "Evaluating..." : "Evaluate Now"}
              </button>
            </div>
            {result && (
              typeof result === "string" ? (
                <div className="evaluation-result">{result}</div>
              ) : (
                <div className={`evaluation-result ${result.isTriggered ? "triggered" : ""}`}>
                  {result.isTriggered ? "Triggered" : "Not triggered"} — current rate{" "}
                  {result.rate.toFixed(4)} ({result.condition} {result.threshold})
                </div>
              )
            )}
          </li>
        );
      })}
    </ul>
  );
}
