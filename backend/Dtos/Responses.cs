namespace Backend.Dtos;

public record BoardResponse(int Id, string Slug, string Name, string Description);

public record PostResponse(
    long Id,
    string PublicId,
    string Title,
    string Content,
    string Author,
    DateTime CreatedAt,
    DateTime BumpedAt
);

public record ReplyResponse(
    long Id,
    string Content,
    string Author,
    DateTime CreatedAt
);

public record ThreadResponse(
    long Id,
    string PublicId,
    string Title,
    string Content,
    string Author,
    DateTime CreatedAt,
    DateTime BumpedAt,
    IReadOnlyList<ReplyResponse> Replies
);