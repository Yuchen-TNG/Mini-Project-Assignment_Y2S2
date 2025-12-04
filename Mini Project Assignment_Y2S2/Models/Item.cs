using System;
using System.ComponentModel.DataAnnotations;


namespace Mini_Project_Assignment_Y2S2.Models
{
    public class Item
    {
        [Key]
        public int ItemID { get; set; }

        [Required]
        public string IName { get; set; }

        [Required]
        public string IType { get; set; }

        [Required]
        public string Idescription { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        public string Image { get; set; }

        public string Category { get; set; }

    }
}
