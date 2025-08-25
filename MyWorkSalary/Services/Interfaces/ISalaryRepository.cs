using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using System;
using System.Collections.Generic;

namespace MyWorkSalary.Services.Interfaces
{
    /// <summary>
    /// Interface för att hämta lönerelaterad data från databasen.
    /// </summary>
    public interface ISalaryRepository
    {
        #region Shifts & arbetstid
        /// <summary>
        /// Hämtar alla arbetspass för ett jobb under en specifik period.
        /// </summary>
        IEnumerable<WorkShift> GetShiftsForPeriod(int jobId, DateTime start, DateTime end);

        /// <summary>
        /// Hämtar alla OB-pass för ett jobb under en specifik period.
        /// </summary>
        IEnumerable<OBRate> GetObShiftsForPeriod(int jobId, DateTime start, DateTime end);
        #endregion

        #region Frånvaro
        /// <summary>
        /// Hämtar alla sjukdagar under perioden.
        /// </summary>
        IEnumerable<SickLeave> GetSickLeavesForPeriod(int jobId, DateTime start, DateTime end);

        /// <summary>
        /// Hämtar alla semesterdagar under perioden.
        /// </summary>
        IEnumerable<VacationLeave> GetVacationForPeriod(int jobId, DateTime start, DateTime end);

        /// <summary>
        /// Hämtar alla VAB-dagar under perioden.
        /// </summary>
        IEnumerable<VABLeave> GetVabForPeriod(int jobId, DateTime start, DateTime end);
        #endregion

        #region OnCall
        /// <summary>
        /// Hämtar alla on-call-pass för ett jobb under en specifik period.
        /// </summary>
        IEnumerable<OnCallShift> GetOnCallForPeriod(int jobId, DateTime start, DateTime end);
        #endregion

        #region Jobbinfo
        /// <summary>
        /// Hämtar jobb-profilen (för att veta månadslön, timlön etc).
        /// </summary>
        JobProfile GetJobProfile(int jobId);
        #endregion
    }
}
