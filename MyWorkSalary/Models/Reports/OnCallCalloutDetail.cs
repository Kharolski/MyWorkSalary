namespace MyWorkSalary.Models.Reports
{
    public class OnCallCalloutDetail
    {
        public DateTime Date { get; set; }  // för grouping/visning
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public decimal Hours { get; set; }
        public string? Notes { get; set; }

        public decimal ActivePay { get; set; }
    }
}
