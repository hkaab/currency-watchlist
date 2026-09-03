import { act, fireEvent, render, renderHook, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ToastProvider, useToast } from "./ToastProvider";

function TriggerButton({ message = "Something went wrong" }: { message?: string }) {
  const { showToast } = useToast();
  return <button onClick={() => showToast(message)}>Trigger</button>;
}

describe("ToastProvider / useToast", () => {
  it("throws when used outside a ToastProvider", () => {
    const { result } = renderHook(() => {
      try {
        return useToast();
      } catch (err) {
        return err;
      }
    });

    expect(result.current).toBeInstanceOf(Error);
  });

  it("renders children without showing any toast initially", () => {
    render(
      <ToastProvider>
        <p>content</p>
      </ToastProvider>,
    );

    expect(screen.getByText("content")).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("shows a toast with the given message and variant, dismissible by click", async () => {
    render(
      <ToastProvider>
        <TriggerButton message="Saved!" />
      </ToastProvider>,
    );

    await userEvent.click(screen.getByRole("button", { name: "Trigger" }));

    const toast = await screen.findByRole("alert");
    expect(toast).toHaveTextContent("Saved!");
    expect(toast).toHaveClass("toast-error");

    await userEvent.click(toast);
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("auto-dismisses a toast after the timeout", () => {
    vi.useFakeTimers();
    try {
      render(
        <ToastProvider>
          <TriggerButton />
        </ToastProvider>,
      );

      fireEvent.click(screen.getByRole("button", { name: "Trigger" }));
      expect(screen.getByRole("alert")).toBeInTheDocument();

      act(() => {
        vi.advanceTimersByTime(5000);
      });

      expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    } finally {
      vi.useRealTimers();
    }
  });
});
