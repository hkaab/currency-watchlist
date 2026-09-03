import type { ProblemDetails } from "@/lib/types";

export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5289";

export class ApiError extends Error {
  status: number;
  /** Per-field messages from the validation filter (e.g. { BaseCurrency: ["..."] }), when present. */
  fieldErrors?: Record<string, string[]>;

  constructor(message: string, status: number, fieldErrors?: Record<string, string[]>) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`;
    let fieldErrors: Record<string, string[]> | undefined;
    try {
      const problem = (await response.json()) as ProblemDetails;
      fieldErrors = problem.errors;
      // Prefer the actual field message(s) over the generic "One or more validation errors occurred." title.
      const fieldMessages = fieldErrors && Object.values(fieldErrors).flat();
      message = fieldMessages?.length ? fieldMessages.join(" ") : (problem.detail ?? problem.title ?? message);
    } catch {
      // response had no JSON body; keep the default message
    }
    throw new ApiError(message, response.status, fieldErrors);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: "POST",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    }),
  delete: (path: string) => request<void>(path, { method: "DELETE" }),
};
