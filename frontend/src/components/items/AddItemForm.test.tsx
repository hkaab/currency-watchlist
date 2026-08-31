import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { AddItemForm } from "./AddItemForm";

describe("AddItemForm", () => {
  it("keeps submit disabled until both currency codes are valid and different", async () => {
    render(<AddItemForm onAdd={vi.fn()} />);
    const submit = screen.getByRole("button", { name: "Add pair" });

    expect(submit).toBeDisabled();

    await userEvent.type(screen.getByLabelText("Base currency"), "US");
    expect(submit).toBeDisabled();

    await userEvent.type(screen.getByLabelText("Base currency"), "D");
    await userEvent.type(screen.getByLabelText("Quote currency"), "USD");
    expect(submit).toBeDisabled();
    expect(screen.getByText("Base and quote currency must differ.")).toBeInTheDocument();

    await userEvent.clear(screen.getByLabelText("Quote currency"));
    await userEvent.type(screen.getByLabelText("Quote currency"), "AUD");
    expect(submit).toBeEnabled();
  });

  it("submits uppercased currency codes and resets the form", async () => {
    const onAdd = vi.fn().mockResolvedValue(undefined);
    render(<AddItemForm onAdd={onAdd} />);

    await userEvent.type(screen.getByLabelText("Base currency"), "usd");
    await userEvent.type(screen.getByLabelText("Quote currency"), "aud");
    await userEvent.click(screen.getByRole("button", { name: "Add pair" }));

    expect(onAdd).toHaveBeenCalledWith("USD", "AUD");
  });
});
