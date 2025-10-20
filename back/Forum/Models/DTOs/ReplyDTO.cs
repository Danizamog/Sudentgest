namespace Forum.Models.DTOs
{
    public class ReplyDTO
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid ThreadId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public int Likes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateReplyDTO
    {
        public Guid ThreadId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
