using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.Data;
using ProjectLibrary.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace ProjectLibrary.Controllers
{
    [Authorize]
    public class ProfilesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfilesController(UserManager<ApplicationUser> userManager,
                                  ApplicationDbContext context,
                                  IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Profiles/MyProfile
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 1. Зареждаме потребителя със значките
            var fullUser = await _context.Users
                .Include(u => u.UserBadges).ThenInclude(ub => ub.Badge)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            // 2. ИЗЧИСЛЯВАНЕ НА ГОТОВНОСТ ЗА МАТУРА
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

            // 3. РЕАЛНИ СТАТИСТИКИ
            ViewBag.CollectionsCount = await _context.BookCollections.CountAsync(c => c.UserId == user.Id);
            ViewBag.TestsCount = await _context.TestResults.CountAsync(t => t.UserId == user.Id);
            ViewBag.BadgesCount = fullUser?.UserBadges?.Count ?? 0;

            ViewBag.BooksCount = await _context.BookCollections
                .Where(c => c.UserId == user.Id)
                .SelectMany(c => c.Books)
                .Distinct()
                .CountAsync();

            ViewBag.AllBooks = await _context.Books
                .OrderBy(b => b.Title)
                .ToListAsync();

            // ==========================================
            // НОВО: АЛГОРИТЪМ ЗА ПРЕПОРЪЧАНА КНИГА
            // ==========================================
            var interactedBookIds = userProgresses
                .Where(p => p.IsTextRead || p.IsAnalysisRead || p.HasPassedTest)
                .Select(p => p.BookId)
                .ToList();

            var recommendedBook = await _context.Books
                .Include(b => b.Author)
                .Where(b => !interactedBookIds.Contains(b.Id))
                .FirstOrDefaultAsync();

            ViewBag.RecommendedBook = recommendedBook;
            // ==========================================

            return View(fullUser);
        }

        // GET: Profiles/Edit
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Profiles/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string firstName, string lastName, IFormFile profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = firstName;
            user.LastName = lastName;

            if (profilePicture != null && profilePicture.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + profilePicture.FileName;
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }

                user.ProfilePictureUrl = "/images/profiles/" + uniqueFileName;
            }

            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Профилът е обновен успешно!";
            return RedirectToAction(nameof(MyProfile));
        }

        public async Task<IActionResult> Badges()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var allBadges = await _context.Badges.ToListAsync();
            var earnedBadgeIds = await _context.UserBadges
                .Where(ub => ub.UserId == user.Id)
                .Select(ub => ub.BadgeId)
                .ToListAsync();

            ViewBag.EarnedBadgeIds = earnedBadgeIds;
            ViewBag.EarnedCount = earnedBadgeIds.Count;
            ViewBag.TotalCount = allBadges.Count;

            return View(allBadges);
        }
        // ========================================================
        // МЕТОДИ ЗА КАЛЕНДАРА (API ENDPOINTS)
        // ========================================================

        // Взимане на всички събития за календара (GET)
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var events = await _context.StudyEvents
                .Where(e => e.UserId == user.Id)
                .Include(e => e.Book)
                .Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    start = e.EventDate.ToString("yyyy-MM-dd"), // Формат, който FullCalendar разбира
                    allDay = true, // Събитията ни засега са за целия ден
                    description = e.Description,
                    bookId = e.BookId,
                    bookTitle = e.Book != null ? e.Book.Title : null,
                    eventType = e.EventType,
                    isCompleted = e.IsCompleted,

                    // Цветова логика според типа
                    backgroundColor = e.EventType == "Reading" ? "#3b82f6" : // Синьо
                                      e.EventType == "Analysis" ? "#8b5cf6" : // Лилаво
                                      e.EventType == "Test" ? "#ef4444" : // Червено
                                      "#10b981", // Зелено за общи бележки
                    borderColor = e.EventType == "Reading" ? "#2563eb" :
                                  e.EventType == "Analysis" ? "#7c3aed" :
                                  e.EventType == "Test" ? "#dc2626" :
                                  "#059669"
                })
                .ToListAsync();

            return Json(events);
        }

        // Създаване на ново събитие (POST)
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] StudyEvent newEvent)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrEmpty(newEvent.Title))
            {
                return BadRequest("Заглавието е задължително.");
            }

            newEvent.UserId = user.Id;

            // Ако потребителят е избрал книга (но идва като празен стринг от JS, го правим на null)
            if (newEvent.BookId == 0) newEvent.BookId = null;

            _context.StudyEvents.Add(newEvent);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, id = newEvent.Id });
        }

        // Изтриване на събитие (POST)
        [HttpPost]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var studyEvent = await _context.StudyEvents
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id);

            if (studyEvent == null) return NotFound();

            _context.StudyEvents.Remove(studyEvent);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // Отмятане/Възстановяване на събитие (POST)
        [HttpPost]
        public async Task<IActionResult> ToggleEventStatus(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var studyEvent = await _context.StudyEvents
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id);

            if (studyEvent == null) return NotFound();

            studyEvent.IsCompleted = !studyEvent.IsCompleted;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, isCompleted = studyEvent.IsCompleted });
        }
    }

}