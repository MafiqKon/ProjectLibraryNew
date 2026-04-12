using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ProjectLibrary.Models
{
    public class Test
    {
        public int Id { get; set; }

        [Required]
        public int? BookId { get; set; }
        public Book Book { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public int TimeLimitMinutes { get; set; } = 45;
        public int TotalPoints { get; set; } = 100;
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
        public string QuestionText { get; set; }
        public QuestionType Type { get; set; }
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
            else
            {
                return AcceptableAnswers != null &&
                       AcceptableAnswers.Any(a => !string.IsNullOrWhiteSpace(a));
            }
        }

        public string GetCorrectAnswerText()
        {
            if (Type == QuestionType.MultipleChoice)
            {
                return Options?.FirstOrDefault(o => o.Id == CorrectOptionId)?.Text ?? "";
            }
            else
            {
                return string.Join(" или ", AcceptableAnswers ?? new List<string>());
            }
        }
    }

    public class QuestionOption
    {
        public int Id { get; set; }
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

        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
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
        public string UserAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public int PointsEarned { get; set; }
        public int MaxPoints { get; set; }
    }
}