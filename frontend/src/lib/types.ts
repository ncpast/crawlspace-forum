export interface Board {
    id: number;
    slug: string;
    name: string;
    description: string;
}

export interface PostSummary {
    id: number;
    publicId: string;
    title: string;
    content: string;
    author: string;
    createdAt: string;
    bumpedAt: string;
    replyCount: number;
}

export interface NewPost {
    title: string;
    content: string;
    author?: string
}

export interface Reply {
    id: number;
    content: string;
    author: string;
    createdAt: string;
}

export interface NewReply {
    content: string;
    author?: string;
}

export interface Thread extends Omit<PostSummary, 'replyCount'> {
    replies: Reply[];
}

export type ApiResult<T> =
    | {ok: true; data: T}
    | {ok: false; error: string};