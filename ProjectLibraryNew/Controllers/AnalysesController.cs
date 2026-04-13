using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.Data;
using ProjectLibrary.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectLibrary.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AnalysesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalysesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Показва всички анализи за дадено произведение
        public async Task<IActionResult> Index(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.Analyses)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null) return NotFound();

            ViewBag.BookTitle = book.Title;
            ViewBag.BookId = book.Id;

            return View(book.Analyses.OrderByDescending(a => a.CreatedDate).ToList());
        }

        // GET: Форма за добавяне на анализ
        public IActionResult Create(int bookId)
        {
            ViewBag.BookId = bookId;
            return View();
        }

        // POST: Записване на новия анализ
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ДОБАВЕНО: "Title" в Bind списъка
        public async Task<IActionResult> Create([Bind("BookId,Title,Content")] Analysis analysis)
        {
            ModelState.Remove("Book");
            ModelState.Remove("CreatedDate");

            if (ModelState.IsValid)
            {
                analysis.CreatedDate = DateTime.Now;

                _context.Add(analysis);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Анализът е добавен успешно!";
                return RedirectToAction(nameof(Index), new { bookId = analysis.BookId });
            }

            ViewBag.BookId = analysis.BookId;
            return View(analysis);
        }

        // GET: Форма за редактиране
        public async Task<IActionResult> Edit(int id)
        {
            var analysis = await _context.Analyses.FindAsync(id);
            if (analysis == null) return NotFound();

            return View(analysis);
        }

        // POST: Записване на редакцията
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ДОБАВЕНО: "Title" в Bind списъка
        public async Task<IActionResult> Edit(int id, [Bind("Id,BookId,Title,Content,CreatedDate")] Analysis analysis)
        {
            if (id != analysis.Id) return NotFound();

            ModelState.Remove("Book");

            if (ModelState.IsValid)
            {
                _context.Update(analysis);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Анализът е редактиран успешно!";
                return RedirectToAction(nameof(Index), new { bookId = analysis.BookId });
            }
            return View(analysis);
        }

        // POST: Изтриване
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var analysis = await _context.Analyses.FindAsync(id);
            if (analysis != null)
            {
                int bookId = analysis.BookId;
                _context.Analyses.Remove(analysis);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Анализът е изтрит успешно!";
                return RedirectToAction(nameof(Index), new { bookId = bookId });
            }

            return RedirectToAction("Books", "Admin");
        }
    }
}