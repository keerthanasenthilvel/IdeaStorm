namespace IdeaStrom.Model
{
    public class UserRegistration
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // In a real app, we would hash this!
        public string Role { get; set; }     // e.g., 'Employee', 'Manager'
    }
}