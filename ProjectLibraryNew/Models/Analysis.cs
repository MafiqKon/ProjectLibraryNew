using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class Analysis
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; }

        [Required]
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}