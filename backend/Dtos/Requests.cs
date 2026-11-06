namespace Backend.Dtos;

public record CreatePostRequest(string Title, string Content, string? Author);

public record CreateReplyRequest(string Content, string? Author);