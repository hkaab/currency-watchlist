import { describe, expect, it } from "vitest";
import { isValidCurrencyCode } from "./validation";

describe("isValidCurrencyCode", () => {
  it.each(["USD", "aud", "Jpy"])("accepts a 3-letter code (%s)", (code) => {
    expect(isValidCurrencyCode(code)).toBe(true);
  });

  it.each(["US", "USDD", "US1", "", "   "])("rejects an invalid code (%s)", (code) => {
    expect(isValidCurrencyCode(code)).toBe(false);
  });

  it("trims surrounding whitespace before validating", () => {
    expect(isValidCurrencyCode("  USD  ")).toBe(true);
  });
});
