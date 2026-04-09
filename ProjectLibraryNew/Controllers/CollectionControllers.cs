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
    public class CollectionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CollectionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Моите колекции
        public async Task<IActionResult> Index(string search)
        {
            var user = await _userManager.GetUserAsync(User);
            var collectionsQuery = _context.BookCollections
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Books)
                .ThenInclude(b => b.Author)
                .AsQueryable();

            // Филтриране по търсене
            if (!string.IsNullOrEmpty(search))
            {
                collectionsQuery = collectionsQuery.Where(c => c.Name.Contains(search));
                ViewBag.SearchQuery = search;
            }

            var collections = await collectionsQuery.ToListAsync();
            return View(collections);
        }

        // GET: Създаване на нова колекция
        public IActionResult Create()
        {
            return View();
        }

        // POST: Създаване на нова колекция
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Name, string Description, bool IsPublic = true)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!string.IsNullOrEmpty(Name))
            {
                var newCollection = new BookCollection
                {
                    Name = Name,
                    Description = Description,
                    IsPublic = IsPublic,
                    UserId = user.Id,
                    CreatedDate = DateTime.Now
                };

                _context.Add(newCollection);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        // GET: Добавяне на книга в колекция
        public async Task<IActionResult> AddBook(int collectionId, string search)
        {
            var user = await _userManager.GetUserAsync(User);
            var collection = await _context.BookCollections
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == user.Id);

            if (collection == null)
            {
                return NotFound();
            }

            var booksQuery = _context.Books
                .Include(b => b.Author)
                .AsQueryable();

            // Филтриране по търсене
            if (!string.IsNullOrEmpty(search))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(search) ||
                    b.Author.Name.Contains(search) ||
                    b.Genre.Contains(search)
                );
                ViewBag.SearchQuery = search;
            }

            var books = await booksQuery.ToListAsync();

            // Вземане на ID-та на вече добавени книги
            var existingBookIds = collection.Books.Select(b => b.Id).ToList();

            ViewBag.Collection = collection;
            ViewBag.Books = books;
            ViewBag.ExistingBookIds = existingBookIds;

            return View();
        }

        // POST: Добавяне на книга в колекция
        [HttpPost]
        public async Task<IActionResult> AddBookToCollection(int collectionId, int bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            var collection = await _context.BookCollections
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == user.Id);

            var book = await _context.Books.FindAsync(bookId);

            if (collection != null && book != null && !collection.Books.Any(b => b.Id == bookId))
            {
                collection.Books.Add(book);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Книгата беше добавена успешно!";
            }
            else
            {
                TempData["ErrorMessage"] = "Грешка при добавяне на книгата!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Преглед на колекция
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var collection = await _context.BookCollections
                .Include(c => c.Books)
                .ThenInclude(b => b.Author)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (collection == null)
            {
                return NotFound();
            }

            return View(collection);
        }

        // GET: Изтриване на колекция
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var collection = await _context.BookCollections
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (collection == null)
            {
                return NotFound();
            }

            return View(collection);
        }

        // POST: Изтриване на колекция
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var collection = await _context.BookCollections
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (collection != null)
            {
                _context.BookCollections.Remove(collection);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Колекцията беше изтрита успешно!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}