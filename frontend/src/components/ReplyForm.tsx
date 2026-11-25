import { useEffect, useRef, useState, type SubmitEvent } from "react";
import { createReply } from "../lib/boards";
import { LIMITS } from "../lib/limits";
import { validateReply } from "../lib/validate";

interface Props {
    slug: string;
    publicId: string;
}

export default function ReplyForm({ slug, publicId }: Props) {
    const [content, setContent] = useState("");
    const [author, setAuthor] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    useEffect(() => {
        function onReplyTo(e: Event) {
            const id = (e as CustomEvent<{ id: string }>).detail.id;
            setContent((prev) => {
                const rest = prev.replace(/^RE: \S+\n?/, "");
                return `RE: ${id}\n${rest}`.slice(0, LIMITS.content);
            });
            textareaRef.current?.focus();
        }
        window.addEventListener("reply-to", onReplyTo);
        return () => window.removeEventListener("reply-to", onReplyTo);
    }, []);

    async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
        e.preventDefault();
        setError(null);

        const c = content.trim();
        const a = author.trim();

        const problem = validateReply(c, a);
        if (problem) {
            setError(problem);
            return;
        }

        setSubmitting(true);
        const res = await createReply(slug, publicId, {
            content: c,
            author: a || undefined,
        });

        if (!res.ok) {
            setError(res.error);
            setSubmitting(false);
            return;
        }

        window.location.reload();
    }

    return (
        <form className="post-form" onSubmit={handleSubmit}>
            <h3>Reply to this thread</h3>

            <input
                type="text"
                placeholder="Name (optional)"
                aria-label="Name"
                value={author}
                maxLength={LIMITS.author}
                onChange={(e) => setAuthor(e.target.value)}
            />

            <textarea
                ref={textareaRef}
                placeholder="Write your reply..."
                aria-label="Content"
                value={content}
                maxLength={LIMITS.content}
                onChange={(e) => setContent(e.target.value)}
            />
            <span className="char-count">
                {content.length}/{LIMITS.content}
            </span>

            {error && <p className="form-error">{error}</p>}

            <button type="submit" disabled={submitting}>
                {submitting ? "Posting..." : "Reply"}
            </button>
        </form>
    );
}