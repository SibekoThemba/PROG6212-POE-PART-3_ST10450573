using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "User Type")]
        public UserType UserType { get; set; } = UserType.Lecturer;

        // Navigation properties
        public virtual ICollection<MonthlyClaim> Claims { get; set; } = new List<MonthlyClaim>();
    }

    public enum UserType
    {
        Lecturer,
        ProgrammeCoordinator,
        AcademicManager,
        HR
    }
}