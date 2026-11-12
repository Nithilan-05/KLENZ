namespace KLENZ.Models
{
    /// <summary>Everything the Overview page shows, for the active financial year.</summary>
    public class DashboardViewModel
    {
        public bool HasFinancialYear { get; set; }
        public string FinancialYear { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int Enquiries { get; set; }
        public int Quotations { get; set; }
        public int Positive { get; set; }
        public int Projects { get; set; }
        public int Tenders { get; set; }
        public int UpcomingTenders { get; set; }
        public int Consultancy { get; set; }

        public decimal QuotedValue { get; set; }
        public decimal WonValue { get; set; }

        public List<DashboardMonth> Months { get; set; } = new();

        public List<DashboardReminder> Overdue { get; set; } = new();
        public List<DashboardReminder> DueToday { get; set; } = new();
        public List<DashboardReminder> Upcoming { get; set; } = new();

        public List<DashboardItem> RecentEnquiries { get; set; } = new();
        public List<DashboardItem> RecentProjects { get; set; } = new();
        public List<DashboardItem> RecentTenders { get; set; } = new();
    }

    public class DashboardMonth
    {
        public string Label { get; set; } = "";
        public decimal Quoted { get; set; }
        public decimal Won { get; set; }
    }

    public class DashboardReminder
    {
        public int Id { get; set; }
        public string Company { get; set; } = "";
        public string Details { get; set; } = "";
        public string Place { get; set; } = "";
        public DateTime Date { get; set; }
    }

    public class DashboardItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? Date { get; set; }
        public decimal? Amount { get; set; }
    }
}
