namespace IdeaStrom.Model
{
    public class NotificationResponse
    {
        public int NotificationID { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; }
    }
}
