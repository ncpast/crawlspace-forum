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

export const getHealth = () => apiGet<{ status: string}>("/health")