using SQLite;
using System.ComponentModel.DataAnnotations;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Core;

namespace MyWorkSalary.Models.Specialized
{
    /// <summary>
    /// Specialiserad modell för sjukskrivningar
    /// Innehåller alla sjuk-specifika fält och frysta beräkningsvärden
    /// </summary>
    public class SickLeave
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Required]
        public int WorkShiftId { get; set; }  // FK till WorkShift

        #region Sjuk-specifik information

        /// <summary>
        /// Typ av sjukdag (jobbat delvis, skulle jobbat, skulle varit ledig)
        /// </summary>
        public SickLeaveType SickType { get; set; }

        /// <summary>
        /// ID för att gruppera sjukdagar i samma period (inom 5 dagar)
        /// </summary>
        public int? SickPeriodId { get; set; }

        /// <summary>
        /// Om detta är återinsjuknande inom 5 dagar (ingen ny karens)
        /// </summary>
        public bool IsRecurrentSickPeriod { get; set; } = false;

        #endregion

        #region Frysta beräkningsvärden (sparas vid sjukperiodens start)

        /// <summary>
        /// Genomsnittliga arbetstimmar per vecka (från 13 veckor eller användarinput)
        /// </summary>
        public decimal? WeeklyHoursUsed { get; set; }

        /// <summary>
        /// Timlön som användes för beräkning
        /// </summary>
        public decimal? HourlyRateUsed { get; set; }

        /// <summary>
        /// Veckoersättning som användes (WeeklyHours × HourlyRate)
        /// </summary>
        public decimal? WeeklyEarningsUsed { get; set; }

        /// <summary>
        /// Karensavdrag för denna sjukperiod (20% av veckoersättning i sjuklön)
        /// </summary>
        public decimal? KarensDeduction { get; set; }

        /// <summary>
        /// Daglig sjuklön efter karensavdrag
        /// </summary>
        public decimal? DailySickEarnings { get; set; }

        #endregion

        #region Arbetstider (för delvis sjuk och schemalagda timmar)

        /// <summary>
        /// Starttid för faktiskt arbete (vid delvis sjuk)
        /// </summary>
        public TimeSpan? WorkedStartTime { get; set; }

        /// <summary>
        /// Sluttid för faktiskt arbete (vid delvis sjuk)
        /// </summary>
        public TimeSpan? WorkedEndTime { get; set; }

        /// <summary>
        /// Schemalagd starttid (vad som skulle jobbats)
        /// </summary>
        public TimeSpan? ScheduledStartTime { get; set; }

        /// <summary>
        /// Schemalagd sluttid (vad som skulle jobbats)
        /// </summary>
        public TimeSpan? ScheduledEndTime { get; set; }

        /// <summary>
        /// Totala schemalagda timmar för dagen
        /// </summary>
        public decimal ScheduledHours { get; set; }

        /// <summary>
        /// Faktiskt arbetade timmar (vid delvis sjuk)
        /// </summary>
        public decimal WorkedHours { get; set; } = 0;

        /// <summary>
        /// Sjuktimmar (ScheduledHours - WorkedHours)
        /// </summary>
        public decimal SickHours { get; set; } = 0;

        #endregion

        #region Metadata

        /// <summary>
        /// Anteckningar specifika för sjukdagen
        /// </summary>
        public string? SickNotes { get; set; }

        /// <summary>
        /// När sjukdagen registrerades
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Senast uppdaterad
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Kopplar till huvudpass i WorkShift
        /// </summary>
        [Ignore]
        public WorkShift? WorkShift { get; set; }

        #endregion
    }
}
