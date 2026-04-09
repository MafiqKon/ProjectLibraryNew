using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.Models;
using ProjectLibrary.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ProjectLibrary.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,BookId,Content,Rating,ParentCommentId")] Comment comment)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Book");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("DatePosted");
            ModelState.Remove("UserName");
            ModelState.Remove("UserProfilePictureUrl");
            ModelState.Remove("ParentComment");
            ModelState.Remove("Replies");
            ModelState.Remove("Rating");

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                comment.UserId = user.Id;
                comment.User = user;
                comment.Replies = new List<Comment>();

                comment.CreatedDate = DateTime.Now;
                if (comment.Rating == 0) comment.Rating = 5;

                _context.Add(comment);
                await _context.SaveChangesAsync();

                // ФИКС: Подаваме и BookId, за да проверим за "Първооткривател"
                await CheckBadges(user.Id, comment.BookId);

                bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

                if (isAjax)
                {
                    return PartialView("_CommentPartial", comment);
                }

                return RedirectToAction("Details", "Books", new { id = comment.BookId });
            }

            return RedirectToAction("Details", "Books", new { id = comment.BookId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (comment.UserId == user.Id || isAdmin)
            {
                await DeleteHierarchy(id);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Books", new { id = comment.BookId });
        }

        private async Task DeleteHierarchy(int commentId)
        {
            var replies = await _context.Comments
                .Where(c => c.ParentCommentId == commentId)
                .ToListAsync();

            foreach (var reply in replies)
            {
                await DeleteHierarchy(reply.Id);
            }

            var currentComment = await _context.Comments.FindAsync(commentId);
            if (currentComment != null)
            {
                _context.Comments.Remove(currentComment);
            }
        }

        // ==========================================================
        // ОБНОВЕН МЕТОД ЗА ПРОВЕРКА И РАЗДАВАНЕ НА ЗНАЧКИ
        // ==========================================================
        private async Task CheckBadges(string userId, int bookId)
        {
            // 1. Колко коментара има потребителят общо?
            int userCommentCount = await _context.Comments.CountAsync(c => c.UserId == userId);

            // 2. Колко коментара има тази конкретна книга?
            int bookCommentCount = await _context.Comments.CountAsync(c => c.BookId == bookId);

            // СТАРИТЕ ТИ ЗНАЧКИ (Ако искаш да ги запазиш)
            if (userCommentCount >= 1) await AddBadgeToUser(userId, "Първи стъпки");
            if (userCommentCount >= 5) await AddBadgeToUser(userId, "Дискусионен лидер");
            if (userCommentCount >= 10) await AddBadgeToUser(userId, "Майстор на словото");

            // НОВИ: Златно перо (над 5 коментара)
            if (userCommentCount >= 5) await AddBadgeToUser(userId, "Златно перо");

            // НОВИ: Първооткривател (ако това е единственият коментар на книгата)
            if (bookCommentCount == 1) await AddBadgeToUser(userId, "Първооткривател");
        }

        // Обновен метод за добавяне, който записва и TempData за изскачащо съобщение
        private async Task AddBadgeToUser(string userId, string badgeName)
        {
            var badge = await _context.Badges.FirstOrDefaultAsync(b => b.Name == badgeName);

            if (badge != null)
            {
                bool alreadyHasIt = await _context.UserBadges
                    .AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badge.Id);

                // Ако потребителят все още няма тази значка, му я даваме!
                if (!alreadyHasIt)
                {
                    var userBadge = new UserBadge
                    {
                        UserId = userId,
                        BadgeId = badge.Id
                    };

                    _context.UserBadges.Add(userBadge);
                    await _context.SaveChangesAsync();

                    // Записваме съобщение, за да може изгледът (View-то) да извади красив Toast/Конфети
                    if (TempData["BadgeUnlocked"] != null)
                    {
                        TempData["BadgeUnlocked"] = TempData["BadgeUnlocked"] + $" И отключихте: {badge.Name}! 🎉";
                    }
                    else
                    {
                        TempData["BadgeUnlocked"] = $"Отключихте нова значка: {badge.Name}! 🎉";
                    }
                }
            }
        }
    }
}