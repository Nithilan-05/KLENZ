using System.Diagnostics;
using KLENZ.Data;
using KLENZ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KLENZ.Controllers
{
    [Authorize] // Ensure only authenticated users can access this controller
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly KLENZDbContext _context;

        public HomeController(ILogger<HomeController> logger, KLENZDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: Overview for the active financial year
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            var fy = await _context.FinancialYear
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.IsActive == 1);

            if (fy == null)
            {
                return View(model);
            }

            var fyId = fy.Id;
            var today = DateTime.Today;

            model.HasFinancialYear = true;
            model.FinancialYear = fy.FyYear ?? "";
            model.StartDate = fy.StartDate;
            model.EndDate = fy.EndDate;

            // Small result sets: fetch the few columns needed and summarise in memory
            var enquiries = await _context.SalesEnquiries
                .AsNoTracking()
                .Where(s => s.FyYear == fyId)
                .Select(s => new
                {
                    s.Id,
                    s.EnquiryDate,
                    s.EnquiryDetails,
                    s.Status,
                    s.ReminderDate,
                    s.ReminderPlace,
                    s.CreatedDateTime,
                    Company = s.Company != null ? s.Company.ShortName : null
                })
                .ToListAsync();

            var quotations = await _context.QuotationReport
                .AsNoTracking()
                .Where(q => q.FyYear == fyId)
                .Select(q => new { q.QuotationDate, q.QuotationValue })
                .ToListAsync();

            var projects = await _context.ProjectList
                .AsNoTracking()
                .Where(p => p.FyYear == fyId)
                .Select(p => new
                {
                    p.Id,
                    p.WorkOrderDate,
                    p.WorkOrderValue,
                    p.WorkDetails,
                    p.CreatedDateTime,
                    Company = p.Company != null ? p.Company.ShortName : null
                })
                .ToListAsync();

            var tenders = await _context.Project_TenderList
                .AsNoTracking()
                .Where(t => t.FyYear == fyId)
                .Select(t => new
                {
                    t.Id,
                    t.WorkOrderDate,
                    t.WorkDetails,
                    t.Total,
                    t.ProjectCost,
                    t.CreatedDateTime,
                    Company = t.Company != null ? t.Company.ShortName : null
                })
                .ToListAsync();

            model.Enquiries = enquiries.Count;
            model.Quotations = quotations.Count;
            model.QuotedValue = quotations.Sum(q => q.QuotationValue ?? 0m);
            model.Positive = await _context.PositiveEnquiry.CountAsync(p => p.FyYear == fyId);
            model.Projects = projects.Count;
            model.WonValue = projects.Sum(p => p.WorkOrderValue ?? 0m);
            model.Tenders = tenders.Count;
            model.UpcomingTenders = await _context.Project_ToBeTenderList.CountAsync(t => t.FyYear == fyId);
            model.Consultancy = await _context.Project_Consultancy.CountAsync(c => c.FyYear == fyId);

            // Month by month, from the start of the financial year up to the current month
            var lastMonth = today < fy.EndDate ? today : fy.EndDate;
            for (var i = 0; i < 12; i++)
            {
                var month = new DateTime(fy.StartDate.Year, fy.StartDate.Month, 1).AddMonths(i);
                if (month > lastMonth)
                {
                    break;
                }

                model.Months.Add(new DashboardMonth
                {
                    Label = month.ToString("MMM"),
                    Quoted = quotations
                        .Where(q => q.QuotationDate.HasValue
                                    && q.QuotationDate.Value.Year == month.Year
                                    && q.QuotationDate.Value.Month == month.Month)
                        .Sum(q => q.QuotationValue ?? 0m),
                    Won = projects
                        .Where(p => p.WorkOrderDate.HasValue
                                    && p.WorkOrderDate.Value.Year == month.Year
                                    && p.WorkOrderDate.Value.Month == month.Month)
                        .Sum(p => p.WorkOrderValue ?? 0m)
                });
            }

            // Follow-ups: overdue (last 30 days), today, and the next 7 days
            var reminders = enquiries
                .Where(e => e.ReminderDate.HasValue)
                .Select(e => new DashboardReminder
                {
                    Id = e.Id,
                    Company = e.Company ?? "Enquiry",
                    Details = e.EnquiryDetails ?? "",
                    Place = e.ReminderPlace ?? "",
                    Date = e.ReminderDate!.Value.Date
                })
                .OrderBy(r => r.Date)
                .ToList();

            model.Overdue = reminders.Where(r => r.Date < today && r.Date >= today.AddDays(-30)).ToList();
            model.DueToday = reminders.Where(r => r.Date == today).ToList();
            model.Upcoming = reminders.Where(r => r.Date > today && r.Date <= today.AddDays(7)).ToList();

            model.RecentEnquiries = enquiries
                .OrderByDescending(e => e.CreatedDateTime)
                .Take(6)
                .Select(e => new DashboardItem
                {
                    Id = e.Id,
                    Title = e.Company ?? "Enquiry",
                    Subtitle = e.EnquiryDetails ?? "",
                    Status = e.Status ?? "",
                    Date = e.EnquiryDate
                })
                .ToList();

            model.RecentProjects = projects
                .OrderByDescending(p => p.CreatedDateTime)
                .Take(5)
                .Select(p => new DashboardItem
                {
                    Id = p.Id,
                    Title = p.Company ?? "Project",
                    Subtitle = p.WorkDetails ?? "",
                    Date = p.WorkOrderDate,
                    Amount = p.WorkOrderValue
                })
                .ToList();

            model.RecentTenders = tenders
                .OrderByDescending(t => t.CreatedDateTime)
                .Take(5)
                .Select(t => new DashboardItem
                {
                    Id = t.Id,
                    Title = t.Company ?? "Tender",
                    Subtitle = t.WorkDetails ?? "",
                    Date = t.WorkOrderDate,
                    Amount = t.Total ?? t.ProjectCost
                })
                .ToList();

            return View(model);
        }

        // GET: Home/Reminders — follow-ups for the bell in the top bar
        [HttpGet]
        public async Task<IActionResult> Reminders()
        {
            var today = DateTime.Today;
            var from = today.AddDays(-30);
            var until = today.AddDays(8);

            var rows = await _context.SalesEnquiries
                .AsNoTracking()
                .Where(s => s.ReminderDate != null && s.ReminderDate >= from && s.ReminderDate < until)
                .OrderBy(s => s.ReminderDate)
                .Take(30)
                .Select(s => new
                {
                    s.Id,
                    s.ReminderDate,
                    s.ReminderPlace,
                    s.EnquiryDetails,
                    Company = s.Company != null ? s.Company.ShortName : null
                })
                .ToListAsync();

            var result = rows.Select(r =>
            {
                var date = r.ReminderDate!.Value.Date;
                return new
                {
                    id = r.Id,
                    company = r.Company,
                    details = r.EnquiryDetails,
                    place = r.ReminderPlace,
                    when = date.ToString("dd MMM"),
                    state = date < today ? "over" : date == today ? "today" : "soon"
                };
            });

            return Json(result);
        }

        // GET: Home/Search?q= — enquiries for the search box (Ctrl + K)
        [HttpGet]
        public async Task<IActionResult> Search(string? q)
        {
            q = (q ?? "").Trim();
            if (q.Length < 2)
            {
                return Json(Array.Empty<object>());
            }

            var rows = await _context.SalesEnquiries
                .AsNoTracking()
                .Where(s => (s.EnquiryDetails != null && s.EnquiryDetails.Contains(q))
                            || (s.CustomerDetails != null && s.CustomerDetails.Contains(q))
                            || (s.Company != null && (s.Company.ShortName.Contains(q) || s.Company.FullName.Contains(q))))
                .OrderByDescending(s => s.CreatedDateTime)
                .Take(8)
                .Select(s => new
                {
                    id = s.Id,
                    company = s.Company != null ? s.Company.ShortName : null,
                    details = s.EnquiryDetails,
                    status = s.Status
                })
                .ToListAsync();

            return Json(rows);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
