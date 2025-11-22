using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContractMonthlyClaimSystem.Models
{
    public class MonthlyClaim
    {
        [Key]
        public int MonthlyClaimId { get; set; }

        [Required]
        public string LecturerId { get; set; }

        [Required]
        [Display(Name = "Hours Worked")]
        [Range(1, 200, ErrorMessage = "Hours worked must be between 1 and 200")]
        public decimal HoursWorked { get; set; }

        [Required]
        [Display(Name = "Hourly Rate")]
        [Range(0, 1000, ErrorMessage = "Hourly rate must be reasonable")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Total Amount")]
        [NotMapped]
        public decimal TotalAmount => HoursWorked * HourlyRate;

        [Required]
        [Display(Name = "Claim Month")]
        public DateTime ClaimMonth { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        [Display(Name = "Additional Notes")]
        [StringLength(500)]
        public string? AdditionalNotes { get; set; }

        [Display(Name = "Supporting Document")]
        public string? SupportingDocumentPath { get; set; }

        [Display(Name = "Original File Name")]
        public string? OriginalFileName { get; set; }

        [Required]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [Display(Name = "Submitted Date")]
        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        [Display(Name = "Reviewed Date")]
        public DateTime? ReviewedDate { get; set; }

        [Display(Name = "Reviewed By")]
        public string? ReviewedBy { get; set; }

        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        // Navigation properties
        public virtual ApplicationUser Lecturer { get; set; }
    }

    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected,
        Paid
    }
}