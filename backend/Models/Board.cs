using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Board
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string Slug { get; set; } = "";

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public List<Post> Posts { get; set; } = [];
}