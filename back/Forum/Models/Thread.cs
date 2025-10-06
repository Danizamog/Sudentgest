namespace Forum.Models
{
    public class Thread
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public Guid CategoryId { get; set; }
        public Category? Category { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = "Usuario";
        public List<string> Tags { get; set; } = new();
        public bool IsPinned { get; set; } = false;
        public int Views { get; set; } = 0;
        public int ReplyCount { get; set; } = 0;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<Reply> Replies { get; set; } = new();
    }
}