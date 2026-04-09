using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }

        // НОВО: Тук ще се съхранява целият текст на произведението!
        public string? FullText { get; set; }

        [Required]
        public int AuthorId { get; set; }
        public Author Author { get; set; }

        public string? Genre { get; set; }
        public int PublishedYear { get; set; }
        public int StudyYear { get; set; }
        public bool IsForMatura { get; set; }
        public string? WritingPeriod { get; set; }
        public string? Tags { get; set; }

        public ICollection<Analysis> Analyses { get; set; } = new List<Analysis>();
        public ICollection<Test> Tests { get; set; } = new List<Test>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<BookCollection> BookCollections { get; set; } = new List<BookCollection>();
    }
}