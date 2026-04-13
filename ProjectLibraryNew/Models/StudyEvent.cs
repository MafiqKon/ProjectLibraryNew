using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class StudyEvent
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required(ErrorMessage = "Заглавието на събитието е задължително.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Заглавието трябва да бъде между 2 и 100 символа.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Датата на събитието е задължителна.")]
        public DateTime EventDate { get; set; }

        [StringLength(500, ErrorMessage = "Бележките не могат да надвишават 500 символа.")]
        public string Description { get; set; }

        public int? BookId { get; set; }
        public virtual Book Book { get; set; }

        [StringLength(20)]
        public string EventType { get; set; } = "General";

        public bool IsCompleted { get; set; } = false;
    }
}