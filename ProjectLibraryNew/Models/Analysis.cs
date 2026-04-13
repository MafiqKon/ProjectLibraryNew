using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class Analysis
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; }

        [Required(ErrorMessage = "Заглавието на анализа е задължително.")]
        [StringLength(150, ErrorMessage = "Заглавието не може да надвишава 150 символа.")]
        public string Title { get; set; } // НОВО: Поле за заглавие

        [Required(ErrorMessage = "Съдържанието на анализа е задължително.")]
        [MaxLength(100000, ErrorMessage = "Анализът е прекалено дълъг (максимум 100 000 символа).")]
        [MinLength(10, ErrorMessage = "Анализът трябва да съдържа поне 10 символа.")]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}