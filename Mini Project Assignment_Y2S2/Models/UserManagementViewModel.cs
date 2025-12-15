namespace Mini_Project_Assignment_Y2S2.ViewModels 
{
    public class UserManagementViewModel
    {
        public string Id { get; set; } // Firestore Document ID
        public string UserID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public bool IsArchived { get; set; }
    }
}