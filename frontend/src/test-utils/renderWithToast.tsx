import { render, type RenderOptions } from "@testing-library/react";
import type { ReactElement } from "react";
import { ToastProvider } from "@/components/common/ToastProvider";

/** Wraps render() with ToastProvider, since useToast() throws without it. */
export function renderWithToast(ui: ReactElement, options?: RenderOptions) {
  return render(ui, { wrapper: ToastProvider, ...options });
}
