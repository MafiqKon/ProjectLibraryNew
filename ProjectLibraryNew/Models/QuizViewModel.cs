using System.Collections.Generic;

namespace ProjectLibrary.Models
{
    public class QuizViewModel
    {
        public int QuizId { get; set; }
        public string BookTitle { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
        public int Score { get; set; }
        public bool IsSubmitted { get; set; }
    }

    public class QuestionViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; }

        // НОВИ ПОЛЕТА ЗА ОТВОРЕНИ ВЪПРОСИ
        public bool IsOpenEnded { get; set; }
        public string UserTextAnswer { get; set; } // Това, което ученикът е написал
        public List<string> AcceptableAnswers { get; set; } = new List<string>(); // Верните думи

        public List<AnswerOption> Options { get; set; } = new List<AnswerOption>();
        public int CorrectOptionId { get; set; }
        public int? SelectedOptionId { get; set; }

        public bool? IsAnswerCorrect { get; set; } // Помощно поле за View-то
    }

    public class AnswerOption
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }
}