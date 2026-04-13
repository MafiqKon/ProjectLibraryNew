using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectLibrary.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Съдържанието на коментара е задължително.")]
        [StringLength(1000, MinimumLength = 2, ErrorMessage = "Коментарът трябва да бъде между 2 и 1000 символа.")]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Рейтингът за оценката
        [Range(0, 5, ErrorMessage = "Рейтингът трябва да бъде между 0 и 5.")]
        public int Rating { get; set; }

        public int BookId { get; set; }
        public Book Book { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int? ParentCommentId { get; set; }
        public Comment ParentComment { get; set; }

        // Списъкът с отговори
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}