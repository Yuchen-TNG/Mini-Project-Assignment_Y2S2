using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mini_Project_Assignment_Y2S2.Models
{
    public class Item
    {
        [Key]
        public int ItemID { get; set; }

        [Required(ErrorMessage = "Item name is required")]
        public string? IName { get; set; }

        [Required(ErrorMessage = "Item type is required")]
        public string? IType { get; set; }

        [Required(ErrorMessage = "Item description is required")]
        public string Idescription { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Location is required")]
        public string? LocationID { get; set; }

        // ⭐ 修正：移除 Images 的 Required，因为它是保存后生成的
        public List<string> Images { get; set; } = new List<string>();

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; }

        // ⭐ 修正：移除 ImageFiles 的 Required，在控制器中验证
        [DataType(DataType.Upload)]
        public List<IFormFile>? ImageFiles { get; set; }

        public string? UserID { get; set; }

        public string? IStatus { get; set; } // "Pending Approval", "Approved", "Rejected"

        public string? LocationFound { get; set; }
    }

    // ⭐ 添加自定义日期验证
    public class DateNotInFutureAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime date)
            {
                if (date > DateTime.Today)
                {
                    return new ValidationResult("Date cannot be in the future");
                }
            }
            return ValidationResult.Success;
        }
    }
}