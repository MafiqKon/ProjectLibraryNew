using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.Data;
using ProjectLibrary.Models;

public class SyncController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public SyncController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // Основен метод за синхронизация на всичко
    public async Task<IActionResult> SyncAll()
    {
        var results = new List<string>();

        try
        {
            // 1. СЪЗДАЙ РОЛИТЕ
            string[] roleNames = { "Admin", "Teacher", "Student" };
            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                    results.Add($"✅ Роля '{roleName}' създадена");
                }
                else
                {
                    results.Add($"ℹ️ Роля '{roleName}' вече съществува");
                }
            }

            // 2. СЪЗДАЙ АДМИНИСТРАТОР
            if (!_userManager.Users.Any(u => u.UserName == "admin@abv.bg"))
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@abv.bg",
                    Email = "admin@abv.bg",
                    FirstName = "Администратор",
                    LastName = "Системен",
                    UserType = UserType.Admin
                };
                await _userManager.CreateAsync(admin, "123456");
                await _userManager.AddToRoleAsync(admin, "Admin");
                results.Add("✅ Администратор създаден (admin@abv.bg / 123456)");
            }
            else
            {
                results.Add("ℹ️ Администратор вече съществува");
            }

            // 3. СЪЗДАЙ УЧИТЕЛ
            if (!_userManager.Users.Any(u => u.UserName == "teacher@abv.bg"))
            {
                var teacher = new ApplicationUser
                {
                    UserName = "teacher@abv.bg",
                    Email = "teacher@abv.bg",
                    FirstName = "Госпожа",
                    LastName = "Учителка",
                    UserType = UserType.Teacher
                };
                await _userManager.CreateAsync(teacher, "123456");
                await _userManager.AddToRoleAsync(teacher, "Teacher");
                results.Add("✅ Учител създаден (teacher@abv.bg / 123456)");
            }
            else
            {
                results.Add("ℹ️ Учител вече съществува");
            }

            // 4. СЪЗДАЙ СТУДЕНТИ
            if (!_userManager.Users.Any(u => u.UserName == "student@abv.bg"))
            {
                var student1 = new ApplicationUser
                {
                    UserName = "student@abv.bg",
                    Email = "student@abv.bg",
                    FirstName = "Иван",
                    LastName = "Студентов",
                    UserType = UserType.Student
                };
                await _userManager.CreateAsync(student1, "123456");
                await _userManager.AddToRoleAsync(student1, "Student");
                results.Add("✅ Студент 1 създаден (student@abv.bg / 123456)");
            }

            if (!_userManager.Users.Any(u => u.UserName == "student2@abv.bg"))
            {
                var student2 = new ApplicationUser
                {
                    UserName = "student2@abv.bg",
                    Email = "student2@abv.bg",
                    FirstName = "Мария",
                    LastName = "Ученичка",
                    UserType = UserType.Student
                };
                await _userManager.CreateAsync(student2, "123456");
                await _userManager.AddToRoleAsync(student2, "Student");
                results.Add("✅ Студент 2 създаден (student2@abv.bg / 123456)");
            }

            // 5. СЪЗДАЙ ПРОИЗВЕДЕНИЯ (ако няма)
            if (!_context.Books.Any())
            {
                var authors = new List<Author>
                {
                    new Author { Name = "Иван Вазов", Biography = "Български поет и писател" },
                    new Author { Name = "Алеко Константинов", Biography = "Български писател" },
                    new Author { Name = "Христо Ботев", Biography = "Български поет и революционер" },
                    new Author { Name = "Елин Пелин", Biography = "Български писател" },
                    new Author { Name = "Пейо Яворов", Biography = "Български поет" }
                };

                _context.Authors.AddRange(authors);
                await _context.SaveChangesAsync();

                var authorVazov = await _context.Authors.FirstAsync(a => a.Name == "Иван Вазов");
                var authorAleko = await _context.Authors.FirstAsync(a => a.Name == "Алеко Константинов");
                var authorBotev = await _context.Authors.FirstAsync(a => a.Name == "Христо Ботев");
                var authorElin = await _context.Authors.FirstAsync(a => a.Name == "Елин Пелин");
                var authorYavorov = await _context.Authors.FirstAsync(a => a.Name == "Пейо Яворов");

                var books = new List<Book>
                {
                    new Book {
                        Title = "Под игото",
                        AuthorId = authorVazov.Id,
                        Description = "Роман за Априлското въстание",
                        Genre = "Роман",
                        StudyYear = 10,
                        IsForMatura = true,
                        WritingPeriod = "Възраждане",
                        PublishedYear = 1894
                    },
                    new Book {
                        Title = "Бай Ганьо",
                        AuthorId = authorAleko.Id,
                        Description = "Сатирични разкази за българския характер",
                        Genre = "Сатира",
                        StudyYear = 9,
                        IsForMatura = true,
                        WritingPeriod = "Възраждане",
                        PublishedYear = 1895
                    },
                    new Book {
                        Title = "Епопея на забравените",
                        AuthorId = authorVazov.Id,
                        Description = "Патриотични стихове",
                        Genre = "Поезия",
                        StudyYear = 8,
                        IsForMatura = true,
                        WritingPeriod = "Възраждане",
                        PublishedYear = 1884
                    },
                    new Book {
                        Title = "Гераците",
                        AuthorId = authorElin.Id,
                        Description = "Разкази за селския живот",
                        Genre = "Разказ",
                        StudyYear = 7,
                        IsForMatura = false,
                        WritingPeriod = "Следосвобожденска",
                        PublishedYear = 1911
                    }
                };

                _context.Books.AddRange(books);
                await _context.SaveChangesAsync();
                results.Add($"✅ Произведения създадени ({books.Count} книги)");
            }
            else
            {
                results.Add($"ℹ️ Вече има {_context.Books.Count()} произведения");
            }

            return Content($"<h2>🔧 Синхронизация завършена</h2>" +
                          string.Join("<br>", results) +
                          "<br><br><a href='/'>🏠 Към начална страница</a>", "text/html");
        }
        catch (Exception ex)
        {
            return Content($"<h2>❌ Грешка при синхронизация</h2>" +
                          $"<p>{ex.Message}</p>" +
                          $"<p>Детайли: {ex.InnerException?.Message}</p>", "text/html");
        }
    }

    // Проверка на текущия потребител
    public async Task<IActionResult> CheckCurrentUser()
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Content("<h2>❌ Няма логнат потребител</h2>" +
                          "<a href='/Identity/Account/Login'>🔐 Вход</a>", "text/html");
        }

        var userRoles = await _userManager.GetRolesAsync(currentUser);

        string result = $"<h2>👤 Проверка на текущия потребител</h2>";
        result += $"<p><strong>Потребител:</strong> {currentUser.FirstName} {currentUser.LastName}</p>";
        result += $"<p><strong>Email:</strong> {currentUser.Email}</p>";
        result += $"<p><strong>UserType:</strong> {currentUser.UserType}</p>";
        result += $"<p><strong>Роли в базата:</strong> {string.Join(", ", userRoles)}</p>";

        // Проверка за конкретни роли
        result += $"<h3>🔍 Проверка на права:</h3>";
        result += $"<p>User.IsInRole('Admin'): {User.IsInRole("Admin")}</p>";
        result += $"<p>User.IsInRole('Teacher'): {User.IsInRole("Teacher")}</p>";
        result += $"<p>User.IsInRole('Student'): {User.IsInRole("Student")}</p>";

        result += $"<h3>✅ Достъпни страници:</h3>";
        if (currentUser.UserType == UserType.Admin || currentUser.UserType == UserType.Teacher)
        {
            result += $"<p><a href='/Teacher'>👨‍🏫 Учителски панел</a></p>";
            result += $"<p><a href='/Tests/Create'>➕ Създай тест</a></p>";
            result += $"<p><a href='/Tests/Manage'>📝 Управление на тестове</a></p>";
        }

        if (currentUser.UserType == UserType.Admin)
        {
            result += $"<p><a href='/Admin'>⚙️ Административен панел</a></p>";
        }

        return Content(result + "<br><a href='/'>🏠 Към начална страница</a>", "text/html");
    }

    // Оправяне на ролите за потребителите
    public async Task<IActionResult> FixUserRoles()
    {
        var adminUser = await _userManager.FindByEmailAsync("admin@abv.bg");
        var teacherUser = await _userManager.FindByEmailAsync("teacher@abv.bg");

        var results = new List<string>();

        if (adminUser != null)
        {
            // Премахни старите роли и добави Admin роля
            var currentRoles = await _userManager.GetRolesAsync(adminUser);
            await _userManager.RemoveFromRolesAsync(adminUser, currentRoles);
            await _userManager.AddToRoleAsync(adminUser, "Admin");

            // Актуализирай UserType
            adminUser.UserType = UserType.Admin;
            await _userManager.UpdateAsync(adminUser);

            results.Add("✅ Администраторът вече има Admin роля и UserType = Admin");
        }

        if (teacherUser != null)
        {
            // Премахни старите роли и добави Teacher роля
            var currentRoles = await _userManager.GetRolesAsync(teacherUser);
            await _userManager.RemoveFromRolesAsync(teacherUser, currentRoles);
            await _userManager.AddToRoleAsync(teacherUser, "Teacher");

            // Актуализирай UserType
            teacherUser.UserType = UserType.Teacher;
            await _userManager.UpdateAsync(teacherUser);

            results.Add("✅ Учителят вече има Teacher роля и UserType = Teacher");
        }

        return Content($"<h2>🔧 Ролите са оправени</h2>" +
                      string.Join("<br>", results) +
                      "<br><br><a href='/Sync/CheckCurrentUser'>🔄 Провери отново</a>" +
                      "<br><a href='/'>🏠 Към начална страница</a>", "text/html");
    }

    // Проверка на базата данни
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            var bookCount = await _context.Books.CountAsync();
            var authorCount = await _context.Authors.CountAsync();
            var userCount = await _userManager.Users.CountAsync();

            string result = $"<h2>📊 Проверка на базата данни</h2>";
            result += $"<p>📚 Книги в базата: <strong>{bookCount}</strong></p>";
            result += $"<p>👨‍🎓 Автори в базата: <strong>{authorCount}</strong></p>";
            result += $"<p>👥 Потребители в базата: <strong>{userCount}</strong></p>";

            if (bookCount > 0)
            {
                result += "<h3>📖 Списък на книгите:</h3>";
                var books = await _context.Books.ToListAsync();
                var authors = await _context.Authors.ToListAsync();

                foreach (var book in books)
                {
                    var author = authors.FirstOrDefault(a => a.Id == book.AuthorId);
                    result += $"<p>- {book.Title} от {author?.Name ?? "Непознат автор"} (Жанр: {book.Genre}, {book.StudyYear} клас)</p>";
                }
            }
            else
            {
                result += "<div style='background: #ffebee; padding: 15px; border-radius: 5px;'>";
                result += "<h3>❌ НЯМА КНИГИ В БАЗАТА!</h3>";
                result += "<p><a href='/Sync/SyncAll' style='color: blue;'>👉 Кликни тук за да добавиш книги ръчно</a></p>";
                result += "</div>";
            }

            return Content(result + "<br><a href='/'>🏠 Към начална страница</a>", "text/html");
        }
        catch (Exception ex)
        {
            return Content($"<h2>❌ Грешка при достъп до базата:</h2>" +
                          $"<p>{ex.Message}</p>" +
                          $"<p>Детайли: {ex.InnerException?.Message}</p>" +
                          "<br><a href='/'>🏠 Към начална страница</a>", "text/html");
        }
    }

    // Добавяне само на тестови книги (без потребители)
    public async Task<IActionResult> AddBooksOnly()
    {
        try
        {
            if (await _context.Books.AnyAsync())
            {
                return Content("<h2>ℹ️ Вече има книги в базата</h2>" +
                              "<p><a href='/Books'>📚 Отиди в библиотеката</a></p>" +
                              "<a href='/'>🏠 Към начална страница</a>", "text/html");
            }

            var authors = new List<Author>
            {
                new Author { Name = "Иван Вазов", Biography = "Български поет и писател" },
                new Author { Name = "Алеко Константинов", Biography = "Български писател" }
            };

            _context.Authors.AddRange(authors);
            await _context.SaveChangesAsync();

            var authorVazov = authors[0];
            var authorAleko = authors[1];

            var books = new List<Book>
            {
                new Book {
                    Title = "Под игото",
                    AuthorId = authorVazov.Id,
                    Description = "Роман за Априлското въстание",
                    Genre = "Роман",
                    StudyYear = 10,
                    IsForMatura = true,
                    WritingPeriod = "Възраждане",
                    PublishedYear = 1894
                },
                new Book {
                    Title = "Бай Ганьо",
                    AuthorId = authorAleko.Id,
                    Description = "Сатирични разкази за българския характер",
                    Genre = "Сатира",
                    StudyYear = 9,
                    IsForMatura = true,
                    WritingPeriod = "Възраждане",
                    PublishedYear = 1895
                }
            };

            _context.Books.AddRange(books);
            await _context.SaveChangesAsync();

            return Content("<h2>✅ Книгите са добавени успешно!</h2>" +
                          "<p>Добавени са 2 произведения:</p>" +
                          "<ul>" +
                          "<li>Под игото - Иван Вазов</li>" +
                          "<li>Бай Ганьо - Алеко Константинов</li>" +
                          "</ul>" +
                          "<p><a href='/Books'>📚 Отиди в библиотеката</a></p>" +
                          "<a href='/'>🏠 Към начална страница</a>", "text/html");
        }
        catch (Exception ex)
        {
            return Content($"<h2>❌ Грешка:</h2>" +
                          $"<p>{ex.Message}</p>" +
                          $"<p>Детайли: {ex.InnerException?.Message}</p>" +
                          "<br><a href='/'>🏠 Към начална страница</a>", "text/html");
        }
    }
}