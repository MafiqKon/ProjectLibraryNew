using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectLibrary.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Рейтингът за оценката
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