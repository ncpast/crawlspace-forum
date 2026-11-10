const API_URL = process.env.API_URL ?? 'http://localhost:8000'

export async function apiGet<T>(path: string): Promise<T | null> {
    const res = await fetch(`${API_URL}${path}`);
    if (res.status === 404) return null;
    if (!res.ok) throw new Error(`API ${res.status} on ${path}`);
    return res.json();
}