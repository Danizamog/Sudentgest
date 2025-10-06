using Forum.Models;
using Microsoft.AspNetCore.Mvc;
using Forum.Services;
using Forum.Models.DTOs;

namespace Forum.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForumController : ControllerBase
    {
        private readonly IForumService _forumService;

        public ForumController(IForumService forumService)
        {
            _forumService = forumService;
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
                // En un sistema real, obtendrías el usuario del token JWT
                var userId = "user-" + Guid.NewGuid().ToString();
                var userName = "Usuario Demo";

                var thread = await _forumService.CreateThreadAsync(threadDto, userId, userName);
                return CreatedAtAction(nameof(GetThread), new { id = thread.Id }, thread);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating thread", error = ex.Message });
            }
        }

        [HttpPost("replies")]
        public async Task<ActionResult<Reply>> CreateReply([FromBody] CreateReplyDTO replyDto)
        {
            try
            {
                // En un sistema real, obtendrías el usuario del token JWT
                var userId = "user-" + Guid.NewGuid().ToString();
                var userName = "Usuario Demo";

                var reply = await _forumService.CreateReplyAsync(replyDto, userId, userName);
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
                var userId = "user-demo"; // En realidad vendría del token
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
                var userId = "user-demo"; // En realidad vendría del token
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
