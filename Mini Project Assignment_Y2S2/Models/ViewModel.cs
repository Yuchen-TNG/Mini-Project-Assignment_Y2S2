using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Mini_Project_Assignment_Y2S2.Models
{
    public class CardDetailsViewModel
    {
        public Item? Item { get; set; }   // Firestore 里的物品信息
        public User? User { get; set; }   // 当前登录用户信息
        public Location? Location { get; set; }
        public bool IsPending { get; set; }
        public bool IsApproved { get; set; }


    }

    public class LocationItemsViewModel
    {
        public List<Location>? Locations { get; set; }      // 所有 Location
        public List<Item>? Items { get; set; }     // 经过过滤的 Item
    }

    public class ItemCardViewModel
    {
        public int ItemID { get; set; }
        public string? IType { get; set; }
        public DateTime Date { get; set; }
        public List<string>? Images { get; set; }
        public string? LocationName { get; set; }
    }

    public class UserViewModel
    {
        // 基本信息
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be 2-50 characters")]
        [RegularExpression(@"^[A-Za-z\s\.\-']+$",
            ErrorMessage = "Name can only contain letters, spaces, dots, hyphens and apostrophes")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        [StringLength(100, ErrorMessage = "Email is too long")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^(01)[0-46-9][0-9]{7,8}$",
            ErrorMessage = "Please enter a valid Malaysian phone number starting with 01")]
        [StringLength(11, MinimumLength = 10, ErrorMessage = "Phone number must be 10-11 digits")]
        public string PhoneNumber { get; set; }

        // 当前密码
        [Display(Name = "Current Password")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        // 新密码
        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special symbol (@$!%*?&)")]
        public string? NewPassword { get; set; }

        // 新密码
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [StringLength(30, MinimumLength = 8, ErrorMessage = "Password must be 8-30 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special symbol (@$!%*?&)")]
        public string? Password { get; set; }

        // 确认密码
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        // 图片URL
        public string? ProfileImageUrl { get; set; }
    }

    public class ChangeCurrentPasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [Display(Name = "Current Password")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        [StringLength(30, MinimumLength = 8, ErrorMessage = "Password must be 8-30 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number, and one special symbol (@$!%*?&)")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password")]
        [Display(Name = "Confirm New Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangePasswordViewModel
    {

        [Required(ErrorMessage = "New password is required")]
        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        [StringLength(30, MinimumLength = 8, ErrorMessage = "Password must be 8-30 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number, and one special symbol (@$!%*?&)")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password")]
        [Display(Name = "Confirm New Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }

}
    