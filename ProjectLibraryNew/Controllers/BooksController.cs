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
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BooksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Books
        public async Task<IActionResult> Index(string searchString, string author, string genre, int? studyYear, bool? forMatura)
        {
            var books = _context.Books.Include(b => b.Author).AsQueryable();

            // Филтриране
            if (!string.IsNullOrEmpty(searchString))
            {
                books = books.Where(b => b.Title.Contains(searchString) ||
                                         b.Author.Name.Contains(searchString));
            }
            if (!string.IsNullOrEmpty(author)) books = books.Where(b => b.Author.Name == author);
            if (!string.IsNullOrEmpty(genre)) books = books.Where(b => b.Genre == genre);
            if (studyYear.HasValue) books = books.Where(b => b.StudyYear == studyYear.Value);
            if (forMatura.HasValue) books = books.Where(b => b.IsForMatura == forMatura.Value);

            // ==========================================
            // ЛОГИКА ЗА ИЗЧИСЛЯВАНЕ НА ПРОГРЕСА
            // ==========================================
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // 1. Взимаме целия прогрес на ученика от базата
                var userProgresses = await _context.UserBookProgresses
                    .Where(p => p.UserId == user.Id)
                    .ToListAsync();

                // 2. Смятаме процента за ВСЯКА книга (30+30+40)
                var progressDict = new Dictionary<int, int>();
                foreach (var p in userProgresses)
                {
                    int score = 0;
                    if (p.IsTextRead) score += 30;
                    if (p.IsAnalysisRead) score += 30;
                    if (p.HasPassedTest) score += 40;
                    progressDict[p.BookId] = score;
                }

                // Подаваме го към View-то, за да сложим тикчетата на картите
                ViewBag.UserProgresses = progressDict;

                // 3. Смятаме ОБЩАТА готовност за матура (Големият прогрес бар)
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
            }

            // Подаване на филтри за View
            ViewBag.Authors = await _context.Books.Select(b => b.Author.Name).Distinct().ToListAsync();
            ViewBag.Genres = await _context.Books.Select(b => b.Genre).Distinct().ToListAsync();
            ViewBag.StudyYears = await _context.Books.Select(b => b.StudyYear).Distinct().OrderBy(y => y).ToListAsync();

            return View(await books.ToListAsync());
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Analyses)
                .Include(b => b.Tests)
                .Include(b => b.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            // Взимаме прогреса на текущия потребител за тази книга
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.UserProgress = await _context.UserBookProgresses
                    .FirstOrDefaultAsync(p => p.BookId == id && p.UserId == user.Id);
            }

            return View(book);
        }

        // ==========================================
        // НОВ МЕТОД ЗА ЗАПАЗВАНЕ НА ПРОГРЕСА (AJAX)
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProgress(int bookId, string progressType, bool isChecked)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var progress = await _context.UserBookProgresses
                .FirstOrDefaultAsync(p => p.BookId == bookId && p.UserId == user.Id);

            // Ако потребителят цъка за първи път, създаваме запис
            if (progress == null)
            {
                progress = new UserBookProgress { BookId = bookId, UserId = user.Id };
                _context.UserBookProgresses.Add(progress);
            }

            if (progressType == "text") progress.IsTextRead = isChecked;
            else if (progressType == "analysis") progress.IsAnalysisRead = isChecked;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET: Books/Read/5
        public async Task<IActionResult> Read(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null) return NotFound();

            return View(book);
        }

        // GET: Books/Create
        [Authorize]
        public IActionResult Create()
        {
            ViewBag.Authors = _context.Authors.ToList();
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create([Bind("Id,Title,AuthorId,Description,StudyYear,Genre,IsForMatura,FullText,PublishedYear,WritingPeriod,Tags")] Book book)
        {
            ModelState.Remove("Author");

            if (ModelState.IsValid)
            {
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction("Books", "Admin");
            }
            ViewBag.Authors = _context.Authors.ToList();
            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            ViewBag.Authors = _context.Authors.ToList();
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,AuthorId,Description,StudyYear,Genre,IsForMatura,FullText,PublishedYear,WritingPeriod,Tags")] Book book)
        {
            if (id != book.Id) return NotFound();

            ModelState.Remove("Author");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Books", "Admin");
            }

            foreach (var modelState in ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    System.Diagnostics.Debug.WriteLine($">>> ГРЕШКА ПРИ ЗАПАЗВАНЕ: {error.ErrorMessage}");
                }
            }

            ViewBag.Authors = _context.Authors.ToList();
            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null) return NotFound();

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Books", "Admin");
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }
    }
}