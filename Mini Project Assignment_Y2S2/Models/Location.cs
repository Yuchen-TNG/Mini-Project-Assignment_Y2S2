using System.ComponentModel.DataAnnotations;

namespace Mini_Project_Assignment_Y2S2.Models
{
    public class Location
    {
        [Key]
        public string? LocationID { get; set; }

        [Required]
        public string? LocationName { get; set; }

        public List<Item> Items { get; set; } = new List<Item>();
    }
}