import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { CreateWatchlistForm } from "./CreateWatchlistForm";
import { ApiError } from "@/lib/api/client";

describe("CreateWatchlistForm", () => {
  it("disables submit until a name is entered", async () => {
    render(<CreateWatchlistForm onCreate={vi.fn()} />);

    expect(screen.getByRole("button", { name: "Create watchlist" })).toBeDisabled();

    await userEvent.type(screen.getByLabelText("Watchlist name"), "My List");

    expect(screen.getByRole("button", { name: "Create watchlist" })).toBeEnabled();
  });

  it("calls onCreate with the trimmed name and clears the input on success", async () => {
    const onCreate = vi.fn().mockResolvedValue(undefined);
    render(<CreateWatchlistForm onCreate={onCreate} />);

    await userEvent.type(screen.getByLabelText("Watchlist name"), "  My List  ");
    await userEvent.click(screen.getByRole("button", { name: "Create watchlist" }));

    expect(onCreate).toHaveBeenCalledWith("My List");
    expect(screen.getByLabelText("Watchlist name")).toHaveValue("");
  });

  it("shows the error message when creation fails", async () => {
    const onCreate = vi.fn().mockRejectedValue(new ApiError("Name already taken", 400));
    render(<CreateWatchlistForm onCreate={onCreate} />);

    await userEvent.type(screen.getByLabelText("Watchlist name"), "Duplicate");
    await userEvent.click(screen.getByRole("button", { name: "Create watchlist" }));

    expect(await screen.findByText("Name already taken")).toBeInTheDocument();
  });
});
