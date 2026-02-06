
namespace IdeaStrom.Model
{
    public class CommentSubmission
    {

        public int IdeaID { get; set; }
        public int UserID { get; set; }
        public string CommentText { get; set; } = string.Empty;
    }
}