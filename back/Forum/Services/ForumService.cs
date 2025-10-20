using Thread = Forum.Models.Thread;
using Reply = Forum.Models.Reply;
using Category = Forum.Models.Category;
using Microsoft.EntityFrameworkCore;
using Forum.Data;
using Forum.Models;
using Forum.Models.DTOs;

namespace Forum.Services
{
    public class ForumService : IForumService
    {
        private readonly ForumContext _context;

        public ForumService(ForumContext context)
        {
            _context = context;
        }

        public async Task<List<ThreadDTO>> GetThreadsAsync(string? category = null, string? search = null)
        {
            var query = _context.Threads
                .Include(t => t.Category)
                .Include(t => t.Replies)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category) && category != "all")
            {
                query = query.Where(t => t.Category != null && t.Category.Name == category);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => 
                    t.Title.Contains(search) || 
                    t.Content.Contains(search) ||
                    t.Tags.Any(tag => tag.Contains(search))
                );
            }

            return await query
                .OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.LastActivity)
                .Select(t => new ThreadDTO
                {
                    Id = t.Id,
                    Title = t.Title,
                    Content = t.Content,
                    Excerpt = t.Excerpt ?? (t.Content.Length > 150 ? t.Content.Substring(0, 150) + "..." : t.Content),
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category!.Name,
                    AuthorName = t.AuthorName,
                    AuthorRole = t.AuthorRole,
                    Tags = t.Tags,
                    IsPinned = t.IsPinned,
                    Views = t.Views,
                    ReplyCount = t.ReplyCount,
                    LastActivity = t.LastActivity,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ThreadDTO?> GetThreadAsync(Guid id)
        {
            var thread = await _context.Threads
                .Include(t => t.Category)
                .Include(t => t.Replies)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (thread == null) return null;

            thread.Views++;
            await _context.SaveChangesAsync();

            return new ThreadDTO
            {
                Id = thread.Id,
                Title = thread.Title,
                Content = thread.Content,
                Excerpt = thread.Excerpt ?? (thread.Content.Length > 150 ? thread.Content.Substring(0, 150) + "..." : thread.Content),
                CategoryId = thread.CategoryId,
                CategoryName = thread.Category!.Name,
                AuthorName = thread.AuthorName,
                AuthorRole = thread.AuthorRole,
                Tags = thread.Tags,
                IsPinned = thread.IsPinned,
                Views = thread.Views,
                ReplyCount = thread.ReplyCount,
                LastActivity = thread.LastActivity,
                CreatedAt = thread.CreatedAt
            };
        }

        public async Task<ThreadDTO> CreateThreadAsync(CreateThreadDTO threadDto, string userId, string userName, string userRole)
        {
            var thread = new Thread
            {
                Title = threadDto.Title,
                Content = threadDto.Content,
                Excerpt = threadDto.Content.Length > 150 ? threadDto.Content.Substring(0, 150) + "..." : threadDto.Content,
                CategoryId = threadDto.CategoryId,
                UserId = userId,
                AuthorName = userName,
                AuthorRole = userRole,
                Tags = threadDto.Tags,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            _context.Threads.Add(thread);
            await _context.SaveChangesAsync();

            var createdThread = await _context.Threads
                .Include(t => t.Category)
                .FirstAsync(t => t.Id == thread.Id);

            return new ThreadDTO
            {
                Id = createdThread.Id,
                Title = createdThread.Title,
                Content = createdThread.Content,
                Excerpt = createdThread.Excerpt!,
                CategoryId = createdThread.CategoryId,
                CategoryName = createdThread.Category!.Name,
                AuthorName = createdThread.AuthorName,
                AuthorRole = createdThread.AuthorRole,
                Tags = createdThread.Tags,
                IsPinned = createdThread.IsPinned,
                Views = createdThread.Views,
                ReplyCount = createdThread.ReplyCount,
                LastActivity = createdThread.LastActivity,
                CreatedAt = createdThread.CreatedAt
            };
        }

        public async Task<List<ReplyDTO>> GetRepliesAsync(Guid threadId)
        {
            var replies = await _context.Replies
                .Where(r => r.ThreadId == threadId)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new ReplyDTO
                {
                    Id = r.Id,
                    Content = r.Content,
                    ThreadId = r.ThreadId,
                    AuthorName = r.AuthorName,
                    AuthorRole = r.AuthorRole,
                    Likes = r.Likes,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return replies;
        }

        public async Task<ReplyDTO> CreateReplyAsync(CreateReplyDTO replyDto, string userId, string userName, string userRole)
        {
            var reply = new Reply
            {
                Content = replyDto.Content,
                ThreadId = replyDto.ThreadId,
                UserId = userId,
                AuthorName = userName,
                AuthorRole = userRole,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Replies.Add(reply);

            var thread = await _context.Threads.FindAsync(replyDto.ThreadId);
            if (thread != null)
            {
                thread.ReplyCount++;
                thread.LastActivity = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            // Return DTO to avoid circular reference
            return new ReplyDTO
            {
                Id = reply.Id,
                Content = reply.Content,
                ThreadId = reply.ThreadId,
                AuthorName = reply.AuthorName,
                AuthorRole = reply.AuthorRole,
                Likes = reply.Likes,
                CreatedAt = reply.CreatedAt
            };
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<bool> DeleteThreadAsync(Guid threadId, string userId)
        {
            var thread = await _context.Threads.FirstOrDefaultAsync(t => t.Id == threadId && t.UserId == userId);
            if (thread == null) return false;

            _context.Threads.Remove(thread);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReplyAsync(Guid replyId, string userId)
        {
            var reply = await _context.Replies.FirstOrDefaultAsync(r => r.Id == replyId && r.UserId == userId);
            if (reply == null) return false;

            _context.Replies.Remove(reply);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}