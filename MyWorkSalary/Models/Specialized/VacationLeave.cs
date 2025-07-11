using SQLite;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Core;

namespace MyWorkSalary.Models.Specialized
{
    public class VacationLeave
    {
        #region Basic Properties
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int WorkShiftId { get; set; }
        public VacationType VacationType { get; set; }
        #endregion

        #region Calculation Properties  
        public decimal? MonthlySalaryUsed { get; set; }
        public decimal? HourlyRateUsed { get; set; }
        public decimal? VacationAllowanceRate { get; set; }
        #endregion

        #region Vacation Days Properties
        public decimal VacationDaysUsed { get; set; }
        public decimal VacationHours { get; set; }
        public decimal TotalVacationDaysPerYear { get; set; } = 25;
        #endregion

        #region Vacation Quota Properties
        public decimal SemesterKvot { get; set; } = 1.0m;
        public decimal VacationDaysConsumed { get; set; }
        #endregion
    }
}
