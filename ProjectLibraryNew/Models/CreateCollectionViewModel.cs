using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class CreateCollectionViewModel
    {
        [Required(ErrorMessage = "Името на колекцията е задължително")]
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsPublic { get; set; } = true;
    }
}