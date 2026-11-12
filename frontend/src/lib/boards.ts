import { apiGet } from "./api";
import type { Board, PostSummary, Thread } from "./types";

export async function getBoards(): Promise<Board[]> {
    return (await apiGet<Board[]>('/boards')) ?? [];
}

export async function getBoard(slug: string | undefined): Promise<Board | null> {
    const boards = await getBoards();
    return boards.find((b) => b.slug === slug) ?? null;
}

export function getThreads(slug: string) {
    return apiGet<PostSummary[]>(`/boards/${slug}/posts`);
}

export function getThread(slug: string, publicId: string) {
    return apiGet<Thread>(`/boards/${slug}/posts/${publicId}`);
}

