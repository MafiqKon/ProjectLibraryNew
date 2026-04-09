using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class Author
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Biography { get; set; }
        public DateTime? BirthDate { get; set; }

        // НОВО: Поле за линк към снимка на автора
        public string? ImageUrl { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}