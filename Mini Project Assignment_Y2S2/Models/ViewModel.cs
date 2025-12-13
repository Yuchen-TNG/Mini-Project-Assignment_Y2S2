using System.ComponentModel.DataAnnotations;

namespace Mini_Project_Assignment_Y2S2.Models
{
    public class CardDetailsViewModel
    {
        public Item Item { get; set; }   // Firestore 里的物品信息
        public User User { get; set; }   // 当前登录用户信息
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [RegularExpression(@"^[A-Za-z\s\.\-_]+$", ErrorMessage = "Username can contain letters, spaces, and symbols .-_ only")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\d{1,10}$", ErrorMessage = "Phone number must be numeric and max 10 digits")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
    public class ChangePasswordViewModel
    {
        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangeCurrentPasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}
