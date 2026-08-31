"use client";

import { FormEvent, useState } from "react";
import { ApiError } from "@/lib/api/client";
import { isValidCurrencyCode } from "@/lib/validation";

interface AddItemFormProps {
  onAdd: (baseCurrency: string, quoteCurrency: string) => Promise<unknown>;
}

export function AddItemForm({ onAdd }: AddItemFormProps) {
  const [baseCurrency, setBaseCurrency] = useState("");
  const [quoteCurrency, setQuoteCurrency] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const baseValid = isValidCurrencyCode(baseCurrency);
  const quoteValid = isValidCurrencyCode(quoteCurrency);
  const samePair = baseValid && quoteValid && baseCurrency.trim().toUpperCase() === quoteCurrency.trim().toUpperCase();
  const isValid = baseValid && quoteValid && !samePair;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!isValid) {
      return;
    }

    setIsSubmitting(true);
    setError(null);
    try {
      await onAdd(baseCurrency.trim().toUpperCase(), quoteCurrency.trim().toUpperCase());
      setBaseCurrency("");
      setQuoteCurrency("");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to add currency pair.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="form-row" noValidate>
      <div className="field">
        <label htmlFor="base-currency">Base currency</label>
        <input
          id="base-currency"
          value={baseCurrency}
          onChange={(e) => setBaseCurrency(e.target.value.toUpperCase())}
          placeholder="USD"
          maxLength={3}
          className={baseCurrency.length > 0 && !baseValid ? "invalid" : undefined}
          style={{ width: "5rem" }}
        />
      </div>
      <div className="field">
        <label htmlFor="quote-currency">Quote currency</label>
        <input
          id="quote-currency"
          value={quoteCurrency}
          onChange={(e) => setQuoteCurrency(e.target.value.toUpperCase())}
          placeholder="AUD"
          maxLength={3}
          className={quoteCurrency.length > 0 && !quoteValid ? "invalid" : undefined}
          style={{ width: "5rem" }}
        />
      </div>
      <button type="submit" disabled={!isValid || isSubmitting}>
        {isSubmitting ? "Adding..." : "Add pair"}
      </button>
      {samePair && <span className="field-error">Base and quote currency must differ.</span>}
      {error && <span className="field-error">{error}</span>}
    </form>
  );
}
