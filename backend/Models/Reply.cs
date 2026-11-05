using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Reply
{
    public long Id { get; set; }

    public long PostId { get; set; }
    public Post Post { get; set; } = null!;

    [Required, MaxLength(5000)]
    public string Content { get; set; } = "";

    [MaxLength(30)]
    public string? Author { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}