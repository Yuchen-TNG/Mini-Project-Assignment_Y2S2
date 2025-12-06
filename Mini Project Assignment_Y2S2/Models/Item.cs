using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public string? Image { get; set; }
    public string Category { get; set; }

    [NotMapped]
    public IFormFile? ImageFile { get; set; }
}
