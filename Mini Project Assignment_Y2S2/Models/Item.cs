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
        public string? Idescription { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        [DateNotInFuture] // 👈 自定义日期验证
        public DateTime Date { get; set; } = DateTime.Today;

        public string? LocationID { get; set; }

        public List<string>? Images { get; set; }

        [Required]
        public string Category { get; set; }

        [NotMapped]
        public List<IFormFile>? ImageFiles { get; set; }

        public string? UserID { get; set; }

        public string Status { get; set; } // ACTIVE | CLAIMED | EXPIRED
    }
}
