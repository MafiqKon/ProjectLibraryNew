using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.Data;
using ProjectLibrary.Models;

public class SeedController : Controller
{
    private readonly ApplicationDbContext _context;

    public SeedController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> SeedData()
    {
        try
        {
            // 1. Изтрий всички данни (ако искаш чисто начало)
            _context.Books.RemoveRange(_context.Books);
            _context.Authors.RemoveRange(_context.Authors);
            await _context.SaveChangesAsync();

            // 2. Създай автори ПЪРВИ
            var author1 = new Author { Name = "Иван Вазов", Biography = "Български поет" };
            var author2 = new Author { Name = "Алеко Константинов", Biography = "Български писател" };

            _context.Authors.Add(author1);
            _context.Authors.Add(author2);
            await _context.SaveChangesAsync(); // ⬅️ ЗАПАЗИ ПЪРВО АВТОРИТЕ!

            // 3. Създай книги СЛЕД като авторите са запазени
            var books = new List<Book>
            {
                new Book {
                    Title = "Под игото",
                    AuthorId = author1.Id, // ⬅️ Използвай ID-то от запазения автор
                    Description = "Роман за Априлското въстание",
                    Genre = "Роман",
                    StudyYear = 10,
                    IsForMatura = true,
                    WritingPeriod = "Възраждане",
                    PublishedYear = 1894
                },
                new Book {
                    Title = "Бай Ганьо",
                    AuthorId = author2.Id, // ⬅️ Използвай ID-то от запазения автор
                    Description = "Сатирични разкази",
                    Genre = "Сатира",
                    StudyYear = 9,
                    IsForMatura = true,
                    WritingPeriod = "Възраждане",
                    PublishedYear = 1895
                }
            };

            _context.Books.AddRange(books);
            await _context.SaveChangesAsync();

            return Content("✅ Данните са добавени успешно! Имаме " + _context.Books.Count() + " книги. Отиди на /Books");
        }
        catch (Exception ex)
        {
            return Content($"❌ Грешка: {ex.Message}<br>Inner: {ex.InnerException?.Message}");
        }
    }
}