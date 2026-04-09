using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectLibrary.Models
{
    public class Badge
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } // Пример: "Коментатор"

        public string Description { get; set; } // Пример: "Написа 5 коментара"

        public string IconUrl { get; set; } // Линк към иконка или име на файл

        // Връзка: Една значка може да я имат много хора
        public virtual ICollection<UserBadge> UserBadges { get; set; }
    }
}