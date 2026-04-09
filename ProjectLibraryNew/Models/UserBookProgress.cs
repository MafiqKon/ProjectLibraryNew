using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class UserBookProgress
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        public int BookId { get; set; }
        public virtual Book Book { get; set; }

        // 30% от прогреса
        public bool IsTextRead { get; set; } = false;

        // 30% от прогреса
        public bool IsAnalysisRead { get; set; } = false;

        // 40% от прогреса (ще се отбелязва автоматично при предаване на успешен тест)
        public bool HasPassedTest { get; set; } = false;
    }
}