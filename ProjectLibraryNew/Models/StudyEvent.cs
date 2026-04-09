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

        [Required]
        [StringLength(100)]
        public string Title { get; set; } // Напр. "Подготовка за класно" или "Железният светилник"

        [Required]
        public DateTime EventDate { get; set; } // На коя дата е събитието

        public string Description { get; set; } // По желание: допълнителни бележки

        // Връзка с произведение (ако събитието е вързано с конкретна книга)
        public int? BookId { get; set; }
        public virtual Book Book { get; set; }

        // Тип на събитието (за да го оцветяваме различно)
        // Може да бъде: "Reading" (Четене), "Analysis" (Анализ), "Test" (Тест), "General" (Общо)
        public string EventType { get; set; } = "General";

        // Дали ученикът го е изпълнил (ако е вързано с прогреса, ще се отмята автоматично!)
        public bool IsCompleted { get; set; } = false;
    }
}