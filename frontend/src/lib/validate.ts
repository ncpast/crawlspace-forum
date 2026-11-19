import { LIMITS } from "./limits";

export function validatePost(title: string, content: string, author: string): string | null {
    if (!title) return "Title is required.";
    if (title.length > LIMITS.title) return `Title must be at most ${LIMITS.title} characters.`;
    if (!content) return "Content is required.";
    if (content.length > LIMITS.content) return `Content must be at most ${LIMITS.content} characters.`;
    if (author.length > LIMITS.author) return `Name must be at most ${LIMITS.author} characters.`;
    return null;
}

export function validateReply(content: string, author: string): string | null {
    if (!content) return "Content is required.";
    if (content.length > LIMITS.content) return `Content must be at most ${LIMITS.content} characters.`;
    if (author.length > LIMITS.author) return `Name must be at most ${LIMITS.author} characters.`;
    return null;
}