using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.Models;
using ProjectLibrary.Data;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectLibrary.Controllers
{
    [Authorize]
    public class TestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // 1. СЪЩЕСТВУВАЩИ МЕТОДИ ЗА ТЕСТОВЕ
        // ==========================================

        public async Task<IActionResult> Index()
        {
            try
            {
                var tests = await _context.Tests
                    .Include(t => t.Book)
                    .ThenInclude(b => b.Author)
                    .Where(t => t.IsPublished)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToListAsync();

                if (!tests.Any())
                {
                    var sampleTest = new Test
                    {
                        Title = "Примерен тест по българска литература",
                        Description = "Това е примерен тест за демонстрация",
                        BookId = _context.Books.FirstOrDefault()?.Id ?? 1,
                        TimeLimitMinutes = 30,
                        TotalPoints = 100,
                        PassingScore = 60,
                        IsPublished = true,
                        CreatedDate = DateTime.Now
                    };

                    sampleTest.SetQuestions(new List<TestQuestion>
                    {
                        new TestQuestion
                        {
                            Id = 1,
                            QuestionText = "Това е примерен въпрос?",
                            Type = QuestionType.MultipleChoice,
                            Points = 5,
                            Options = new List<QuestionOption>
                            {
                                new QuestionOption { Id = 1, Text = "Да" },
                                new QuestionOption { Id = 2, Text = "Не" },
                                new QuestionOption { Id = 3, Text = "Може би" },
                                new QuestionOption { Id = 4, Text = "Не знам" }
                            },
                            CorrectOptionId = 1
                        }
                    });

                    _context.Tests.Add(sampleTest);
                    await _context.SaveChangesAsync();

                    tests = await _context.Tests
                        .Include(t => t.Book)
                        .ThenInclude(b => b.Author)
                        .Where(t => t.IsPublished)
                        .OrderByDescending(t => t.CreatedDate)
                        .ToListAsync();
                }

                return View(tests);
            }
            catch (Exception ex)
            {
                return View(new List<Test>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var test = await _context.Tests
                .Include(t => t.Book)
                .ThenInclude(b => b.Author)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null) return NotFound();

            return View(test);
        }

        public async Task<IActionResult> Start(int id)
        {
            var test = await _context.Tests
                .Include(t => t.Book)
                .ThenInclude(b => b.Author)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null) return NotFound();

            ViewBag.Test = test;
            ViewBag.Questions = test.GetQuestions();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit(int testId, Dictionary<int, string> answers)
        {
            var test = await _context.Tests.FindAsync(testId);
            var user = await _userManager.GetUserAsync(User);

            if (test == null || user == null) return NotFound();

            var questions = test.GetQuestions();
            var resultDetails = new List<QuestionResult>();
            int totalScore = 0;
            int correctAnswersCount = 0;

            foreach (var question in questions)
            {
                var userAnswer = answers.ContainsKey(question.Id) ? (answers[question.Id] ?? string.Empty) : string.Empty;
                var isCorrect = false;
                var pointsEarned = 0;

                // ЗАТВОРЕН ВЪПРОС
                if ((int)question.Type == 0)
                {
                    if (int.TryParse(userAnswer, out int selectedOption) && selectedOption == question.CorrectOptionId)
                    {
                        isCorrect = true;
                        pointsEarned = question.Points;
                        correctAnswersCount++;
                    }
                }
                // ОТВОРЕН ВЪПРОС
                else if ((int)question.Type == 1)
                {
                    var normalizedUserAnswer = question.IsCaseSensitive ? userAnswer.Trim() : userAnswer.Trim().ToLower();
                    var normalizedAcceptableAnswers = question.AcceptableAnswers
                        .Select(a => question.IsCaseSensitive ? a.Trim() : a.Trim().ToLower())
                        .ToList();

                    if (normalizedAcceptableAnswers.Contains(normalizedUserAnswer))
                    {
                        isCorrect = true;
                        pointsEarned = question.Points;
                        correctAnswersCount++;
                    }
                }
                // СВЪРЗВАНЕ 
                else if ((int)question.Type == 2)
                {
                    var submittedPairs = userAnswer.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(p => p.Trim())
                                                   .ToList();

                    var correctPairs = question.Options.Select(o => o.Text.Trim()).ToList();
                    int correctMatches = 0;

                    foreach (var pair in submittedPairs)
                    {
                        if (correctPairs.Contains(pair))
                        {
                            correctMatches++;
                        }
                    }

                    if (correctPairs.Count > 0)
                    {
                        double pointsPerPair = (double)question.Points / correctPairs.Count;
                        pointsEarned = (int)Math.Round(correctMatches * pointsPerPair);
                    }

                    if (correctMatches == correctPairs.Count && correctPairs.Count > 0)
                    {
                        isCorrect = true;
                        correctAnswersCount++;
                    }
                }

                totalScore += pointsEarned;

                resultDetails.Add(new QuestionResult
                {
                    QuestionId = question.Id,
                    IsCorrect = isCorrect,
                    UserAnswer = userAnswer,
                    CorrectAnswer = (int)question.Type == 0 ?
                        question.Options.FirstOrDefault(o => o.Id == question.CorrectOptionId)?.Text ?? "" :
                        (int)question.Type == 1 ? string.Join("; ", question.AcceptableAnswers) :
                        string.Join(", ", question.Options.Select(o => o.Text)),
                    PointsEarned = pointsEarned,
                    MaxPoints = question.Points
                });
            }

            double finalPercentage = (double)totalScore / test.TotalPoints * 100;

            var testResult = new TestResult
            {
                TestId = testId,
                UserId = user.Id,
                Score = totalScore,
                TotalQuestions = questions.Count,
                CorrectAnswers = correctAnswersCount,
                Percentage = finalPercentage,
                TimeSpent = TimeSpan.FromMinutes(45)
            };

            testResult.SetResultDetails(resultDetails);
            _context.TestResults.Add(testResult);

            if (finalPercentage >= test.PassingScore && test.BookId != null)
            {
                var progress = await _context.UserBookProgresses
                    .FirstOrDefaultAsync(p => p.BookId == test.BookId && p.UserId == user.Id);

                if (progress == null)
                {
                    progress = new UserBookProgress
                    {
                        BookId = test.BookId.Value,
                        UserId = user.Id,
                        IsTextRead = false,
                        IsAnalysisRead = false,
                        HasPassedTest = true
                    };
                    _context.UserBookProgresses.Add(progress);
                }
                else
                {
                    progress.HasPassedTest = true;
                }
            }

            // Първо запазваме резултата в базата данни
            await _context.SaveChangesAsync();

            // ==========================================
            // НОВО: ПРОВЕРЯВАМЕ И РАЗДАВАМЕ ЗНАЧКИ!
            // ==========================================
            await CheckTestBadges(user.Id, finalPercentage, testId);

            return RedirectToAction(nameof(Results), new { id = testResult.Id });
        }

        public async Task<IActionResult> Results(int id)
        {
            var result = await _context.TestResults
                .Include(r => r.Test)
                .Include(r => r.Test.Book)
                .ThenInclude(b => b.Author)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (result == null || result.UserId != _userManager.GetUserId(User)) return NotFound();

            ViewBag.ResultDetails = result.GetResultDetails();
            ViewBag.TestQuestions = result.Test.GetQuestions();

            return View(result);
        }

        // ==========================================
        // 2. СТУДЕНТСКО ТАБЛО (MyResults)
        // ==========================================
        public async Task<IActionResult> MyResults()
        {
            var user = await _userManager.GetUserAsync(User);

            var results = await _context.TestResults
                .Include(r => r.Test)
                .Include(r => r.Test.Book)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            ViewBag.TotalTestsTaken = results.Count;
            ViewBag.AverageScore = results.Any() ? Math.Round(results.Average(r => r.Percentage), 1) : 0;

            ViewBag.MostPracticedBook = results.Any() && results.Any(r => r.Test != null && r.Test.Book != null)
                ? results.Where(r => r.Test != null && r.Test.Book != null)
                         .GroupBy(r => r.Test.Book.Title)
                         .OrderByDescending(g => g.Count())
                         .FirstOrDefault()?.Key
                : "Общи тестове / Няма данни";

            ViewBag.Passed = results.Count(r => r.Percentage >= 60);

            var interactedBookIds = await _context.UserBookProgresses
                .Where(p => p.UserId == user.Id && (p.IsTextRead || p.IsAnalysisRead || p.HasPassedTest))
                .Select(p => p.BookId)
                .ToListAsync();

            var recommendedBook = await _context.Books
                .Include(b => b.Author)
                .Where(b => !interactedBookIds.Contains(b.Id))
                .FirstOrDefaultAsync();

            ViewBag.RecommendedBook = recommendedBook;

            return View(results);
        }

        // ==========================================
        // 3. МЕТОДИ ЗА СТАТИЧНИТЕ ТРЕНИРОВЪЧНИ ТЕСТОВЕ
        // ==========================================
        public IActionResult PracticeTest(int id = 1)
        {
            var quiz = GenerateStaticQuiz(id);
            if (quiz == null) return NotFound();
            return View(quiz);
        }

        [HttpPost]
        public async Task<IActionResult> PracticeTest(int id, QuizViewModel model)
        {
            var originalQuiz = GenerateStaticQuiz(id);
            if (originalQuiz == null) return NotFound();

            model.QuizId = id;
            model.BookTitle = originalQuiz.BookTitle;
            model.IsSubmitted = true;
            model.Score = 0;

            for (int i = 0; i < originalQuiz.Questions.Count; i++)
            {
                model.Questions[i].Text = originalQuiz.Questions[i].Text;
                model.Questions[i].IsOpenEnded = originalQuiz.Questions[i].IsOpenEnded;
                model.Questions[i].Options = originalQuiz.Questions[i].Options;
                model.Questions[i].CorrectOptionId = originalQuiz.Questions[i].CorrectOptionId;
                model.Questions[i].AcceptableAnswers = originalQuiz.Questions[i].AcceptableAnswers;

                if (originalQuiz.Questions[i].IsOpenEnded)
                {
                    var userAnswer = model.Questions[i].UserTextAnswer?.Trim().ToLower();
                    var isCorrect = !string.IsNullOrEmpty(userAnswer) &&
                                    originalQuiz.Questions[i].AcceptableAnswers.Any(a => a.ToLower() == userAnswer);
                    model.Questions[i].IsAnswerCorrect = isCorrect;
                    if (isCorrect) model.Score++;
                }
                else
                {
                    var isCorrect = model.Questions[i].SelectedOptionId == originalQuiz.Questions[i].CorrectOptionId;
                    model.Questions[i].IsAnswerCorrect = isCorrect;
                    if (isCorrect) model.Score++;
                }
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var dummyTest = await _context.Tests.FirstOrDefaultAsync();
                if (dummyTest != null)
                {
                    double currentPercentage = ((double)model.Score / originalQuiz.Questions.Count) * 100;

                    var resultRecord = new TestResult
                    {
                        TestId = dummyTest.Id,
                        UserId = user.Id,
                        Score = model.Score,
                        TotalQuestions = originalQuiz.Questions.Count,
                        CorrectAnswers = model.Score,
                        Percentage = currentPercentage,
                        TimeSpent = TimeSpan.FromMinutes(5)
                    };
                    _context.TestResults.Add(resultRecord);
                    await _context.SaveChangesAsync();

                    // Добавяме проверка за значки и при тренировъчните тестове!
                    await CheckTestBadges(user.Id, currentPercentage, dummyTest.Id);
                }
            }

            return View(model);
        }

        private QuizViewModel GenerateStaticQuiz(int id)
        {
            if (id == 1)
            {
                return new QuizViewModel
                {
                    QuizId = 1,
                    BookTitle = "Железният светилник (Тренировка)",
                    Questions = new List<QuestionViewModel>
                    {
                        new QuestionViewModel { Id = 1, Text = "Кой е основателят на рода Глаушеви?", CorrectOptionId = 1, Options = new List<AnswerOption> { new AnswerOption { Id = 1, Text = "Стоян" }, new AnswerOption { Id = 2, Text = "Лазар" }, new AnswerOption { Id = 3, Text = "Климент" }, new AnswerOption { Id = 4, Text = "Рафе" } } },
                        new QuestionViewModel { Id = 2, Text = "Как се казва майката на Лазар?", CorrectOptionId = 2, Options = new List<AnswerOption> { new AnswerOption { Id = 1, Text = "Ния" }, new AnswerOption { Id = 2, Text = "Султана" }, new AnswerOption { Id = 3, Text = "Божана" }, new AnswerOption { Id = 4, Text = "Катерина" } } },
                        new QuestionViewModel { Id = 3, Text = "Къде се развива действието в романа?", CorrectOptionId = 3, Options = new List<AnswerOption> { new AnswerOption { Id = 1, Text = "София" }, new AnswerOption { Id = 2, Text = "Търново" }, new AnswerOption { Id = 3, Text = "Преспа" }, new AnswerOption { Id = 4, Text = "Охрид" } } }
                    }
                };
            }
            else if (id == 2)
            {
                return new QuizViewModel
                {
                    QuizId = 2,
                    BookTitle = "Под игото (Тренировка)",
                    Questions = new List<QuestionViewModel>
                    {
                        new QuestionViewModel { Id = 1, Text = "Кой е авторът на романа?", CorrectOptionId = 2, Options = new List<AnswerOption> { new AnswerOption { Id = 1, Text = "Христо Ботев" }, new AnswerOption { Id = 2, Text = "Иван Вазов" }, new AnswerOption { Id = 3, Text = "Елин Пелин" }, new AnswerOption { Id = 4, Text = "Пейо Яворов" } } },
                        new QuestionViewModel { Id = 2, Text = "Къде се развива основното действие?", CorrectOptionId = 1, Options = new List<AnswerOption> { new AnswerOption { Id = 1, Text = "Бяла черква" }, new AnswerOption { Id = 2, Text = "София" }, new AnswerOption { Id = 3, Text = "Пловдив" }, new AnswerOption { Id = 4, Text = "Преспа" } } }
                    }
                };
            }
            return null;
        }

        // ==========================================
        // 4. АДМИН/УЧИТЕЛСКИ МЕТОДИ ЗА СЪЗДАВАНЕ
        // ==========================================
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Books = await _context.Books.Include(b => b.Author).OrderBy(b => b.Title).ToListAsync();
            return View(new Test());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Create(Test test, string questionsJson)
        {
            var user = await _userManager.GetUserAsync(User);
            test.CreatedByUserId = user.Id;
            test.CreatedDate = DateTime.Now;

            try
            {
                if (!string.IsNullOrEmpty(questionsJson))
                {
                    var questions = JsonSerializer.Deserialize<List<TestQuestion>>(questionsJson);
                    if (questions != null && questions.Any())
                    {
                        test.SetQuestions(questions);
                    }
                }

                if (string.IsNullOrEmpty(test.QuestionsJson))
                {
                    var questionsFromForm = GetQuestionsFromForm(Request.Form);
                    if (questionsFromForm.Any())
                    {
                        test.SetQuestions(questionsFromForm);
                    }
                }

                if (string.IsNullOrEmpty(test.QuestionsJson) || test.GetQuestions().Count == 0)
                {
                    ModelState.AddModelError("", "❌ Тестът трябва да има поне един въпрос!");
                    ViewBag.Books = await _context.Books.Include(b => b.Author).OrderBy(b => b.Title).ToListAsync();
                    return View(test);
                }

                _context.Tests.Add(test);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "✅ Тестът беше създаден успешно!";
                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "❌ Грешка при запис в базата данни: " + ex.Message);
                ViewBag.Books = await _context.Books.Include(b => b.Author).OrderBy(b => b.Title).ToListAsync();
                return View(test);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> BulkImport(int testId, string jsonQuestions)
        {
            var test = await _context.Tests.FindAsync(testId);
            if (test == null) return NotFound("Тестът не е намерен.");

            try
            {
                var questions = JsonSerializer.Deserialize<List<TestQuestion>>(jsonQuestions);
                if (questions != null && questions.Any())
                {
                    test.SetQuestions(questions);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Успешно добавени {questions.Count} въпроса чрез JSON импорт!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Списъкът с въпроси е празен или JSON кодът е невалиден.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Грешка при импорт на JSON: " + ex.Message;
            }

            return RedirectToAction(nameof(Manage));
        }

        private List<TestQuestion> GetQuestionsFromForm(Microsoft.AspNetCore.Http.IFormCollection form)
        {
            var questions = new List<TestQuestion>();
            var questionIndexes = form.Keys
                .Where(k => k.StartsWith("questions["))
                .Select(k => {
                    var parts = k.Split('[');
                    if (parts.Length > 1)
                    {
                        var indexPart = parts[1].Split(']')[0];
                        int.TryParse(indexPart, out int result);
                        return result;
                    }
                    return -1;
                })
                .Where(idx => idx != -1)
                .Distinct()
                .ToList();

            foreach (var index in questionIndexes)
            {
                var questionText = form[$"questions[{index}].QuestionText"].ToString();
                var questionType = form[$"questions[{index}].Type"].ToString();
                var pointsStr = form[$"questions[{index}].Points"].ToString();

                if (!string.IsNullOrEmpty(questionText))
                {
                    var question = new TestQuestion
                    {
                        Id = index + 1,
                        QuestionText = questionText,
                        Points = int.TryParse(pointsStr, out int p) ? p : 1,
                        Type = questionType == "1" ? QuestionType.OpenEnded : (questionType == "2" ? QuestionType.Matching : QuestionType.MultipleChoice)
                    };

                    if ((int)question.Type == 0 || (int)question.Type == 2)
                    {
                        for (int i = 1; i <= 10; i++)
                        {
                            var optionText = form[$"questions[{index}].Options[{i}].Text"].ToString();
                            if (!string.IsNullOrEmpty(optionText))
                            {
                                question.Options.Add(new QuestionOption
                                {
                                    Id = i,
                                    Text = optionText
                                });
                            }
                        }

                        var correctOption = form[$"questions[{index}].CorrectOptionId"].ToString();
                        if (int.TryParse(correctOption, out int correctId))
                        {
                            question.CorrectOptionId = correctId;
                        }
                    }
                    else if ((int)question.Type == 1)
                    {
                        var answers = form[$"questions[{index}].AcceptableAnswers"].ToString();
                        if (!string.IsNullOrEmpty(answers))
                        {
                            question.AcceptableAnswers = answers.Split(';')
                                .Where(a => !string.IsNullOrWhiteSpace(a))
                                .Select(a => a.Trim())
                                .ToList();
                        }

                        var isCaseStr = form[$"questions[{index}].IsCaseSensitive"].ToString();
                        question.IsCaseSensitive = isCaseStr.ToLower() == "true" || isCaseStr == "on";
                    }

                    questions.Add(question);
                }
            }

            return questions;
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public IActionResult AddQuestion([FromBody] TestQuestion question)
        {
            if (question == null) return Json(new { success = false, error = "Невалиден въпрос" });
            if (!question.IsValid()) return Json(new { success = false, error = "Невалидни данни за въпроса" });

            question.Id = DateTime.Now.Millisecond + new Random().Next(1000);
            return Json(new { success = true, question = question, message = "Въпросът е добавен успешно!" });
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            var tests = await _context.Tests
                .Include(t => t.Book)
                .ThenInclude(b => b.Author)
                .Where(t => t.CreatedByUserId == user.Id || User.IsInRole("Admin"))
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return View(tests);
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> TestStatistics(int id)
        {
            var test = await _context.Tests.Include(t => t.Book).FirstOrDefaultAsync(t => t.Id == id);
            if (test == null) return NotFound();

            var results = await _context.TestResults
                .Include(r => r.User)
                .Where(r => r.TestId == id)
                .OrderByDescending(r => r.Score)
                .ToListAsync();

            var statistics = new TestStatistics
            {
                Test = test,
                Results = results,
                AverageScore = results.Any() ? results.Average(r => r.Score) : 0,
                HighestScore = results.Any() ? results.Max(r => r.Score) : 0,
                LowestScore = results.Any() ? results.Min(r => r.Score) : 0,
                PassRate = results.Any() ? (double)results.Count(r => r.Score >= test.PassingScore) / results.Count * 100 : 0
            };

            return View(statistics);
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var test = await _context.Tests.Include(t => t.Book).FirstOrDefaultAsync(t => t.Id == id);
            if (test == null) return NotFound();

            ViewBag.Books = await _context.Books.Include(b => b.Author).OrderBy(b => b.Title).ToListAsync();
            return View(test);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Edit(int id, Test test)
        {
            if (id != test.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(test);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Тестът беше актуализиран успешно!";
                    return RedirectToAction(nameof(Manage));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TestExists(test.Id)) return NotFound();
                    else throw;
                }
            }

            ViewBag.Books = await _context.Books.Include(b => b.Author).OrderBy(b => b.Title).ToListAsync();
            return View(test);
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var test = await _context.Tests.Include(t => t.Book).FirstOrDefaultAsync(t => t.Id == id);
            if (test == null) return NotFound();

            return View(test);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var test = await _context.Tests.FindAsync(id);
            if (test != null)
            {
                _context.Tests.Remove(test);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Тестът беше изтрит успешно!";
            }
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var test = await _context.Tests.FindAsync(id);
            if (test != null)
            {
                test.IsPublished = !test.IsPublished;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Тестът беше {(test.IsPublished ? "публикуван" : "скрит")} успешно!";
            }
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTest(int id)
        {
            var test = await _context.Tests
                .Include(t => t.TestResults)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test != null)
            {
                if (test.TestResults.Any())
                {
                    _context.TestResults.RemoveRange(test.TestResults);
                }

                _context.Tests.Remove(test);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Тестът беше изтрит успешно!";
            }

            return RedirectToAction(nameof(Manage));
        }

        private bool TestExists(int id)
        {
            return _context.Tests.Any(e => e.Id == id);
        }


        // ==========================================================
        // 5. НОВО: МЕТОДИ ЗА ПРОВЕРКА И РАЗДАВАНЕ НА ЗНАЧКИ ЗА ТЕСТОВЕ
        // ==========================================================
        private async Task CheckTestBadges(string userId, double currentPercentage, int currentTestId)
        {
            // 1. Взимаме всички резултати на този потребител (Сортираме по Id вместо по CreatedDate)
            var userResults = await _context.TestResults
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            int totalTestsTaken = userResults.Count;

            // ЗНАЧКА: Отличник (пълен 100%)
            if (currentPercentage == 100)
            {
                await AddBadgeToUser(userId, "Отличник");
            }

            // ЗНАЧКА: Маратонец 
            // Тъй като нямаме поле за дата в TestResult, засега я даваме при достигане на 5 теста общо.
            if (totalTestsTaken == 5)
            {
                await AddBadgeToUser(userId, "Маратонец");
            }

            // ЗНАЧКА: Безупречна серия (Три поредни теста със 100%)
            if (totalTestsTaken >= 3)
            {
                var lastThree = userResults.Take(3).ToList();
                if (lastThree.All(r => r.Percentage == 100))
                {
                    await AddBadgeToUser(userId, "Безупречна серия");
                }
            }

            // ЗНАЧКА: Завръщането (Значително подобрение на същия тест)
            var previousAttempts = userResults.Where(r => r.TestId == currentTestId && r.Id != userResults.First().Id).ToList();
            if (previousAttempts.Any())
            {
                var lowestPreviousScore = previousAttempts.Min(r => r.Percentage);
                if (currentPercentage >= lowestPreviousScore + 30)
                {
                    await AddBadgeToUser(userId, "Завръщането");
                }
            }
        }

        private async Task AddBadgeToUser(string userId, string badgeName)
        {
            // ЗАБЕЛЕЖКА: Името "badgeName" тук трябва да съвпада точно с това в базата ти данни!
            var badge = await _context.Badges.FirstOrDefaultAsync(b => b.Name == badgeName);

            if (badge != null)
            {
                bool alreadyHasIt = await _context.UserBadges
                    .AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badge.Id);

                if (!alreadyHasIt)
                {
                    var userBadge = new UserBadge
                    {
                        UserId = userId,
                        BadgeId = badge.Id
                    };

                    _context.UserBadges.Add(userBadge);
                    await _context.SaveChangesAsync();

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

    public class TestStatistics
    {
        public Test Test { get; set; }
        public List<TestResult> Results { get; set; }
        public double AverageScore { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
        public double PassRate { get; set; }
    }
}