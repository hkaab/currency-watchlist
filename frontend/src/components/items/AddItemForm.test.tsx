import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { AddItemForm } from "./AddItemForm";
import { ApiError } from "@/lib/api/client";
import { renderWithToast } from "@/test-utils/renderWithToast";

describe("AddItemForm", () => {
  it("keeps submit disabled until both currency codes are valid and different", async () => {
    renderWithToast(<AddItemForm onAdd={vi.fn()} />);
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
    renderWithToast(<AddItemForm onAdd={onAdd} />);

    await userEvent.type(screen.getByLabelText("Base currency"), "usd");
    await userEvent.type(screen.getByLabelText("Quote currency"), "aud");
    await userEvent.click(screen.getByRole("button", { name: "Add pair" }));

    expect(onAdd).toHaveBeenCalledWith("USD", "AUD");
  });

  it("highlights the specific field the server rejected and shows a toast", async () => {
    const onAdd = vi.fn().mockRejectedValue(
      new ApiError("'ZZZ' is not a currency the rate provider supports.", 400, {
        QuoteCurrency: ["'ZZZ' is not a currency the rate provider supports."],
      }),
    );
    renderWithToast(<AddItemForm onAdd={onAdd} />);

    await userEvent.type(screen.getByLabelText("Base currency"), "USD");
    await userEvent.type(screen.getByLabelText("Quote currency"), "ZZZ");
    await userEvent.click(screen.getByRole("button", { name: "Add pair" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "'ZZZ' is not a currency the rate provider supports.",
    );
    expect(screen.getByLabelText("Quote currency")).toHaveClass("invalid");
    expect(screen.getByLabelText("Base currency")).not.toHaveClass("invalid");
    expect(screen.getByText("'ZZZ' is not a currency the rate provider supports.", { selector: ".field-error" }))
      .toBeInTheDocument();
  });

  it("clears the field-level error once the user edits that field again", async () => {
    const onAdd = vi.fn().mockRejectedValue(
      new ApiError("bad currency", 400, { QuoteCurrency: ["bad currency"] }),
    );
    renderWithToast(<AddItemForm onAdd={onAdd} />);

    await userEvent.type(screen.getByLabelText("Base currency"), "USD");
    await userEvent.type(screen.getByLabelText("Quote currency"), "ZZZ");
    await userEvent.click(screen.getByRole("button", { name: "Add pair" }));
    await screen.findByRole("alert");

    await userEvent.clear(screen.getByLabelText("Quote currency"));
    await userEvent.type(screen.getByLabelText("Quote currency"), "AUD");

    expect(screen.getByLabelText("Quote currency")).not.toHaveClass("invalid");
  });
});
