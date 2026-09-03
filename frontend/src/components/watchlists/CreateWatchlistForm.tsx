"use client";

import { FormEvent, useState } from "react";
import { ApiError } from "@/lib/api/client";
import { useToast } from "@/components/common/ToastProvider";

interface CreateWatchlistFormProps {
  onCreate: (name: string) => Promise<unknown>;
}

export function CreateWatchlistForm({ onCreate }: CreateWatchlistFormProps) {
  const { showToast } = useToast();
  const [name, setName] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const trimmed = name.trim();
  const isValid = trimmed.length > 0 && trimmed.length <= 100;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!isValid) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onCreate(trimmed);
      setName("");
    } catch (err) {
      showToast(err instanceof ApiError ? err.message : "Failed to create watchlist.", "error");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="form-row" noValidate>
      <div className="field">
        <label htmlFor="watchlist-name">Watchlist name</label>
        <input
          id="watchlist-name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="e.g. Travel Money"
          className={name.length > 0 && !isValid ? "invalid" : undefined}
        />
      </div>
      <button type="submit" disabled={!isValid || isSubmitting}>
        {isSubmitting ? "Creating..." : "Create watchlist"}
      </button>
    </form>
  );
}
