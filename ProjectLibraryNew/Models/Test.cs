using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ProjectLibrary.Models
{
    public class Test
    {
        public int Id { get; set; }

        public int? BookId { get; set; }
        public Book Book { get; set; }

        [Required(ErrorMessage = "Заглавието на теста е задължително.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Заглавието трябва да е между 3 и 150 символа.")]
        [Display(Name = "Заглавие")]
        public string Title { get; set; }

        [StringLength(1500, ErrorMessage = "Описанието не може да надвишава 1500 символа.")]
        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Времето за решаване е задължително.")]
        [Range(5, 180, ErrorMessage = "Времето трябва да е между 5 и 180 минути (3 часа).")]
        [Display(Name = "Време (минути)")]
        public int TimeLimitMinutes { get; set; } = 45;

        [Required(ErrorMessage = "Общият брой точки е задължителен.")]
        [Range(1, 500, ErrorMessage = "Точките трябва да са между 1 и 500.")]
        [Display(Name = "Общо точки")]
        public int TotalPoints { get; set; } = 100;

        [Required(ErrorMessage = "Полето за минимален успех е задължително.")]
        [Range(0, 100, ErrorMessage = "Процентът за успех трябва да е между 0 и 100.")]
        [Display(Name = "Минимален успех (%)")]
        public int PassingScore { get; set; } = 60;

        public string QuestionsJson { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedByUserId { get; set; }
        public bool IsPublished { get; set; } = false;

        public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();

        public List<TestQuestion> GetQuestions()
        {
            if (string.IsNullOrEmpty(QuestionsJson))
                return new List<TestQuestion>();

            return JsonSerializer.Deserialize<List<TestQuestion>>(QuestionsJson) ?? new List<TestQuestion>();
        }

        public void SetQuestions(List<TestQuestion> questions)
        {
            QuestionsJson = JsonSerializer.Serialize(questions);
        }

        public bool IsValid()
        {
            var questions = GetQuestions();
            return questions.Any() && questions.All(q => q.IsValid());
        }

        public int GetQuestionCount()
        {
            return GetQuestions().Count;
        }

        public int GetTotalPossiblePoints()
        {
            return GetQuestions().Sum(q => q.Points);
        }
    }

    public class TestQuestion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 2, ErrorMessage = "Текстът на въпроса трябва да е между 2 и 1000 символа.")]
        public string QuestionText { get; set; }

        [Required]
        public QuestionType Type { get; set; }

        [Range(1, 100, ErrorMessage = "Точките за въпрос трябва да са между 1 и 100.")]
        public int Points { get; set; } = 1;

        public List<QuestionOption> Options { get; set; } = new List<QuestionOption>();
        public int CorrectOptionId { get; set; }

        public List<string> AcceptableAnswers { get; set; } = new List<string>();
        public bool IsCaseSensitive { get; set; } = false;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(QuestionText) || Points <= 0)
                return false;

            if (Type == QuestionType.MultipleChoice)
            {
                return Options != null && Options.Count == 4 &&
                       Options.All(o => !string.IsNullOrWhiteSpace(o.Text)) &&
                       CorrectOptionId >= 1 && CorrectOptionId <= 4;
            }
            else if (Type == QuestionType.OpenEnded)
            {
                return AcceptableAnswers != null &&
                       AcceptableAnswers.Any(a => !string.IsNullOrWhiteSpace(a));
            }
            else // Matching
            {
                return Options != null && Options.Count > 0 &&
                       Options.All(o => o.Text.Contains("|"));
            }
        }

        public string GetCorrectAnswerText()
        {
            if (Type == QuestionType.MultipleChoice)
            {
                return Options?.FirstOrDefault(o => o.Id == CorrectOptionId)?.Text ?? "";
            }
            else if (Type == QuestionType.OpenEnded)
            {
                return string.Join(" или ", AcceptableAnswers ?? new List<string>());
            }
            else // Matching
            {
                return string.Join("; ", Options?.Select(o => o.Text) ?? new List<string>());
            }
        }
    }

    public class QuestionOption
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Text { get; set; }
    }

    public enum QuestionType
    {
        MultipleChoice = 0,
        OpenEnded = 1,
        Matching = 2
    }

    public class TestResult
    {
        public int Id { get; set; }

        [Required]
        public int TestId { get; set; }
        public Test Test { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Range(0, 1000)]
        public int Score { get; set; }

        [Range(1, 1000)]
        public int TotalQuestions { get; set; }

        [Range(0, 1000)]
        public int CorrectAnswers { get; set; }

        [Range(0, 100, ErrorMessage = "Процентът трябва да е между 0 и 100.")]
        public double Percentage { get; set; }

        public DateTime CompletedAt { get; set; } = DateTime.Now;
        public TimeSpan TimeSpent { get; set; }

        public string ResultDetailsJson { get; set; }

        public List<QuestionResult> GetResultDetails()
        {
            if (string.IsNullOrEmpty(ResultDetailsJson))
                return new List<QuestionResult>();

            return JsonSerializer.Deserialize<List<QuestionResult>>(ResultDetailsJson) ?? new List<QuestionResult>();
        }

        public void SetResultDetails(List<QuestionResult> details)
        {
            ResultDetailsJson = JsonSerializer.Serialize(details);
        }
    }

    public class QuestionResult
    {
        public int QuestionId { get; set; }
        public bool IsCorrect { get; set; }

        [StringLength(2000)]
        public string UserAnswer { get; set; } = string.Empty;

        [StringLength(2000)]
        public string CorrectAnswer { get; set; } = string.Empty;

        [Range(0, 100)]
        public int PointsEarned { get; set; }

        [Range(1, 100)]
        public int MaxPoints { get; set; }
    }
}