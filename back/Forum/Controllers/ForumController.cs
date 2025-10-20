using Forum.Models;
using Microsoft.AspNetCore.Mvc;
using Forum.Services;
using Forum.Models.DTOs;
using Forum.Helpers;

namespace Forum.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForumController : ControllerBase
    {
        private readonly IForumService _forumService;
        private readonly AuthHelper _authHelper;

        public ForumController(IForumService forumService, AuthHelper authHelper)
        {
            _forumService = forumService;
            _authHelper = authHelper;
        }

        private async Task<(string userId, string userName, string userRole)?> GetAuthenticatedUser()
        {
            var userEmail = Request.Headers["X-User-Email"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(userEmail))
            {
                return null;
            }

            var userInfo = await _authHelper.GetUserInfoFromEmail(userEmail);
            if (userInfo == null)
            {
                return null;
            }

            var fullName = _authHelper.GetFullName(userInfo.Nombre, userInfo.Apellido);
            return (userInfo.Id, fullName, userInfo.Rol);
        }

        [HttpGet("threads")]
        public async Task<ActionResult<List<ThreadDTO>>> GetThreads([FromQuery] string? category, [FromQuery] string? search)
        {
            try
            {
                var threads = await _forumService.GetThreadsAsync(category, search);
                return Ok(threads);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving threads", error = ex.Message });
            }
        }

        [HttpGet("threads/{id}")]
        public async Task<ActionResult<ThreadDTO>> GetThread(Guid id)
        {
            try
            {
                var thread = await _forumService.GetThreadAsync(id);
                if (thread == null) return NotFound();
                return Ok(thread);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving thread", error = ex.Message });
            }
        }

        [HttpPost("threads")]
        public async Task<ActionResult<ThreadDTO>> CreateThread([FromBody] CreateThreadDTO threadDto)
        {
            try
            {
                var user = await GetAuthenticatedUser();
                if (user == null)
                {
                    return Unauthorized(new { message = "X-User-Email header is required" });
                }

                var (userId, userName, userRole) = user.Value;
                var thread = await _forumService.CreateThreadAsync(threadDto, userId, userName, userRole);
                return CreatedAtAction(nameof(GetThread), new { id = thread.Id }, thread);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating thread", error = ex.Message });
            }
        }

        [HttpGet("threads/{threadId}/replies")]
        public async Task<ActionResult<List<ReplyDTO>>> GetReplies(Guid threadId)
        {
            try
            {
                var replies = await _forumService.GetRepliesAsync(threadId);
                return Ok(replies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving replies", error = ex.Message });
            }
        }

        [HttpPost("threads/{threadId}/replies")]
        public async Task<ActionResult<ReplyDTO>> CreateReply(Guid threadId, [FromBody] CreateReplyDTO replyDto)
        {
            try
            {
                var user = await GetAuthenticatedUser();
                if (user == null)
                {
                    return Unauthorized(new { message = "X-User-Email header is required" });
                }

                // Ensure the threadId from route matches the DTO
                replyDto.ThreadId = threadId;

                var (userId, userName, userRole) = user.Value;
                var reply = await _forumService.CreateReplyAsync(replyDto, userId, userName, userRole);
                return Ok(reply);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating reply", error = ex.Message });
            }
        }

        [HttpGet("categories")]
        public async Task<ActionResult<List<Category>>> GetCategories()
        {
            try
            {
                var categories = await _forumService.GetCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving categories", error = ex.Message });
            }
        }

        [HttpDelete("threads/{threadId}")]
        public async Task<ActionResult> DeleteThread(Guid threadId)
        {
            try
            {
                var user = await GetAuthenticatedUser();
                if (user == null)
                {
                    return Unauthorized(new { message = "X-User-Email header is required" });
                }

                var (userId, _, _) = user.Value;
                var result = await _forumService.DeleteThreadAsync(threadId, userId);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting thread", error = ex.Message });
            }
        }

        [HttpDelete("replies/{replyId}")]
        public async Task<ActionResult> DeleteReply(Guid replyId)
        {
            try
            {
                var user = await GetAuthenticatedUser();
                if (user == null)
                {
                    return Unauthorized(new { message = "X-User-Email header is required" });
                }

                var (userId, _, _) = user.Value;
                var result = await _forumService.DeleteReplyAsync(replyId, userId);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting reply", error = ex.Message });
            }
        }
    }
}
