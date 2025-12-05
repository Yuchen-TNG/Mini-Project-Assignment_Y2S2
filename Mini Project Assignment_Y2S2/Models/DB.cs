using System.ComponentModel.DataAnnotations;

namespace Mini_Project_Assignment_Y2S2.Models
{
    // ------------------- USER TABLE -------------------
    public class User
    {
        [Key]
        public string UserID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; }  // Student/Admin
    }
}