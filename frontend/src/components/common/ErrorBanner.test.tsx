import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ErrorBanner } from "./ErrorBanner";

describe("ErrorBanner", () => {
  it("renders the message in an alert role", () => {
    render(<ErrorBanner message="Something went wrong" />);

    expect(screen.getByRole("alert")).toHaveTextContent("Something went wrong");
  });
});
