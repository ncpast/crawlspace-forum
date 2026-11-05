using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Post
{
    public long Id { get; set; }

    public int BoardId { get; set; }
    public Board Board { get; set; } = null!;

    [Required]
    public string PublicId { get; set; } = "";

    [Required, MaxLength(100)]
    public string Title { get; set; } = "";

    [Required, MaxLength(5000)]
    public string Content { get; set; } = "";

    [Required, MaxLength(30)]
    public string Author { get; set; } = "Anonymous";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime BumpedAt { get; set; } = DateTime.UtcNow;

    public List<Reply> Replies { get; set; } = [];
}