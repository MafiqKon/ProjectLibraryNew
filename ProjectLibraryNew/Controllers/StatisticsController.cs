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
    [Authorize]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StatisticsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var statistics = new UserStatistics
            {
                TotalCollections = await _context.BookCollections
                    .Where(c => c.UserId == user.Id)
                    .CountAsync(),

                TotalBooksInCollections = await _context.BookCollections
                    .Where(c => c.UserId == user.Id)
                    .SelectMany(c => c.Books)
                    .CountAsync(),

                BooksForMatura = await _context.BookCollections
                    .Where(c => c.UserId == user.Id)
                    .SelectMany(c => c.Books)
                    .CountAsync(b => b.IsForMatura),

                CommentsWritten = await _context.Comments
                    .Where(c => c.UserId == user.Id)
                    .CountAsync(),

                MemberSince = user.RegistrationDate,
                LastActivity = DateTime.Now
            };

            // Статистика по жанрове
            var genreStats = await _context.BookCollections
                .Where(c => c.UserId == user.Id)
                .SelectMany(c => c.Books)
                .GroupBy(b => b.Genre)
                .Select(g => new GenreStatistic
                {
                    Genre = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Статистика по периоди
            var periodStats = await _context.BookCollections
                .Where(c => c.UserId == user.Id)
                .SelectMany(c => c.Books)
                .GroupBy(b => b.WritingPeriod)
                .Select(g => new PeriodStatistic
                {
                    Period = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            ViewBag.GenreStats = genreStats;
            ViewBag.PeriodStats = periodStats;

            return View(statistics);
        }

        public async Task<IActionResult> Progress()
        {
            var user = await _userManager.GetUserAsync(User);

            // РЕАЛНИ ДАННИ - книги по учебни години
            var userBooksByGrade = await _context.BookCollections
                .Where(c => c.UserId == user.Id)
                .SelectMany(c => c.Books)
                .GroupBy(b => b.StudyYear)
                .Select(g => new
                {
                    Grade = g.Key,
                    Count = g.Count(),
                    Books = g.ToList()
                })
                .ToListAsync();

            // Дефиниране на общ брой книги по години (може да се вземе от базата)
            var gradeTotals = new Dictionary<int, int>
            {
                { 8, 12 },  // 8-ми клас: 12 произведения
                { 9, 15 },  // 9-ти клас: 15 произведения  
                { 10, 18 }, // 10-ти клас: 18 произведения
                { 11, 15 }, // 11-ти клас: 15 произведения
                { 12, 15 }  // 12-ти клас: 15 произведения
            };

            var progressData = new List<object>();
            var gradeDetails = new Dictionary<int, List<Book>>();

            foreach (var gradeTotal in gradeTotals)
            {
                var userBooks = userBooksByGrade.FirstOrDefault(g => g.Grade == gradeTotal.Key);
                var readCount = userBooks?.Count ?? 0;
                var percentage = gradeTotal.Value > 0 ? (readCount * 100) / gradeTotal.Value : 0;

                progressData.Add(new
                {
                    Grade = gradeTotal.Key,
                    Read = readCount,
                    Total = gradeTotal.Value,
                    Percentage = percentage,
                    Books = userBooks?.Books ?? new List<Book>()
                });

                if (userBooks != null)
                {
                    gradeDetails[gradeTotal.Key] = userBooks.Books;
                }
            }

            ViewBag.ProgressData = progressData;
            ViewBag.GradeDetails = gradeDetails;
            ViewBag.HasRealData = userBooksByGrade.Any(g => g.Count > 0);

            return View();
        }

        public async Task<IActionResult> Monthly()
        {
            var user = await _userManager.GetUserAsync(User);

            // Статистика за последните 6 месеца
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);

            var monthlyStats = await _context.BookCollections
                .Where(c => c.UserId == user.Id && c.CreatedDate >= sixMonthsAgo)
                .GroupBy(c => new { c.CreatedDate.Year, c.CreatedDate.Month })
                .Select(g => new
                {
                    Period = $"{g.Key.Month}/{g.Key.Year}",
                    CollectionsCreated = g.Count(),
                    BooksAdded = g.SelectMany(c => c.Books).Count()
                })
                .OrderBy(g => g.Period)
                .ToListAsync();

            ViewBag.MonthlyStats = monthlyStats;

            return View();
        }

        public async Task<IActionResult> Comparison()
        {
            var user = await _userManager.GetUserAsync(User);

            // Средна статистика на всички потребители
            var averageStats = new
            {
                AvgCollections = await _context.BookCollections
                    .GroupBy(c => c.UserId)
                    .Select(g => g.Count())
                    .DefaultIfEmpty(0)
                    .AverageAsync(),

                AvgBooks = await _context.BookCollections
                    .SelectMany(c => c.Books)
                    .GroupBy(b => b.Id)
                    .CountAsync() / Math.Max(await _context.Users.CountAsync(), 1),

                AvgComments = await _context.Comments
                    .GroupBy(c => c.UserId)
                    .Select(g => g.Count())
                    .DefaultIfEmpty(0)
                    .AverageAsync()
            };

            // Статистика на текущия потребител
            var userStats = new
            {
                Collections = await _context.BookCollections
                    .CountAsync(c => c.UserId == user.Id),

                Books = await _context.BookCollections
                    .Where(c => c.UserId == user.Id)
                    .SelectMany(c => c.Books)
                    .CountAsync(),

                Comments = await _context.Comments
                    .CountAsync(c => c.UserId == user.Id)
            };

            ViewBag.AverageStats = averageStats;
            ViewBag.UserStats = userStats;

            return View();
        }

        public async Task<IActionResult> MaturaProgress()
        {
            var user = await _userManager.GetUserAsync(User);

            // Всички книги за матура
            var allMaturaBooks = await _context.Books
                .Where(b => b.IsForMatura)
                .ToListAsync();

            // Книги за матура в колекциите на потребителя
            var userMaturaBooks = await _context.BookCollections
                .Where(c => c.UserId == user.Id)
                .SelectMany(c => c.Books)
                .Where(b => b.IsForMatura)
                .Select(b => b.Id)
                .ToListAsync();

            var maturaStats = new
            {
                TotalMaturaBooks = allMaturaBooks.Count,
                UserMaturaBooks = userMaturaBooks.Count,
                Percentage = allMaturaBooks.Count > 0 ?
                    (userMaturaBooks.Count * 100) / allMaturaBooks.Count : 0,
                MissingBooks = allMaturaBooks
                    .Where(b => !userMaturaBooks.Contains(b.Id))
                    .Take(10)
                    .ToList()
            };

            ViewBag.MaturaStats = maturaStats;

            return View();
        }
    }
}