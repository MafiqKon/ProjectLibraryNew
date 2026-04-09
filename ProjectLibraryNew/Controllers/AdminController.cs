using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.Models;
using ProjectLibrary.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectLibrary.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Административен панел
        public async Task<IActionResult> Index()
        {
            var stats = new AdminStatistics
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalBooks = await _context.Books.CountAsync(),
                TotalCollections = await _context.BookCollections.CountAsync(),
                TotalComments = await _context.Comments.CountAsync(),
                TotalTests = await _context.Tests.CountAsync(),
                RecentRegistrations = await _userManager.Users
                    .OrderByDescending(u => u.RegistrationDate)
                    .Take(10)
                    .ToListAsync()
            };

            return View(stats);
        }

        // GET: Управление на потребители (С ТЪРСАЧКА)
        public async Task<IActionResult> Users(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            // 👑 ПРОВЕРКА: Текущият потребител Суперадмин ли е?
            ViewBag.IsSuperAdmin = User.Identity.Name == "admin@abv.bg";

            var usersQuery = _userManager.Users.AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                usersQuery = usersQuery.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(lowerSearch)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(lowerSearch)) ||
                    (u.Email != null && u.Email.ToLower().Contains(lowerSearch)));
            }

            var users = await usersQuery
                .OrderBy(u => u.UserType)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var userViewModels = new List<AdminUserViewModel>();
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                userViewModels.Add(new AdminUserViewModel
                {
                    User = user,
                    Roles = userRoles.ToList(),
                    CollectionsCount = await _context.BookCollections.CountAsync(c => c.UserId == user.Id),
                    BooksCount = await _context.BookCollections
                        .Where(c => c.UserId == user.Id)
                        .SelectMany(c => c.Books)
                        .CountAsync(),
                    CommentsCount = await _context.Comments.CountAsync(c => c.UserId == user.Id)
                });
            }

            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(userViewModels);
        }

        // GET: Управление на произведения (Books)
        public async Task<IActionResult> Books(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var works = from b in _context.Books
                        .Include(b => b.Author)
                        .Include(b => b.Analyses)
                        .Include(b => b.Tests)
                        select b;

            if (!String.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                works = works.Where(b => b.Title.ToLower().Contains(lowerSearch)
                                      || b.Author.Name.ToLower().Contains(lowerSearch));
            }

            return View(await works.OrderBy(b => b.Title).ToListAsync());
        }

        // GET: Управление на автори
        public async Task<IActionResult> Authors(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var authors = from a in _context.Authors
                          .Include(a => a.Books)
                          select a;

            if (!String.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                authors = authors.Where(a => a.Name.ToLower().Contains(lowerSearch));
            }

            return View(await authors.OrderBy(a => a.Name).ToListAsync());
        }

        // GET: Управление на коментари
        public async Task<IActionResult> Comments()
        {
            var comments = await _context.Comments
                .Include(c => c.Book)
                .Include(c => c.Book.Author)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            return View(comments);
        }

        // POST: Изтриване на коментар
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Коментарът беше изтрит успешно!";
            }
            else
            {
                TempData["ErrorMessage"] = "Коментарът не беше намерен!";
            }

            return RedirectToAction(nameof(Comments));
        }

        // POST: Промяна на роля на потребител
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
        {
            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser != null)
            {
                // 1. ЗАЩИТА: Никой не може да пипа Суперадмина!
                if (targetUser.UserName == "admin@abv.bg")
                {
                    TempData["ErrorMessage"] = "Правата на Главния Суперадминистратор не могат да бъдат променяни!";
                    return RedirectToAction(nameof(Users));
                }

                // Взимаме данните на човека, който цъка бутона в момента
                var currentLoggedInUser = await _userManager.GetUserAsync(User);
                bool isSuperAdmin = currentLoggedInUser.UserName == "admin@abv.bg";

                var targetUserRoles = await _userManager.GetRolesAsync(targetUser);

                // 2. ЗАЩИТА: Обикновен админ опитва да понижи друг Админ
                if (targetUserRoles.Contains("Admin") && !isSuperAdmin)
                {
                    TempData["ErrorMessage"] = "Само Суперадминистраторът може да променя правата на други администратори!";
                    return RedirectToAction(nameof(Users));
                }

                // 3. ЗАЩИТА: Обикновен админ опитва да повиши някого в Админ
                if (newRole == "Admin" && !isSuperAdmin)
                {
                    TempData["ErrorMessage"] = "Само Суперадминистраторът може да назначава нови администратори!";
                    return RedirectToAction(nameof(Users));
                }

                // Премахване на всички текущи роли
                var currentRoles = await _userManager.GetRolesAsync(targetUser);
                await _userManager.RemoveFromRolesAsync(targetUser, currentRoles);

                // Добавяне на новата роля
                await _userManager.AddToRoleAsync(targetUser, newRole);

                // Актуализиране на UserType
                targetUser.UserType = newRole switch
                {
                    "Admin" => UserType.Admin,
                    "Teacher" => UserType.Teacher,
                    "Student" => UserType.Student,
                    _ => targetUser.UserType
                };

                await _userManager.UpdateAsync(targetUser);
                TempData["SuccessMessage"] = $"Ролята на потребителя е променена на {newRole} успешно!";
            }
            else
            {
                TempData["ErrorMessage"] = "Потребителят не беше намерен!";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: Изтриване на потребител
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var targetUser = await _userManager.FindByIdAsync(userId);

            if (targetUser != null)
            {
                // 1. ЗАЩИТА: Никой не може да изтрие Суперадмина!
                if (targetUser.UserName == "admin@abv.bg")
                {
                    TempData["ErrorMessage"] = "Главният Суперадминистратор не може да бъде изтрит!";
                    return RedirectToAction(nameof(Users));
                }

                // Взимаме данните на човека, който цъка бутона
                var currentLoggedInUser = await _userManager.GetUserAsync(User);
                bool isSuperAdmin = currentLoggedInUser.UserName == "admin@abv.bg";

                var targetUserRoles = await _userManager.GetRolesAsync(targetUser);

                // 2. ЗАЩИТА: Обикновен админ опитва да изтрие друг Админ
                if (targetUserRoles.Contains("Admin") && !isSuperAdmin)
                {
                    TempData["ErrorMessage"] = "Само Суперадминистраторът може да изтрива други администратори!";
                    return RedirectToAction(nameof(Users));
                }

                // Изтриване на колекциите на потребителя
                var userCollections = await _context.BookCollections
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                _context.BookCollections.RemoveRange(userCollections);

                // Изтриване на коментарите на потребителя
                var userComments = await _context.Comments
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                _context.Comments.RemoveRange(userComments);

                await _context.SaveChangesAsync();

                // Изтриване на потребителя
                var result = await _userManager.DeleteAsync(targetUser);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Потребителят беше изтрит успешно!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Грешка при изтриване на потребителя!";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Не може да изтриете този потребител!";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: Системна статистика
        public async Task<IActionResult> SystemStats()
        {
            var stats = new SystemStatistics
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalBooks = await _context.Books.CountAsync(),
                TotalCollections = await _context.BookCollections.CountAsync(),
                TotalComments = await _context.Comments.CountAsync(),
                TotalTests = await _context.Tests.CountAsync(),
                TotalAuthors = await _context.Authors.CountAsync(),
                TotalAnalyses = await _context.Analyses.CountAsync(),

                UsersByType = await _userManager.Users
                    .GroupBy(u => u.UserType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type.ToString(), x => x.Count),

                RecentActivity = await _context.BookCollections
                    .OrderByDescending(c => c.CreatedDate)
                    .Take(10)
                    .Select(c => new {
                        Name = c.Name,
                        CreatedDate = c.CreatedDate,
                        UserName = c.User.FirstName + " " + c.User.LastName
                    })
                    .ToListAsync()
            };

            return View(stats);
        }

        public async Task<IActionResult> ManageComments()
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Book)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            return View(comments);
        }

        // =========================================================================
        // АВТОМАТИЧНО ГЕНЕРИРАНЕ НА ТЕСТОВЕ (С ПРЕЗАПИСВАНЕ НА СТАРИТЕ)
        // =========================================================================
        // GET: Admin/SeedAutomatedTests
        public async Task<IActionResult> SeedAutomatedTests()
        {
            // === 0. МАХАМЕ СТАРИТЕ ТЕСТОВЕ (Ако ги има) ===
            // Търсим старите тестове и техните резултати, за да ги изтрием чисто
            var oldTests = await _context.Tests
                .Include(t => t.TestResults)
                .Where(t => t.Title.Contains("Тренировка: Железният светилник") ||
                            t.Title.Contains("Бърз тест: Бай Ганьо журналист") ||
                            t.Title.Contains("Официален тест: Андрешко"))
                .ToListAsync();

            if (oldTests.Any())
            {
                foreach (var t in oldTests)
                {
                    _context.TestResults.RemoveRange(t.TestResults); // Трием резултатите първо
                }
                _context.Tests.RemoveRange(oldTests); // Трием самите тестове
                await _context.SaveChangesAsync();
            }
            // ===============================================

            // 1. Опитваме се да намерим книгите в базата (търсим по съдържание на заглавието)
            var zhelezniyat = await _context.Books.FirstOrDefaultAsync(b => b.Title.Contains("Железният светилник"));
            var baiGanio = await _context.Books.FirstOrDefaultAsync(b => b.Title.Contains("Бай Ганьо"));
            var andreshko = await _context.Books.FirstOrDefaultAsync(b => b.Title.Contains("Андрешко"));

            // Взимаме текущия потребител като създател
            var user = await _userManager.GetUserAsync(User);
            string userId = user?.Id;

            int testsAdded = 0;

            // ТЕСТ 1: Железният светилник (13 затворени, 3 отворени)
            if (zhelezniyat != null)
            {
                var test1 = new Test
                {
                    Title = "Тренировка: Железният светилник",
                    Description = "Бърза тренировка върху романа 'Железният светилник' от Димитър Талев.",
                    BookId = zhelezniyat.Id,
                    IsPublished = true,
                    CreatedByUserId = userId,
                    TimeLimitMinutes = 30,
                    TotalPoints = 28, // 13*1 + 3*5
                    PassingScore = 14
                };
                test1.SetQuestions(GenerateQuestions("Тренировка: Железният светилник", 13, 3));
                _context.Tests.Add(test1);
                testsAdded++;
            }

            // ТЕСТ 2: Бай Ганьо журналист (13 затворени, 3 отворени)
            if (baiGanio != null)
            {
                var test2 = new Test
                {
                    Title = "Бърз тест: Бай Ганьо журналист",
                    Description = "Провери знанията си за фейлетона на Алеко Константинов.",
                    BookId = baiGanio.Id,
                    IsPublished = true,
                    CreatedByUserId = userId,
                    TimeLimitMinutes = 30,
                    TotalPoints = 28,
                    PassingScore = 14
                };
                test2.SetQuestions(GenerateQuestions("Бърз тест: Бай Ганьо журналист", 13, 3));
                _context.Tests.Add(test2);
                testsAdded++;
            }

            // ТЕСТ 3: Андрешко (20 затворени, 5 отворени)
            if (andreshko != null)
            {
                var test3 = new Test
                {
                    Title = "Официален тест: Андрешко (Матура)",
                    Description = "Официален изпитен формат за разказа 'Андрешко' от Елин Пелин.",
                    BookId = andreshko.Id,
                    IsPublished = true,
                    CreatedByUserId = userId,
                    TimeLimitMinutes = 60,
                    TotalPoints = 45, // 20*1 + 5*5
                    PassingScore = 25
                };
                test3.SetQuestions(GenerateQuestions("Официален тест: Андрешко (Матура)", 20, 5));
                _context.Tests.Add(test3);
                testsAdded++;
            }

            if (testsAdded > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Успешно презаписани {testsAdded} автоматични теста с реалистични въпроси!";
            }
            else
            {
                TempData["ErrorMessage"] = "Книгите не са намерени. Уверете се, че имате произведения, съдържащи 'Железният светилник', 'Бай Ганьо' и 'Андрешко'.";
            }

            return RedirectToAction("Index", "Admin");
        }

        // Помощна функция за генериране на РЕАЛИСТИЧНИ въпроси
        private List<TestQuestion> GenerateQuestions(string testName, int closedCount, int openCount)
        {
            var questions = new List<TestQuestion>();
            int idCounter = 1;

            // ЗАТВОРЕНИ ВЪПРОСИ
            for (int i = 1; i <= closedCount; i++)
            {
                questions.Add(new TestQuestion
                {
                    Id = idCounter++,
                    QuestionText = $"Кой от следните мотиви е най-силно застъпен в глава {i} на произведението?",
                    Type = QuestionType.MultipleChoice,
                    Points = 1,
                    // Задаваме отговор 2 (Б) да е верен в системата, но вече НЕ го пише в текста!
                    CorrectOptionId = 2,
                    Options = new List<QuestionOption>
                    {
                        new QuestionOption { Id = 1, Text = "Стремежът към лична свобода и бунт срещу нормите" },
                        new QuestionOption { Id = 2, Text = "Сблъсъкът между патриархалното и модерното време" },
                        new QuestionOption { Id = 3, Text = "Трагичната и неосъществена любов" },
                        new QuestionOption { Id = 4, Text = "Примирението с тежката съдба и робството" }
                    }
                });
            }

            // ОТВОРЕНИ ВЪПРОСИ
            for (int i = 1; i <= openCount; i++)
            {
                questions.Add(new TestQuestion
                {
                    Id = idCounter++,
                    QuestionText = $"Отворен въпрос {i}: Анализирайте накратко поведението на героя в кулминационния момент. (За да мине теста, напишете думата 'свобода')",
                    Type = QuestionType.OpenEnded,
                    Points = 5,
                    AcceptableAnswers = new List<string> { "свобода", "бунт", "любов" },
                    IsCaseSensitive = false
                });
            }

            return questions;
        }

        // =========================================================================
        // ГЕНЕРАТОР НА ЗНАЧКИ ЗА СТЕНАТА НА СЛАВАТА
        // =========================================================================
        // GET: Admin/SeedBadges
        public async Task<IActionResult> SeedBadges()
        {
            // Списък с 12-те уникални значки и техните Lucide иконки
            var badgesToAdd = new List<Badge>
            {
                new Badge { Name = "Светкавица", Description = "Реши тест за рекордно кратко време с отличен резултат.", IconUrl = "zap" },
                new Badge { Name = "Безупречна серия", Description = "Три поредни теста без нито една грешка.", IconUrl = "flame" },
                new Badge { Name = "Маратонец", Description = "Реши 5 теста в един ден!", IconUrl = "activity" },
                new Badge { Name = "Завръщането", Description = "Значително подобри резултата си на един и същ тест.", IconUrl = "trending-up" },
                new Badge { Name = "Готов за матура", Description = "Справи се с най-тежките изпитни формати.", IconUrl = "graduation-cap" },
                new Badge { Name = "Изследовател", Description = "Излязъл си извън зоната си на комфорт и изследваш различни литературни жанрове.", IconUrl = "compass" },
                new Badge { Name = "Златно перо", Description = "Написа 5 коментара и активно участваш в дискусиите.", IconUrl = "pen-tool" },
                new Badge { Name = "Нощна птица", Description = "Когато другите спят, ти трупаш знания!", IconUrl = "moon" },
                new Badge { Name = "Литературен критик", Description = "Не четеш просто повърхностно, а се задълбочаваш в смисъла. Прочете 5 анализа.", IconUrl = "glasses" },
                new Badge { Name = "Ветеран", Description = "Постоянството е ключът към успеха. Посети библиотеката 7 дни подред.", IconUrl = "shield-check" },
                new Badge { Name = "Първооткривател", Description = "Смело стъпваш на нови територии. Пръв остави коментар на произведение!", IconUrl = "rocket" },
                new Badge { Name = "Фен на класиката", Description = "Истински ценител на творчеството на един автор.", IconUrl = "book-heart" }
            };

            int addedCount = 0;

            // Проверяваме за всяка значка дали вече я има в базата (по име)
            foreach (var b in badgesToAdd)
            {
                if (!await _context.Badges.AnyAsync(x => x.Name == b.Name))
                {
                    _context.Badges.Add(b);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Успешно добавени {addedCount} нови значки към Стената на славата!";
            }
            else
            {
                TempData["ErrorMessage"] = "Всички значки вече съществуват в базата данни. Няма нови за добавяне.";
            }

            return RedirectToAction("Index", "Admin");
        }
    }

    // МОДЕЛИ ЗА АДМИНИСТРАЦИЯТА
    public class AdminStatistics
    {
        public int TotalUsers { get; set; }
        public int TotalBooks { get; set; }
        public int TotalCollections { get; set; }
        public int TotalComments { get; set; }
        public int TotalTests { get; set; }
        public List<ApplicationUser> RecentRegistrations { get; set; } = new List<ApplicationUser>();
    }

    public class AdminUserViewModel
    {
        public ApplicationUser User { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int CollectionsCount { get; set; }
        public int BooksCount { get; set; }
        public int CommentsCount { get; set; }
    }

    public class SystemStatistics
    {
        public int TotalUsers { get; set; }
        public int TotalBooks { get; set; }
        public int TotalCollections { get; set; }
        public int TotalComments { get; set; }
        public int TotalTests { get; set; }
        public int TotalAuthors { get; set; }
        public int TotalAnalyses { get; set; }
        public Dictionary<string, int> UsersByType { get; set; } = new Dictionary<string, int>();
        public dynamic RecentActivity { get; set; }
    }
}