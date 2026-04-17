using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Заглавието на произведението е задължително.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Заглавието трябва да е между 1 и 200 символа.")]
        public string Title { get; set; }

        [StringLength(2000, ErrorMessage = "Описанието не може да надвишава 2000 символа.")]
        public string? Description { get; set; }

        [MaxLength(100000, ErrorMessage = "Текстът на произведението е прекалено дълъг.")]
        public string? FullText { get; set; }

        [Required(ErrorMessage = "Изборът на автор е задължителен.")]
        public int AuthorId { get; set; }
        public Author Author { get; set; }

        [StringLength(50, ErrorMessage = "Жанрът не може да надвишава 50 символа.")]
        public string? Genre { get; set; }

        [Range(800, 2100, ErrorMessage = "Годината на издаване трябва да е между 800 и 2100.")]
        public int PublishedYear { get; set; }

        [Range(1, 12, ErrorMessage = "Класът трябва да е между 1 и 12.")]
        public int StudyYear { get; set; }

        public bool IsForMatura { get; set; }

        [StringLength(100, ErrorMessage = "Периодът не може да надвишава 100 символа.")]
        public string? WritingPeriod { get; set; }

        [StringLength(200, ErrorMessage = "Таговете не могат да надвишават 200 символа.")]
        public string? Tags { get; set; }

        // --- ПОЛЕТА ЗА POP-UP ---
        public bool HasPopup { get; set; }

        [StringLength(5000, ErrorMessage = "Текстът в прозореца е прекалено дълъг.")]
        public string? PopupContent { get; set; }

        public bool HasPopupLink { get; set; }

        [StringLength(500)]
        public string? PopupLinkUrl { get; set; }

        public ICollection<Analysis> Analyses { get; set; } = new List<Analysis>();
        public ICollection<Test> Tests { get; set; } = new List<Test>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<BookCollection> BookCollections { get; set; } = new List<BookCollection>();
    }
}