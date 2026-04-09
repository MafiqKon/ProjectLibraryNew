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
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    Console.WriteLine("USER IS NULL!");
                    return NotFound();
                }

                Console.WriteLine($"User found: {user.FirstName} {user.LastName}");

                // =========================================================
                // ЛОГИКА ЗА ИЗЧИСЛЯВАНЕ НА ГОТОВНОСТТА ЗА МАТУРА (СИНХРОН)
                // =========================================================
                var userProgresses = await _context.UserBookProgresses
                    .Where(p => p.UserId == user.Id)
                    .ToListAsync();

                var progressDict = new Dictionary<int, int>();
                foreach (var p in userProgresses)
                {
                    int score = 0;
                    if (p.IsTextRead) score += 30;
                    if (p.IsAnalysisRead) score += 30;
                    if (p.HasPassedTest) score += 40;
                    progressDict[p.BookId] = score;
                }

                var allMaturaBooks = await _context.Books.Where(b => b.IsForMatura).Select(b => b.Id).ToListAsync();
                if (allMaturaBooks.Any())
                {
                    int totalScore = 0;
                    foreach (var id in allMaturaBooks)
                    {
                        if (progressDict.ContainsKey(id)) totalScore += progressDict[id];
                    }
                    ViewBag.OverallMaturaProgress = totalScore / allMaturaBooks.Count;
                }
                else
                {
                    ViewBag.OverallMaturaProgress = 0;
                }
                // =========================================================

                // Статистика за потребителя (твоят оригинален код)
                var userStats = new UserStatistics
                {
                    TotalBooksInCollections = await _context.BookCollections
                        .Where(bc => bc.UserId == user.Id)
                        .SelectMany(bc => bc.Books)
                        .CountAsync(),
                    TotalCollections = await _context.BookCollections
                        .Where(bc => bc.UserId == user.Id)
                        .CountAsync(),
                    BooksForMatura = await _context.BookCollections
                        .Where(bc => bc.UserId == user.Id)
                        .SelectMany(bc => bc.Books)
                        .CountAsync(b => b.IsForMatura),
                    MemberSince = user.RegistrationDate,
                    LastActivity = DateTime.Now
                };

                var model = new ProfileViewModel
                {
                    User = user,
                    Statistics = userStats
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Profile: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                throw;
            }
        }
    }
}