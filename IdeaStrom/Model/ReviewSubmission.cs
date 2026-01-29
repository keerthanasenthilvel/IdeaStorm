namespace IdeaStrom.Model
{
    public class ReviewSubmission
    {
        public int IdeaID { get; set; }
        public int ReviewerID { get; set; }
        public string Feedback { get; set; }
        public string Decision { get; set; }
    }
}
