using System.ComponentModel.DataAnnotations;

namespace Mini_Project_Assignment_Y2S2.Models
{
    public class Location
    {
        [Key]
        public string? ID { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Address { get; set; }

        public List<Item> Items { get; set; } = new List<Item>();
    }
}