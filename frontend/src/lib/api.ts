import type { ApiResult } from "./types";

// const API_URL = process.env.API_URL ?? 'http://localhost:8000'
const BASE = import.meta.env.SSR
  ? (process.env.API_URL ?? "http://localhost:8000")
  : "/api";

export async function apiGet<T>(path: string): Promise<T | null> {
  const res = await fetch(`${BASE}${path}`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`API ${res.status} on ${path}`);
  return res.json() as Promise<T>;
}

export async function apiPost<T>(
  path: string,
  data: unknown,
): Promise<ApiResult<T>> {
  let res: Response;
  try {
    res = await fetch(`${BASE}${path}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  } catch {
    return { ok: false, error: "Couldn't reach the server." };
  }

  if (res.ok) {
    const created = (await res.json()) as T;
    return { ok: true, data: created };
  }

  if (res.status === 404) {
    return { ok: false, error: "This board or thread no longer exists." };
  }
  if (res.status === 429) {
    return { ok: false, error: "You are posting too fast. Chill." };
  }

  return { ok: false, error: "Something went wrong. Try again." };
}

export const getHealth = () => apiGet<{ status: string }>("/health");
