"use client";

import { FormEvent, useState } from "react";
import { ApiError } from "@/lib/api/client";
import { isValidCurrencyCode } from "@/lib/validation";
import { useToast } from "@/components/common/ToastProvider";

interface AddItemFormProps {
  onAdd: (baseCurrency: string, quoteCurrency: string) => Promise<unknown>;
}

interface ServerFieldErrors {
  base?: string;
  quote?: string;
}

export function AddItemForm({ onAdd }: AddItemFormProps) {
  const { showToast } = useToast();
  const [baseCurrency, setBaseCurrency] = useState("");
  const [quoteCurrency, setQuoteCurrency] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [serverErrors, setServerErrors] = useState<ServerFieldErrors>({});

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
    setServerErrors({});
    try {
      await onAdd(baseCurrency.trim().toUpperCase(), quoteCurrency.trim().toUpperCase());
      setBaseCurrency("");
      setQuoteCurrency("");
    } catch (err) {
      const message = err instanceof ApiError ? err.message : "Failed to add currency pair.";
      const fieldErrors = err instanceof ApiError ? err.fieldErrors : undefined;
      setServerErrors({
        base: fieldErrors?.BaseCurrency?.[0],
        quote: fieldErrors?.QuoteCurrency?.[0],
      });
      showToast(message, "error");
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
          onChange={(e) => {
            setBaseCurrency(e.target.value.toUpperCase());
            setServerErrors((prev) => ({ ...prev, base: undefined }));
          }}
          placeholder="USD"
          maxLength={3}
          className={(baseCurrency.length > 0 && !baseValid) || serverErrors.base ? "invalid" : undefined}
          style={{ width: "5rem" }}
        />
        {serverErrors.base && <span className="field-error">{serverErrors.base}</span>}
      </div>
      <div className="field">
        <label htmlFor="quote-currency">Quote currency</label>
        <input
          id="quote-currency"
          value={quoteCurrency}
          onChange={(e) => {
            setQuoteCurrency(e.target.value.toUpperCase());
            setServerErrors((prev) => ({ ...prev, quote: undefined }));
          }}
          placeholder="AUD"
          maxLength={3}
          className={(quoteCurrency.length > 0 && !quoteValid) || serverErrors.quote ? "invalid" : undefined}
          style={{ width: "5rem" }}
        />
        {serverErrors.quote && <span className="field-error">{serverErrors.quote}</span>}
      </div>
      <button type="submit" disabled={!isValid || isSubmitting}>
        {isSubmitting ? "Adding..." : "Add pair"}
      </button>
      {samePair && <span className="field-error">Base and quote currency must differ.</span>}
    </form>
  );
}
