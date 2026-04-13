using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class Author
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Името на автора е задължително.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 100 символа.")]
        public string Name { get; set; }

        [StringLength(5000, ErrorMessage = "Биографията не може да надвишава 5000 символа.")]
        public string? Biography { get; set; }

        public DateTime? BirthDate { get; set; }

        [Url(ErrorMessage = "Моля, въведете валиден уеб адрес (URL) за снимката.")]
        [StringLength(500, ErrorMessage = "Линкът към снимката е прекалено дълъг.")]
        public string? ImageUrl { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}