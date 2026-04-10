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
    [Authorize(Roles = "Teacher,Admin")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Учителски панел - основна страница
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // Проверка дали потребителят е учител или администратор
            if (user.UserType != UserType.Teacher && user.UserType != UserType.Admin)
            {
                return Forbid();
            }

            // РЕАЛНИ ДАННИ - брой на студентите
            var totalStudents = await _context.Users
                .Where(u => u.UserType == UserType.Student)
                .CountAsync();

            // РЕАЛНИ ДАННИ - брой на тестовете
            var totalTests = await _context.Tests
                .CountAsync();

            // РЕАЛНИ ДАННИ - активни студенти (студенти с поне 1 колекция)
            var activeStudents = await _context.Users
                .Where(u => u.UserType == UserType.Student)
                .Where(u => _context.BookCollections.Any(c => c.UserId == u.Id))
                .CountAsync();

            // РЕАЛНИ ДАННИ - общ брой книги в системата
            var totalBooks = await _context.Books
                .CountAsync();

            // РЕАЛНИ ДАННИ - брой книги за матура
            var maturaBooks = await _context.Books
                .Where(b => b.IsForMatura)
                .CountAsync();

            // РЕАЛНИ ДАННИ - последни регистрирани студенти
            var recentStudentsData = await _context.Users
                .Where(u => u.UserType == UserType.Student)
                .OrderByDescending(u => u.RegistrationDate)
                .Take(5)
                .Select(u => new { u.FirstName, u.LastName, u.RegistrationDate })
                .ToListAsync();

            var teacherStats = new TeacherStatistics
            {
                TotalStudents = totalStudents,
                TotalTestsCreated = totalTests,
                AverageTestScore = await CalculateAverageTestScore(),
                ActiveStudents = activeStudents,
                TotalBooks = totalBooks,
                MaturaBooks = maturaBooks
            };

            ViewBag.TeacherStats = teacherStats;
            ViewBag.RecentStudents = recentStudentsData;
            ViewBag.HasRealData = totalStudents > 0;

            return View();
        }

        // GET: Текущи тестове (за учители)
        public async Task<IActionResult> CurrentTests()
        {
            var user = await _userManager.GetUserAsync(User);

            var currentTests = await _context.Tests
                .Include(t => t.Book)
                .Include(t => t.Book.Author)
                .Include(t => t.TestResults)
                .Where(t => t.CreatedByUserId == user.Id || User.IsInRole("Admin"))
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            // Статистика за всеки тест
            var testStats = currentTests.Select(t => new TeacherTestViewModel
            {
                Test = t,
                TotalAttempts = t.TestResults.Count,
                AverageScore = t.TestResults.Any() ? t.TestResults.Average(r => r.Score) : 0,
                PassRate = t.TestResults.Any() ?
                    (double)t.TestResults.Count(r => r.Score >= t.PassingScore) / t.TestResults.Count * 100 : 0
            }).ToList();

            return View(testStats);
        }

        // GET: Управление на студенти
        public async Task<IActionResult> Students()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.UserType != UserType.Teacher && user.UserType != UserType.Admin)
            {
                return Forbid();
            }

            // РЕАЛНИ ДАННИ - всички студенти с техните статистики
            var students = await _context.Users
                .Where(u => u.UserType == UserType.Student)
                .Select(s => new StudentViewModel
                {
                    Id = s.Id,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Email = s.Email,
                    RegistrationDate = s.RegistrationDate,
                    CollectionsCount = _context.BookCollections.Count(c => c.UserId == s.Id),
                    BooksCount = _context.BookCollections
                                .Where(c => c.UserId == s.Id)
                                .SelectMany(c => c.Books)
                                .Count(),
                    LastActivity = _context.BookCollections
                                .Where(c => c.UserId == s.Id)
                                .OrderByDescending(c => c.CreatedDate)
                                .Select(c => c.CreatedDate)
                                .FirstOrDefault()
                })
                .OrderByDescending(s => s.BooksCount)
                .ToListAsync();

            return View(students);
        }

        // GET: Статистика на класа
        public async Task<IActionResult> ClassStatistics()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.UserType != UserType.Teacher && user.UserType != UserType.Admin)
            {
                return Forbid();
            }

            // РЕАЛНИ ДАННИ - най-популярни книги сред студентите
            var popularBooks = await _context.BookCollections
                .Where(bc => bc.User.UserType == UserType.Student)
                .SelectMany(bc => bc.Books)
                .GroupBy(b => new { b.Id, b.Title, b.Author.Name })
                .Select(g => new BookStatistic
                {
                    BookId = g.Key.Id,
                    BookTitle = g.Key.Title,
                    AuthorName = g.Key.Name,
                    StudentCount = g.Count(),
                    Percentage = (g.Count() * 100) / Math.Max(_context.Users.Count(u => u.UserType == UserType.Student), 1)
                })
                .OrderByDescending(bs => bs.StudentCount)
                .Take(10)
                .ToListAsync();

            // РЕАЛНИ ДАННИ - разпределение по жанрове
            var genreDistribution = await _context.BookCollections
                .Where(bc => bc.User.UserType == UserType.Student)
                .SelectMany(bc => bc.Books)
                .GroupBy(b => b.Genre)
                .Select(g => new { Genre = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            // 1. Взимаме само суровите числа от базата данни
            var monthlyActivityRaw = await _context.BookCollections
                .Where(bc => bc.User.UserType == UserType.Student && bc.CreatedDate >= DateTime.Now.AddMonths(-6))
                .GroupBy(bc => new { bc.CreatedDate.Year, bc.CreatedDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Activity = g.Count()
                })
                .ToListAsync();

            // 2. Сглобяваме красивия текст в паметта на сървъра
            var monthlyActivity = monthlyActivityRaw
                .Select(g => new {
                    Period = $"{g.Month:D2}/{g.Year}",
                    Activity = g.Activity,
                    SortKey = g.Year * 100 + g.Month
                })
                .OrderBy(g => g.SortKey)
                .ToList();

            ViewBag.PopularBooks = popularBooks;
            ViewBag.GenreDistribution = genreDistribution;
            ViewBag.MonthlyActivity = monthlyActivity;

            return View();
        }

        // GET: Детайли за студент
        public async Task<IActionResult> StudentDetails(string id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.UserType != UserType.Teacher && user.UserType != UserType.Admin)
            {
                return Forbid();
            }

            var student = await _context.Users
                .Where(u => u.Id == id && u.UserType == UserType.Student)
                .Select(s => new StudentViewModel
                {
                    Id = s.Id,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Email = s.Email,
                    RegistrationDate = s.RegistrationDate,
                    CollectionsCount = _context.BookCollections.Count(c => c.UserId == s.Id),
                    BooksCount = _context.BookCollections
                                .Where(c => c.UserId == s.Id)
                                .SelectMany(c => c.Books)
                                .Count(),
                    LastActivity = _context.BookCollections
                                .Where(c => c.UserId == s.Id)
                                .OrderByDescending(c => c.CreatedDate)
                                .Select(c => c.CreatedDate)
                                .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (student == null)
            {
                return NotFound();
            }

            // РЕАЛНИ ДАННИ - книги на студента
            var studentBooks = await _context.BookCollections
                .Where(c => c.UserId == id)
                .SelectMany(c => c.Books)
                .Include(b => b.Author)
                .ToListAsync();

            // РЕАЛНИ ДАННИ - статистика по жанрове за студента
            var studentGenreStats = studentBooks
                .GroupBy(b => b.Genre)
                .Select(g => new { Genre = g.Key, Count = g.Count() })
                .ToList();

            // НОВО: Взимаме решените тестове на ученика!
            var studentTestResults = await _context.TestResults
                .Include(r => r.Test)
                .Where(r => r.UserId == id)
                .ToListAsync();

            ViewBag.StudentTestResults = studentTestResults;
            ViewBag.StudentBooks = studentBooks;
            ViewBag.StudentGenreStats = studentGenreStats;

            return View(student);
        }

        private async Task<int> CalculateAverageTestScore()
        {
            // Временно - ще се имплементира когато имаме реални тестове
            return 75;
        }
    }

    // МОДЕЛИ ЗА УЧИТЕЛСКИЯ ПАНЕЛ
    public class TeacherTestViewModel
    {
        public Test Test { get; set; }
        public int TotalAttempts { get; set; }
        public double AverageScore { get; set; }
        public double PassRate { get; set; }
    }

    public class TeacherStatistics
    {
        public int TotalStudents { get; set; }
        public int TotalTestsCreated { get; set; }
        public int AverageTestScore { get; set; }
        public int ActiveStudents { get; set; }
        public int TotalBooks { get; set; }
        public int MaturaBooks { get; set; }
    }

    public class StudentViewModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int CollectionsCount { get; set; }
        public int BooksCount { get; set; }
        public DateTime? LastActivity { get; set; }
    }

    public class BookStatistic
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public string AuthorName { get; set; }
        public int StudentCount { get; set; }
        public int Percentage { get; set; }
    }
}