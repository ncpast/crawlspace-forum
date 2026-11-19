import { useState, type SubmitEvent } from "react";
import { createPost } from "../lib/boards";
import { LIMITS } from "../lib/limits";

interface Props {
  slug: string;
}

export default function NewPostForm({ slug }: Props) {
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [author, setAuthor] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);

    const t = title.trim();
    const c = content.trim();
    const a = author.trim();

    if (!t) {
      setError("Title is required.");
      return;
    }

    if (!c) {
      setError("Content is required.");
      return;
    }

    setSubmitting(true);
    const res = await createPost(slug, {
      title: t,
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
      <h3>Start a new thread</h3>

      <input
        type="text"
        placeholder="Name (optional)"
        aria-label="Name"
        value={author}
        maxLength={LIMITS.author}
        onChange={(e) => setAuthor(e.target.value)}
      />

      <input
        type="text"
        placeholder="Title"
        aria-label="Title"
        value={title}
        maxLength={LIMITS.title}
        onChange={(e) => setTitle(e.target.value)}
      />

      <textarea
        placeholder="What's on your mind?"
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
        {submitting ? "Posting..." : "Post"}
      </button>
    </form>
  );
}
