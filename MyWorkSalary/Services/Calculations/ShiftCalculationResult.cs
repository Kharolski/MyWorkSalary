namespace MyWorkSalary.Services.Calculations
{
    public class ShiftCalculationResult
    {
        // Totalt
        public decimal TotalHours { get; set; }
        public decimal RegularHours { get; set; }

        // OB
        public decimal OBHours => EveningHours + NightHours;
        public decimal EveningHours { get; set; }
        public decimal NightHours { get; set; }

        // Ekonomi
        public decimal RegularPay { get; set; }
        public decimal EveningOBRate { get; set; }
        public decimal NightOBRate { get; set; }
        public decimal EveningOBPay { get; set; }
        public decimal NightOBPay { get; set; }
        public decimal OBPay => EveningOBPay + NightOBPay;
        public decimal TotalPay => RegularPay + OBPay;

        // Snapshot (för historik & icons)
        public TimeSpan EveningStart { get; set; }
        public TimeSpan NightStart { get; set; }
        public bool EveningActive { get; set; }
        public bool NightActive { get; set; }
    }
}
