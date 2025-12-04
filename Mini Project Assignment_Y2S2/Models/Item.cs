using System;
using System.ComponentModel.DataAnnotations;

namespace Mini_Project_Assignment_Y2S2.Models
{
    public class Item
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "请输入物品名称")]
        public string? Name { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "请选择丢失日期")]
        public DateTime LostDate { get; set; }

        public string? Location { get; set; }

        public string? ContactInfo { get; set; }
    }
}
