using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Models.Reports
{
    public class ObDetails
    {
        public DateTime Date { get; set; }           // Vilket datum skiftet var
        public decimal Hours { get; set; }           // Antal OB-timmar
        public decimal RatePerHour { get; set; }     // OB-taxa
        public OBCategory Category { get; set; }     // OB-kategori
        public decimal Pay { get; set; }             // OB-lön

        // Översatt kategori (inte i originalmodellen)
        public string CategoryName { get; set; }
    }
}
