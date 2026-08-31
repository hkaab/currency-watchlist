import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LoadingSpinner } from "./LoadingSpinner";

describe("LoadingSpinner", () => {
  it("renders the default label", () => {
    render(<LoadingSpinner />);
    expect(screen.getByRole("status")).toHaveTextContent("Loading...");
  });

  it("renders a custom label", () => {
    render(<LoadingSpinner label="Fetching rates..." />);
    expect(screen.getByRole("status")).toHaveTextContent("Fetching rates...");
  });
});
