using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ⬅️ ДОБАВИ ТОВА!
using ProjectLibrary.Data;
using ProjectLibrary.Models;

public class DebugController : Controller
{
    private readonly ApplicationDbContext _context;

    public DebugController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> CheckBooks()
    {
        var books = await _context.Books
            .Include(b => b.Author)
            .ToListAsync();

        return View(books);
    }
    public async Task<IActionResult> AddTestBooks()
    {
        try
        {
            // Провери дали вече има книги
            if (await _context.Books.AnyAsync())
            {
                return Content("✅ Вече има книги в базата. Отиди на <a href='/Books'>/Books</a>");
            }

            // Създай автори
            var author1 = new Author { Name = "Иван Вазов", Biography = "Български поет и писател" };
            var author2 = new Author { Name = "Алеко Константинов", Biography = "Български писател" };

            _context.Authors.Add(author1);
            _context.Authors.Add(author2);
            await _context.SaveChangesAsync();

            // Създай книги
            var books = new List<Book>
        {
            new Book {
                Title = "Под игото",
                AuthorId = author1.Id,
                Description = "Роман за Априлското въстание",
                Genre = "Роман",
                StudyYear = 10,
                IsForMatura = true,
                WritingPeriod = "Възраждане",
                PublishedYear = 1894
            },
            new Book {
                Title = "Бай Ганьо",
                AuthorId = author2.Id,
                Description = "Сатирични разкази за българския характер",
                Genre = "Сатира",
                StudyYear = 9,
                IsForMatura = true,
                WritingPeriod = "Възраждане",
                PublishedYear = 1895
            }
        };

            _context.Books.AddRange(books);
            await _context.SaveChangesAsync();

            return Content("✅ Книгите са добавени успешно! <a href='/Books'>Отиди в библиотеката</a>", "text/html");
        }
        catch (Exception ex)
        {
            return Content($"❌ Грешка: {ex.Message}<br>Детайли: {ex.InnerException?.Message}", "text/html");
        }
    }
    public async Task<IActionResult> TestBooksView()
    {
        var books = await _context.Books
            .Include(b => b.Author)
            .ToListAsync();

        // Връщаме същия view като Books/Index, но директно
        return View("~/Views/Books/Index.cshtml", books);
    }
}