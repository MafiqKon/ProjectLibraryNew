using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class BookCollection
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Името на колекцията е задължително")]
        [Display(Name = "Име на колекция")]
        public string Name { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }

        // БЕЗ [Required] - това е ключът
        public string UserId { get; set; }

        // БЕЗ [Required] - това е ключът
        public ApplicationUser User { get; set; }

        [Display(Name = "Публична колекция")]
        public bool IsPublic { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}