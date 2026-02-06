using IdeaStrom.Data; // Ensure this points to your AppDbContext location
using IdeaStrom.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace IdeaStrom.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IdeasController : Controller
    {
        private readonly AppDbContext _context;

        // EF Core is injected here automatically now
        public IdeasController(AppDbContext context)
        {
            _context = context;
        }


        // -- AUDIT HELPER -- //
        private async Task LogToAudit(string? name, string func, string status, string? detail, string? ex = null)
        {
            try
            {
                var log = new AuditLog
                {
                    UserName = name ?? "Guest/System",
                    Functionality = func,
                    ActionStatus = status,
                    Details = detail,
                    Exception = ex,
                    InsertedOn = DateTime.Now
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch(Exception error) {
                Console.WriteLine("AUDIT ERROR: " + error.Message);
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitIdea([FromBody] IdeaRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Title))
            {
                await LogToAudit("Unknown", "Submit Idea", "Failure", "Client sent empty request body", "Null Request Object");
                return BadRequest(new { Error = "Request body cannot be empty!" });
            }
            try
            {
                // For SPs that return a value (like an ID), we use SqlQueryRaw
                var result = await _context.Database.SqlQueryRaw<int>(
                    "EXEC sp_SubmitIdea @Title={0}, @Description={1}, @CategoryId={2}, @UserId={3}",
                    request.Title, request.Description, request.CategoryID, request.UserID
                ).ToListAsync();

                int ideaId = Convert.ToInt32(result.FirstOrDefault());
                await LogToAudit(null, "Submit Idea", "Success", $"New Idea ID: {ideaId}");
                return Ok(new { Message = "Idea Created!", IdeaId = ideaId });
            }
            catch (Exception ex)
            {
                await LogToAudit(null, "Submit Idea", "Failure", "Idea insertion failed", ex.Message);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("comment")]
        public async Task<IActionResult> AddComment([FromBody] CommentSubmission request)
        {
            try
            {
                // Use ExecuteSqlRawAsync for SPs that don't return data (void/int status)
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_AddComment @IdeaId={0}, @UserId={1}, @Text={2}",
                    request.IdeaID, request.UserID, request.CommentText
                );
                return Ok(new { Message = "Comment added Successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("notifications/{userId}")]
        public async Task<IActionResult> GetNotifications(int userId)
        {
            try
            {
                // Mapping the result directly to your NotificationResponse model
                var notifications = await _context.Notifications
                    .FromSqlRaw("EXEC sp_GetUserNotifications @UserId={0}", userId)
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("vote")]
        public async Task<IActionResult> VoteIdea([FromBody] VoteSubmission request)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_VoteIdea @IdeaID={0}, @UserID={1}, @VoteType={2}",
                    request.IdeaID, request.UserID, request.VoteType
                );
                await LogToAudit(null, "Vote Idea", "Success", $"Voted {request.VoteType} on Idea {request.IdeaID}");
                return Ok(new { Message = "Vote recorded successfully!" });
            }
            catch (Exception ex)
            {
                await LogToAudit(null, "Vote Idea", "Failure", $"User {request.UserID} vote failed", ex.Message);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopIdeas()
        {
            try
            {
                var topIdeas = await _context.TopIdeas
                    .FromSqlRaw("EXEC sp_GetTopIdeas")
                    .ToListAsync();


                return Ok(topIdeas);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Logic for Login and Register would follow the same pattern 
        // using the LoginResult helper we created in the DbContext earlier.

        // --- REGISTER ---
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistration request)
        {
            try
            {
                // 1. CHANGE <int> TO <decimal> 
                // This stops the "Unable to cast" error because it matches the SQL return type
                var result = await _context.Database.SqlQueryRaw<decimal>(
                    "EXEC sp_RegisterUser @Name={0}, @Email={1}, @Password={2}, @Role={3}",
                    request.Name, request.Email, request.Password, request.Role
                ).ToListAsync();

                // 2. Safely get the first value and cast it to int for your response
                int userId = Convert.ToInt32(result.FirstOrDefault());
                await LogToAudit(request.Name, "User Registration", "Success", $"User ID: {userId}");
                return Ok(new { Message = "Registered!", UserID = userId });
            }
            catch (Exception ex)
            {
                // This helps you see if there are any other underlying issues
                await LogToAudit(request.Name, "User Registration", "Failure", "Registration crashed", ex.Message);
                return BadRequest(new { Error = ex.Message });
            }
        }

        // --- LOGIN ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin request)
        {
            try
            {
                // We map the result to our LoginResult helper class
                var users = await _context.LoginResults
                    .FromSqlRaw("EXEC sp_LoginUser @Email={0}, @Password={1}",
                                 request.Email, request.Password)
                    .ToListAsync();

                var user = users.FirstOrDefault();

                if (user != null)
                {
                    await LogToAudit(user.Name, "Login", "Success", "User logged in successfully");
                    return Ok(new
                    {
                        Message = "Login Successful",
                        UserID = user.UserID,
                        Name = user.Name,
                        Role = user.Role
                    });
                }
                await LogToAudit(null, "Login", "Failure", $"Invalid attempt for {request.Email}");
                return Unauthorized(new { Message = "Invalid Email or Password" });
            }
            catch (Exception ex)
            {
                await LogToAudit(null, "Login", "Failure", "Login error", ex.Message);
                return BadRequest(new { Error = ex.Message });
            }
        }
        public class IdeaRequest
        {
            public string Title { get; set; }        // Changed from object[] to string
            public string Description { get; set; }  // Changed from object to string
            public int CategoryID { get; set; }      // Changed from object to int
            public int UserID { get; set; }          // Changed from object to int
        }
    }
}
