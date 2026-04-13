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
using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Controllers
{
    // НОВО: Създаваме сигурен модел за данните (DTO)
    public class EditProfileDto
    {
        [Required(ErrorMessage = "Полето за име е задължително.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Името трябва да бъде между 2 и 50 символа.")]
        [RegularExpression(@"^[a-zA-Zа-яА-Я\s\-]*$", ErrorMessage = "Името може да съдържа само букви и тирета.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Полето за фамилия е задължително.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилията трябва да бъде между 2 и 50 символа.")]
        [RegularExpression(@"^[a-zA-Zа-яА-Я\s\-]*$", ErrorMessage = "Фамилията може да съдържа само букви и тирета.")]
        public string LastName { get; set; }

        // Добавяме и снимката тук
        public IFormFile ProfilePicture { get; set; }
    }

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

            var fullUser = await _context.Users
                .Include(u => u.UserBadges).ThenInclude(ub => ub.Badge)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

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

            var interactedBookIds = userProgresses
                .Where(p => p.IsTextRead || p.IsAnalysisRead || p.HasPassedTest)
                .Select(p => p.BookId)
                .ToList();

            var recommendedBook = await _context.Books
                .Include(b => b.Author)
                .Where(b => !interactedBookIds.Contains(b.Id))
                .FirstOrDefaultAsync();

            ViewBag.RecommendedBook = recommendedBook;

            return View(fullUser);
        }

        // GET: Profiles/Edit
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Прехвърляме данните към DTO-то
            var dto = new EditProfileDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            return View(dto);
        }

        // POST: Profiles/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Променяме параметрите - вече приемаме нашия сигурен DTO
        public async Task<IActionResult> Edit(EditProfileDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Ако някой хакер прати цифри за име, ModelState.IsValid ще го хване и ще го върне!
            if (!ModelState.IsValid)
            {
                // Връщаме изгледа с грешките
                return View(dto);
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;

            if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + dto.ProfilePicture.FileName;
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ProfilePicture.CopyToAsync(fileStream);
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
                    start = e.EventDate.ToString("yyyy-MM-dd"),
                    allDay = true,
                    description = e.Description,
                    bookId = e.BookId,
                    bookTitle = e.Book != null ? e.Book.Title : null,
                    eventType = e.EventType,
                    isCompleted = e.IsCompleted,
                    backgroundColor = e.EventType == "Reading" ? "#3b82f6" :
                                      e.EventType == "Analysis" ? "#8b5cf6" :
                                      e.EventType == "Test" ? "#ef4444" :
                                      "#10b981",
                    borderColor = e.EventType == "Reading" ? "#2563eb" :
                                  e.EventType == "Analysis" ? "#7c3aed" :
                                  e.EventType == "Test" ? "#dc2626" :
                                  "#059669"
                })
                .ToListAsync();

            return Json(events);
        }

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

            if (newEvent.BookId == 0) newEvent.BookId = null;

            _context.StudyEvents.Add(newEvent);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, id = newEvent.Id });
        }

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

        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(IFormFile profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || profilePicture == null || profilePicture.Length == 0)
            {
                return RedirectToAction(nameof(MyProfile));
            }

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
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(MyProfile));
        }
    }
}