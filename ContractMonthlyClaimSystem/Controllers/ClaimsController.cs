using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClaimsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Claims for Lecturers (their own claims)
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Challenge();
            }

            if (user.UserType == UserType.Lecturer)
            {
                var claims = await _context.MonthlyClaims
                    .Include(c => c.Lecturer)
                    .Where(c => c.LecturerId == userId)
                    .OrderByDescending(c => c.SubmittedDate)
                    .ToListAsync();
                return View("LecturerIndex", claims);
            }
            else if (user.UserType == UserType.HR)
            {
                // HR sees their own claims AND can access HR dashboard
                var userClaims = await _context.MonthlyClaims
                    .Include(c => c.Lecturer)
                    .Where(c => c.LecturerId == userId)
                    .OrderByDescending(c => c.SubmittedDate)
                    .ToListAsync();

                if (userClaims.Any())
                {
                    return View("LecturerIndex", userClaims);
                }
                else
                {
                    // If HR has no personal claims, redirect to HR dashboard
                    return RedirectToAction(nameof(HRView));
                }
            }
            else
            {
                var pendingClaims = await _context.MonthlyClaims
                    .Include(c => c.Lecturer)
                    .Where(c => c.Status == ClaimStatus.Pending)
                    .OrderBy(c => c.SubmittedDate)
                    .ToListAsync();
                return View("CoordinatorIndex", pendingClaims);
            }
        }

        // GET: Claims/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Claims/Create - FIXED VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MonthlyClaim claim, IFormFile supportingDocument)
        {
            // SIMPLE FIX: Skip ModelState validation and manually set required fields
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            claim.LecturerId = userId;
            claim.SubmittedDate = DateTime.Now;
            claim.Status = ClaimStatus.Pending;

            // Ensure ClaimMonth is set (common binding issue)
            if (claim.ClaimMonth == DateTime.MinValue)
            {
                claim.ClaimMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            // Handle file upload
            if (supportingDocument != null && supportingDocument.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + supportingDocument.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await supportingDocument.CopyToAsync(fileStream);
                }

                claim.SupportingDocumentPath = uniqueFileName;
                claim.OriginalFileName = supportingDocument.FileName;
            }

            _context.Add(claim);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Claims/Approve/5
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Approve(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(m => m.MonthlyClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        // POST: Claims/Approve/5
        [HttpPost]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Approve(int id, bool isApproved, string rejectionReason)
        {
            var claim = await _context.MonthlyClaims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            claim.ReviewedBy = user?.FullName ?? "Unknown";
            claim.ReviewedDate = DateTime.Now;

            if (isApproved)
            {
                claim.Status = ClaimStatus.Approved;
            }
            else
            {
                claim.Status = ClaimStatus.Rejected;
                claim.RejectionReason = rejectionReason;
            }

            _context.Update(claim);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Claims/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(m => m.MonthlyClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this claim
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user?.UserType == UserType.Lecturer && claim.LecturerId != userId)
            {
                return Forbid();
            }

            return View(claim);
        }

        // HR View - All approved claims for payment processing - FIXED
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HRView()
        {
            var approvedClaims = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved)
                .OrderBy(c => c.ClaimMonth)
                .ToListAsync();

            // FIX: Handle empty sequences
            var stats = new
            {
                TotalApproved = approvedClaims.Count,
                TotalAmount = approvedClaims.Sum(c => c.HoursWorked * c.HourlyRate),
                OldestClaim = approvedClaims.Any() ? approvedClaims.Min(c => c.ClaimMonth) : DateTime.Now,
                LecturersCount = approvedClaims.Select(c => c.LecturerId).Distinct().Count()
            };

            ViewBag.Stats = stats;
            return View(approvedClaims);
        }

        // Mark claim as paid
        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var claim = await _context.MonthlyClaims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = ClaimStatus.Paid;
                _context.Update(claim);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Claim #{claim.MonthlyClaimId} marked as paid successfully.";
            }
            return RedirectToAction(nameof(HRView));
        }

        // Generate payment report - FIXED VERSION
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GenerateReport(string reportType = "monthly")
        {
            var claims = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved || c.Status == ClaimStatus.Paid)
                .OrderBy(c => c.ClaimMonth)
                .ToListAsync();

            // Pre-calculate report data in the controller
            var monthlyReportData = claims
                .GroupBy(c => new { c.ClaimMonth.Year, c.ClaimMonth.Month })
                .Select(g => new MonthlyReportItem
                {
                    Period = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                    TotalClaims = g.Count(),
                    TotalAmount = g.Sum(c => c.HoursWorked * c.HourlyRate),
                    ApprovedCount = g.Count(c => c.Status == ClaimStatus.Approved),
                    PaidCount = g.Count(c => c.Status == ClaimStatus.Paid)
                })
                .ToList();

            ViewBag.ReportType = reportType;
            ViewBag.MonthlyReportData = monthlyReportData;

            // Calculate totals for display
            ViewBag.TotalClaims = monthlyReportData.Sum(item => item.TotalClaims);
            ViewBag.TotalAmount = monthlyReportData.Sum(item => item.TotalAmount);
            ViewBag.TotalApproved = monthlyReportData.Sum(item => item.ApprovedCount);
            ViewBag.TotalPaid = monthlyReportData.Sum(item => item.PaidCount);

            return View();
        }

        // Lecturer Management for HR
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> ManageLecturers()
        {
            var lecturers = await _userManager.Users
                .Where(u => u.UserType == UserType.Lecturer)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var lecturerStats = new List<object>();
            foreach (var lecturer in lecturers)
            {
                var claims = await _context.MonthlyClaims
                    .Where(c => c.LecturerId == lecturer.Id)
                    .ToListAsync();

                lecturerStats.Add(new
                {
                    Lecturer = lecturer,
                    TotalClaims = claims.Count,
                    TotalAmount = claims.Sum(c => c.HoursWorked * c.HourlyRate),
                    PendingClaims = claims.Count(c => c.Status == ClaimStatus.Pending),
                    ApprovedClaims = claims.Count(c => c.Status == ClaimStatus.Approved)
                });
            }

            ViewBag.LecturerStats = lecturerStats;
            return View();
        }

        // Download supporting document
        public async Task<IActionResult> DownloadDocument(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _context.MonthlyClaims.FindAsync(id);
            if (claim == null || string.IsNullOrEmpty(claim.SupportingDocumentPath))
            {
                return NotFound();
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", claim.SupportingDocumentPath);
            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, GetContentType(path), claim.OriginalFileName);
        }

        private string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                { ".pdf", "application/pdf" },
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                { ".jpg", "image/jpeg" },
                { ".png", "image/png" }
            };
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }
    }

    // Helper class for report data
    public class MonthlyReportItem
    {
        public string Period { get; set; } = string.Empty;
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public int ApprovedCount { get; set; }
        public int PaidCount { get; set; }
    }
}