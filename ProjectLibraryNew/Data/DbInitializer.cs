using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectLibrary.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Уверяваме се, че базата е създадена
            context.Database.EnsureCreated();

            await CreateRoles(roleManager);
            await CreateUsers(userManager);
            await CreateBadges(context);
            await CreateBooksAndAuthors(context); // Тук е новият голям списък!
        }

        private static async Task CreateBadges(ApplicationDbContext context)
        {
            if (context.Badges.Any()) return;

            var badges = new List<Badge>
            {
                new Badge { Name = "Първи стъпки", Description = "Написахте първия си коментар!", IconUrl = "🥉" },
                new Badge { Name = "Дискусионен лидер", Description = "Написахте 5 коментара", IconUrl = "🥈" },
                new Badge { Name = "Майстор на словото", Description = "Написахте 10 коментара", IconUrl = "🥇" },
                new Badge { Name = "Книжен плъх", Description = "Добавихте 3 книги в колекция", IconUrl = "📚" },
                new Badge { Name = "Отличник", Description = "Изкарахте пълен отличен на тест", IconUrl = "🏆" }
            };
            context.Badges.AddRange(badges);
            await context.SaveChangesAsync();
        }

        private static async Task CreateBooksAndAuthors(ApplicationDbContext context)
        {
            if (!context.Books.Any())
            {
                // Списък с всички автори от програмата
                var authorNames = new[]
                {
                    "Димитър Талев", "Алеко Константинов", "Станислав Стратиев", "Иван Вазов",
                    "Никола Вапцаров", "Йордан Радичков", "Христо Ботев", "Елин Пелин",
                    "Христо Смирненски", "Емилиян Станев", "Пейо Яворов", "Пенчо Славейков",
                    "Димчо Дебелянов", "Христо Фотев", "Петя Дубарова", "Атанас Далчев",
                    "Йордан Йовков", "Виктор Пасков", "Елисавета Багряна", "Борис Христов"
                };

                var authorsDict = new Dictionary<string, Author>();

                // Създаваме авторите
                foreach (var name in authorNames)
                {
                    // Проверяваме дали вече не съществува в базата
                    var existingAuthor = await context.Authors.FirstOrDefaultAsync(a => a.Name == name);
                    if (existingAuthor == null)
                    {
                        var author = new Author { Name = name, Biography = "Български автор (Класика)" };
                        context.Authors.Add(author);
                        authorsDict.Add(name, author);
                    }
                    else
                    {
                        authorsDict.Add(name, existingAuthor);
                    }
                }
                await context.SaveChangesAsync();

                // Създаваме книгите
                var books = new List<Book>
                {
                    // === 11 КЛАС ===
                    new Book { Title = "Железният светилник", AuthorId = authorsDict["Димитър Талев"].Id, StudyYear = 11, IsForMatura = true, Tags = "Родното и чуждото", Genre = "Роман", Description = "Роман от темата Родното и чуждото" },
                    new Book { Title = "Бай Ганьо журналист", AuthorId = authorsDict["Алеко Константинов"].Id, StudyYear = 11, IsForMatura = true, Tags = "Родното и чуждото", Genre = "Фейлетон", Description = "Откъс от книгата Бай Ганьо" },
                    new Book { Title = "Балкански синдром", AuthorId = authorsDict["Станислав Стратиев"].Id, StudyYear = 11, IsForMatura = true, Tags = "Родното и чуждото", Genre = "Комедия", Description = "Сатирична пиеса" },
                    new Book { Title = "Паисий", AuthorId = authorsDict["Иван Вазов"].Id, StudyYear = 11, IsForMatura = true, Tags = "Миналото и паметта", Genre = "Ода", Description = "От цикъла Епопея на забравените" },
                    new Book { Title = "История", AuthorId = authorsDict["Никола Вапцаров"].Id, StudyYear = 11, IsForMatura = true, Tags = "Миналото и паметта", Genre = "Стихотворение", Description = "Стихотворение за обикновения човек" },
                    new Book { Title = "Ноев ковчег", AuthorId = authorsDict["Йордан Радичков"].Id, StudyYear = 11, IsForMatura = true, Tags = "Миналото и паметта", Genre = "Роман", Description = "Фрагментарен роман" },
                    new Book { Title = "Борба", AuthorId = authorsDict["Христо Ботев"].Id, StudyYear = 11, IsForMatura = true, Tags = "Обществото и властта", Genre = "Стихотворение", Description = "Стихотворение за свободата" },
                    new Book { Title = "Андрешко", AuthorId = authorsDict["Елин Пелин"].Id, StudyYear = 11, IsForMatura = true, Tags = "Обществото и властта", Genre = "Разказ", Description = "Социален разказ" },
                    new Book { Title = "Приказка за стълбата", AuthorId = authorsDict["Христо Смирненски"].Id, StudyYear = 11, IsForMatura = true, Tags = "Обществото и властта", Genre = "Сатира", Description = "Сатирична приказка" },
                    new Book { Title = "До моето първо либе", AuthorId = authorsDict["Христо Ботев"].Id, StudyYear = 11, IsForMatura = true, Tags = "Животът и смъртта", Genre = "Стихотворение", Description = "Стихотворение за саможертвата" },
                    new Book { Title = "Новото гробище над Сливница", AuthorId = authorsDict["Иван Вазов"].Id, StudyYear = 11, IsForMatura = true, Tags = "Животът и смъртта", Genre = "Стихотворение", Description = "Стихотворение за падналите в бой" },
                    new Book { Title = "Крадецът на праскови", AuthorId = authorsDict["Емилиян Станев"].Id, StudyYear = 11, IsForMatura = true, Tags = "Животът и смъртта", Genre = "Повест", Description = "Повест за любовта и войната" },
                    new Book { Title = "При Рилския манастир", AuthorId = authorsDict["Иван Вазов"].Id, StudyYear = 11, IsForMatura = true, Tags = "Природата", Genre = "Стихотворение", Description = "Възхвала на българската природа" },
                    new Book { Title = "Градушка", AuthorId = authorsDict["Пейо Яворов"].Id, StudyYear = 11, IsForMatura = true, Tags = "Природата", Genre = "Поема", Description = "Поема за селския труд и стихиите" },
                    new Book { Title = "Спи езерото", AuthorId = authorsDict["Пенчо Славейков"].Id, StudyYear = 11, IsForMatura = true, Tags = "Природата", Genre = "Стихотворение", Description = "Стихотворение от Сън за щастие" },

                    // === 12 КЛАС ===
                    new Book { Title = "Аз искам да те помня все така", AuthorId = authorsDict["Димчо Дебелянов"].Id, StudyYear = 12, IsForMatura = true, Tags = "Любовта", Genre = "Елегия", Description = "Елегия за раздялата" },
                    new Book { Title = "Колко си хубава", AuthorId = authorsDict["Христо Фотев"].Id, StudyYear = 12, IsForMatura = true, Tags = "Любовта", Genre = "Стихотворение", Description = "Стихотворение за красотата" },
                    new Book { Title = "Посвещение", AuthorId = authorsDict["Петя Дубарова"].Id, StudyYear = 12, IsForMatura = true, Tags = "Любовта", Genre = "Стихотворение", Description = "Стихотворение за младостта" },
                    new Book { Title = "Спасова могила", AuthorId = authorsDict["Елин Пелин"].Id, StudyYear = 12, IsForMatura = true, Tags = "Вярата и надеждата", Genre = "Разказ", Description = "Разказ за надеждата за спасение" },
                    new Book { Title = "Молитва", AuthorId = authorsDict["Атанас Далчев"].Id, StudyYear = 12, IsForMatura = true, Tags = "Вярата и надеждата", Genre = "Стихотворение", Description = "Стихотворение за връзката със света" },
                    new Book { Title = "Вяра", AuthorId = authorsDict["Никола Вапцаров"].Id, StudyYear = 12, IsForMatura = true, Tags = "Вярата и надеждата", Genre = "Стихотворение", Description = "Стихотворение за непобедимата вяра" },
                    new Book { Title = "Ветрената мелница", AuthorId = authorsDict["Елин Пелин"].Id, StudyYear = 12, IsForMatura = true, Tags = "Трудът и творчеството", Genre = "Разказ", Description = "Разказ за творческия дух" },
                    new Book { Title = "Песента на колелетата", AuthorId = authorsDict["Йордан Йовков"].Id, StudyYear = 12, IsForMatura = true, Tags = "Трудът и творчеството", Genre = "Разказ", Description = "Разказ за смисъла на труда" },
                    new Book { Title = "Балада за Георг Хених", AuthorId = authorsDict["Виктор Пасков"].Id, StudyYear = 12, IsForMatura = true, Tags = "Трудът и творчеството", Genre = "Роман", Description = "Роман за изкуството и бедността" },
                    new Book { Title = "Две души", AuthorId = authorsDict["Пейо Яворов"].Id, StudyYear = 12, IsForMatura = true, Tags = "Изборът и раздвоението", Genre = "Стихотворение", Description = "Стихотворение за вътрешния конфликт" },
                    new Book { Title = "Потомка", AuthorId = authorsDict["Елисавета Багряна"].Id, StudyYear = 12, IsForMatura = true, Tags = "Изборът и раздвоението", Genre = "Стихотворение", Description = "Стихотворение за свободния дух" },
                    new Book { Title = "Честен кръст", AuthorId = authorsDict["Борис Христов"].Id, StudyYear = 12, IsForMatura = true, Tags = "Изборът и раздвоението", Genre = "Стихотворение", Description = "Стихотворение за творческия избор" }
                };

                // ... тук свършва списъкът с книгите ...

                // === ДОБАВИ ТОЗИ ЦИКЪЛ ===
                // Той минава през всички 27 книги и им слага задължителните липсващи полета
                foreach (var b in books)
                {
                    b.PublishedYear = 1900; // Примерна година, за да е доволна базата
                    b.WritingPeriod = "Българска литература"; // Примерна епоха
                }
                // =========================

                context.Books.AddRange(books);
                await context.SaveChangesAsync();
            }

        }


        private static async Task CreateRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "Teacher", "Student" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task CreateUsers(UserManager<ApplicationUser> userManager)
        {
            string defaultAvatar = "https://cdn-icons-png.flaticon.com/512/149/149071.png";

            if (await userManager.FindByEmailAsync("admin@abv.bg") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@abv.bg",
                    Email = "admin@abv.bg",
                    FirstName = "Администратор",
                    LastName = "Системен",
                    UserType = UserType.Admin,
                    EmailConfirmed = true,
                    ProfilePictureUrl = defaultAvatar
                };
                await userManager.CreateAsync(admin, "123456");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            if (await userManager.FindByEmailAsync("teacher@abv.bg") == null)
            {
                var teacher = new ApplicationUser
                {
                    UserName = "teacher@abv.bg",
                    Email = "teacher@abv.bg",
                    FirstName = "Госпожа",
                    LastName = "Учителка",
                    UserType = UserType.Teacher,
                    EmailConfirmed = true,
                    ProfilePictureUrl = defaultAvatar
                };
                await userManager.CreateAsync(teacher, "123456");
                await userManager.AddToRoleAsync(teacher, "Teacher");
            }

            if (await userManager.FindByEmailAsync("student@abv.bg") == null)
            {
                var student1 = new ApplicationUser
                {
                    UserName = "student@abv.bg",
                    Email = "student@abv.bg",
                    FirstName = "Иван",
                    LastName = "Студентов",
                    UserType = UserType.Student,
                    EmailConfirmed = true,
                    ProfilePictureUrl = defaultAvatar
                };
                await userManager.CreateAsync(student1, "123456");
                await userManager.AddToRoleAsync(student1, "Student");
            }

            if (await userManager.FindByEmailAsync("student2@abv.bg") == null)
            {
                var student2 = new ApplicationUser
                {
                    UserName = "student2@abv.bg",
                    Email = "student2@abv.bg",
                    FirstName = "Мария",
                    LastName = "Ученичка",
                    UserType = UserType.Student,
                    EmailConfirmed = true,
                    ProfilePictureUrl = defaultAvatar
                };
                await userManager.CreateAsync(student2, "123456");
                await userManager.AddToRoleAsync(student2, "Student");
            }

            if (await userManager.FindByEmailAsync("student3@abv.bg") == null)
            {
                var student3 = new ApplicationUser
                {
                    UserName = "student3@abv.bg",
                    Email = "student3@abv.bg",
                    FirstName = "Петър",
                    LastName = "Петров",
                    UserType = UserType.Student,
                    EmailConfirmed = true,
                    ProfilePictureUrl = defaultAvatar
                };
                await userManager.CreateAsync(student3, "Student3!");
                await userManager.AddToRoleAsync(student3, "Student");
            }

            if (await userManager.FindByEmailAsync("student4@abv.bg") == null)
            {
                var student4 = new ApplicationUser
                {
                    UserName = "student4@abv.bg",
                    Email = "student4@abv.bg",
                    FirstName = "Елена",
                    LastName = "Иванова",
                    UserType = UserType.Student,
                    EmailConfirmed = true,
                    ProfilePictureUrl = defaultAvatar
                };
                await userManager.CreateAsync(student4, "Student4!");
                await userManager.AddToRoleAsync(student4, "Student");
            }

            if (await userManager.FindByEmailAsync("student5@abv.bg") == null)
            {
                var student5 = new ApplicationUser
                {
                    UserName = "student5@abv.bg",
                    Email = "student5@abv.bg",
                    FirstName = "Георги",
                    LastName = "Георгиев",
                    UserType = UserType.Student,
                    EmailConfirmed = true,
                    ProfilePictureUrl = defaultAvatar
                };
                await userManager.CreateAsync(student5, "Student5!");
                await userManager.AddToRoleAsync(student5, "Student");
            }
        }
    }
}