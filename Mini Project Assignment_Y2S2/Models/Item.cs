using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mini_Project_Assignment_Y2S2.Models
{
    public class Item
    {
        [Key]
        public int ItemID { get; set; }

        [Required]
        public string? IName { get; set; }

        [Required]
        public string? IType { get; set; }

        [Required]
        public string? Idescription { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        public string? LocationID { get; set; }
        public List<string>? Images { get; set; }
        public string Category { get; set; }

        [NotMapped]
        public List<IFormFile>? ImageFiles { get; set; }
    }
}
