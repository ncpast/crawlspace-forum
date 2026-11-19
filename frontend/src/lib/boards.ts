import { apiGet, apiPost } from "./api";
import type { Board, PostSummary, Thread, NewPost, Reply, NewReply } from "./types";

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

export function createPost(slug: string, post: NewPost) {
    return apiPost<PostSummary>(`/boards/${slug}/posts`, post)
}

export function createReply(slug: string, publicId: string, reply: NewReply) {
    return apiPost<Reply>(`/boards/${slug}/posts/${publicId}/replies`, reply)
}