using Forum.Models;
using Forum.Models.DTOs;

namespace Forum.Services
{
    public interface IForumService
    {
        Task<List<ThreadDTO>> GetThreadsAsync(string? category = null, string? search = null);
        Task<ThreadDTO?> GetThreadAsync(Guid id);
        Task<ThreadDTO> CreateThreadAsync(CreateThreadDTO threadDto, string userId, string userName);
        Task<Reply> CreateReplyAsync(CreateReplyDTO replyDto, string userId, string userName);
        Task<List<Category>> GetCategoriesAsync();
        Task<bool> DeleteThreadAsync(Guid threadId, string userId);
        Task<bool> DeleteReplyAsync(Guid replyId, string userId);
    }
}