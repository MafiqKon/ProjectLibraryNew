namespace ProjectLibrary.Models
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public UserStatistics Statistics { get; set; }
    }

    // САМО ЕДИН UserStatistics КЛАС - изтрийте всички други дефиниции
    public class UserStatistics
    {
        public int TotalBooksInCollections { get; set; }
        public int TotalCollections { get; set; }
        public int TestsCompleted { get; set; }
        public int TestsScore { get; set; }
        public int AchievementsEarned { get; set; }
        public int CommentsWritten { get; set; }
        public DateTime MemberSince { get; set; }
        public TimeSpan TimeAsMember => DateTime.Now - MemberSince;

        // Детайлна статистика по категории
        public int BooksReadByGenre { get; set; }
        public int BooksReadByPeriod { get; set; }
        public int BooksForMatura { get; set; }

        // Активност
        public DateTime LastActivity { get; set; }
        public int LoginCount { get; set; }
    }

    public class GenreStatistic
    {
        public string Genre { get; set; }
        public int Count { get; set; }
        public int Percentage { get; set; }
    }

    public class PeriodStatistic
    {
        public string Period { get; set; }
        public int Count { get; set; }
    }

    // Добавете след съществуващите класове

    // В края на файла, добавете/актуализирайте TeacherStatistics класа:
    public class TeacherStatistics
    {
        public int TotalStudents { get; set; }
        public int TotalTestsCreated { get; set; }
        public int AverageTestScore { get; set; }
        public int ActiveStudents { get; set; }
        public int TotalBooks { get; set; } // ДОБАВЕНО
        public int MaturaBooks { get; set; } // ДОБАВЕНО
    }

    public class StudentViewModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int CollectionsCount { get; set; }
        public int BooksCount { get; set; }
        public DateTime? LastActivity { get; set; }
    }

    public class BookStatistic
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public string AuthorName { get; set; }
        public int StudentCount { get; set; }
        public int Percentage { get; set; }
    }
}
