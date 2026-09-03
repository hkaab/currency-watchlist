"use client";

import { FormEvent, useState } from "react";
import { ApiError } from "@/lib/api/client";
import { useToast } from "@/components/common/ToastProvider";
import type { AlertCondition, WatchlistItemResponse } from "@/lib/types";

interface CreateAlertFormProps {
  items: WatchlistItemResponse[];
  onCreate: (watchlistItemId: number, condition: AlertCondition, threshold: number) => Promise<unknown>;
}

export function CreateAlertForm({ items, onCreate }: CreateAlertFormProps) {
  const { showToast } = useToast();
  // null = no explicit choice yet; falls back to the first item below. Deriving the
  // effective selection during render (rather than syncing it via an effect) keeps it
  // correct even when items arrive or get removed after this form has already mounted.
  const [explicitSelection, setExplicitSelection] = useState<number | null>(null);
  const watchlistItemId =
    explicitSelection !== null && items.some((i) => i.id === explicitSelection)
      ? explicitSelection
      : items[0]?.id ?? 0;

  const [condition, setCondition] = useState<AlertCondition>("Above");
  const [threshold, setThreshold] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const thresholdValue = Number(threshold);
  const isValid = watchlistItemId > 0 && threshold.trim().length > 0 && Number.isFinite(thresholdValue) && thresholdValue > 0;

  if (items.length === 0) {
    return <p className="empty-state">Add a currency pair first to create an alert.</p>;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!isValid) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onCreate(watchlistItemId, condition, thresholdValue);
      setThreshold("");
    } catch (err) {
      showToast(err instanceof ApiError ? err.message : "Failed to create alert.", "error");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="form-row" noValidate>
      <div className="field">
        <label htmlFor="alert-pair">Pair</label>
        <select
          id="alert-pair"
          value={watchlistItemId}
          onChange={(e) => setExplicitSelection(Number(e.target.value))}
        >
          {items.map((item) => (
            <option key={item.id} value={item.id}>
              {item.baseCurrency} → {item.quoteCurrency}
            </option>
          ))}
        </select>
      </div>
      <div className="field">
        <label htmlFor="alert-condition">Condition</label>
        <select
          id="alert-condition"
          value={condition}
          onChange={(e) => setCondition(e.target.value as AlertCondition)}
        >
          <option value="Above">Above</option>
          <option value="Below">Below</option>
        </select>
      </div>
      <div className="field">
        <label htmlFor="alert-threshold">Threshold</label>
        <input
          id="alert-threshold"
          value={threshold}
          onChange={(e) => setThreshold(e.target.value)}
          placeholder="1.60"
          inputMode="decimal"
          style={{ width: "6rem" }}
          className={threshold.length > 0 && !isValid ? "invalid" : undefined}
        />
      </div>
      <button type="submit" disabled={!isValid || isSubmitting}>
        {isSubmitting ? "Creating..." : "Create alert"}
      </button>
    </form>
  );
}
