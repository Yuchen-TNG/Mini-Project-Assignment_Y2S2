using Microsoft.EntityFrameworkCore;
using Mini_Project_Assignment_Y2S2.Models;
using System.Collections.Generic;

namespace Mini_Project_Assignment_Y2S2.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }

    }
}
