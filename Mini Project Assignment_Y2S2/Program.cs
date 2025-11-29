using Microsoft.EntityFrameworkCore;
using Mini_Project_Assignment_Y2S2.Data;

var builder = WebApplication.CreateBuilder(args);

// 1?? ?? DbContext??? LocalDB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2?? ?? MVC ??
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 3?? ?? HTTP ????
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();  // ???? wwwroot ????

app.UseRouting();

app.UseAuthorization();

// 4?? ????
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
