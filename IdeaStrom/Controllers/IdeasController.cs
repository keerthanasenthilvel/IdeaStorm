using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using IdeaStrom.Model;
using Microsoft.Extensions.Configuration;
namespace IdeaStrom.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IdeasController: Controller
    {
        private readonly string _connectionString;

        // .NET automatically finds the appsettings.json and passes it here
        public IdeasController(IConfiguration configuration)
        {git 
            _connectionString = configuration.GetConnectionString("DBConnection");
        }

        // api to submit a new idea
        [HttpPost("submit")] 
        public async Task<IActionResult> SubmitIdea([FromBody] IdeaRequest request)
        {
            using(IDbConnection db = new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new
                    {
                        Title = request.Title,
                        Description = request.Description,
                        CategoryId = request.CategoryID,
                        UserId = request.UserID
                    };
                    var newIdeaId = await db.QuerySingleOrDefaultAsync<int>(
                        "sp_SubmitIdea",
                        parameters,
                        commandType: CommandType.StoredProcedure
                        );
                    return Ok(new { Message = "Idea Created!", IdeaId = newIdeaId });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Error = ex.Message });
                }
            }
        }
        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("comment")]
        public async Task<IActionResult> AddComment([FromBody] CommentSubmission request)
        {
            using (IDbConnection db= new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new
                    {
                        IdeaId = request.IdeaID,
                        UserId = request.UserID,
                        Text = request.CommentText
                    };

                    await db.ExecuteAsync(
                        "sp_AddComment",
                        parameters,
                        commandType: CommandType.StoredProcedure
                        );
                    return Ok(new { Message = "Comment added Successfully!" });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Error = ex.Message });
                }
            }
        }

        [HttpGet("notifications/{userId}")]
        public async Task<IActionResult> GetNotifications(int userId)
        {
            using(IDbConnection db=new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters=new { UserId = userId };
                    var notifications=await db.QueryAsync<NotificationResponse>(
                        "sp_GetUserNotifications",
                        parameters,
                        commandType: CommandType.StoredProcedure
                        );
                    return Ok(notifications);
                }
                catch(Exception ex)
                {
                    return BadRequest(new { Error = ex.Message });
                }
            }
        }
        // --- VOTING API ---
        [HttpPost("vote")]
        public async Task<IActionResult> VoteIdea([FromBody] VoteSubmission request)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new
                    {
                        IdeaID = request.IdeaID,
                        UserID = request.UserID,
                        VoteType = request.VoteType
                    };

                    // We use ExecuteAsync because we are just updating a record
                    await db.ExecuteAsync("sp_VoteIdea", parameters, commandType: CommandType.StoredProcedure);
                    return Ok(new { Message = "Vote recorded successfully!" });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Error = ex.Message });
                }
            }
        }

        // --- REVIEW API ---
        [HttpPost("review")]
        public async Task<IActionResult> ReviewIdea([FromBody] ReviewSubmission request)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new
                    {
                        IdeaID = request.IdeaID,
                        ReviewerID = request.ReviewerID,
                        Feedback = request.Feedback,
                        Decision = request.Decision
                    };

                    await db.ExecuteAsync("sp_ReviewIdea", parameters, commandType: CommandType.StoredProcedure);
                    return Ok(new { Message = "Review decision processed and idea status updated!" });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Error = ex.Message });
                }
            }
        }


        [HttpGet("top")]
        public async Task<IActionResult> GetTopIdeas()
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                try
                {
                    // We use QueryAsync because we want a list of the top 5 ideas
                    var topIdeas = await db.QueryAsync<TopIdeaResponse>(
                        "sp_GetTopIdeas",
                        commandType: CommandType.StoredProcedure
                    );

                    return Ok(topIdeas);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Error = ex.Message });
                }
            }
        }

        //Register

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistration request)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new
                    {
                        Name = request.Name,
                        Email = request.Email,
                        Password = request.Password, // Ensure this property exists in your Model
                        Role = request.Role
                    };

                    var userId = await db.QuerySingleOrDefaultAsync<int>(
                        "sp_RegisterUser",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return Ok(new { Message = "User registered successfully!", UserID = userId });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Error = ex.Message });
                }
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin request)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new { Email = request.Email, Password = request.Password };

                    // We use QuerySingleOrDefault because we expect exactly one user or nothing
                    var user = await db.QuerySingleOrDefaultAsync<dynamic>(
                        "sp_LoginUser",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    if (user != null)
                    {
                        // Return the user info so the Frontend knows the Role
                        return Ok(new
                        {
                            Message = "Login Successful",
                            UserID = user.UserID,
                            Name = user.Name,
                            Role = user.Role
                        });
                    }
                    else
                    {
                        return Unauthorized(new { Message = "Invalid Email or Password" });
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Error = ex.Message });
                }
            }
        }
        public class IdeaRequest
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public int CategoryID { get; set; }
            public int UserID { get; set; }
        }
    }
}
