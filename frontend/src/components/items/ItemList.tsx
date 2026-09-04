"use client";

import { useState } from "react";
import { ApiError } from "@/lib/api/client";
import { useToast } from "@/components/common/ToastProvider";
import type { WatchlistItemResponse } from "@/lib/types";

interface ItemListProps {
  items: WatchlistItemResponse[];
  selectedItemId: number | null;
  onSelect: (itemId: number) => void;
  onRemove: (itemId: number) => Promise<unknown>;
}

export function ItemList({ items, selectedItemId, onSelect, onRemove }: ItemListProps) {
  const { showToast } = useToast();
  const [removingId, setRemovingId] = useState<number | null>(null);

  if (items.length === 0) {
    return <p className="empty-state">No currency pairs yet. Add one above.</p>;
  }

  async function handleRemove(itemId: number) {
    setRemovingId(itemId);
    try {
      await onRemove(itemId);
    } catch (err) {
      showToast(err instanceof ApiError ? err.message : "Failed to remove item.");
    } finally {
      setRemovingId(null);
    }
  }

  return (
    <ul className="list">
      {items.map((item) => (
        <li
          key={item.id}
          className="list-row"
          style={{
            cursor: "pointer",
            outline: selectedItemId === item.id ? "2px solid var(--primary)" : undefined,
          }}
          onClick={() => onSelect(item.id)}
        >
          <div className="list-row-main">
            <span>
              {item.baseCurrency} → {item.quoteCurrency}
            </span>
            {item.latestRate ? (
              <span className="muted">
                Latest rate: <span className="rate-value">{item.latestRate.rate.toFixed(4)}</span>{" "}
                (as of {new Date(item.latestRate.sourceTimestamp).toLocaleDateString()})
              </span>
            ) : (
              <span className="muted">No rate fetched yet — click Refresh Rates.</span>
            )}
          </div>
          <button
            type="button"
            className="danger"
            onClick={(e) => {
              e.stopPropagation();
              handleRemove(item.id);
            }}
            disabled={removingId === item.id}
          >
            {removingId === item.id ? "Removing..." : "Remove"}
          </button>
        </li>
      ))}
    </ul>
  );
}
