//using MyWorkSalary.Models.Enums;
//using MyWorkSalary.Models.Specialized;
//using SQLite;
//using System.ComponentModel.DataAnnotations;

//namespace MyWorkSalary.Models
//{
//    public class WorkShift
//    {
//        [PrimaryKey, AutoIncrement]
//        public int Id { get; set; }

//        public int JobProfileId { get; set; }               // Kopplar till JobProfile

//        public DateTime? StartTime { get; set; }             
//        public DateTime? EndTime { get; set; }               

//        [Required]
//        public DateTime ShiftDate { get; set; }             // Datum för passet/perioden
//        public ShiftType ShiftType { get; set; }

//        public int? NumberOfDays { get; set; }          // Antal dagar för Semester/Sjuk

//        // === SJUK-SPECIFIKA FÄLT - SKA BORT (FLYTAT TILL SICKLEAVE) ===
//        public decimal? SickPayPercentage { get; set; }     // <-- SKA BORT (FLYTAT SEPARAT)
//        public bool IsKarensDay { get; set; }               // <-- SKA BORT (FLYTAT SEPARAT)
//        public decimal? WeeklyHoursUsed { get; set; }       // <-- SKA BORT (FLYTAT SEPARAT)
//        public decimal? HourlyRateUsed { get; set; }        // <-- SKA BORT (FLYTAT SEPARAT)
//        public decimal? WeeklyEarningsUsed { get; set; }    // <-- SKA BORT (FLYTAT SEPARAT)
//        public decimal? KarensDeduction { get; set; }       // <-- SKA BORT (FLYTAT SEPARAT)
//        public decimal? DailySickEarnings { get; set; }     // <-- SKA BORT (FLYTAT SEPARAT)
//        public int? SickPeriodId { get; set; }              // <-- SKA BORT (FLYTAT SEPARAT)
//        public bool IsRecurrentSickPeriod { get; set; } = false; // <-- SKA BORT (FLYTAT SEPARAT)

//        // Beräknade värden (sparas för snabbhet)
//        public decimal TotalHours { get; set; }             
//        public decimal RegularHours { get; set; }           // Vanliga arbetstimmar
//        public decimal SickHours { get; set; } = 0;         // <-- SKA BORT (FLYTAT SEPARAT)
//        public int BreakMinutes { get; set; } = 0;          // Rast i minuter
//        public decimal OBHours { get; set; }                // OB-timmar totalt

//        // Ekonomi
//        public decimal RegularPay { get; set; }             // Grundlön för timmarna
//        public decimal SickPay { get; set; } = 0;           // <-- SKA BORT (FLYTAT SEPARAT)
//        public decimal OBPay { get; set; }                  // Totalt OB-tillägg
//        public decimal TotalPay { get; set; }               // Summa

//        // Detaljerad OB-uppdelning (JSON eller separata tabeller)
//        public string OBBreakdown { get; set; } = string.Empty;  // JSON med detaljer

//        // Metadata
//        public string? Notes { get; set; }                  // Anteckningar från användaren
//        public DateTime CreatedDate { get; set; } = DateTime.Now;
//        public DateTime? ModifiedDate { get; set; }
//        public bool IsConfirmed { get; set; } = false;     // Användaren har verifierat

//        #region Navigation Properties (NYA)

//        /// <summary>
//        /// Sjukskrivningsdata (bara om ShiftType = SickLeave)
//        /// </summary>
//        [Ignore]
//        public SickLeave? SickLeave { get; set; }

//        /// <summary>
//        /// VAB-data (bara om ShiftType = VAB) - kommer senare
//        /// </summary>
//        //[Ignore]
//        //public VABLeave? VABLeave { get; set; }

//        /// <summary>
//        /// Semester-data (bara om ShiftType = Vacation) - kommer senare
//        /// </summary>
//        //[Ignore]
//        //public VacationLeave? VacationLeave { get; set; }

//        /// <summary>
//        /// Jour-data (bara om ShiftType = OnCall) - kommer senare
//        /// </summary>
//        //[Ignore]
//        //public OnCallShift? OnCallShift { get; set; }

//        #endregion
//    }

//}
