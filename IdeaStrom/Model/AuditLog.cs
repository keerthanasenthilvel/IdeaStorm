namespace IdeaStrom.Model
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string Functionality { get; set; }
        public string ActionStatus { get; set; } // Success or Failure
        public string? Exception { get; set; }
        public string? Details { get; set; }
        public DateTime InsertedOn { get; set; } = DateTime.Now;
    }
}
