"use client";

import { FormEvent, useState } from "react";
import { ApiError } from "@/lib/api/client";

interface CreateWatchlistFormProps {
  onCreate: (name: string) => Promise<unknown>;
}

export function CreateWatchlistForm({ onCreate }: CreateWatchlistFormProps) {
  const [name, setName] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const trimmed = name.trim();
  const isValid = trimmed.length > 0 && trimmed.length <= 100;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!isValid) {
      return;
    }

    setIsSubmitting(true);
    setError(null);
    try {
      await onCreate(trimmed);
      setName("");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to create watchlist.");
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
      {error && <span className="field-error">{error}</span>}
    </form>
  );
}
