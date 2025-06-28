using System.ComponentModel.DataAnnotations;

namespace MyWorkSalary.Models
{
    public class JobProfile
    {
        public int Id { get; set; }

        [Required]
        public string JobTitle { get; set; } = string.Empty;        // "Undersköterska"

        [Required]
        public string Workplace { get; set; } = string.Empty;       // "Karolinska sjukhuset"

        public EmploymentType EmploymentType { get; set; }

        // Grundlön
        public decimal? MonthlySalary { get; set; }         // För fast anställda
        public decimal? HourlyRate { get; set; }            // För vikarier

        // Arbetstid
        public decimal ExpectedHoursPerMonth { get; set; }  // Flexibelt för alla typer

        // Löneperiod
        public PayPeriodType PayPeriodType { get; set; } = PayPeriodType.CalendarMonth;
        public int PayPeriodStartDay { get; set; } = 25;    // Bra att du ändrade till 25!

        // Skattinställningar 
        public TaxCalculationMethod TaxMethod { get; set; } = TaxCalculationMethod.Manual;

        // Manuell metod
        public decimal ManualTaxRate { get; set; } = 0.33m;  // 33% default

        // Automatisk metod (från lönespec)
        public decimal? LastMonthGrossPay { get; set; }
        public decimal? LastMonthTaxDeduction { get; set; }

        // Beräknad skattesats som används
        public decimal EffectiveTaxRate => TaxMethod switch
        {
            TaxCalculationMethod.FromPayslip when LastMonthGrossPay > 0 && LastMonthTaxDeduction > 0
                => (decimal)(LastMonthTaxDeduction / LastMonthGrossPay),
            _ => ManualTaxRate
        };

        // OB-satser
        public List<OBRate> OBRates { get; set; } = new();

        // Metadata
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }

    public enum EmploymentType
    {
        Permanent,      // Fast anställd
        Temporary,      // Vikarie/timanställd  
        OnCall          // Behovsanställd
    }

    public enum PayPeriodType
    {
        CalendarMonth,  // 1-31 (eller 1-30, 1-28)
        CustomPeriod    // t.ex. 20-20
    }

    // NYTT: Enum för skattberäkning
    public enum TaxCalculationMethod
    {
        Manual,        // Användaren anger procent direkt
        FromPayslip    // Beräknas från lönespec-data
    }
}
