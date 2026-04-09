using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.Data;
using ProjectLibrary.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка на базата данни
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Настройка на Identity (Потребители и Роли)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // За по-лесно тестване

    // Опростени пароли за development (може да ги затегнеш по-късно)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 3;
})
.AddRoles<IdentityRole>() // ВАЖНО: Активира системата за роли
.AddEntityFrameworkStores<ApplicationDbContext>();

// 3. Добавяне на MVC и Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// 4. Конфигурация на HTTP пайплайна
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ВАЖНО: Редът е Authentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// === 5. АВТОМАТИЧНО СЪЗДАВАНЕ И ПЪЛНЕНЕ НА БАЗАТА ПРИ СМЯНА НА КОМПЮТЪР ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Магия 1: Създава базата и таблиците от нулата на новия компютър!
        context.Database.Migrate();

        // Магия 2: Пълни новата база със стартови данни (Значки, Роли, Админ)
        await DbInitializer.Initialize(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Грешка при създаване/инициализация на базата данни.");
    }
}
// ==========================================================================

app.Run();