using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace ProjectLibrary.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public UserType UserType { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        // Това е новото поле за снимката - ТО Е ОК
        public string ProfilePictureUrl { get; set; }

        // ЗАКОМЕНТИРАЙ ТЕЗИ РЕДОВЕ С ДВЕ НАКЛОНЕНИ ЧЕРТИ, АКО СВЕТЯТ В ЧЕРВЕНО:
         public virtual ICollection<UserBadge> UserBadges { get; set; }
         public virtual ICollection<BookCollection> Collections { get; set; }
        // Флаг, който показва дали профилът (особено учителският) чака одобрение от Админ
        public bool IsPendingApproval { get; set; } = false;
    }

    public enum UserType
    {
        Student,
        Teacher,
        Admin
    }
}