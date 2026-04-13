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

        [Required(ErrorMessage = "Съдържанието на анализа е задължително.")]
        [MaxLength(20000, ErrorMessage = "Анализът е прекалено дълъг (максимум 20 000 символа).")]
        [MinLength(10, ErrorMessage = "Анализът трябва да съдържа поне 10 символа.")]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}